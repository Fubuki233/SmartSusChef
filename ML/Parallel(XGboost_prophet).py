# -*- coding: utf-8 -*-
"""
对所有菜品做销量预测（批量）。

说明：此文件与 `Parallel(XGboost_prpphet).py` 保持同样逻辑，避免两个脚本行为不一致。
"""

from __future__ import annotations

# 直接复用同目录脚本内容（为了保持一致，这里做一次“源码复制”式同步）

import re
from dataclasses import dataclass
from pathlib import Path

import numpy as np
import pandas as pd
from sklearn.metrics import mean_absolute_error
from xgboost import XGBRegressor


try:
    import seaborn as sns  # type: ignore

    sns.set_theme()
except Exception:
    sns = None

import matplotlib.pyplot as plt


HORIZON_DAYS = 14
MAX_DISHES: int | None = None
TOP_N_PLOT = 12
HISTORY_PLOT_DAYS = 90

REG_COLS = ["rain_mm", "avg_temp_c", "avg_humidity_pct"]
LAGS = (1, 7, 14)
ROLL_WINDOWS = (7, 14, 28)

DATA_FOOD = Path("food_sales.csv")
DATA_WEATHER = Path("df_weather.csv")
DATA_HOLIDAY = Path("df_holiday.csv")
DATA_WEATHER_FUTURE = Path("df_weather_future.csv")
OUT_DIR = Path("outputs")
OUT_FORECASTS_DIR = OUT_DIR / "forecasts"


@dataclass(frozen=True)
class DishForecast:
    dish: str
    val_mae: float | None
    pred_future: pd.DataFrame
    history_tail: pd.DataFrame


def _sanitize_filename(name: str, max_len: int = 120) -> str:
    s = re.sub(r"[\\\\/:*?\"<>|]+", "_", name.strip())
    s = re.sub(r"\\s+", " ", s).strip()
    if not s:
        s = "unnamed"
    return s[:max_len]


def _ensure_dirs() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    OUT_FORECASTS_DIR.mkdir(parents=True, exist_ok=True)


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
        start = (np.datetime64(row.ds, "D") + np.timedelta64(row.lower_window, "D"))
        end = (np.datetime64(row.ds, "D") + np.timedelta64(row.upper_window, "D"))
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


def _merge_exog(
    dish_daily: pd.DataFrame, weather: pd.DataFrame, holidays: pd.DataFrame
) -> pd.DataFrame:
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


def _train_xgb(df_feat: pd.DataFrame) -> tuple[XGBRegressor, list[str], float | None]:
    df_feat = df_feat.dropna().copy()
    if len(df_feat) < max(120, HORIZON_DAYS * 3):
        feature_cols = [c for c in df_feat.columns if c not in {"ds", "y"}]
        model = XGBRegressor(
            n_estimators=600,
            learning_rate=0.05,
            max_depth=6,
            subsample=0.9,
            colsample_bytree=0.9,
            reg_lambda=1.0,
            objective="reg:squarederror",
            random_state=42,
        )
        model.fit(df_feat[feature_cols], df_feat["y"])
        return model, feature_cols, None

    split = len(df_feat) - HORIZON_DAYS
    train_df = df_feat.iloc[:split].copy()
    val_df = df_feat.iloc[split:].copy()

    feature_cols = [c for c in df_feat.columns if c not in {"ds", "y"}]
    X_train, y_train = train_df[feature_cols], train_df["y"]
    X_val, y_val = val_df[feature_cols], val_df["y"]

    model = XGBRegressor(
        n_estimators=1200,
        learning_rate=0.03,
        max_depth=6,
        subsample=0.9,
        colsample_bytree=0.9,
        reg_lambda=1.0,
        objective="reg:squarederror",
        random_state=42,
    )
    model.fit(X_train, y_train)

    pred_val = model.predict(X_val)
    val_mae = float(mean_absolute_error(y_val, pred_val))

    model.fit(df_feat[feature_cols], df_feat["y"])
    return model, feature_cols, val_mae


def _prepare_future_weather(
    weather_hist: pd.DataFrame, future_ds: pd.DatetimeIndex
) -> pd.DataFrame:
    if DATA_WEATHER_FUTURE.exists():
        wf = pd.read_csv(DATA_WEATHER_FUTURE, encoding="utf-8-sig")
        wf["ds"] = pd.to_datetime(wf["ds"])
        ok = {"ds", *REG_COLS}.issubset(set(wf.columns))
        if ok:
            wf = wf[["ds"] + REG_COLS].copy()
            wf = wf[wf["ds"].isin(future_ds)].sort_values("ds")
            if len(wf) == len(future_ds):
                return wf.reset_index(drop=True)

    last = weather_hist.sort_values("ds").iloc[-1]
    out = pd.DataFrame({"ds": future_ds})
    for c in REG_COLS:
        out[c] = float(last.get(c, 0.0))
    return out


def _recursive_forecast(
    model: XGBRegressor,
    feature_cols: list[str],
    hist_df: pd.DataFrame,
    future_weather: pd.DataFrame,
    holidays: pd.DataFrame,
) -> pd.DataFrame:
    hist_df = hist_df.sort_values("ds").copy()
    y_hist = hist_df["y"].astype(float).tolist()

    rows = []
    for _, row in future_weather.sort_values("ds").reset_index(drop=True).iterrows():
        ds = pd.to_datetime(row["ds"])
        feat = {
            "ds": ds,
            "is_holiday": int(_add_holiday_flag(pd.Series([ds]), holidays).iloc[0]),
            "dow": int(ds.dayofweek),
            "month": int(ds.month),
            "day": int(ds.day),
            "dayofyear": int(ds.dayofyear),
            "is_weekend": int(ds.dayofweek >= 5),
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

        X_one = pd.DataFrame([{k: feat.get(k) for k in feature_cols}])
        yhat = float(model.predict(X_one)[0])
        yhat = max(0.0, yhat)

        rows.append({"ds": ds, "yhat": yhat})
        y_hist.append(yhat)

    return pd.DataFrame(rows)


def forecast_one_dish(
    df: pd.DataFrame, weather: pd.DataFrame, holidays: pd.DataFrame, dish: str
) -> DishForecast:
    dish_daily = _prepare_dish_daily_series(df, dish)
    df_all = _merge_exog(dish_daily, weather, holidays)
    df_all = _add_date_features(df_all)
    df_feat = _add_lag_roll_features(df_all)

    model, feature_cols, val_mae = _train_xgb(df_feat)

    future_ds = pd.date_range(
        df_all["ds"].max() + pd.Timedelta(days=1), periods=HORIZON_DAYS, freq="D"
    )
    future_weather = _prepare_future_weather(weather, future_ds)
    pred_future = _recursive_forecast(
        model, feature_cols, df_all[["ds", "y"]], future_weather, holidays
    )

    history_tail = df_all[["ds", "y"]].tail(HISTORY_PLOT_DAYS).copy()
    return DishForecast(dish=dish, val_mae=val_mae, pred_future=pred_future, history_tail=history_tail)


def plot_results(
    results: list[DishForecast],
    summary: pd.DataFrame,
    top_n: int = TOP_N_PLOT,
) -> None:
    if not results:
        return

    top = summary.sort_values("forecast_sum", ascending=False).head(top_n)
    top_dishes = top["dish"].tolist()

    plt.figure(figsize=(12, max(4, 0.4 * len(top))))
    plt.barh(top["dish"], top["forecast_sum"])
    plt.gca().invert_yaxis()
    plt.xlabel(f"未来 {HORIZON_DAYS} 天预测销量总和")
    plt.title(f"未来 {HORIZON_DAYS} 天销量预测 Top{len(top)}")
    plt.tight_layout()
    plt.savefig(OUT_DIR / f"forecast_top{len(top)}_bar.png", dpi=200)
    plt.close()

    n = len(top_dishes)
    cols = 3 if n >= 6 else 2
    rows = int(np.ceil(n / cols))
    fig, axes = plt.subplots(rows, cols, figsize=(6 * cols, 3.5 * rows), sharex=False)
    axes_list = np.array(axes).reshape(-1)

    by_dish = {r.dish: r for r in results}
    for idx, dish in enumerate(top_dishes):
        ax = axes_list[idx]
        r = by_dish[dish]
        ax.plot(r.history_tail["ds"], r.history_tail["y"], label="历史销量", linewidth=1.5)
        ax.plot(r.pred_future["ds"], r.pred_future["yhat"], label="预测销量", linewidth=2)
        ax.set_title(dish)
        ax.tick_params(axis="x", rotation=30)
        ax.grid(True, alpha=0.2)
        if idx == 0:
            ax.legend()

    for j in range(n, len(axes_list)):
        axes_list[j].axis("off")

    fig.suptitle(
        f"Top{len(top_dishes)}：历史（近 {HISTORY_PLOT_DAYS} 天）+ 未来 {HORIZON_DAYS} 天预测",
        y=1.02,
    )
    fig.tight_layout()
    fig.savefig(OUT_DIR / f"forecast_top{len(top_dishes)}_lines.png", dpi=200, bbox_inches="tight")
    plt.close(fig)


def main() -> None:
    _ensure_dirs()
    df, weather, hol = _load_inputs()
    holidays = _build_holidays(hol)

    dish_series = df["菜品"].dropna().astype(str)
    dish_series = dish_series[dish_series.str.strip().ne("")]
    dishes = dish_series.unique().tolist()
    dishes.sort()

    if MAX_DISHES is not None:
        dishes = dishes[:MAX_DISHES]

    results: list[DishForecast] = []
    for i, dish in enumerate(dishes, start=1):
        try:
            r = forecast_one_dish(df, weather, holidays, dish)
            results.append(r)

            out_path = OUT_FORECASTS_DIR / f"{_sanitize_filename(dish)}.csv"
            r.pred_future.to_csv(out_path, index=False, encoding="utf-8-sig")

            mae_str = "NA" if r.val_mae is None else f"{r.val_mae:.4f}"
            print(f"[{i}/{len(dishes)}] {dish} done | val_MAE={mae_str}")
        except Exception as e:
            print(f"[{i}/{len(dishes)}] {dish} failed: {e}")

    if not results:
        print("没有生成任何结果（请检查数据/列名/依赖包）。")
        return

    summary_rows = []
    for r in results:
        summary_rows.append(
            {
                "dish": r.dish,
                "val_mae": r.val_mae,
                "forecast_sum": float(r.pred_future["yhat"].sum()),
                "forecast_mean": float(r.pred_future["yhat"].mean()),
                "forecast_start": pd.to_datetime(r.pred_future["ds"].min()),
                "forecast_end": pd.to_datetime(r.pred_future["ds"].max()),
            }
        )
    summary = pd.DataFrame(summary_rows).sort_values("forecast_sum", ascending=False)
    summary.to_csv(OUT_DIR / "summary.csv", index=False, encoding="utf-8-sig")

    plot_results(results, summary, top_n=TOP_N_PLOT)
    print(f"已输出：{OUT_DIR / 'summary.csv'}")


if __name__ == "__main__":
    main()
