# -*- coding: utf-8 -*-
"""
Final Prophet + Tree Residual Stacking Pipeline (Optuna)

目标
----
对不同菜品选择最优模型与参数，并进行未来预测与输出：

1) Prophet 建模趋势/季节性/假期 + 天气回归变量
2) Tree 模型（XGBoost / CatBoost / LightGBM）学习残差
3) Optuna 搜索每个模型最优参数
4) 选择每道菜最优算法与参数
5) 保存模型与注册表，生成未来预测、CSV 与可视化，并提供 SHAP 解释

运行
----
    python3 "Final_model.py"
"""

from __future__ import annotations

import os
# Must be set before cmdstanpy/prophet import
os.environ.setdefault("CMDSTANPY_LOG_LEVEL", "WARNING")

import logging
import pickle
import warnings
from pathlib import Path
from concurrent.futures import ProcessPoolExecutor, wait, FIRST_COMPLETED
from dataclasses import dataclass
from typing import Any, Dict, Tuple, List

import numpy as np
import optuna
import pandas as pd
import time
from sklearn.metrics import mean_absolute_error
import matplotlib.pyplot as plt
import holidays
try:
    from requests import RequestsDependencyWarning
except Exception:  # pragma: no cover
    RequestsDependencyWarning = None  # type: ignore
try:
    from tqdm import tqdm
except Exception:  # pragma: no cover
    tqdm = None  # type: ignore

from training_logic import (
    PipelineConfig,
    fetch_training_data,
    add_local_context,
    sanitize_sparse_data,
    safe_filename,
    WEATHER_COLS,
)

try:
    from prophet import Prophet  # type: ignore
except Exception:  # pragma: no cover
    Prophet = None  # type: ignore

try:
    from xgboost import XGBRegressor
except Exception:  # pragma: no cover
    XGBRegressor = None  # type: ignore

try:
    from catboost import CatBoostRegressor
except Exception:  # pragma: no cover
    CatBoostRegressor = None  # type: ignore

try:
    import lightgbm as lgb
except Exception:  # pragma: no cover
    lgb = None  # type: ignore

try:
    import shap  # type: ignore
except Exception:  # pragma: no cover
    shap = None  # type: ignore

try:
    import openmeteo_requests  # type: ignore
    from retry_requests import retry  # type: ignore
except Exception:  # pragma: no cover
    openmeteo_requests = None  # type: ignore
    retry = None  # type: ignore


warnings.filterwarnings("ignore")
if RequestsDependencyWarning is not None:
    warnings.filterwarnings("ignore", category=RequestsDependencyWarning)
optuna.logging.set_verbosity(optuna.logging.WARNING)


def _silence_logs() -> None:
    os.environ["CMDSTANPY_LOG_LEVEL"] = "ERROR"
    for name in ("cmdstanpy", "prophet", "stan", "pystan"):
        logger = logging.getLogger(name)
        logger.setLevel(logging.ERROR)
        logger.propagate = False
        logger.disabled = True


_silence_logs()


def _log(msg: str) -> None:
    if tqdm is not None:
        tqdm.write(msg)
    else:
        print(msg)


# ==========================
# 基本配置
# ==========================
CFG = PipelineConfig()
ADDRESS_INPUT = "Shanghai, China"
RANDOM_SEED = CFG.random_seed

# Prophet 参数（沿用 Parallel(XP_all).py）
PROPHET_PARAMS = {
    "changepoint_prior_scale": 0.5,
    "daily_seasonality": False,
    "holidays_prior_scale": 10.0,
    "seasonality_mode": "additive",
    "seasonality_prior_scale": 10.0,
    "weekly_seasonality": True,
    "yearly_seasonality": False,
}

# Parallel 风格特征
TIME_FEATURES = ["day_of_week", "month", "day", "dayofyear", "is_weekend"]
LAGS = (1, 7, 14)
ROLL_WINDOWS = (7, 14, 28)
TREE_FEATURES = TIME_FEATURES + ["is_public_holiday"] + WEATHER_COLS + [
    "y_lag_1", "y_lag_7", "y_lag_14",
    "y_roll_mean_7", "y_roll_std_7",
    "y_roll_mean_14", "y_roll_std_14",
    "y_roll_mean_28", "y_roll_std_28",
    "prophet_yhat",
]

# 输出路径
OUT_DIR = Path("outputs")
OUT_FORECASTS_DIR = OUT_DIR / "forecasts"

# 未来预测
HORIZON_DAYS = CFG.forecast_horizon
TOP_N_PLOT = 12
HISTORY_PLOT_DAYS = 90


# ==========================
# 工具函数
# ==========================

def _generate_cv_folds(df: pd.DataFrame, config: PipelineConfig):
    """
    Expanding-window time-series CV.
    复用 training_logic 的逻辑：最终一折为最近 test_window_days 天。
    """
    dates = df["date"].sort_values()
    end_date = dates.max()

    for fold_i in range(config.n_cv_folds, 0, -1):
        test_end = end_date - pd.Timedelta(days=config.test_window_days * (fold_i - 1))
        test_start = test_end - pd.Timedelta(days=config.test_window_days)

        train = df[df["date"] < test_start].copy()
        test = df[(df["date"] >= test_start) & (df["date"] < test_end)].copy()

        train_span = (train["date"].max() - train["date"].min()).days if len(train) > 1 else 0
        if train_span < config.min_train_days or len(test) < 1:
            continue

        yield train, test


def _ensure_dirs() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    OUT_FORECASTS_DIR.mkdir(parents=True, exist_ok=True)


def _add_date_features(df: pd.DataFrame) -> pd.DataFrame:
    ds = pd.to_datetime(df["date"])
    out = df.copy()
    out["day_of_week"] = ds.dt.dayofweek
    out["month"] = ds.dt.month
    out["day"] = ds.dt.day
    out["dayofyear"] = ds.dt.dayofyear
    out["is_weekend"] = (out["day_of_week"] >= 5).astype(int)
    return out


def _add_lag_roll_features(df: pd.DataFrame) -> pd.DataFrame:
    out = df.copy()
    for lag in LAGS:
        out[f"y_lag_{lag}"] = out["sales"].shift(lag)
    for w in ROLL_WINDOWS:
        s = out["sales"].shift(1)
        out[f"y_roll_mean_{w}"] = s.rolling(w).mean()
        out[f"y_roll_std_{w}"] = s.rolling(w).std()
    return out


def _fit_prophet(train_df: pd.DataFrame, country_code: str) -> Prophet:
    if Prophet is None:
        raise ImportError("缺少依赖：prophet。请先安装 prophet。")

    m = Prophet(**PROPHET_PARAMS)  # type: ignore[misc]
    try:
        m.add_country_holidays(country_name=country_code)  # type: ignore[no-untyped-call]
    except Exception:
        pass

    # Prophet 回归变量：weather cols（由 add_local_context 写入）
    for col in WEATHER_COLS:
        if col in train_df.columns:
            m.add_regressor(col)  # type: ignore[no-untyped-call]

    fit_df = train_df.rename(columns={"date": "ds", "sales": "y"})
    m.fit(fit_df[["ds", "y"] + WEATHER_COLS])  # type: ignore[no-untyped-call]
    return m


def _prophet_predict(model: Prophet, df: pd.DataFrame) -> np.ndarray:
    pred_df = df.rename(columns={"date": "ds"})
    yhat = model.predict(pred_df[["ds"] + WEATHER_COLS])  # type: ignore[no-untyped-call]
    return yhat["yhat"].astype(float).to_numpy()


def _build_residual_features(df: pd.DataFrame, prophet_yhat: np.ndarray) -> pd.DataFrame:
    out = df.copy()
    out["prophet_yhat"] = prophet_yhat
    out["resid"] = out["sales"].astype(float) - out["prophet_yhat"].astype(float)
    return out


def _eval_hybrid_mae(
    model_type: str,
    fold_cache: List[Dict[str, Any]],
    trial_params: Dict[str, Any],
) -> float:
    """
    给定模型类型和参数，基于已缓存的 fold 数据跑 CV 并返回平均 MAE。
    """
    fold_maes = []
    tree_n_jobs = 1 if CFG.max_workers > 1 else -1

    for fold in fold_cache:
        X_train = fold["X_train"]
        y_train = fold["y_train"]
        X_test = fold["X_test"]
        y_test = fold["y_test"]
        prophet_test = fold["prophet_test"]
        sales_test = fold["sales_test"]

        if model_type == "xgboost":
            if XGBRegressor is None:
                raise ImportError("缺少依赖：xgboost。")
            model = XGBRegressor(
                n_estimators=100,
                random_state=RANDOM_SEED,
                n_jobs=tree_n_jobs,
                **trial_params,
            )
            model.fit(X_train, y_train, verbose=False)
        elif model_type == "catboost":
            if CatBoostRegressor is None:
                raise ImportError("缺少依赖：catboost。")
            model = CatBoostRegressor(
                iterations=100,
                random_seed=RANDOM_SEED,
                verbose=False,
                **trial_params,
            )
            model.fit(X_train, y_train, verbose=False)
        elif model_type == "lightgbm":
            if lgb is None:
                raise ImportError("缺少依赖：lightgbm。")
            model = lgb.LGBMRegressor(
                n_estimators=100,
                random_state=RANDOM_SEED,
                n_jobs=tree_n_jobs,
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


def _prepare_cv_fold_cache(
    df_feat: pd.DataFrame,
    country_code: str,
    config: PipelineConfig,
) -> List[Dict[str, Any]]:
    """Pre-compute Prophet + residual training matrices once per fold."""
    feature_cols = TREE_FEATURES
    fold_cache: List[Dict[str, Any]] = []

    for train, test in _generate_cv_folds(df_feat, config):
        pm = _fit_prophet(train, country_code)
        p_train = _prophet_predict(pm, train)
        p_test = _prophet_predict(pm, test)

        train_r = _build_residual_features(train, p_train)
        test_r = _build_residual_features(test, p_test)

        X_train = train_r[feature_cols].dropna()
        X_test = test_r[feature_cols].dropna()
        if X_train.empty or X_test.empty:
            continue

        fold_cache.append(
            {
                "X_train": X_train,
                "y_train": train_r.loc[X_train.index, "resid"],
                "X_test": X_test,
                "y_test": test_r.loc[X_test.index, "resid"],
                "prophet_test": test_r.loc[X_test.index, "prophet_yhat"].to_numpy(),
                "sales_test": test_r.loc[X_test.index, "sales"].to_numpy(),
            }
        )

    return fold_cache


def _optimize_hybrid(model_type: str, fold_cache: List[Dict[str, Any]], config: PipelineConfig):
    """Optuna for hybrid residual stacking per model type."""
    def objective(trial: optuna.Trial) -> float:
        if model_type == "xgboost":
            params = {
                "max_depth": trial.suggest_int("max_depth", 3,10),
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

        return _eval_hybrid_mae(model_type, fold_cache, params)

    study = optuna.create_study(
        direction="minimize",
        sampler=optuna.samplers.TPESampler(seed=RANDOM_SEED),
    )
    study.optimize(objective, n_trials=config.n_optuna_trials)
    return float(study.best_value), study.best_params


@dataclass(frozen=True)
class DishResult:
    dish: str
    champion: str
    mae: Dict[str, float]
    best_params: Dict[str, Dict[str, Any]]


@dataclass(frozen=True)
class DishForecast:
    dish: str
    val_mae: float | None
    pred_future: pd.DataFrame  # date, yhat, lower, upper
    history_tail: pd.DataFrame  # date, sales


def _save_models(dish: str, prophet_model: Prophet, tree_model: Any, config: PipelineConfig, champion: str) -> None:
    safe_name = safe_filename(dish)
    model_dir = Path(config.model_dir)
    model_dir.mkdir(parents=True, exist_ok=True)

    with open(model_dir / f"prophet_{safe_name}.pkl", "wb") as f:
        pickle.dump(prophet_model, f)
    with open(model_dir / f"{champion}_{safe_name}.pkl", "wb") as f:
        pickle.dump(tree_model, f)


def _load_models(dish: str, config: PipelineConfig, champion: str) -> Tuple[Prophet, Any]:
    safe_name = safe_filename(dish)
    model_dir = Path(config.model_dir)
    with open(model_dir / f"prophet_{safe_name}.pkl", "rb") as f:
        prophet_model = pickle.load(f)
    with open(model_dir / f"{champion}_{safe_name}.pkl", "rb") as f:
        tree_model = pickle.load(f)
    return prophet_model, tree_model


def process_dish(dish_name: str, dish_data: pd.DataFrame, country_code: str, config: PipelineConfig) -> DishResult:
    _silence_logs()
    # Prepare data
    dish_data = dish_data.copy()
    dish_data = sanitize_sparse_data(dish_data, country_code)

    # Add date + lag/rolling features (Parallel style)
    dish_feat = _add_date_features(dish_data.copy())
    dish_feat = _add_lag_roll_features(dish_feat)
    fold_cache = _prepare_cv_fold_cache(dish_feat, country_code, config)
    if not fold_cache:
        raise RuntimeError(f"{dish_name}: CV folds unavailable after feature dropna.")

    # Optuna per model
    mae_map: Dict[str, float] = {}
    params_map: Dict[str, Dict[str, Any]] = {}

    for model_type in ["xgboost", "catboost", "lightgbm"]:
        best_mae, best_params = _optimize_hybrid(model_type, fold_cache, config)
        mae_map[model_type] = round(best_mae, 4)
        params_map[model_type] = best_params

    champion = min(mae_map, key=mae_map.get)

    # Retrain champion on full data (Prophet + residual model)
    pm = _fit_prophet(dish_feat, country_code)
    p_full = _prophet_predict(pm, dish_feat)
    train_r = _build_residual_features(dish_feat, p_full)
    X_full = train_r[TREE_FEATURES].dropna()
    y_full = train_r.loc[X_full.index, "resid"]

    if champion == "xgboost":
        model = XGBRegressor(
            n_estimators=100,
            random_state=RANDOM_SEED,
            n_jobs=(1 if CFG.max_workers > 1 else -1),
            **params_map[champion],
        )
        model.fit(X_full, y_full, verbose=False)
    elif champion == "catboost":
        model = CatBoostRegressor(
            iterations=100, random_seed=RANDOM_SEED, verbose=False, **params_map[champion]
        )
        model.fit(X_full, y_full, verbose=False)
    else:
        model = lgb.LGBMRegressor(
            n_estimators=100,
            random_state=RANDOM_SEED,
            n_jobs=(1 if CFG.max_workers > 1 else -1),
            **params_map[champion],
        )
        model.fit(X_full, y_full)

    _save_models(dish_name, pm, model, config, champion)

    return DishResult(
        dish=dish_name,
        champion=champion,
        mae=mae_map,
        best_params=params_map,
    )


def _compute_lag_features_from_history(sales_history: list[float]) -> Dict[str, float]:
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


def _get_weather_forecast(latitude: float, longitude: float) -> pd.DataFrame | None:
    if openmeteo_requests is None or retry is None:
        return None

    try:
        session = retry(retries=3, backoff_factor=0.5)
        om = openmeteo_requests.Client(session=session)

        url = "https://api.open-meteo.com/v1/forecast"
        params = {
            "latitude": latitude,
            "longitude": longitude,
            "daily": WEATHER_COLS,
            "forecast_days": 16,
            "timezone": "auto",
        }

        responses = om.weather_api(url, params=params)
        response = responses[0]
        daily = response.Daily()

        dates = pd.date_range(
            start=pd.to_datetime(daily.Time(), unit="s", utc=True),
            end=pd.to_datetime(daily.TimeEnd(), unit="s", utc=True),
            freq=pd.Timedelta(seconds=daily.Interval()),
            inclusive="left",
        )

        forecast_df = pd.DataFrame({
            "date": dates,
            WEATHER_COLS[0]: daily.Variables(0).ValuesAsNumpy(),
            WEATHER_COLS[1]: daily.Variables(1).ValuesAsNumpy(),
            WEATHER_COLS[2]: daily.Variables(2).ValuesAsNumpy(),
            WEATHER_COLS[3]: daily.Variables(3).ValuesAsNumpy(),
        })
        forecast_df["date"] = forecast_df["date"].dt.tz_localize(None).dt.normalize()
        return forecast_df
    except Exception:
        return None


def _predict_future(
    dish: str,
    dish_data: pd.DataFrame,
    country_code: str,
    prophet_model: Prophet,
    tree_model: Any,
    dish_mae: float | None,
    forecast_weather: pd.DataFrame,
) -> DishForecast:
    dish_data = dish_data.sort_values("date").copy()

    # 历史尾部
    history_tail = dish_data[["date", "sales"]].tail(HISTORY_PLOT_DAYS).copy()

    # 未来天气（在 main 中一次性获取后传入，避免重复请求）
    if forecast_weather is None or forecast_weather.empty:
        raise RuntimeError("无法获取未来天气预测（Open-Meteo）。")

    start_date = dish_data["date"].max() + pd.Timedelta(days=1)
    future_dates = pd.date_range(start=start_date, periods=HORIZON_DAYS, freq="D")
    future_weather = forecast_weather[forecast_weather["date"].isin(future_dates)].copy()
    if len(future_weather) < len(future_dates):
        mean_vals = {c: float(forecast_weather[c].mean()) for c in WEATHER_COLS}
        missing = [d for d in future_dates if d not in set(future_weather["date"])]
        fill = pd.DataFrame({"date": missing})
        for c in WEATHER_COLS:
            fill[c] = mean_vals[c]
        future_weather = pd.concat([future_weather, fill], ignore_index=True).sort_values("date")

    # 先用 Prophet 预测趋势
    future_weather = future_weather.sort_values("date").reset_index(drop=True)
    prophet_yhat_future = _prophet_predict(prophet_model, future_weather)

    # 递归预测残差
    sales_history = dish_data["sales"].astype(float).tolist()
    local_hols = holidays.country_holidays(country_code, years=CFG.holiday_years) if country_code else None

    rows = []
    for i, row in future_weather.sort_values("date").reset_index(drop=True).iterrows():
        dt = pd.to_datetime(row["date"])
        feat = {
            "day_of_week": dt.dayofweek,
            "month": dt.month,
            "day": dt.day,
            "dayofyear": dt.dayofyear,
            "is_weekend": int(dt.dayofweek >= 5),
            "is_public_holiday": int(dt in local_hols) if local_hols is not None else 0,
            "prophet_yhat": float(prophet_yhat_future[i]),
        }
        for c in WEATHER_COLS:
            feat[c] = float(row.get(c, 0.0))

        lag_feats = _compute_lag_features_from_history(sales_history)
        feat.update(lag_feats)

        X_one = pd.DataFrame([{k: feat.get(k, 0.0) for k in TREE_FEATURES}])
        resid_hat = float(tree_model.predict(X_one)[0])
        yhat = max(0.0, float(feat["prophet_yhat"]) + resid_hat)

        if dish_mae is None:
            lower = yhat
            upper = yhat
        else:
            lower = max(0.0, yhat - dish_mae)
            upper = yhat + dish_mae

        # SHAP 解释（分组）
        expl = None
        if shap is not None:
            try:
                explainer = shap.TreeExplainer(tree_model)
                sv = explainer.shap_values(X_one)[0]
                base_val = float(explainer.expected_value)

                group_map = {
                    "Seasonality": TIME_FEATURES,
                    "Holiday": ["is_public_holiday"],
                    "Weather": WEATHER_COLS,
                    "Lags/Trend": [
                        "y_lag_1", "y_lag_7", "y_lag_14",
                        "y_roll_mean_7", "y_roll_std_7",
                        "y_roll_mean_14", "y_roll_std_14",
                        "y_roll_mean_28", "y_roll_std_28",
                    ],
                    "ProphetTrend": ["prophet_yhat"],
                }
                feat_to_group = {}
                for g, feats in group_map.items():
                    for f in feats:
                        feat_to_group[f] = g

                group_shap: Dict[str, float] = {}
                for j, feat_name in enumerate(TREE_FEATURES):
                    group = feat_to_group.get(feat_name, "Other")
                    group_shap[group] = group_shap.get(group, 0.0) + float(sv[j])

                expl = {
                    "ProphetTrend": round(float(feat["prophet_yhat"]), 2),
                    "Seasonality": round(group_shap.get("Seasonality", 0.0), 2),
                    "Holiday": round(group_shap.get("Holiday", 0.0), 2),
                    "Weather": round(group_shap.get("Weather", 0.0), 2),
                    "Lags/Trend": round(group_shap.get("Lags/Trend", 0.0), 2),
                    "ResidualBase": round(base_val, 2),
                }
            except Exception:
                expl = None

        rows.append({
            "date": dt,
            "yhat": yhat,
            "lower": lower,
            "upper": upper,
            "explanation": expl,
        })
        sales_history.append(yhat)

    pred_future = pd.DataFrame(rows)
    return DishForecast(dish=dish, val_mae=dish_mae, pred_future=pred_future, history_tail=history_tail)


def plot_results(results: list[DishForecast], summary: pd.DataFrame, top_n: int = TOP_N_PLOT) -> None:
    if not results:
        return

    top = summary.sort_values("forecast_sum", ascending=False).head(top_n)
    top_dishes = top["dish"].tolist()

    # 条形图
    plt.figure(figsize=(12, max(4, 0.4 * len(top))))
    plt.barh(top["dish"], top["forecast_sum"])
    plt.gca().invert_yaxis()
    plt.xlabel(f"未来 {HORIZON_DAYS} 天预测销量总和")
    plt.title(f"未来 {HORIZON_DAYS} 天销量预测 Top{len(top)}")
    plt.tight_layout()
    plt.savefig(OUT_DIR / f"forecast_top{len(top)}_bar.png", dpi=200)
    plt.close()

    # 折线面板
    n = len(top_dishes)
    cols = 3 if n >= 6 else 2
    rows = int(np.ceil(n / cols))
    fig, axes = plt.subplots(rows, cols, figsize=(6 * cols, 3.5 * rows), sharex=False)
    axes_list = np.array(axes).reshape(-1)

    by_dish = {r.dish: r for r in results}
    for idx, dish in enumerate(top_dishes):
        ax = axes_list[idx]
        r = by_dish[dish]
        ax.plot(r.history_tail["date"], r.history_tail["sales"], label="历史销量", linewidth=1.5)
        ax.plot(r.pred_future["date"], r.pred_future["yhat"], label="预测销量", linewidth=2)
        ax.set_title(dish)
        ax.tick_params(axis="x", rotation=30)
        ax.grid(True, alpha=0.2)
        if idx == 0:
            ax.legend()

    for j in range(n, len(axes_list)):
        axes_list[j].axis("off")

    fig.suptitle(f"Top{len(top_dishes)}：历史（近 {HISTORY_PLOT_DAYS} 天）+ 未来 {HORIZON_DAYS} 天预测", y=1.02)
    fig.tight_layout()
    fig.savefig(OUT_DIR / f"forecast_top{len(top_dishes)}_lines.png", dpi=200, bbox_inches="tight")
    plt.close(fig)


def main() -> None:
    # Load data and enrich with context (same as model.ipynb)
    _ensure_dirs()
    raw_df = fetch_training_data()
    enriched_df, country, lat, lon = add_local_context(raw_df, ADDRESS_INPUT)
    forecast_weather = _get_weather_forecast(lat, lon)
    if forecast_weather is None or forecast_weather.empty:
        raise RuntimeError("无法获取未来天气预测（Open-Meteo）。")

    dishes = enriched_df["dish"].unique().tolist()
    dish_frames = {d: g.copy() for d, g in enriched_df.groupby("dish", sort=False)}
    _log(f"STARTING HYBRID OPTUNA SEARCH | dishes={len(dishes)} | trials={CFG.n_optuna_trials}")

    results: list[DishResult] = []
    with ProcessPoolExecutor(max_workers=CFG.max_workers) as executor:
        futures = {
            executor.submit(process_dish, dish, dish_frames[dish], country, CFG): dish
            for dish in dishes
        }
        total = len(futures)
        pending = set(futures.keys())
        completed = 0
        heartbeat_sec = 30
        t0 = time.time()
        pbar = tqdm(total=total, desc="Train Dishes", unit="dish") if tqdm is not None else None
        try:
            while pending:
                done, pending = wait(pending, timeout=heartbeat_sec, return_when=FIRST_COMPLETED)
                if not done:
                    elapsed = int(time.time() - t0)
                    _log(f"[TRAIN] still running... {completed}/{total} done | elapsed={elapsed}s")
                    continue

                for future in done:
                    dish = futures[future]
                    try:
                        r = future.result()
                        results.append(r)
                        _log(
                            f"{dish:<30} | X={r.mae['xgboost']:<7} C={r.mae['catboost']:<7} "
                            f"L={r.mae['lightgbm']:<7} -> {r.champion.upper()}"
                        )
                    except Exception as e:
                        _log(f"{dish:<30} | FAILED: {e}")
                    completed += 1
                    if pbar is not None:
                        pbar.update(1)
        finally:
            if pbar is not None:
                pbar.close()

    if not results:
        _log("没有生成任何结果（请检查数据/列名/依赖包）。")
        return

    # 保存注册表
    champion_map: Dict[str, Dict[str, Any]] = {}
    for r in results:
        champion_map[r.dish] = {
            "model": r.champion,
            "mae": r.mae.get(r.champion, 0.0),
            "all_mae": r.mae,
            "best_params": r.best_params,
        }

    model_dir = Path(CFG.model_dir)
    model_dir.mkdir(parents=True, exist_ok=True)
    with open(model_dir / "champion_registry.pkl", "wb") as f:
        pickle.dump(champion_map, f)

    # 未来预测与输出
    forecasts: list[DishForecast] = []
    forecast_iter = results
    if tqdm is not None:
        forecast_iter = tqdm(results, total=len(results), desc="Forecast Dishes", unit="dish")
    for r in forecast_iter:
        dish_data = enriched_df[enriched_df["dish"] == r.dish].copy()
        dish_data = sanitize_sparse_data(dish_data, country)
        dish_data = _add_date_features(dish_data)
        dish_data = _add_lag_roll_features(dish_data)

        try:
            prophet_model, tree_model = _load_models(r.dish, CFG, r.champion)
            fc = _predict_future(
                dish=r.dish,
                dish_data=dish_data,
                country_code=country,
                prophet_model=prophet_model,
                tree_model=tree_model,
                dish_mae=r.mae.get(r.champion),
                forecast_weather=forecast_weather,
            )
            forecasts.append(fc)
            out_path = OUT_FORECASTS_DIR / f"{safe_filename(r.dish)}.csv"
            fc.pred_future.to_csv(out_path, index=False, encoding="utf-8-sig")
        except Exception as e:
            _log(f"{r.dish:<30} | FORECAST FAILED: {e}")

    if not forecasts:
        _log("预测失败：未生成任何预测结果。")
        return

    summary_rows = []
    for r in forecasts:
        summary_rows.append(
            {
                "dish": r.dish,
                "val_mae": r.val_mae,
                "forecast_sum": float(r.pred_future["yhat"].sum()),
                "forecast_mean": float(r.pred_future["yhat"].mean()),
                "forecast_start": pd.to_datetime(r.pred_future["date"].min()),
                "forecast_end": pd.to_datetime(r.pred_future["date"].max()),
            }
        )
    summary = pd.DataFrame(summary_rows).sort_values("forecast_sum", ascending=False)
    summary.to_csv(OUT_DIR / "summary.csv", index=False, encoding="utf-8-sig")

    plot_results(forecasts, summary, top_n=TOP_N_PLOT)
    _log("Done.")


if __name__ == "__main__":
    main()
