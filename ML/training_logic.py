"""
Core ML pipeline logic for the SmartSus Chef project.

This module contains all the data processing, model evaluation, and training functions.
It is designed to be imported by an orchestrator (e.g., a Jupyter Notebook) and
is safe to be used with `concurrent.futures.ProcessPoolExecutor` because it has
no global side-effects on import. The main worker function is `process_dish`.
"""

import pandas as pd
import numpy as np
import holidays
import pickle
import os
import tempfile
import shutil
import logging
import optuna
import warnings
from dataclasses import dataclass, field
from typing import List, Dict
from sqlalchemy import create_engine
import lightgbm as lgb
from catboost import CatBoostRegressor
from xgboost import XGBRegressor
from sklearn.metrics import mean_absolute_error
from geopy.geocoders import Nominatim
import openmeteo_requests
from retry_requests import retry

warnings.filterwarnings('ignore')

# Suppress verbose output
optuna.logging.set_verbosity(optuna.logging.WARNING)


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
    tree_features: List[str] = field(default_factory=lambda: [
        'day_of_week', 'month', 'is_public_holiday',
        'temperature_2m_max', 'temperature_2m_min',
        'relative_humidity_2m_mean', 'precipitation_sum',
        'lag_1', 'lag_7', 'lag_14', 'lag_21', 'lag_28',
        'rolling_mean_7', 'rolling_mean_14',
        'rolling_std_7', 'rolling_std_14',
        'trend_ratio', 'expanding_mean',
        'lag_same_weekday_avg', 'lag_same_weekday_std'
    ])
    tree_cat_features: List[str] = field(default_factory=lambda: [
        'day_of_week', 'month', 'is_public_holiday'
    ])
    feature_groups: Dict[str, List[str]] = field(default_factory=lambda: {
        "Seasonality": ["day_of_week", "month"],
        "Holiday": ["is_public_holiday"],
        "Weather": ["temperature_2m_max", "temperature_2m_min",
                    "relative_humidity_2m_mean", "precipitation_sum"],
        "Lags/Trend": [
            "lag_1", "lag_7", "lag_14", "lag_21", "lag_28",
            "rolling_mean_7", "rolling_mean_14",
            "rolling_std_7", "rolling_std_14",
            "trend_ratio", "expanding_mean",
            "lag_same_weekday_avg", "lag_same_weekday_std"
        ]
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
            print(f"WARNING: Could not geocode address: '{address}'")
            return None, None, None
        lat = location.latitude
        lon = location.longitude
        country_code = location.raw.get('address', {}).get('country_code', '').upper()
        print(f"Geocoded '{address}' -> Lat: {lat:.4f}, Lon: {lon:.4f}, Country: {country_code}")
        return lat, lon, country_code
    except Exception as e:
        print(f"WARNING: Geocoding failed for '{address}': {e}")
        return None, None, None


def get_historical_weather(latitude, longitude, start_date, end_date):
    """
    Fetch historical daily weather data from the Open-Meteo Archive API.
    Returns a DataFrame with columns: date, temperature_2m_max, temperature_2m_min,
    relative_humidity_2m_mean, precipitation_sum.
    """
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

        print(f"Fetched {len(weather_df)} days of historical weather data.")
        return weather_df
    except Exception as e:
        print(f"WARNING: Failed to fetch historical weather: {e}")
        return None


def fetch_weather_from_db(start_date, end_date):
    """
    Attempt to fetch historical weather data from the local MySQL Weather table.
    Returns a DataFrame with columns: date + WEATHER_COLS, or None if unavailable.
    """
    DB_URL = "mysql+pymysql://root:password123@localhost:3306/SmartSusChef"
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
        print(f"Fetched {len(df)} days of weather data from database.")
        return df
    except Exception as e:
        print(f"Weather DB lookup failed: {e}")
        return None


def add_local_context(df, address):
    """
    Enriches the sales data with local context features (Holidays + Weather).
    Uses geocoding to detect location and Open-Meteo for real weather data.
    Raises ValueError if historical weather data cannot be fetched.
    """
    lat, lon, country_code = get_location_details(address)

    # Fallback if geocoding fails
    if lat is None:
        print("WARNING: Geocoding failed. Falling back to default location (Shanghai).")
        country_code = 'CN'
        lat, lon = 31.23, 121.47

    # Ensure valid country code for holidays
    if not country_code or country_code not in holidays.list_supported_countries():
        print(f"WARNING: Country '{country_code}' not supported for holidays. Defaulting to 'CN'.")
        country_code = 'CN'

    print(f"Location: {country_code} (Lat: {lat:.4f}, Lon: {lon:.4f})")

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
        raise ValueError(
            "Training cannot proceed: failed to fetch historical weather data from both "
            "the database and Open-Meteo API. Please check your connections and try again."
        )

    return df, country_code, lat, lon


# ---------------------------------------------------------------------------
# Data Ingestion & Sanitation
# ---------------------------------------------------------------------------
def fetch_training_data():
    """
    Tries to connect to MySQL. If it fails, falls back to 'food_sales_eng.csv'.
    """
    DB_URL = "mysql+pymysql://root:password123@localhost:3306/SmartSusChef"

    try:
        engine = create_engine(DB_URL)
        query = """
        SELECT s.Date as date, r.Name as dish, s.QuantitySold as sales
        FROM Sales s JOIN Recipes r ON s.RecipeId = r.Id
        ORDER BY s.Date ASC
        """
        df = pd.read_sql(query, engine)
        df['date'] = pd.to_datetime(df['date'])
        print(f"Loaded {len(df)} rows from MySQL.")
    except Exception:
        print("MySQL Connection failed. Falling back to CSV.")
        df = pd.read_csv('food_sales_eng.csv')
        df['date'] = pd.to_datetime(df['date'], format='%m/%d/%Y')

    # Normalize the date to remove time component, ensuring one record per day
    df['date'] = df['date'].dt.normalize()

    # Aggregate sales to ensure each dish has exactly one entry per day.
    # This is critical for preventing errors in time-series models.
    df = df.groupby(['date', 'dish']).agg(sales=('sales', 'sum')).reset_index()

    return df.sort_values('date')


def sanitize_sparse_data(df, country_code):
    """The 'Anti-Lazy Employee' Logic — fill missing days with interpolation."""
    # Create a full date range and reindex the DataFrame to identify missing dates.
    all_dates = pd.date_range(start=df['date'].min(), end=df['date'].max(), freq='D')
    df = df.set_index('date').reindex(all_dates)

    day_counts = df.groupby(df.index.dayofweek)['sales'].count()
    expected_count = day_counts.mean()
    weak_days = day_counts[day_counts < expected_count * 0.5].index
    if len(weak_days) > 0:
        df.loc[df.index.dayofweek.isin(weak_days), 'sales'] = np.nan

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


def add_lag_features(df):
    """Add comprehensive lag, rolling, and trend features for tree-based models."""
    df = df.sort_values('date').copy()

    # Basic lags
    for lag in [1, 7, 14, 21, 28]:
        df[f'lag_{lag}'] = df['sales'].shift(lag)

    # Rolling means (shifted by 1 to prevent leakage)
    shifted = df['sales'].shift(1)
    df['rolling_mean_7'] = shifted.rolling(7).mean()
    df['rolling_mean_14'] = shifted.rolling(14).mean()

    # Rolling standard deviations (shifted by 1)
    df['rolling_std_7'] = shifted.rolling(7).std()
    df['rolling_std_14'] = shifted.rolling(14).std()

    # Trend ratio
    df['trend_ratio'] = (df['rolling_mean_7'] / df['rolling_mean_14']).fillna(1.0)

    # Expanding mean (shifted by 1)
    df['expanding_mean'] = shifted.expanding().mean()

    # Same-weekday lag features (shifts 7, 14, 21, 28)
    weekday_shifts = pd.concat(
        [df['sales'].shift(s) for s in [7, 14, 21, 28]], axis=1
    )
    df['lag_same_weekday_avg'] = weekday_shifts.mean(axis=1)
    df['lag_same_weekday_std'] = weekday_shifts.std(axis=1)

    df = df.dropna(subset=CFG.tree_features)
    return df


# ---------------------------------------------------------------------------
# Optuna-Based Backtesting
# ---------------------------------------------------------------------------
def _generate_cv_folds(df, config):
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


def _optimize_catboost(df, config):
    """Optuna-based hyperparameter optimization for CatBoost with early stopping."""
    features = config.tree_features
    cat_features = config.tree_cat_features
    best_last_fold_preds = None

    def objective(trial):
        nonlocal best_last_fold_preds
        depth = trial.suggest_int('depth', 3, 10)
        learning_rate = trial.suggest_float('learning_rate', 0.01, 0.3, log=True)
        l2_leaf_reg = trial.suggest_float('l2_leaf_reg', 1.0, 10.0)

        fold_maes = []
        last_preds = None

        for train, test in _generate_cv_folds(df, config):
            model = CatBoostRegressor(
                iterations=1000, depth=depth, learning_rate=learning_rate,
                l2_leaf_reg=l2_leaf_reg, cat_features=cat_features,
                random_seed=config.random_seed, verbose=0
            )
            model.fit(
                train[features], train['sales'],
                eval_set=(test[features], test['sales']),
                early_stopping_rounds=50, verbose=False
            )
            preds = np.maximum(model.predict(test[features]), 0)
            fold_maes.append(mean_absolute_error(test['sales'], preds))
            last_preds = {
                'dates': test['date'].values,
                'actual': test['sales'].values,
                'predicted': preds
            }

        if not fold_maes:
            return float('inf')

        avg_mae = np.mean(fold_maes)
        if best_last_fold_preds is None or avg_mae < trial.study.best_value:
            best_last_fold_preds = last_preds
        return avg_mae

    study = optuna.create_study(direction='minimize',
                                sampler=optuna.samplers.TPESampler(seed=config.random_seed))
    study.optimize(objective, n_trials=config.n_optuna_trials)

    return round(study.best_value, 2), study.best_params, best_last_fold_preds


def _optimize_xgboost(df, config):
    """Optuna-based hyperparameter optimization for XGBoost with early stopping."""
    features = config.tree_features
    best_last_fold_preds = None

    def objective(trial):
        nonlocal best_last_fold_preds
        max_depth = trial.suggest_int('max_depth', 3, 10)
        learning_rate = trial.suggest_float('learning_rate', 0.01, 0.3, log=True)
        reg_alpha = trial.suggest_float('reg_alpha', 0.0, 10.0)
        reg_lambda = trial.suggest_float('reg_lambda', 0.0, 10.0)

        fold_maes = []
        last_preds = None

        for train, test in _generate_cv_folds(df, config):
            model = XGBRegressor(
                n_estimators=1000, max_depth=max_depth, learning_rate=learning_rate,
                reg_alpha=reg_alpha, reg_lambda=reg_lambda,
                random_state=config.random_seed, n_jobs=-1,
                early_stopping_rounds=50, eval_metric='mae'
            )
            model.fit(
                train[features], train['sales'],
                eval_set=[(test[features], test['sales'])],
                verbose=False
            )
            preds = np.maximum(model.predict(test[features]), 0)
            fold_maes.append(mean_absolute_error(test['sales'], preds))
            last_preds = {
                'dates': test['date'].values,
                'actual': test['sales'].values,
                'predicted': preds
            }

        if not fold_maes:
            return float('inf')

        avg_mae = np.mean(fold_maes)
        if best_last_fold_preds is None or avg_mae < trial.study.best_value:
            best_last_fold_preds = last_preds
        return avg_mae

    study = optuna.create_study(direction='minimize',
                                sampler=optuna.samplers.TPESampler(seed=config.random_seed))
    study.optimize(objective, n_trials=config.n_optuna_trials)

    return round(study.best_value, 2), study.best_params, best_last_fold_preds


def _optimize_lightgbm(df, config):
    """Optuna-based hyperparameter optimization for LightGBM with early stopping."""
    features = config.tree_features
    best_last_fold_preds = None

    def objective(trial):
        nonlocal best_last_fold_preds
        num_leaves = trial.suggest_int('num_leaves', 20, 150)
        learning_rate = trial.suggest_float('learning_rate', 0.01, 0.3, log=True)
        reg_alpha = trial.suggest_float('reg_alpha', 0.0, 10.0)
        reg_lambda = trial.suggest_float('reg_lambda', 0.0, 10.0)

        fold_maes = []
        last_preds = None

        for train, test in _generate_cv_folds(df, config):
            model = lgb.LGBMRegressor(
                n_estimators=1000, num_leaves=num_leaves, learning_rate=learning_rate,
                reg_alpha=reg_alpha, reg_lambda=reg_lambda,
                random_state=config.random_seed, n_jobs=-1, verbose=-1
            )
            model.fit(
                train[features], train['sales'],
                eval_set=[(test[features], test['sales'])],
                callbacks=[lgb.early_stopping(50, verbose=False)]
            )
            preds = np.maximum(model.predict(test[features]), 0)
            fold_maes.append(mean_absolute_error(test['sales'], preds))
            last_preds = {
                'dates': test['date'].values,
                'actual': test['sales'].values,
                'predicted': preds
            }

        if not fold_maes:
            return float('inf')

        avg_mae = np.mean(fold_maes)
        if best_last_fold_preds is None or avg_mae < trial.study.best_value:
            best_last_fold_preds = last_preds
        return avg_mae

    study = optuna.create_study(direction='minimize',
                                sampler=optuna.samplers.TPESampler(seed=config.random_seed))
    study.optimize(objective, n_trials=config.n_optuna_trials)

    return round(study.best_value, 2), study.best_params, best_last_fold_preds


def evaluate_model(df, model_type, config=CFG):
    """
    Dispatcher: runs Optuna-based optimization for the given model type.
    Returns (best_mae, best_params, last_fold_preds).
    """
    if model_type == 'catboost':
        return _optimize_catboost(df, config)
    elif model_type == 'xgboost':
        return _optimize_xgboost(df, config)
    elif model_type == 'lightgbm':
        return _optimize_lightgbm(df, config)
    else:
        raise ValueError(f"Unknown model type: {model_type}")


# ---------------------------------------------------------------------------
# Per-Dish Processing
# ---------------------------------------------------------------------------
def process_dish(dish_name, shared_df, country_code, config):
    """
    Process a single dish: evaluate all models, determine champion, retrain on full data.
    This function is standalone (no closures) so it can be pickled by ProcessPoolExecutor.
    """
    safe_name = safe_filename(dish_name)
    os.makedirs(config.model_dir, exist_ok=True)

    # Isolate and sanitize dish data
    dish_data = shared_df[shared_df['dish'] == dish_name].copy()
    dish_data = sanitize_sparse_data(dish_data, country_code)

    # Full ML pipeline
    dish_data_with_lags = add_lag_features(dish_data.copy())

    # Evaluate all 3 models
    l_mae, l_params, l_preds = evaluate_model(dish_data_with_lags, 'lightgbm', config)
    c_mae, c_params, c_preds = evaluate_model(dish_data_with_lags, 'catboost', config)
    x_mae, x_params, x_preds = evaluate_model(dish_data_with_lags, 'xgboost', config)

    # Determine champion
    scores = {'lightgbm': l_mae, 'catboost': c_mae, 'xgboost': x_mae}
    champion = min(scores, key=scores.get)

    # Production training on 100% data with best params

    # LightGBM
    lgb_prod_params = {k: v for k, v in l_params.items()}
    ml = lgb.LGBMRegressor(
        n_estimators=1000, random_state=config.random_seed, n_jobs=-1,
        verbose=-1, **lgb_prod_params
    )
    ml.fit(dish_data_with_lags[config.tree_features], dish_data_with_lags['sales'])
    with open(f'{config.model_dir}/lightgbm_{safe_name}.pkl', 'wb') as f:
        pickle.dump(ml, f)

    # CatBoost with best Optuna params
    cb_params = {k: v for k, v in c_params.items()}
    mc = CatBoostRegressor(
        iterations=1000, cat_features=config.tree_cat_features,
        random_seed=config.random_seed, verbose=False, **cb_params
    )
    mc.fit(dish_data_with_lags[config.tree_features], dish_data_with_lags['sales'])
    with open(f'{config.model_dir}/catboost_{safe_name}.pkl', 'wb') as f:
        pickle.dump(mc, f)

    # XGBoost with best Optuna params
    xgb_params = {k: v for k, v in x_params.items()}
    mx = XGBRegressor(
        n_estimators=1000, random_state=config.random_seed, n_jobs=-1, verbosity=0, **xgb_params
    )
    mx.fit(dish_data_with_lags[config.tree_features], dish_data_with_lags['sales'])
    with open(f'{config.model_dir}/xgboost_{safe_name}.pkl', 'wb') as f:
        pickle.dump(mx, f)

    # Save recent sales for lag computation at prediction time
    recent_sales = dish_data[['date', 'sales']].tail(28).copy()
    with open(f'{config.model_dir}/recent_sales_{safe_name}.pkl', 'wb') as f:
        pickle.dump(recent_sales, f)

    preds_map = {'lightgbm': l_preds, 'catboost': c_preds, 'xgboost': x_preds}

    return {
        'dish': dish_name,
        'champion': champion,
        'mae': scores,
        'best_params': {
            'lightgbm': l_params,
            'catboost': c_params,
            'xgboost': x_params
        },
        'backtest_preds': preds_map,
        'champion_mae': scores[champion]
    }
