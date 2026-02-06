#!/usr/bin/env python
# coding: utf-8

# # SmartSus Chef: The Universal Predictive Engine
# **Version:** 4.0 (Hybrid Prophet + Tree) | **Architecture:** Prophet + Tree Residual Stacking
#
# ## How to read this Notebook
# This notebook runs a robust, parallelized ML pipeline to predict food demand using a
# Prophet + Tree Residual Stacking methodology. It acts as a high-level **orchestrator**,
# delegating the complex implementation details to the `training_logic_v2.py` module.
#
# ### The End-to-End Workflow:
# 1.  **Context Detection:** Automatically determines the restaurant's location to load correct holiday/weather data.
# 2.  **Data Ingestion:** Fetches the raw sales history from a database or a fallback CSV file.
# 3.  **Data Cleaning & Sanitation:** Aggregates data to have one record per dish-day and fills gaps.
# 4.  **Hybrid Model Training:** For each dish:
#     - Prophet models trend/seasonality/holidays with weather regressors
#     - Tree models (XGBoost, CatBoost, LightGBM) learn the Prophet residuals
#     - Optuna optimizes hyperparameters for each tree model
#     - The champion is selected based on lowest CV MAE
# 5.  **Parallel Execution:** Each dish is processed on a separate CPU core.
# 6.  **Forecasting:** Multi-day rolling forecast combines Prophet trend + tree residual predictions.
# 7.  **Explainability:** SHAP-based explanations show contribution of each feature group.

# In[ ]:

# --- IMPORTS & SETUP ---
from __future__ import annotations

import os
os.environ.setdefault("CMDSTANPY_LOG_LEVEL", "WARNING")

# Load environment variables from .env file
try:
    from dotenv import load_dotenv
    load_dotenv()
except ImportError:
    pass  # python-dotenv not installed, use system env vars

import numpy as np
import pandas as pd
import joblib
import holidays
import warnings
import logging
import traceback
from typing import Optional, Dict, Any, List

# Set up logging for this module
logger = logging.getLogger(__name__)
logger.setLevel(logging.INFO)
if not logger.handlers:
    _handler = logging.StreamHandler()
    _handler.setFormatter(logging.Formatter(
        "%(asctime)s [%(levelname)s] %(name)s: %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S"
    ))
    logger.addHandler(_handler)

try:
    import shap
except Exception:
    shap = None

try:
    import openmeteo_requests
    from retry_requests import retry
except Exception:
    openmeteo_requests = None
    retry = None

from concurrent.futures import ProcessPoolExecutor, as_completed

try:
    from tqdm.notebook import tqdm
except Exception:
    try:
        from tqdm import tqdm
    except Exception:
        tqdm = None

try:
    from IPython.display import display
except Exception:
    display = print

import matplotlib.pyplot as plt
import matplotlib.ticker as ticker

# Import core pipeline logic from our module
from training_logic_v2 import (
    PipelineConfig,
    CFG,
    process_dish,
    fetch_training_data,
    add_local_context,
    get_location_details,
    safe_filename,
    compute_lag_features_from_history,
    _load_hybrid_models,
    _prophet_predict,
    _silence_logs,
    WEATHER_COLS,
)

# Silence verbose logging (centralized in training_logic_v2)
warnings.filterwarnings('ignore')
_silence_logs()

logger.info("Libraries loaded successfully.")


# --- MODEL & GEOCODE CACHES ---
_model_cache = {}
_geocode_cache = {}
_forecast_cache = {}


def _load_cached(filepath):
    """Load a joblib-serialized object with caching."""
    if filepath not in _model_cache:
        _model_cache[filepath] = joblib.load(filepath)
    return _model_cache[filepath]


def clear_model_cache():
    """Clear all caches."""
    _model_cache.clear()
    _geocode_cache.clear()
    _forecast_cache.clear()


def _get_location_cached(address):
    """Cached geocoding lookup to avoid redundant API calls."""
    if address not in _geocode_cache:
        _geocode_cache[address] = get_location_details(address)
    return _geocode_cache[address]


def get_weather_forecast(latitude, longitude):
    """
    Fetch 14-day weather forecast from the Open-Meteo Forecast API.
    Returns a DataFrame with columns: date + WEATHER_COLS, or None if the API call fails.
    """
    if openmeteo_requests is None or retry is None:
        logger.warning("openmeteo_requests or retry_requests not available.")
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

        forecast_df = pd.DataFrame({
            "date": dates,
            WEATHER_COLS[0]: daily.Variables(0).ValuesAsNumpy(),
            WEATHER_COLS[1]: daily.Variables(1).ValuesAsNumpy(),
            WEATHER_COLS[2]: daily.Variables(2).ValuesAsNumpy(),
            WEATHER_COLS[3]: daily.Variables(3).ValuesAsNumpy(),
        })
        forecast_df['date'] = forecast_df['date'].dt.tz_localize(None).dt.normalize()

        logger.info("Fetched %d-day weather forecast.", len(forecast_df))
        return forecast_df
    except Exception as e:
        logger.error("Failed to fetch weather forecast: %s", e)
        return None


def _get_forecast_cached(lat, lon):
    """Cached weather forecast lookup to avoid redundant API calls."""
    key = (round(lat, 4), round(lon, 4))
    if key not in _forecast_cache:
        _forecast_cache[key] = get_weather_forecast(lat, lon)
    return _forecast_cache[key]


def _predict_hybrid_multiday(
    prophet_model,
    tree_model,
    forecast_weather_df: pd.DataFrame,
    start_date: pd.Timestamp,
    recent_sales_df: pd.DataFrame,
    config: PipelineConfig,
    country_code: str,
    dish_mae: float
) -> list:
    """
    Recursive multi-day forecast using Prophet + Tree hybrid model.

    For each day:
    1. Generate Prophet's trend/seasonality prediction
    2. Predict residual using tree model
    3. Combine: final_pred = prophet_yhat + tree_resid

    Returns list of {date, qty, lower, upper, explanation} dicts.
    """
    results = []
    sales_history = recent_sales_df['sales'].values.tolist()

    # Build feature name -> group mapping from config
    feat_to_group = {}
    for group_name, feat_list in config.feature_groups.items():
        for feat in feat_list:
            feat_to_group[feat] = group_name

    local_hols = holidays.country_holidays(country_code, years=config.holiday_years)

    # Instantiate SHAP explainer ONCE before the loop for efficiency
    shap_explainer = None
    if shap is not None:
        try:
            shap_explainer = shap.TreeExplainer(tree_model)
        except Exception:
            shap_explainer = None

    for day_offset in range(config.forecast_horizon):
        dt = start_date + pd.Timedelta(days=day_offset)
        is_hol = 1 if dt in local_hols else 0

        # Get weather from real forecast data for this date
        weather_row = forecast_weather_df[
            forecast_weather_df['date'].dt.normalize() == dt.normalize()
        ]
        if len(weather_row) > 0:
            weather_vals = {col: float(weather_row[col].iloc[0]) for col in WEATHER_COLS}
        else:
            # Fallback: use forecast average for dates beyond range
            weather_vals = {col: float(forecast_weather_df[col].mean()) for col in WEATHER_COLS}

        # Get Prophet prediction for this date
        future_df_prophet = pd.DataFrame([{
            "date": dt,
            **weather_vals
        }])
        prophet_yhat = float(_prophet_predict(prophet_model, future_df_prophet)[0])

        # Compute lag features from history
        lag_feats = compute_lag_features_from_history(sales_history, config)

        # Build feature row for tree model
        row = {
            "day_of_week": dt.dayofweek,
            "month": dt.month,
            "day": dt.day,
            "dayofyear": dt.dayofyear,
            "is_weekend": int(dt.dayofweek >= 5),
            "is_public_holiday": is_hol,
            "prophet_yhat": prophet_yhat,
        }
        row.update(weather_vals)
        row.update(lag_feats)

        # Create feature DataFrame with correct column order
        X_one = pd.DataFrame([{k: row.get(k, 0.0) for k in config.hybrid_tree_features}])

        # Predict residual with tree model
        resid_hat = float(tree_model.predict(X_one)[0])

        # Final prediction = Prophet trend + Tree residual
        yhat = max(0.0, prophet_yhat + resid_hat)
        qty = int(round(yhat))

        if dish_mae > 0:
            pred_lower = int(max(0, yhat - dish_mae))
            pred_upper = int(yhat + dish_mae)
        else:
            pred_lower = qty
            pred_upper = qty

        # SHAP explanation with grouped features (using pre-instantiated explainer)
        expl = None
        if shap_explainer is not None:
            try:
                sv = shap_explainer.shap_values(X_one)[0]
                base_val = float(shap_explainer.expected_value)

                group_shap = {}
                for i, feat_name in enumerate(config.hybrid_tree_features):
                    group = feat_to_group.get(feat_name, "Other")
                    group_shap[group] = group_shap.get(group, 0.0) + float(sv[i])

                expl = {
                    "ProphetTrend": round(prophet_yhat, 2),
                    "Seasonality": round(group_shap.get("Seasonality", 0.0), 2),
                    "Holiday": round(group_shap.get("Holiday", 0.0), 2),
                    "Weather": round(group_shap.get("Weather", 0.0), 2),
                    "Lags/Trend": round(group_shap.get("Lags/Trend", 0.0), 2),
                    "ResidualBase": round(base_val, 2),
                }
            except Exception:
                pass  # expl remains None, will be set to default below

        if expl is None:
            expl = {
                "ProphetTrend": round(prophet_yhat, 2),
                "Seasonality": 0.0,
                "Holiday": 0.0,
                "Weather": 0.0,
                "Lags/Trend": 0.0,
                "ResidualBase": 0.0,
            }

        results.append({
            "date": dt.strftime('%Y-%m-%d'),
            "qty": qty,
            "lower": pred_lower,
            "upper": pred_upper,
            "explanation": expl
        })

        # Append prediction to history for next iteration
        sales_history.append(yhat)

    return results


def get_prediction(dish: str, date_str: str, address: str, model: str = 'auto', config: PipelineConfig = CFG):
    """
    Multi-day prediction API using hybrid Prophet + Tree model.

    Returns a list of dicts (one per forecast day, up to config.forecast_horizon days).
    Each dict contains: Dish, Date, Model Used, Prediction, Prediction_Lower, Prediction_Upper, Explanation
    """
    dt = pd.to_datetime(date_str)

    # Resolve location from address (cached)
    lat, lon, country = _get_location_cached(address)
    if lat is None:
        country, lat, lon = 'CN', 31.23, 121.47
    if not country or country not in holidays.list_supported_countries():
        country = 'CN'

    safe_name = safe_filename(dish)
    dish_mae = 0.0

    # Registry lookup
    try:
        registry = _load_cached(f'{config.model_dir}/champion_registry.pkl')
        dish_info = registry[dish]
        if model == 'auto':
            model = dish_info['model']
        dish_mae = dish_info.get('all_mae', {}).get(model, 0.0) if dish_info.get('all_mae') else 0.0
    except Exception:
        if model == 'auto':
            model = 'lightgbm'

    # Average-only dishes (fallback for very sparse data)
    if model == 'average':
        try:
            avg_sales = _load_cached(f'{config.model_dir}/average_{safe_name}.pkl')
        except Exception:
            avg_sales = 0
        results = []
        for day_offset in range(config.forecast_horizon):
            d = dt + pd.Timedelta(days=day_offset)
            results.append({
                "Dish": dish, "Date": d.strftime('%Y-%m-%d'),
                "Model Used": "AVERAGE",
                "Prediction": avg_sales,
                "Prediction_Lower": avg_sales,
                "Prediction_Upper": avg_sales,
                "Explanation": {"ProphetTrend": float(avg_sales), "Seasonality": 0.0,
                                "Holiday": 0.0, "Weather": 0.0, "Lags/Trend": 0.0, "ResidualBase": 0.0}
            })
        return results

    # Get real weather forecast (cached)
    forecast_weather = _get_forecast_cached(lat, lon)
    if forecast_weather is None:
        return [{"Error": "Cannot generate forecast without valid weather forecast data"}]

    try:
        if model in ('catboost', 'xgboost', 'lightgbm'):
            # Load both Prophet and tree models
            prophet_model, tree_model = _load_hybrid_models(dish, model, config)
            recent = _load_cached(f'{config.model_dir}/recent_sales_{safe_name}.pkl')

            multiday = _predict_hybrid_multiday(
                prophet_model=prophet_model,
                tree_model=tree_model,
                forecast_weather_df=forecast_weather,
                start_date=dt,
                recent_sales_df=recent,
                config=config,
                country_code=country,
                dish_mae=dish_mae
            )

            results = []
            model_label = f"Prophet+{model.upper()}"
            for entry in multiday:
                results.append({
                    "Dish": dish,
                    "Date": entry['date'],
                    "Model Used": model_label,
                    "Prediction": entry['qty'],
                    "Prediction_Lower": entry['lower'],
                    "Prediction_Upper": entry['upper'],
                    "Explanation": entry['explanation']
                })
            return results

    except FileNotFoundError as e:
        return [{"Error": f"Model file not found for {dish}: {str(e)}"}]
    except Exception as e:
        return [{"Error": f"Model error for {dish}: {str(e)}"}]


# --- VISUALIZATION FUNCTIONS ---
def plot_mae_comparison(results_table: pd.DataFrame) -> None:
    """Plot MAE comparison bar chart for all models."""
    ml_rows = results_table[~results_table['Winner'].str.contains('AVERAGE', na=False)].copy()

    if len(ml_rows) == 0:
        logger.info("No ML-trained dishes to plot.")
        return

    fig, ax = plt.subplots(figsize=(16, 6))

    dishes = ml_rows['Dish']
    x = np.arange(len(dishes))
    width = 0.25

    ax.bar(x - width, ml_rows['XGBoost MAE'].astype(float), width,
           label='XGBoost', color='#55A868')
    ax.bar(x, ml_rows['CatBoost MAE'].astype(float), width,
           label='CatBoost', color='#DD8452')
    ax.bar(x + width, ml_rows['LightGBM MAE'].astype(float), width,
           label='LightGBM', color='#4C72B0')

    # Mark winners with stars
    for i, (_, row) in enumerate(ml_rows.iterrows()):
        x_mae = float(row['XGBoost MAE'])
        c_mae = float(row['CatBoost MAE'])
        l_mae = float(row['LightGBM MAE'])
        winner_mae = min(x_mae, c_mae, l_mae)
        if 'XGBOOST' in row['Winner']:
            offset = -width
        elif 'CATBOOST' in row['Winner']:
            offset = 0
        else:
            offset = width
        ax.plot(x[i] + offset, winner_mae, marker='*', color='gold', markersize=14, zorder=5)

    ax.set_xlabel('Dish')
    ax.set_ylabel('MAE (plates)')
    ax.set_title('Prophet + Tree Hybrid Model MAE Comparison by Dish (lower is better)')
    ax.set_xticks(x)
    ax.set_xticklabels(dishes, rotation=45, ha='right', fontsize=8)
    ax.legend()
    ax.yaxis.set_minor_locator(ticker.AutoMinorLocator())
    plt.tight_layout()
    plt.show()


def plot_forecasts(all_forecasts: dict, forecast_horizon: int) -> None:
    """Plot multi-day forecasts for all dishes."""
    n_dishes = len(all_forecasts)
    if n_dishes == 0:
        logger.info("No forecasts to plot.")
        return

    ncols = 4
    nrows = int(np.ceil(n_dishes / ncols))
    fig, axes = plt.subplots(nrows, ncols, figsize=(18, 4 * nrows), squeeze=False)

    colors_map = {
        'Prophet+LIGHTGBM': '#4C72B0',
        'Prophet+CATBOOST': '#DD8452',
        'Prophet+XGBOOST': '#55A868',
        'AVERAGE': '#999999'
    }

    for idx, (dish_name, preds) in enumerate(all_forecasts.items()):
        ax = axes[idx // ncols][idx % ncols]
        dates = [pd.to_datetime(p['Date']) for p in preds]
        qtys = [p['Prediction'] for p in preds]
        lowers = [p['Prediction_Lower'] for p in preds]
        uppers = [p['Prediction_Upper'] for p in preds]
        model_used = preds[0]['Model Used']
        color = colors_map.get(model_used, '#333333')

        ax.plot(dates, qtys, marker='o', markersize=3, color=color, linewidth=1.5,
                label=model_used)
        ax.fill_between(dates, lowers, uppers, alpha=0.2, color=color)

        ax.set_title(f"{dish_name}\n({model_used})", fontsize=8, fontweight='bold')
        ax.tick_params(axis='x', rotation=30, labelsize=6)
        ax.tick_params(axis='y', labelsize=7)
        ax.legend(fontsize=7)

    for idx in range(n_dishes, nrows * ncols):
        axes[idx // ncols][idx % ncols].set_visible(False)

    fig.suptitle(f'{forecast_horizon}-Day Rolling Forecast per Dish (Prophet + Tree Hybrid)',
                 fontsize=13, y=1.01)
    plt.tight_layout()
    plt.show()


# ## Parallel Execution & Results
# Run all dishes in parallel using `ProcessPoolExecutor`. Each dish is independently
# processed across CPU cores using the hybrid Prophet + Tree methodology.


# --- CELERY TASK: TRAINING ORCHESTRATOR ---
# TODO: Uncomment the decorator below once celery_app is defined in app/celery_app.py
# from app.celery_app import celery_app
# @celery_app.task(name="train_models")
def train_models(
    address: str = "Shanghai, China",
    retrain_all: bool = True,
    sales_data_source: Optional[str] = None,
) -> Dict[str, Any]:
    """
    Execute the full training pipeline for all dishes.

    This function encapsulates the complete training workflow and is designed
    to be called as a Celery task for asynchronous execution.

    Parameters
    ----------
    address : str
        Restaurant location address for weather/holiday context.
    retrain_all : bool
        If True, retrain all dishes. If False, skip dishes that already
        have an entry in the champion registry.
    sales_data_source : Optional[str]
        Path to a CSV file for sales data. If None, uses the default
        behavior (database first, then 'food_sales_eng.csv' fallback).

    Returns
    -------
    dict
        Summary containing 'champion_registry', 'results_table' (list of row dicts),
        'trained_dishes' count, 'skipped_dishes' count, and 'errors' list.
    """
    config = PipelineConfig()
    os.makedirs(config.model_dir, exist_ok=True)

    # 1. Load sales data
    if sales_data_source is not None:
        logger.info("Loading sales data from provided source: %s", sales_data_source)
        raw_df = pd.read_csv(sales_data_source)
        raw_df['date'] = pd.to_datetime(raw_df['date'], format='%m/%d/%Y')
        raw_df['date'] = raw_df['date'].dt.normalize()
        raw_df = raw_df.groupby(['date', 'dish']).agg(sales=('sales', 'sum')).reset_index()
        raw_df = raw_df.sort_values('date')
    else:
        raw_df = fetch_training_data()

    # 2. Enrich with location context (holidays + weather)
    enriched_df, country, lat, lon = add_local_context(raw_df, address)

    # 3. Determine which dishes to train
    unique_dishes = enriched_df['dish'].unique()
    dishes_to_train = list(unique_dishes)

    existing_registry: Dict[str, Any] = {}
    registry_path = f'{config.model_dir}/champion_registry.pkl'
    if not retrain_all:
        try:
            existing_registry = joblib.load(registry_path)
            dishes_to_train = [d for d in unique_dishes if d not in existing_registry]
            logger.info(
                "retrain_all=False: skipping %d dishes with existing models, training %d new dishes.",
                len(unique_dishes) - len(dishes_to_train), len(dishes_to_train)
            )
        except FileNotFoundError:
            logger.info("No existing champion registry found. Training all %d dishes.", len(unique_dishes))

    if len(dishes_to_train) == 0:
        logger.info("No dishes to train. All models are up to date.")
        return {
            'champion_registry': existing_registry,
            'results_table': [],
            'trained_dishes': 0,
            'skipped_dishes': len(unique_dishes),
            'errors': [],
        }

    logger.info("=" * 95)
    logger.info(
        "STARTING HYBRID PROPHET + TREE TRAINING FOR %d DISHES (%d workers, %d Optuna trials each)",
        len(dishes_to_train), config.max_workers, config.n_optuna_trials
    )
    logger.info("=" * 95)

    # 4. Run the full training pipeline in parallel for each dish
    results: List[Dict[str, Any]] = []
    errors: List[Dict[str, str]] = []

    with ProcessPoolExecutor(max_workers=config.max_workers) as executor:
        futures = {
            executor.submit(process_dish, dish, enriched_df, country, config): dish
            for dish in dishes_to_train
        }

        if tqdm is not None:
            pbar = tqdm(as_completed(futures), total=len(dishes_to_train), desc="Training Dishes")
        else:
            pbar = as_completed(futures)

        for future in pbar:
            dish_name = futures[future]
            try:
                result = future.result()
                results.append(result)
                mae = result['mae']
                msg = (f"  {dish_name:<35} | X={mae['xgboost']:<7} C={mae['catboost']:<7} "
                       f"L={mae['lightgbm']:<7} -> Prophet+{result['champion'].upper()}")
                if tqdm is not None:
                    tqdm.write(msg)
                else:
                    logger.info(msg)
            except Exception as e:
                msg = f"  {dish_name:<35} | FAILED: {e}"
                if tqdm is not None:
                    tqdm.write(msg)
                else:
                    logger.error(msg)
                logger.error(traceback.format_exc())
                errors.append({'dish': dish_name, 'error': str(e)})

    # 5. Aggregate results and build champion registry
    champion_map = dict(existing_registry)  # Preserve existing entries if retrain_all=False

    results_rows: List[Dict[str, Any]] = []
    for r in results:
        dish = r['dish']
        champion_map[dish] = {
            'model': r['champion'],
            'mae': r.get('champion_mae', 0.0),
            'all_mae': r['mae'],
            'best_params': r['best_params'],
            'model_type': r.get('model_type', 'hybrid'),
        }
        results_rows.append({
            'Dish': dish,
            'XGBoost MAE': r['mae']['xgboost'],
            'CatBoost MAE': r['mae']['catboost'],
            'LightGBM MAE': r['mae']['lightgbm'],
            'Winner': f"Prophet+{r['champion'].upper()}"
        })

    # Save champion registry
    joblib.dump(champion_map, registry_path)

    clear_model_cache()

    logger.info("=" * 60)
    logger.info("MODEL LEADERBOARD (Lower MAE is Better)")
    logger.info("Model Type: Prophet + Tree Residual Stacking")
    logger.info("=" * 60)

    if results_rows:
        results_table = pd.DataFrame(results_rows)
        logger.info("\n%s", results_table.to_string(index=False))
    else:
        logger.warning("No results to display. Check for errors above.")

    logger.info("Training complete. %d dishes trained, %d errors.", len(results), len(errors))

    return {
        'champion_registry': champion_map,
        'results_table': results_rows,
        'trained_dishes': len(results),
        'skipped_dishes': len(unique_dishes) - len(dishes_to_train),
        'errors': errors,
    }
