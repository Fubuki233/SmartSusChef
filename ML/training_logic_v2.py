"""
Core ML pipeline logic for the SmartSus Chef project.

This module contains all the data processing, model evaluation, and training functions.
It implements a Prophet + Tree Residual Stacking methodology where:
1. Prophet models trend/seasonality/holidays + weather regressors
2. Tree models (XGBoost, CatBoost, LightGBM) learn the residuals
3. Optuna optimizes hyperparameters for each tree model
4. The champion model is selected based on lowest CV MAE

It is designed to be imported by an orchestrator (e.g., Final_model_v2.py) and
is safe to be used with `concurrent.futures.ProcessPoolExecutor` because it has
no global side-effects on import. The main worker function is `process_dish`.
"""

from __future__ import annotations

import os
# Must be set before cmdstanpy/prophet import
os.environ.setdefault("CMDSTANPY_LOG_LEVEL", "WARNING")

# Load environment variables from .env file
try:
    from dotenv import load_dotenv
    load_dotenv()
except ImportError:
    pass  # python-dotenv not installed, use system env vars

import pandas as pd
import numpy as np
import holidays
import joblib
import logging
import optuna
import warnings
from dataclasses import dataclass, field
from typing import List, Dict, Any, Tuple
from sqlalchemy import create_engine
from sklearn.metrics import mean_absolute_error
from geopy.geocoders import Nominatim

try:
    import openmeteo_requests
    from retry_requests import retry
except Exception:
    openmeteo_requests = None
    retry = None

try:
    from prophet import Prophet
except Exception:
    Prophet = None

try:
    import lightgbm as lgb
except Exception:
    lgb = None

try:
    from catboost import CatBoostRegressor
except Exception:
    CatBoostRegressor = None

try:
    from xgboost import XGBRegressor
except Exception:
    XGBRegressor = None

warnings.filterwarnings('ignore')

# Suppress verbose output
optuna.logging.set_verbosity(optuna.logging.WARNING)

# ---------------------------------------------------------------------------
# Logging Setup (9.4)
# ---------------------------------------------------------------------------
logger = logging.getLogger(__name__)
logger.setLevel(logging.INFO)
if not logger.handlers:
    _handler = logging.StreamHandler()
    _handler.setFormatter(logging.Formatter(
        "%(asctime)s [%(levelname)s] %(name)s: %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S"
    ))
    logger.addHandler(_handler)


def _silence_logs() -> None:
    """Silence verbose logging from Prophet/CmdStan."""
    os.environ["CMDSTANPY_LOG_LEVEL"] = "ERROR"
    for name in ("cmdstanpy", "prophet", "stan", "pystan"):
        logger = logging.getLogger(name)
        logger.setLevel(logging.ERROR)
        logger.propagate = False
        logger.disabled = True


_silence_logs()


# ---------------------------------------------------------------------------
# GPU Detection
# ---------------------------------------------------------------------------
def _detect_gpu() -> Dict[str, bool]:
    """Auto-detect available GPU support for each tree framework."""
    gpu = {"xgboost": False, "catboost": False, "lightgbm": False}

    # XGBoost: check for CUDA device
    try:
        import xgboost as _xgb
        _test = _xgb.XGBRegressor(tree_method="hist", device="cuda", n_estimators=1)
        _test.fit(np.array([[0]]), np.array([0]))
        gpu["xgboost"] = True
    except Exception:
        pass

    # CatBoost: check for GPU task_type
    try:
        from catboost import CatBoostRegressor as _CB
        _test = _CB(iterations=1, task_type="GPU", verbose=False)
        _test.fit(np.array([[0]]), np.array([0]))
        gpu["catboost"] = True
    except Exception:
        pass

    # LightGBM: check for GPU device
    try:
        import lightgbm as _lgb
        _test = _lgb.LGBMRegressor(n_estimators=1, device="gpu", verbose=-1)
        _test.fit(np.array([[0]]), np.array([0]))
        gpu["lightgbm"] = True
    except Exception:
        pass

    return gpu


_GPU_AVAILABLE: Dict[str, bool] | None = None


def get_gpu_flags() -> Dict[str, bool]:
    """Return cached GPU detection results (runs once per process)."""
    global _GPU_AVAILABLE
    if _GPU_AVAILABLE is None:
        _GPU_AVAILABLE = _detect_gpu()
        for fw, ok in _GPU_AVAILABLE.items():
            logger.info("GPU %s for %s", "enabled" if ok else "not available", fw)
    return _GPU_AVAILABLE


# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------
@dataclass
class PipelineConfig:
    n_cv_folds: int = 3
    test_window_days: int = 30
    min_train_days: int = 60
    min_ml_days: int = 90
    random_seed: int = 42
    holiday_years: List[int] = field(default_factory=lambda: [2024, 2025, 2026])
    forecast_horizon: int = 14
    n_optuna_trials: int = 30
    max_workers: int = 4
    model_dir: str = "models"
    use_gpu: bool = True  # Auto-detect GPU; set False to force CPU

    # Fallback location for geocoding failures (9.2)
    default_fallback_address: str = "Shanghai, China"
    default_fallback_lat: float = 31.23
    default_fallback_lon: float = 121.47
    default_fallback_country: str = "CN"

    # Prophet parameters
    prophet_params: Dict[str, Any] = field(default_factory=lambda: {
        "changepoint_prior_scale": 0.5,
        "daily_seasonality": False,
        "holidays_prior_scale": 10.0,
        "seasonality_mode": "additive",
        "seasonality_prior_scale": 10.0,
        "weekly_seasonality": True,
        "yearly_seasonality": False,
    })

    # Time-based features
    time_features: List[str] = field(default_factory=lambda: [
        "day_of_week", "month", "day", "dayofyear", "is_weekend"
    ])

    # Lag and rolling window settings
    lags: Tuple[int, ...] = field(default_factory=lambda: (1, 7, 14))
    roll_windows: Tuple[int, ...] = field(default_factory=lambda: (7, 14, 28))

    # Features for hybrid tree model (predicting residuals)
    hybrid_tree_features: List[str] = field(default_factory=lambda: [
        "day_of_week", "month", "day", "dayofyear", "is_weekend",
        "is_public_holiday",
        "temperature_2m_max", "temperature_2m_min",
        "relative_humidity_2m_mean", "precipitation_sum",
        "y_lag_1", "y_lag_7", "y_lag_14",
        "y_roll_mean_7", "y_roll_std_7",
        "y_roll_mean_14", "y_roll_std_14",
        "y_roll_mean_28", "y_roll_std_28",
        "prophet_yhat",
    ])

    # Feature groups for SHAP explanation
    feature_groups: Dict[str, List[str]] = field(default_factory=lambda: {
        "Seasonality": ["day_of_week", "month", "day", "dayofyear", "is_weekend"],
        "Holiday": ["is_public_holiday"],
        "Weather": ["temperature_2m_max", "temperature_2m_min",
                    "relative_humidity_2m_mean", "precipitation_sum"],
        "Lags/Trend": [
            "y_lag_1", "y_lag_7", "y_lag_14",
            "y_roll_mean_7", "y_roll_std_7",
            "y_roll_mean_14", "y_roll_std_14",
            "y_roll_mean_28", "y_roll_std_28",
        ],
        "ProphetTrend": ["prophet_yhat"],
    })


CFG = PipelineConfig()

WEATHER_COLS = ['temperature_2m_max', 'temperature_2m_min',
                'relative_humidity_2m_mean', 'precipitation_sum']


def safe_filename(name):
    """Sanitize dish name for use as a filename."""
    return name.replace(' ', '_').replace('-', '_').replace('/', '_')


# ---------------------------------------------------------------------------
# Context Awareness (Location + Weather)
# ---------------------------------------------------------------------------
def get_location_details(address):
    """
    Convert an address or postal code to (latitude, longitude, country_code)
    using the Nominatim geocoding service (OpenStreetMap).
    """
    try:
        geolocator = Nominatim(user_agent="smartsus_chef_v3")
        location = geolocator.geocode(address, addressdetails=True)
        if location is None:
            logger.warning("Could not geocode address: '%s'", address)
            return None, None, None
        lat = location.latitude
        lon = location.longitude
        country_code = location.raw.get('address', {}).get('country_code', '').upper()
        logger.info("Geocoded '%s' -> Lat: %.4f, Lon: %.4f, Country: %s", address, lat, lon, country_code)
        return lat, lon, country_code
    except Exception as e:
        logger.warning("Geocoding failed for '%s': %s", address, e)
        return None, None, None


def get_historical_weather(latitude, longitude, start_date, end_date):
    """
    Fetch historical daily weather data from the Open-Meteo Archive API.
    Returns a DataFrame with columns: date, temperature_2m_max, temperature_2m_min,
    relative_humidity_2m_mean, precipitation_sum.
    """
    if openmeteo_requests is None or retry is None:
        logger.warning("openmeteo_requests or retry_requests not available.")
        return None

    try:
        session = retry(retries=3, backoff_factor=0.5)
        om = openmeteo_requests.Client(session=session)

        url = "https://archive-api.open-meteo.com/v1/archive"
        params = {
            "latitude": latitude,
            "longitude": longitude,
            "start_date": start_date.strftime('%Y-%m-%d'),
            "end_date": end_date.strftime('%Y-%m-%d'),
            "daily": ["temperature_2m_max", "temperature_2m_min",
                      "relative_humidity_2m_mean", "precipitation_sum"],
            "timezone": "auto"
        }

        responses = om.weather_api(url, params=params)
        response = responses[0]
        daily = response.Daily()

        dates = pd.date_range(
            start=pd.to_datetime(daily.Time(), unit="s", utc=True),
            end=pd.to_datetime(daily.TimeEnd(), unit="s", utc=True),
            freq=pd.Timedelta(seconds=daily.Interval()),
            inclusive="left"
        )

        weather_df = pd.DataFrame({
            "date": dates,
            "temperature_2m_max": daily.Variables(0).ValuesAsNumpy(),
            "temperature_2m_min": daily.Variables(1).ValuesAsNumpy(),
            "relative_humidity_2m_mean": daily.Variables(2).ValuesAsNumpy(),
            "precipitation_sum": daily.Variables(3).ValuesAsNumpy(),
        })
        weather_df['date'] = weather_df['date'].dt.tz_localize(None).dt.normalize()

        logger.info("Fetched %d days of historical weather data.", len(weather_df))
        return weather_df
    except Exception as e:
        logger.warning("Failed to fetch historical weather: %s", e)
        return None


def fetch_weather_from_db(start_date, end_date):
    """
    Attempt to fetch historical weather data from the local MySQL Weather table.
    Returns a DataFrame with columns: date + WEATHER_COLS, or None if unavailable.
    """
    DB_URL = os.getenv("DATABASE_URL")
    if not DB_URL:
        logger.debug("DATABASE_URL not set; skipping weather DB lookup.")
        return None
    try:
        engine = create_engine(DB_URL)
        query = """
        SELECT Date as date, TemperatureMax as temperature_2m_max,
               TemperatureMin as temperature_2m_min,
               HumidityMean as relative_humidity_2m_mean,
               PrecipitationSum as precipitation_sum
        FROM Weather
        WHERE Date BETWEEN %s AND %s
        ORDER BY Date ASC
        """
        df = pd.read_sql(query, engine, params=[
            start_date.strftime('%Y-%m-%d'),
            end_date.strftime('%Y-%m-%d')
        ])
        if len(df) == 0:
            return None
        df['date'] = pd.to_datetime(df['date']).dt.normalize()
        logger.info("Fetched %d days of weather data from database.", len(df))
        return df
    except Exception as e:
        logger.warning("Weather DB lookup failed: %s", e)
        return None


def add_local_context(df, address, config: PipelineConfig = CFG,
                      latitude=None, longitude=None, country_code=None):
    """
    Enriches the sales data with local context features (Holidays + Weather).
    Uses geocoding to detect location and Open-Meteo for real weather data.
    Falls back to config.default_fallback_* if geocoding or weather fails.

    If latitude, longitude, and country_code are provided directly,
    geocoding is skipped.
    """
    if latitude is not None and longitude is not None and country_code:
        lat, lon = latitude, longitude
    else:
        lat, lon, country_code = get_location_details(address)

    # Fallback if geocoding fails (using configurable defaults from 9.2)
    if lat is None:
        logger.warning(
            "Geocoding failed. Falling back to default location (%s).",
            config.default_fallback_address
        )
        country_code = config.default_fallback_country
        lat, lon = config.default_fallback_lat, config.default_fallback_lon

    # Ensure valid country code for holidays
    if not country_code or country_code not in holidays.list_supported_countries():
        logger.warning(
            "Country '%s' not supported for holidays. Defaulting to '%s'.",
            country_code, config.default_fallback_country
        )
        country_code = config.default_fallback_country

    logger.info("Location: %s (Lat: %.4f, Lon: %.4f)", country_code, lat, lon)

    df['day_of_week'] = df['date'].dt.dayofweek
    df['month'] = df['date'].dt.month

    local_holidays = holidays.country_holidays(country_code, years=CFG.holiday_years)
    df['is_public_holiday'] = df['date'].apply(lambda x: 1 if x in local_holidays else 0)

    # Try to fetch weather data: DB first, then API fallback
    weather_df = fetch_weather_from_db(df['date'].min(), df['date'].max())
    if weather_df is None:
        weather_df = get_historical_weather(lat, lon, df['date'].min(), df['date'].max())

    if weather_df is not None and len(weather_df) > 0:
        df = df.merge(weather_df, on='date', how='left')
        df = df.set_index('date')
        for col in WEATHER_COLS:
            df[col] = df[col].interpolate(method='time').bfill().ffill().fillna(0)
        df = df.reset_index()
    else:
        # Graceful fallback: proceed without weather features
        logger.warning(
            "Failed to fetch historical weather data from both the database "
            "and Open-Meteo API. Proceeding with weather features set to 0."
        )
        for col in WEATHER_COLS:
            df[col] = 0.0

    return df, country_code, lat, lon


# ---------------------------------------------------------------------------
# Data Ingestion & Sanitation
# ---------------------------------------------------------------------------
def fetch_training_data():
    """
    Tries to connect to MySQL using DATABASE_URL env var.
    If it fails or is not set, falls back to 'food_sales_eng.csv'.
    """
    DB_URL = os.getenv("DATABASE_URL")

    df = None
    if DB_URL:
        try:
            engine = create_engine(DB_URL)
            query = """
            SELECT s.Date as date, r.Name as dish, s.Quantity as sales
            FROM SalesData s JOIN Recipes r ON s.RecipeId = r.Id
            ORDER BY s.Date ASC
            """
            df = pd.read_sql(query, engine)
            df['date'] = pd.to_datetime(df['date'])
            logger.info("Loaded %d rows from MySQL.", len(df))
        except Exception as e:
            logger.warning("MySQL connection failed: %s. Falling back to CSV.", e)
            df = None
    else:
        logger.info("DATABASE_URL not set. Using CSV fallback.")

    if df is None:
        df = pd.read_csv('food_sales_eng.csv')
        df['date'] = pd.to_datetime(df['date'], format='%m/%d/%Y')
        logger.info("Loaded %d rows from CSV.", len(df))

    # Normalize the date to remove time component, ensuring one record per day
    df['date'] = df['date'].dt.normalize()

    # Aggregate sales to ensure each dish has exactly one entry per day.
    # This is critical for preventing errors in time-series models.
    df = df.groupby(['date', 'dish']).agg(sales=('sales', 'sum')).reset_index()

    return df.sort_values('date')


def sanitize_sparse_data(df, country_code):
    """Fill missing days with time-based interpolation."""
    # Create a full date range and reindex the DataFrame to identify missing dates.
    all_dates = pd.date_range(start=df['date'].min(), end=df['date'].max(), freq='D')
    df = df.set_index('date').reindex(all_dates)

    # Interpolate missing sales values and fill remaining gaps with 0
    df['sales'] = df['sales'].interpolate(method='time').fillna(0)

    if 'dish' in df.columns:
        df['dish'] = df['dish'].dropna().iloc[0] if not df['dish'].dropna().empty else "Unknown"

    for col in WEATHER_COLS:
        if col in df.columns:
            df[col] = df[col].interpolate(method='time').bfill().ffill()
        else:
            df[col] = 0.0

    if country_code and country_code in holidays.list_supported_countries():
        local_holidays = holidays.country_holidays(country_code, years=CFG.holiday_years)
    else:
        local_holidays = holidays.country_holidays('CN', years=CFG.holiday_years)

    df['is_public_holiday'] = df.index.to_series().apply(lambda x: 1 if x in local_holidays else 0)
    df['day_of_week'] = df.index.dayofweek
    df['month'] = df.index.month
    df = df.reset_index().rename(columns={'index': 'date'})

    return df


# ---------------------------------------------------------------------------
# Feature Engineering for Hybrid Model
# ---------------------------------------------------------------------------
def _add_date_features(df: pd.DataFrame, config: PipelineConfig) -> pd.DataFrame:
    """Add date-based features for the hybrid model based on config.time_features."""
    ds = pd.to_datetime(df["date"])
    out = df.copy()

    # Dynamically add features based on config.time_features
    feature_map = {
        "day_of_week": lambda: ds.dt.dayofweek,
        "month": lambda: ds.dt.month,
        "day": lambda: ds.dt.day,
        "dayofyear": lambda: ds.dt.dayofyear,
        "is_weekend": lambda: (ds.dt.dayofweek >= 5).astype(int),
    }

    for feat_name in config.time_features:
        if feat_name in feature_map:
            out[feat_name] = feature_map[feat_name]()

    return out


def _add_lag_roll_features(df: pd.DataFrame, config: PipelineConfig) -> pd.DataFrame:
    """Add lag and rolling window features for the hybrid model."""
    out = df.copy()
    for lag in config.lags:
        out[f"y_lag_{lag}"] = out["sales"].shift(lag)
    for w in config.roll_windows:
        s = out["sales"].shift(1)
        out[f"y_roll_mean_{w}"] = s.rolling(w).mean()
        out[f"y_roll_std_{w}"] = s.rolling(w).std()
    return out


def add_hybrid_features(df: pd.DataFrame, config: PipelineConfig = CFG) -> pd.DataFrame:
    """Add all features needed for hybrid Prophet + Tree model."""
    df = _add_date_features(df.copy(), config)
    df = _add_lag_roll_features(df, config)
    return df


# ---------------------------------------------------------------------------
# Prophet Model Functions
# ---------------------------------------------------------------------------
def _fit_prophet(train_df: pd.DataFrame, country_code: str, config: PipelineConfig) -> Any:
    """Fit a Prophet model on training data."""
    if Prophet is None:
        raise ImportError("Prophet is required but not installed. Please install prophet.")

    m = Prophet(**config.prophet_params)
    try:
        m.add_country_holidays(country_name=country_code)
    except Exception:
        pass

    # Add weather columns as regressors
    for col in WEATHER_COLS:
        if col in train_df.columns:
            m.add_regressor(col)

    fit_df = train_df.rename(columns={"date": "ds", "sales": "y"})
    cols_to_use = ["ds", "y"] + [c for c in WEATHER_COLS if c in fit_df.columns]
    m.fit(fit_df[cols_to_use])
    return m


def _prophet_predict(model: Any, df: pd.DataFrame) -> np.ndarray:
    """Generate predictions from a fitted Prophet model."""
    pred_df = df.rename(columns={"date": "ds"})
    cols_to_use = ["ds"] + [c for c in WEATHER_COLS if c in pred_df.columns]
    yhat = model.predict(pred_df[cols_to_use])
    return yhat["yhat"].astype(float).to_numpy()


def _build_residual_features(df: pd.DataFrame, prophet_yhat: np.ndarray) -> pd.DataFrame:
    """Add Prophet predictions and residuals to the dataframe."""
    out = df.copy()
    out["prophet_yhat"] = prophet_yhat
    out["resid"] = out["sales"].astype(float) - out["prophet_yhat"].astype(float)
    return out


# ---------------------------------------------------------------------------
# Cross-Validation for Hybrid Model
# ---------------------------------------------------------------------------
def _generate_cv_folds(df: pd.DataFrame, config: PipelineConfig):
    """
    Expanding-window time-series CV fold generator.
    Final fold test period = most recent config.test_window_days days.
    Yields (train_df, test_df) tuples.
    """
    dates = df['date'].sort_values()
    end_date = dates.max()

    for fold_i in range(config.n_cv_folds, 0, -1):
        test_end = end_date - pd.Timedelta(days=config.test_window_days * (fold_i - 1))
        test_start = test_end - pd.Timedelta(days=config.test_window_days)

        train = df[df['date'] < test_start].copy()
        test = df[(df['date'] >= test_start) & (df['date'] < test_end)].copy()

        train_span = (train['date'].max() - train['date'].min()).days if len(train) > 1 else 0
        if train_span < config.min_train_days or len(test) < 1:
            continue

        yield train, test


def _prepare_cv_fold_cache(
    df_feat: pd.DataFrame,
    country_code: str,
    config: PipelineConfig,
) -> List[Dict[str, Any]]:
    """
    Pre-compute Prophet + residual training matrices once per fold.

    IMPORTANT: Sanitation (interpolation) is applied per-fold to prevent data leakage.
    This ensures that training data interpolation doesn't use future (test) information.
    """
    _silence_logs()
    feature_cols = config.hybrid_tree_features
    fold_cache: List[Dict[str, Any]] = []

    for raw_train, raw_test in _generate_cv_folds(df_feat, config):
        # Apply sanitation to train and test sets SEPARATELY to prevent data leakage
        train = sanitize_sparse_data(raw_train.copy(), country_code)
        test = sanitize_sparse_data(raw_test.copy(), country_code)

        # Re-check data sufficiency after sanitation
        if len(train) < config.min_train_days or len(test) < 1:
            continue

        pm = _fit_prophet(train, country_code, config)
        p_train = _prophet_predict(pm, train)
        p_test = _prophet_predict(pm, test)

        train_r = _build_residual_features(train, p_train)
        test_r = _build_residual_features(test, p_test)

        X_train = train_r[feature_cols].dropna()
        X_test = test_r[feature_cols].dropna()
        if X_train.empty or X_test.empty:
            continue

        fold_cache.append({
            "X_train": X_train,
            "y_train": train_r.loc[X_train.index, "resid"],
            "X_test": X_test,
            "y_test": test_r.loc[X_test.index, "resid"],
            "prophet_test": test_r.loc[X_test.index, "prophet_yhat"].to_numpy(),
            "sales_test": test_r.loc[X_test.index, "sales"].to_numpy(),
        })

    return fold_cache


# ---------------------------------------------------------------------------
# Hybrid Model Evaluation and Optimization
# ---------------------------------------------------------------------------
def _eval_hybrid_mae(
    model_type: str,
    fold_cache: List[Dict[str, Any]],
    trial_params: Dict[str, Any],
    config: PipelineConfig,
) -> float:
    """
    Given a model type and parameters, run CV and return average MAE.
    """
    fold_maes = []
    tree_n_jobs = 1 if config.max_workers > 1 else -1
    gpu = get_gpu_flags() if config.use_gpu else {}

    for fold in fold_cache:
        X_train = fold["X_train"]
        y_train = fold["y_train"]
        X_test = fold["X_test"]
        prophet_test = fold["prophet_test"]
        sales_test = fold["sales_test"]

        if model_type == "xgboost":
            if XGBRegressor is None:
                raise ImportError("XGBoost is required but not installed.")
            xgb_extra = {"tree_method": "hist", "device": "cuda"} if gpu.get("xgboost") else {}
            model = XGBRegressor(
                n_estimators=100,
                random_state=config.random_seed,
                n_jobs=tree_n_jobs,
                **xgb_extra,
                **trial_params,
            )
            model.fit(X_train, y_train, verbose=False)
        elif model_type == "catboost":
            if CatBoostRegressor is None:
                raise ImportError("CatBoost is required but not installed.")
            cb_extra = {"task_type": "GPU"} if gpu.get("catboost") else {}
            model = CatBoostRegressor(
                iterations=100,
                random_seed=config.random_seed,
                verbose=False,
                **cb_extra,
                **trial_params,
            )
            model.fit(X_train, y_train, verbose=False)
        elif model_type == "lightgbm":
            if lgb is None:
                raise ImportError("LightGBM is required but not installed.")
            lgb_extra = {"device": "gpu"} if gpu.get("lightgbm") else {}
            model = lgb.LGBMRegressor(
                n_estimators=100,
                random_state=config.random_seed,
                n_jobs=tree_n_jobs,
                verbose=-1,
                **lgb_extra,
                **trial_params,
            )
            model.fit(X_train, y_train)
        else:
            raise ValueError(f"Unknown model type: {model_type}")

        resid_pred = model.predict(X_test)
        yhat = np.maximum(prophet_test + resid_pred, 0.0)
        fold_maes.append(mean_absolute_error(sales_test, yhat))

    if not fold_maes:
        return float("inf")
    return float(np.mean(fold_maes))


def _optimize_hybrid(
    model_type: str,
    fold_cache: List[Dict[str, Any]],
    config: PipelineConfig
) -> Tuple[float, Dict[str, Any]]:
    """Optuna optimization for hybrid residual stacking per model type."""

    def objective(trial: optuna.Trial) -> float:
        if model_type == "xgboost":
            params = {
                "max_depth": trial.suggest_int("max_depth", 3, 10),
                "learning_rate": trial.suggest_float("learning_rate", 0.01, 0.3, log=True),
                "subsample": trial.suggest_float("subsample", 0.6, 1.0),
                "reg_alpha": trial.suggest_float("reg_alpha", 0.0, 10.0),
                "reg_lambda": trial.suggest_float("reg_lambda", 0.0, 10.0),
            }
        elif model_type == "catboost":
            params = {
                "depth": trial.suggest_int("depth", 3, 10),
                "learning_rate": trial.suggest_float("learning_rate", 0.01, 0.3, log=True),
                "l2_leaf_reg": trial.suggest_float("l2_leaf_reg", 1.0, 10.0),
                "subsample": trial.suggest_float("subsample", 0.6, 1.0),
            }
        elif model_type == "lightgbm":
            params = {
                "num_leaves": trial.suggest_int("num_leaves", 20, 150),
                "learning_rate": trial.suggest_float("learning_rate", 0.01, 0.3, log=True),
                "reg_alpha": trial.suggest_float("reg_alpha", 0.0, 10.0),
                "reg_lambda": trial.suggest_float("reg_lambda", 0.0, 10.0),
            }
        else:
            raise ValueError(f"Unknown model type: {model_type}")

        return _eval_hybrid_mae(model_type, fold_cache, params, config)

    study = optuna.create_study(
        direction="minimize",
        sampler=optuna.samplers.TPESampler(seed=config.random_seed),
    )
    study.optimize(objective, n_trials=config.n_optuna_trials)
    return float(study.best_value), study.best_params


# ---------------------------------------------------------------------------
# Model Persistence
# ---------------------------------------------------------------------------
def _save_hybrid_models(
    dish: str,
    prophet_model: Any,
    tree_model: Any,
    champion: str,
    config: PipelineConfig
) -> None:
    """Save both Prophet and tree models for a dish using joblib (safer than pickle)."""
    safe_name = safe_filename(dish)
    model_dir = config.model_dir
    os.makedirs(model_dir, exist_ok=True)

    joblib.dump(prophet_model, f"{model_dir}/prophet_{safe_name}.pkl")
    joblib.dump(tree_model, f"{model_dir}/{champion}_{safe_name}.pkl")


def _load_hybrid_models(dish: str, champion: str, config: PipelineConfig) -> Tuple[Any, Any]:
    """Load both Prophet and tree models for a dish using joblib (safer than pickle)."""
    safe_name = safe_filename(dish)
    model_dir = config.model_dir

    prophet_model = joblib.load(f"{model_dir}/prophet_{safe_name}.pkl")
    tree_model = joblib.load(f"{model_dir}/{champion}_{safe_name}.pkl")
    return prophet_model, tree_model


# ---------------------------------------------------------------------------
# Per-Dish Processing (Hybrid Prophet + Tree)
# ---------------------------------------------------------------------------
def process_dish(dish_name: str, shared_df: pd.DataFrame, country_code: str, config: PipelineConfig) -> Dict[str, Any]:
    """
    Process a single dish using Prophet + Tree Residual Stacking:
    1. Sanitize data and add features
    2. Run Optuna optimization for each tree model type
    3. Select champion based on lowest CV MAE
    4. Retrain Prophet and champion tree on full data
    5. Save models to disk

    This function is standalone (no closures) so it can be pickled by ProcessPoolExecutor.
    """
    _silence_logs()
    safe_name = safe_filename(dish_name)
    os.makedirs(config.model_dir, exist_ok=True)

    # Isolate dish data (sanitation will happen per-fold to prevent data leakage)
    dish_data = shared_df[shared_df['dish'] == dish_name].copy()

    if len(dish_data) == 0:
        raise RuntimeError(f"{dish_name}: No data found for this dish.")

    if len(dish_data) < config.min_train_days:
        raise RuntimeError(f"{dish_name}: Insufficient raw data ({len(dish_data)} days < {config.min_train_days} required).")

    # Add all features for hybrid model BEFORE CV splits
    # Note: Sanitation (interpolation) happens per-fold to prevent data leakage
    dish_feat = add_hybrid_features(dish_data.copy(), config)

    if len(dish_feat) < config.min_train_days:
        raise RuntimeError(f"{dish_name}: Insufficient data after feature engineering ({len(dish_feat)} rows).")

    # Prepare CV fold cache (pre-compute Prophet for each fold)
    # Sanitation is applied per-fold inside this function
    fold_cache = _prepare_cv_fold_cache(dish_feat, country_code, config)
    if not fold_cache:
        raise RuntimeError(f"{dish_name}: CV folds unavailable after feature processing.")

    # Optuna optimization for each model type
    mae_map: Dict[str, float] = {}
    params_map: Dict[str, Dict[str, Any]] = {}

    for model_type in ["xgboost", "catboost", "lightgbm"]:
        best_mae, best_params = _optimize_hybrid(model_type, fold_cache, config)
        mae_map[model_type] = round(best_mae, 4)
        params_map[model_type] = best_params

    # Select champion (lowest MAE)
    champion = min(mae_map, key=mae_map.get)

    # Retrain on full data (sanitize the full dataset for production model)
    dish_feat_sanitized = sanitize_sparse_data(dish_feat.copy(), country_code)

    # 1. Fit Prophet on full sanitized data
    pm = _fit_prophet(dish_feat_sanitized, country_code, config)
    p_full = _prophet_predict(pm, dish_feat_sanitized)
    train_r = _build_residual_features(dish_feat_sanitized, p_full)

    X_full = train_r[config.hybrid_tree_features].dropna()
    y_full = train_r.loc[X_full.index, "resid"]

    if len(X_full) == 0:
        raise RuntimeError(f"{dish_name}: No valid training data after feature/residual processing.")

    # 2. Train champion tree model on residuals
    tree_n_jobs = 1 if config.max_workers > 1 else -1
    gpu = get_gpu_flags() if config.use_gpu else {}

    if champion == "xgboost":
        xgb_extra = {"tree_method": "hist", "device": "cuda"} if gpu.get("xgboost") else {}
        model = XGBRegressor(
            n_estimators=100,
            random_state=config.random_seed,
            n_jobs=tree_n_jobs,
            **xgb_extra,
            **params_map[champion],
        )
        model.fit(X_full, y_full, verbose=False)
    elif champion == "catboost":
        cb_extra = {"task_type": "GPU"} if gpu.get("catboost") else {}
        model = CatBoostRegressor(
            iterations=100,
            random_seed=config.random_seed,
            verbose=False,
            **cb_extra,
            **params_map[champion],
        )
        model.fit(X_full, y_full, verbose=False)
    else:  # lightgbm
        lgb_extra = {"device": "gpu"} if gpu.get("lightgbm") else {}
        model = lgb.LGBMRegressor(
            n_estimators=100,
            random_state=config.random_seed,
            n_jobs=tree_n_jobs,
            verbose=-1,
            **lgb_extra,
            **params_map[champion],
        )
        model.fit(X_full, y_full)

    # Save both models
    _save_hybrid_models(dish_name, pm, model, champion, config)

    # Save recent sales for lag computation at prediction time
    recent_sales = dish_feat_sanitized[['date', 'sales']].tail(28).copy()
    joblib.dump(recent_sales, f'{config.model_dir}/recent_sales_{safe_name}.pkl')

    return {
        'dish': dish_name,
        'champion': champion,
        'mae': mae_map,
        'best_params': params_map,
        'champion_mae': mae_map[champion],
        'model_type': 'hybrid',  # Indicates Prophet + Tree stacking
    }


# ---------------------------------------------------------------------------
# Utility function for computing lag features at prediction time
# ---------------------------------------------------------------------------
def compute_lag_features_from_history(
    sales_history: List[float],
    config: PipelineConfig = CFG
) -> Dict[str, float]:
    """Compute lag and rolling features from a sales history array."""
    features: Dict[str, float] = {}

    for lag in config.lags:
        if len(sales_history) >= lag:
            features[f"y_lag_{lag}"] = float(sales_history[-lag])
        else:
            features[f"y_lag_{lag}"] = 0.0  # Use 0.0 for insufficient history

    for w in config.roll_windows:
        window = sales_history[-w:] if len(sales_history) >= w else sales_history
        if window:
            features[f"y_roll_mean_{w}"] = float(np.mean(window))
            features[f"y_roll_std_{w}"] = float(np.std(window, ddof=1)) if len(window) >= 2 else 0.0
        else:
            features[f"y_roll_mean_{w}"] = 0.0
            features[f"y_roll_std_{w}"] = 0.0

    return features
