from __future__ import annotations

import os
import joblib
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Dict, List, Optional

import holidays
import numpy as np
import pandas as pd

from training_logic import PipelineConfig, WEATHER_COLS, get_location_details, safe_filename

try:
    import openmeteo_requests  # type: ignore
    from retry_requests import retry  # type: ignore
except Exception:  # pragma: no cover
    openmeteo_requests = None  # type: ignore
    retry = None  # type: ignore


TIME_FEATURES = ["day_of_week", "month", "day", "dayofyear", "is_weekend"]
LAGS = (1, 7, 14)
ROLL_WINDOWS = (7, 14, 28)
TREE_FEATURES = TIME_FEATURES + ["is_public_holiday"] + WEATHER_COLS + [
    "y_lag_1",
    "y_lag_7",
    "y_lag_14",
    "y_roll_mean_7",
    "y_roll_std_7",
    "y_roll_mean_14",
    "y_roll_std_14",
    "y_roll_mean_28",
    "y_roll_std_28",
    "prophet_yhat",
]


@dataclass(frozen=True)
class LoadedDishModel:
    dish: str
    champion: str
    prophet_model: Any
    tree_model: Any


class ModelStore:
    def __init__(self, model_dir: str = "models") -> None:
        self.model_dir = Path(model_dir)
        self.registry: Dict[str, Dict[str, Any]] = {}
        self._cache: Dict[str, LoadedDishModel] = {}

    def load_registry(self) -> None:
        registry_path = self.model_dir / "champion_registry.pkl"
        if not registry_path.exists():
            raise FileNotFoundError(f"Missing registry: {registry_path}")
        self.registry = joblib.load(str(registry_path))

    def list_dishes(self) -> List[str]:
        return sorted(self.registry.keys())

    def get_dish_model(self, dish: str) -> LoadedDishModel:
        if dish in self._cache:
            return self._cache[dish]

        meta = self.registry.get(dish)
        if meta is None:
            raise KeyError(f"Dish not found in registry: {dish}")

        champion = meta.get("model")
        if not champion:
            raise ValueError(f"Champion model missing for dish: {dish}")

        safe = safe_filename(dish)
        prophet_path = self.model_dir / f"prophet_{safe}.pkl"
        tree_path = self.model_dir / f"{champion}_{safe}.pkl"

        if not prophet_path.exists() or not tree_path.exists():
            raise FileNotFoundError(
                f"Missing model files for '{dish}': {prophet_path.name}, {tree_path.name}"
            )

        prophet_model = joblib.load(str(prophet_path))
        tree_model = joblib.load(str(tree_path))

        loaded = LoadedDishModel(
            dish=dish,
            champion=champion,
            prophet_model=prophet_model,
            tree_model=tree_model,
        )
        self._cache[dish] = loaded
        return loaded


def _compute_lag_features_from_history(sales_history: List[float]) -> Dict[str, float]:
    features: Dict[str, float] = {}
    fallback = float(sales_history[-1]) if sales_history else 0.0

    for lag in LAGS:
        features[f"y_lag_{lag}"] = (
            float(sales_history[-lag]) if len(sales_history) >= lag else fallback
        )

    for w in ROLL_WINDOWS:
        window = sales_history[-w:] if len(sales_history) >= w else sales_history
        if window:
            features[f"y_roll_mean_{w}"] = float(np.mean(window))
            features[f"y_roll_std_{w}"] = float(np.std(window, ddof=1)) if len(window) >= 2 else 0.0
        else:
            features[f"y_roll_mean_{w}"] = 0.0
            features[f"y_roll_std_{w}"] = 0.0

    return features


def _fetch_weather_forecast(latitude: float, longitude: float, forecast_days: int) -> pd.DataFrame:
    if openmeteo_requests is None or retry is None:
        raise RuntimeError("openmeteo-requests / retry-requests not available")

    session = retry(retries=3, backoff_factor=0.5)
    om = openmeteo_requests.Client(session=session)

    url = "https://api.open-meteo.com/v1/forecast"
    params = {
        "latitude": latitude,
        "longitude": longitude,
        "daily": WEATHER_COLS,
        "forecast_days": max(1, min(16, forecast_days)),
        "timezone": "auto",
    }

    responses = om.weather_api(url, params=params)
    daily = responses[0].Daily()

    dates = pd.date_range(
        start=pd.to_datetime(daily.Time(), unit="s", utc=True),
        end=pd.to_datetime(daily.TimeEnd(), unit="s", utc=True),
        freq=pd.Timedelta(seconds=daily.Interval()),
        inclusive="left",
    )

    df = pd.DataFrame(
        {
            "date": dates,
            WEATHER_COLS[0]: daily.Variables(0).ValuesAsNumpy(),
            WEATHER_COLS[1]: daily.Variables(1).ValuesAsNumpy(),
            WEATHER_COLS[2]: daily.Variables(2).ValuesAsNumpy(),
            WEATHER_COLS[3]: daily.Variables(3).ValuesAsNumpy(),
        }
    )
    df["date"] = pd.to_datetime(df["date"]).dt.tz_localize(None).dt.normalize()
    return df


def _prepare_future_weather(
    start_date: pd.Timestamp,
    horizon_days: int,
    latitude: float,
    longitude: float,
    weather_rows: Optional[List[Dict[str, Any]]] = None,
) -> pd.DataFrame:
    future_dates = pd.date_range(start=start_date, periods=horizon_days, freq="D")

    if weather_rows:
        forecast_weather = pd.DataFrame(weather_rows).copy()
        forecast_weather["date"] = pd.to_datetime(forecast_weather["date"]).dt.normalize()
    else:
        try:
            forecast_weather = _fetch_weather_forecast(latitude, longitude, forecast_days=max(16, horizon_days))
        except Exception:
            # Weather API unavailable — use sensible defaults so prediction still works
            forecast_weather = pd.DataFrame({
                "date": future_dates,
                "temperature_2m_mean": 20.0,
                "precipitation_sum": 0.0,
                "wind_speed_10m_max": 10.0,
                "relative_humidity_2m_mean": 60.0,
            })

    if forecast_weather.empty:
        # Last-resort fallback instead of crashing
        forecast_weather = pd.DataFrame({
            "date": future_dates,
            "temperature_2m_mean": 20.0,
            "precipitation_sum": 0.0,
            "wind_speed_10m_max": 10.0,
            "relative_humidity_2m_mean": 60.0,
        })

    for col in WEATHER_COLS:
        if col not in forecast_weather.columns:
            forecast_weather[col] = float(forecast_weather[col].mean()) if col in forecast_weather else 0.0

    future_weather = forecast_weather[forecast_weather["date"].isin(future_dates)].copy()
    if len(future_weather) < len(future_dates):
        mean_vals = {c: float(forecast_weather[c].mean()) for c in WEATHER_COLS}
        missing = [d for d in future_dates if d not in set(future_weather["date"])]
        fill = pd.DataFrame({"date": missing})
        for c in WEATHER_COLS:
            fill[c] = mean_vals[c]
        future_weather = pd.concat([future_weather, fill], ignore_index=True)

    return future_weather.sort_values("date").reset_index(drop=True)


def predict_dish(
    store: ModelStore,
    dish: str,
    recent_sales: List[float],
    horizon_days: int,
    start_date: Optional[str] = None,
    address: str = "Shanghai, China",
    latitude: Optional[float] = None,
    longitude: Optional[float] = None,
    country_code: Optional[str] = None,
    weather_rows: Optional[List[Dict[str, Any]]] = None,
) -> Dict[str, Any]:
    if not recent_sales:
        raise ValueError("recent_sales cannot be empty")

    if horizon_days < 1 or horizon_days > 30:
        raise ValueError("horizon_days must be in [1, 30]")

    loaded = store.get_dish_model(dish)
    cfg = PipelineConfig()

    lat = latitude
    lon = longitude
    cc = country_code
    if lat is None or lon is None or not cc:
        lat_geo, lon_geo, cc_geo = get_location_details(address)
        lat = lat if lat is not None else lat_geo
        lon = lon if lon is not None else lon_geo
        cc = cc or cc_geo

    if lat is None or lon is None:
        raise RuntimeError("Unable to resolve latitude/longitude")

    start = pd.to_datetime(start_date).normalize() if start_date else pd.Timestamp.now().normalize() + pd.Timedelta(days=1)
    future_weather = _prepare_future_weather(
        start_date=start,
        horizon_days=horizon_days,
        latitude=float(lat),
        longitude=float(lon),
        weather_rows=weather_rows,
    )

    prophet_input = future_weather.rename(columns={"date": "ds"})
    prophet_pred = loaded.prophet_model.predict(prophet_input[["ds"] + WEATHER_COLS])
    prophet_yhat = prophet_pred["yhat"].astype(float).to_numpy()

    local_hols = holidays.country_holidays(cc, years=cfg.holiday_years) if cc else None
    sales_history = [float(x) for x in recent_sales]

    rows: List[Dict[str, Any]] = []
    for i, row in future_weather.iterrows():
        dt = pd.to_datetime(row["date"])
        feat: Dict[str, float] = {
            "day_of_week": float(dt.dayofweek),
            "month": float(dt.month),
            "day": float(dt.day),
            "dayofyear": float(dt.dayofyear),
            "is_weekend": float(int(dt.dayofweek >= 5)),
            "is_public_holiday": float(int(dt in local_hols)) if local_hols is not None else 0.0,
            "prophet_yhat": float(prophet_yhat[i]),
        }

        for c in WEATHER_COLS:
            feat[c] = float(row.get(c, 0.0))

        feat.update(_compute_lag_features_from_history(sales_history))

        X_one = pd.DataFrame([{k: feat.get(k, 0.0) for k in TREE_FEATURES}])
        resid_hat = float(loaded.tree_model.predict(X_one)[0])
        yhat = max(0.0, feat["prophet_yhat"] + resid_hat)

        rows.append(
            {
                "date": dt.strftime("%Y-%m-%d"),
                "yhat": yhat,
                "prophet_yhat": feat["prophet_yhat"],
                "residual_hat": resid_hat,
            }
        )
        sales_history.append(yhat)

    return {
        "dish": dish,
        "model": loaded.champion,
        "model_combo": f"Prophet+{loaded.champion}",
        "horizon_days": horizon_days,
        "start_date": start.strftime("%Y-%m-%d"),
        "predictions": rows,
    }


def create_store_from_env() -> ModelStore:
    model_dir = os.getenv("MODEL_DIR", "models")
    store = ModelStore(model_dir=model_dir)
    store.load_registry()
    return store
