# -*- coding: utf-8 -*-
"""
Prophet + XGBoost（并行链接/残差堆叠）参数评估脚本

目标
----
为“Prophet + XGBoost 并行链接模型”设计一套可复现的参数组合评估方案：

- Prophet：建模趋势/季节性/假期（可选）+ 天气回归变量
- XGBoost：学习 Prophet 的残差（residual stacking），并使用滞后/滚动/日期/天气/假期等特征
- 评估：滚动回测（rolling origin backtest），递归多步预测（与线上预测一致）

输出
----
- 仅在控制台输出最优参数组合（按 mae_mean）

运行
----
    python3 "PXP Evaluation.py"

依赖
----
- numpy, pandas, scikit-learn, xgboost
- prophet（可选但本脚本评估 Prophet+XGB 时必需）

如果本机未安装 prophet：
    pip install prophet
"""

from __future__ import annotations

import itertools
import json
from dataclasses import dataclass
from pathlib import Path
from typing import Any

import numpy as np
import pandas as pd
from sklearn.metrics import mean_absolute_error, mean_squared_error
from xgboost import XGBRegressor

try:
    from prophet import Prophet  # type: ignore
except Exception:  # pragma: no cover
    Prophet = None  # type: ignore


# ==========================
# 可调参数（按需改这里）
# ==========================
HORIZON_DAYS = 14

# 回测切分：至少训练多少天、每次往前滚动多少天、最多做多少折
MIN_TRAIN_DAYS = 180
STEP_DAYS = 7
MAX_FOLDS: int | None = 6  # None = 尽可能多

# 评估哪些菜品
MAX_DISHES_EVAL=3  # 随机三个菜品
DISH_PICK_STRATEGY = "random"  # top_sales | random
RANDOM_SEED = 42

# 外生变量/特征配置
REG_COLS = ["rain_mm", "avg_temp_c", "avg_humidity_pct"]
LAGS = (1, 7, 14)
ROLL_WINDOWS = (7, 14, 28)

# 输入数据
DATA_FOOD = Path("food_sales.csv")
DATA_WEATHER = Path("df_weather.csv")
DATA_HOLIDAY = Path("df_holiday.csv")



# ==========================
# 参数网格（默认一套可跑的示例）
# ==========================
# 说明：参数组合数 = len(PROPHET_GRID) * len(XGB_GRID)，会乘以 (菜品数 * 折数)。
# 建议先小网格跑通流程，再逐步扩大。
PROPHET_GRID: list[dict[str, Any]] = [
    {
        "seasonality_mode": "additive",
        "changepoint_prior_scale": 0.05,
        "seasonality_prior_scale": 10.0,
        "holidays_prior_scale": 10.0,
        "weekly_seasonality": True,
        "yearly_seasonality": False,
        "daily_seasonality": False,
    },
    {
        "seasonality_mode": "multiplicative",
        "changepoint_prior_scale": 0.05,
        "seasonality_prior_scale": 10.0,
        "holidays_prior_scale": 10.0,
        "weekly_seasonality": True,
        "yearly_seasonality": False,
        "daily_seasonality": False,
    },
    {
        "seasonality_mode": "additive",
        "changepoint_prior_scale": 0.5,
        "seasonality_prior_scale": 10.0,
        "holidays_prior_scale": 10.0,
        "weekly_seasonality": True,
        "yearly_seasonality": False,
        "daily_seasonality": False,
    },
    {
        "seasonality_mode": "multiplicative",
        "changepoint_prior_scale": 0.5,
        "seasonality_prior_scale": 10.0,
        "holidays_prior_scale": 10.0,
        "weekly_seasonality": True,
        "yearly_seasonality": False,
        "daily_seasonality": False,
    },
]

XGB_GRID: list[dict[str, Any]] = [
    {
        "n_estimators": 600, #boosting 的轮数
        "learning_rate": 0.05,
        "max_depth": 6, #每棵树的最大深度
        "subsample": 0.9, #每一棵树用多少比例的样本来训练
        "colsample_bytree": 0.9,  #每棵树使用多少比例的特征
        "reg_lambda": 1.0, #L2 正则化参数
    },
    {
        "n_estimators": 1200,
        "learning_rate": 0.03,
        "max_depth": 6,
        "subsample": 0.9,
        "colsample_bytree": 0.9,
        "reg_lambda": 1.0,
    },
    {
        "n_estimators": 800,
        "learning_rate": 0.04,
        "max_depth": 4,
        "subsample": 0.9,
        "colsample_bytree": 0.9,
        "reg_lambda": 1.0,
    },
    {
        "n_estimators": 1000,
        "learning_rate": 0.05,
        "max_depth": 5,
        "subsample": 0.8,
        "colsample_bytree": 0.8,
        "reg_lambda": 1.0,
    },
    {
        "n_estimators": 1400,
        "learning_rate": 0.03,
        "max_depth": 7,
        "subsample": 0.9,
        "colsample_bytree": 0.8,
        "reg_lambda": 1.0,
    },
    {
        "n_estimators": 700,
        "learning_rate": 0.08,
        "max_depth": 3,
        "subsample": 0.8,
        "colsample_bytree": 0.9,
        "reg_lambda": 1.0,
    },
]


@dataclass(frozen=True)
class FoldResult:
    dish: str
    fold: int
    train_end: pd.Timestamp
    val_start: pd.Timestamp
    val_end: pd.Timestamp
    prophet_params: str
    xgb_params: str
    mae: float
    rmse: float
    smape: float


def _to_params_str(d: dict[str, Any]) -> str:
    return json.dumps(d, ensure_ascii=False, sort_keys=True)


def _load_inputs() -> tuple[pd.DataFrame, pd.DataFrame, pd.DataFrame]:
    if not DATA_FOOD.exists():
        raise FileNotFoundError(f"找不到数据文件：{DATA_FOOD}")
    if not DATA_WEATHER.exists():
        raise FileNotFoundError(f"找不到数据文件：{DATA_WEATHER}")
    if not DATA_HOLIDAY.exists():
        raise FileNotFoundError(f"找不到数据文件：{DATA_HOLIDAY}")

    df = pd.read_csv(DATA_FOOD, encoding="utf-8-sig")
    weather = pd.read_csv(DATA_WEATHER, encoding="utf-8-sig")
    hol = pd.read_csv(DATA_HOLIDAY, encoding="utf-8-sig")

    required = {"日期", "菜品", "销量"}
    missing = required - set(df.columns)
    if missing:
        raise ValueError(f"`food_sales.csv` 缺少列：{sorted(missing)}")

    df["日期"] = pd.to_datetime(df["日期"])
    weather["ds"] = pd.to_datetime(weather["ds"])

    hol.columns = hol.columns.str.replace("\ufeff", "", regex=False)
    if "date" not in hol.columns:
        raise ValueError("`df_holiday.csv` 缺少 `date` 列")
    hol["date"] = pd.to_datetime(hol["date"])

    for c in REG_COLS:
        if c not in weather.columns:
            weather[c] = np.nan
    weather = weather[["ds"] + REG_COLS].copy().sort_values("ds")
    return df, weather, hol


def _build_holidays(hol: pd.DataFrame) -> pd.DataFrame:
    name_col = "holiday_name" if "holiday_name" in hol.columns else "holiday"
    needed = {"date", name_col, "lower_window", "upper_window"}
    missing = needed - set(hol.columns)
    if missing:
        raise ValueError(f"`df_holiday.csv` 缺少列：{sorted(missing)}")

    holidays = hol.rename(columns={"date": "ds", name_col: "holiday"})[
        ["ds", "holiday", "lower_window", "upper_window"]
    ].copy()
    holidays["holiday"] = holidays["holiday"].fillna("").astype(str)
    holidays = holidays[holidays["holiday"].ne("")]
    holidays["lower_window"] = holidays["lower_window"].fillna(0).astype(int)
    holidays["upper_window"] = holidays["upper_window"].fillna(0).astype(int)
    return holidays


def _add_holiday_flag(ds: pd.Series, holidays: pd.DataFrame) -> pd.Series:
    if holidays.empty:
        return pd.Series(np.zeros(len(ds), dtype=int), index=ds.index)

    ds_vals = pd.to_datetime(ds).to_numpy(dtype="datetime64[D]")
    flag = np.zeros(len(ds_vals), dtype=bool)
    for row in holidays.itertuples(index=False):
        start = np.datetime64(row.ds, "D") + np.timedelta64(row.lower_window, "D")
        end = np.datetime64(row.ds, "D") + np.timedelta64(row.upper_window, "D")
        flag |= (ds_vals >= start) & (ds_vals <= end)
    return pd.Series(flag.astype(int), index=ds.index)


def _prepare_dish_daily_series(df: pd.DataFrame, dish: str) -> pd.DataFrame:
    one = df.loc[df["菜品"] == dish, ["日期", "销量"]].copy()
    daily = (
        one.groupby("日期", as_index=False)["销量"]
        .sum()
        .rename(columns={"日期": "ds", "销量": "y"})
        .sort_values("ds")
    )
    full_ds = pd.date_range(daily["ds"].min(), daily["ds"].max(), freq="D")
    daily = daily.set_index("ds").reindex(full_ds).rename_axis("ds").reset_index()
    daily["y"] = daily["y"].fillna(0.0)
    return daily


def _merge_exog(dish_daily: pd.DataFrame, weather: pd.DataFrame, holidays: pd.DataFrame) -> pd.DataFrame:
    df_all = dish_daily.merge(weather, on="ds", how="left").sort_values("ds")
    for c in REG_COLS:
        df_all[c] = df_all[c].ffill()
        df_all[c] = df_all[c].fillna(df_all[c].mean())
        df_all[c] = df_all[c].fillna(0.0)
    df_all["is_holiday"] = _add_holiday_flag(df_all["ds"], holidays)
    return df_all


def _add_date_features(df_all: pd.DataFrame) -> pd.DataFrame:
    ds = pd.to_datetime(df_all["ds"])
    out = df_all.copy()
    out["dow"] = ds.dt.dayofweek
    out["month"] = ds.dt.month
    out["day"] = ds.dt.day
    out["dayofyear"] = ds.dt.dayofyear
    out["is_weekend"] = (out["dow"] >= 5).astype(int)
    return out


def _add_lag_roll_features(df_all: pd.DataFrame) -> pd.DataFrame:
    out = df_all.copy()
    for lag in LAGS:
        out[f"y_lag_{lag}"] = out["y"].shift(lag)
    for w in ROLL_WINDOWS:
        s = out["y"].shift(1)
        out[f"y_roll_mean_{w}"] = s.rolling(w).mean()
        out[f"y_roll_std_{w}"] = s.rolling(w).std()
    return out


def _iter_backtest_splits(n: int, min_train: int, horizon: int, step: int, max_folds: int | None) -> list[int]:
    """
    返回一组 train_end 索引（exclusive），对应滚动回测的每一折。
    例：train = [0:train_end), val = [train_end:train_end+horizon)
    """
    last_train_end = n - horizon
    if last_train_end <= min_train:
        return []

    ends = list(range(min_train, last_train_end + 1, step))
    if max_folds is not None:
        ends = ends[-max_folds:]  # 取最后若干折（更接近“最近”的表现）
    return ends


def _build_xgb_model(params: dict[str, Any]) -> XGBRegressor:
    base = {
        "objective": "reg:squarederror",
        "random_state": 42,
        "n_jobs": -1,
    }
    merged = {**base, **params}
    return XGBRegressor(**merged)


def _smape(y_true: np.ndarray, y_pred: np.ndarray) -> float:
    eps = 1e-6
    denom = np.abs(y_true) + np.abs(y_pred) + eps
    return float(np.mean(200.0 * np.abs(y_true - y_pred) / denom))


def _fit_prophet_and_predict(
    train_df: pd.DataFrame,
    val_df: pd.DataFrame,
    holidays: pd.DataFrame,
    prophet_params: dict[str, Any],
) -> tuple[np.ndarray, np.ndarray]:
    """
    返回 (train_yhat, val_yhat)。
    Prophet 只使用：ds, y, REG_COLS（天气回归变量）+ holidays（可选）。
    """
    if Prophet is None:
        raise ImportError(
            "缺少依赖：prophet。请在你的运行环境中安装：pip install prophet"
        )

    model = Prophet(holidays=holidays, **prophet_params)  # type: ignore[misc]
    for c in REG_COLS:
        model.add_regressor(c)  # type: ignore[no-untyped-call]

    fit_cols = ["ds", "y"] + REG_COLS
    model.fit(train_df[fit_cols])  # type: ignore[no-untyped-call]

    pred_train = model.predict(train_df[["ds"] + REG_COLS])  # type: ignore[no-untyped-call]
    pred_val = model.predict(val_df[["ds"] + REG_COLS])  # type: ignore[no-untyped-call]
    return pred_train["yhat"].astype(float).to_numpy(), pred_val["yhat"].astype(float).to_numpy()


def _train_xgb_on_residuals(
    train_feat: pd.DataFrame,
    prophet_yhat_train: np.ndarray,
    xgb_params: dict[str, Any],
) -> tuple[XGBRegressor, list[str]]:
    """
    训练残差模型：target = y - prophet_yhat。
    训练特征 = 日期/天气/假期 + lag/rolling + prophet_yhat。
    """
    df = train_feat.copy()
    df["prophet_yhat"] = prophet_yhat_train
    df["resid"] = df["y"].astype(float) - df["prophet_yhat"].astype(float)

    feature_cols = [c for c in df.columns if c not in {"ds", "y", "resid"}]
    df = df.dropna(subset=feature_cols + ["resid"]).copy()

    model = _build_xgb_model(xgb_params)
    model.fit(df[feature_cols], df["resid"].astype(float))
    return model, feature_cols


def _recursive_hybrid_forecast(
    train_hist: pd.DataFrame,
    future_exog: pd.DataFrame,
    prophet_yhat_future: np.ndarray,
    xgb_model: XGBRegressor,
    xgb_feature_cols: list[str],
) -> pd.DataFrame:
    """
    递归预测未来 horizon：用“历史真实 + 已预测”构造 lag/rolling 特征。
    """
    y_hist = train_hist["y"].astype(float).tolist()
    rows: list[dict[str, Any]] = []

    for i, row in future_exog.sort_values("ds").reset_index(drop=True).iterrows():
        ds = pd.to_datetime(row["ds"])
        feat: dict[str, Any] = {
            "is_holiday": int(row.get("is_holiday", 0)),
            "dow": int(ds.dayofweek),
            "month": int(ds.month),
            "day": int(ds.day),
            "dayofyear": int(ds.dayofyear),
            "is_weekend": int(ds.dayofweek >= 5),
            "prophet_yhat": float(prophet_yhat_future[i]),
        }

        for c in REG_COLS:
            feat[c] = float(row.get(c, 0.0))

        for lag in LAGS:
            feat[f"y_lag_{lag}"] = float(y_hist[-lag]) if len(y_hist) >= lag else 0.0

        for w in ROLL_WINDOWS:
            window = y_hist[-w:] if len(y_hist) >= w else y_hist[:]
            if window:
                feat[f"y_roll_mean_{w}"] = float(np.mean(window))
                feat[f"y_roll_std_{w}"] = float(np.std(window, ddof=1)) if len(window) >= 2 else 0.0
            else:
                feat[f"y_roll_mean_{w}"] = 0.0
                feat[f"y_roll_std_{w}"] = 0.0

        X_one = pd.DataFrame([{k: feat.get(k, 0.0) for k in xgb_feature_cols}])
        resid_hat = float(xgb_model.predict(X_one)[0])
        yhat = float(feat["prophet_yhat"]) + resid_hat
        yhat = max(0.0, yhat)

        rows.append(
            {
                "ds": ds,
                "prophet_yhat": float(feat["prophet_yhat"]),
                "resid_hat": resid_hat,
                "yhat": yhat,
            }
        )
        y_hist.append(yhat)

    return pd.DataFrame(rows)


def _evaluate_one_fold(
    df_feat_all: pd.DataFrame,
    train_end_idx: int,
    holidays: pd.DataFrame,
    prophet_params: dict[str, Any],
    xgb_params: dict[str, Any],
) -> tuple[pd.DataFrame, float, float, float]:
    """
    单折评估（递归多步）：返回 (pred_df, mae, rmse, smape)。
    """
    train_df = df_feat_all.iloc[:train_end_idx].copy()
    val_df = df_feat_all.iloc[train_end_idx : train_end_idx + HORIZON_DAYS].copy()

    prophet_yhat_train, prophet_yhat_val = _fit_prophet_and_predict(
        train_df=train_df[["ds", "y"] + REG_COLS],
        val_df=val_df[["ds", "y"] + REG_COLS],
        holidays=holidays,
        prophet_params=prophet_params,
    )

    # 残差模型训练（用训练段的特征：天气/假期/日期/lag/rolling）
    xgb_model, feat_cols = _train_xgb_on_residuals(train_df, prophet_yhat_train, xgb_params)

    # 递归预测（未来段只提供 exog：天气 + is_holiday + ds）
    future_exog = val_df[["ds", "is_holiday"] + REG_COLS].copy()
    pred = _recursive_hybrid_forecast(
        train_hist=train_df[["ds", "y"]],
        future_exog=future_exog,
        prophet_yhat_future=prophet_yhat_val,
        xgb_model=xgb_model,
        xgb_feature_cols=feat_cols,
    )

    y_true = val_df["y"].astype(float).to_numpy()
    y_pred = pred["yhat"].astype(float).to_numpy()

    mae = float(mean_absolute_error(y_true, y_pred))
    rmse = float(mean_squared_error(y_true, y_pred, squared=False))
    smape = _smape(y_true, y_pred)
    return pred, mae, rmse, smape


def _pick_dishes(df: pd.DataFrame) -> list[str]:
    dish_series = df["菜品"].dropna().astype(str)
    dish_series = dish_series[dish_series.str.strip().ne("")]
    dishes = dish_series.unique().tolist()

    if MAX_DISHES_EVAL is None or MAX_DISHES_EVAL >= len(dishes):
        dishes.sort()
        return dishes

    if DISH_PICK_STRATEGY == "random":
        rng = np.random.default_rng(RANDOM_SEED)
        dishes = rng.choice(dishes, size=MAX_DISHES_EVAL, replace=False).tolist()
        dishes.sort()
        return dishes

    # 默认：按总销量从高到低取 TopK
    tmp = df.groupby("菜品", as_index=False)["销量"].sum().sort_values("销量", ascending=False)
    dishes = tmp["菜品"].astype(str).head(MAX_DISHES_EVAL).tolist()
    return dishes


def evaluate() -> tuple[pd.DataFrame, pd.DataFrame]:
    df, weather, hol = _load_inputs()
    holidays = _build_holidays(hol)
    dishes = _pick_dishes(df)

    combos = list(itertools.product(PROPHET_GRID, XGB_GRID))
    print(f"评估菜品数={len(dishes)}，参数组合数={len(combos)}，每折预测步长={HORIZON_DAYS} 天")

    rows: list[FoldResult] = []

    for dish_idx, dish in enumerate(dishes, start=1):
        dish_daily = _prepare_dish_daily_series(df, dish)
        df_all = _merge_exog(dish_daily, weather, holidays)
        df_all = _add_date_features(df_all)
        df_all = _add_lag_roll_features(df_all)
        df_all = df_all.sort_values("ds").reset_index(drop=True)

        split_ends = _iter_backtest_splits(
            n=len(df_all),
            min_train=MIN_TRAIN_DAYS,
            horizon=HORIZON_DAYS,
            step=STEP_DAYS,
            max_folds=MAX_FOLDS,
        )
        if not split_ends:
            print(f"[{dish_idx}/{len(dishes)}] {dish} 跳过：数据不足以回测（len={len(df_all)})")
            continue

        for combo_idx, (pp, xp) in enumerate(combos, start=1):
            pp_s = _to_params_str(pp)
            xp_s = _to_params_str(xp)

            for fold, train_end in enumerate(split_ends, start=1):
                try:
                    _, mae, rmse, smape = _evaluate_one_fold(
                        df_feat_all=df_all,
                        train_end_idx=train_end,
                        holidays=holidays,
                        prophet_params=pp,
                        xgb_params=xp,
                    )
                    train_end_ts = pd.to_datetime(df_all.iloc[train_end - 1]["ds"])
                    val_start_ts = pd.to_datetime(df_all.iloc[train_end]["ds"])
                    val_end_ts = pd.to_datetime(df_all.iloc[train_end + HORIZON_DAYS - 1]["ds"])
                    rows.append(
                        FoldResult(
                            dish=dish,
                            fold=fold,
                            train_end=train_end_ts,
                            val_start=val_start_ts,
                            val_end=val_end_ts,
                            prophet_params=pp_s,
                            xgb_params=xp_s,
                            mae=mae,
                            rmse=rmse,
                            smape=smape,
                        )
                    )
                except Exception as e:
                    print(
                        f"[{dish_idx}/{len(dishes)}] {dish} combo {combo_idx}/{len(combos)} fold {fold} 失败: {e}"
                    )

        print(f"[{dish_idx}/{len(dishes)}] {dish} done | folds={len(split_ends)}")

    if not rows:
        raise RuntimeError("未生成任何评估结果（请检查数据、依赖、参数网格）。")

    results = pd.DataFrame([r.__dict__ for r in rows])

    # 聚合：按参数组合统计 across dishes/folds
    summary = (
        results.groupby(["prophet_params", "xgb_params"], as_index=False)
        .agg(
            n=("mae", "size"),
            mae_mean=("mae", "mean"),
            mae_std=("mae", "std"),
            rmse_mean=("rmse", "mean"),
            rmse_std=("rmse", "std"),
            smape_mean=("smape", "mean"),
            smape_std=("smape", "std"),
        )
        .sort_values(["mae_mean", "rmse_mean", "smape_mean"], ascending=True)
        .reset_index(drop=True)
    )

    best = summary.iloc[0]
    print("最佳组合（按 mae_mean 升序）：")
    print(f"  mae_mean={best['mae_mean']:.4f} | rmse_mean={best['rmse_mean']:.4f} | smape_mean={best['smape_mean']:.2f}")
    print(f"  prophet_params={best['prophet_params']}")
    print(f"  xgb_params={best['xgb_params']}")
    return results, summary


def main() -> None:
    evaluate()


if __name__ == "__main__":
    main()
