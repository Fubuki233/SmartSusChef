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
from prophet import Prophet
from catboost import CatBoostRegressor
from xgboost import XGBRegressor
from sklearn.metrics import mean_absolute_error

warnings.filterwarnings('ignore')

# Suppress verbose output
optuna.logging.set_verbosity(optuna.logging.WARNING)
logging.getLogger('prophet').setLevel(logging.WARNING)
logging.getLogger('cmdstanpy').setLevel(logging.WARNING)


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
        'rain_lunch_vol', 'temperature',
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
        "Weather": ["rain_lunch_vol", "temperature"],
        "Lags/Trend": [
            "lag_1", "lag_7", "lag_14", "lag_21", "lag_28",
            "rolling_mean_7", "rolling_mean_14",
            "rolling_std_7", "rolling_std_14",
            "trend_ratio", "expanding_mean",
            "lag_same_weekday_avg", "lag_same_weekday_std"
        ]
    })


CFG = PipelineConfig()


def safe_filename(name):
    """Sanitize dish name for use as a filename."""
    return name.replace(' ', '_').replace('-', '_').replace('/', '_')


# ---------------------------------------------------------------------------
# Context Awareness (Location Logic)
# ---------------------------------------------------------------------------
def get_country_code(lat, lon):
    """
    Simple Bounding Box Logic.
    If the coordinates are inside Singapore box, return 'SG'. Else, default to 'CN'.
    """
    if (1.1 <= lat <= 1.5) and (103.5 <= lon <= 104.1):
        return 'SG'
    return 'CN'


def estimate_temperature(date, country_code):
    """
    Simulate temperature for a given date and country.
    Uses date-based seed so each date gets a unique but reproducible value.
    """
    rng = np.random.default_rng(CFG.random_seed + date.toordinal())
    m = date.month
    if country_code == 'SG':
        return rng.uniform(25, 34)
    else:
        if m in [12, 1, 2]:
            return rng.uniform(2, 10)
        elif m in [3, 4, 5]:
            return rng.uniform(12, 25)
        elif m in [6, 7, 8]:
            return rng.uniform(26, 38)
        else:
            return rng.uniform(12, 25)


def add_local_context(df, lat, lon):
    """Enriches the sales data with local context features (Holidays + Weather)."""
    country_code = get_country_code(lat, lon)
    print(f"Detected Location: {country_code} (Lat: {lat}, Lon: {lon})")

    df['day_of_week'] = df['date'].dt.dayofweek
    df['month'] = df['date'].dt.month

    if country_code == 'SG':
        local_holidays = holidays.SG(years=CFG.holiday_years)
    else:
        local_holidays = holidays.CN(years=CFG.holiday_years)

    df['is_public_holiday'] = df['date'].apply(lambda x: 1 if x in local_holidays else 0)

    rng = np.random.default_rng(CFG.random_seed)

    def estimate_rain(row):
        m = row['month']
        if country_code == 'SG':
            return rng.uniform(15, 60) if m in [11, 12, 1] else rng.uniform(5, 30)
        else:
            return rng.uniform(20, 80) if m in [6, 7, 8] else rng.uniform(5, 25)

    df['rain_lunch_vol'] = df.apply(estimate_rain, axis=1)
    df['temperature'] = df['date'].apply(lambda d: estimate_temperature(d, country_code))

    return df, country_code


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
    # This is critical for preventing errors in time-series models like Prophet.
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

    if 'rain_lunch_vol' in df.columns:
        df['rain_lunch_vol'] = df['rain_lunch_vol'].interpolate(method='time').bfill().ffill()
    else:
        df['rain_lunch_vol'] = 0.0

    if 'temperature' in df.columns:
        df['temperature'] = df['temperature'].interpolate(method='time').bfill().ffill()
    else:
        df['temperature'] = 0.0

    if country_code == 'SG':
        local_holidays = holidays.SG(years=CFG.holiday_years)
    else:
        local_holidays = holidays.CN(years=CFG.holiday_years)

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
                random_seed=config.random_seed, verbose=False
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


def _optimize_prophet(df, config, country_code):
    """Optuna-based hyperparameter optimization for Prophet."""
    best_last_fold_preds = None

    def objective(trial):
        nonlocal best_last_fold_preds
        changepoint_prior = trial.suggest_float('changepoint_prior_scale', 0.001, 0.5, log=True)
        seasonality_prior = trial.suggest_float('seasonality_prior_scale', 0.01, 10.0, log=True)

        fold_maes = []
        last_preds = None

        for train, test in _generate_cv_folds(df, config):
            p_train = train[['date', 'sales', 'rain_lunch_vol', 'temperature']].rename(
                columns={'date': 'ds', 'sales': 'y'})
            p_train = p_train.sort_values(by='ds')
            m = Prophet(daily_seasonality=False,
                        changepoint_prior_scale=changepoint_prior,
                        seasonality_prior_scale=seasonality_prior)
            try:
                import holidays
                if country_code and country_code in holidays.list_supported_countries():
                    m.add_country_holidays(country_name=country_code)
                else:
                    print(f"WARNING: Skipping country holidays for invalid or empty country_code: {country_code}")
            except Exception as e:
                print(f"WARNING: Error adding country holidays for {country_code}: {e}")
            m.add_regressor('rain_lunch_vol')
            m.add_regressor('temperature')
            # Fit the Prophet model. This is where Prophet uses cmdstanpy for optimization.
            m.fit(p_train)

            p_test = test[['date', 'rain_lunch_vol', 'temperature']].rename(columns={'date': 'ds'})
            forecast = m.predict(p_test)
            preds = np.maximum(forecast['yhat'].values, 0)
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


def evaluate_model(df, model_type, country_code, config=CFG):
    """
    Dispatcher: runs Optuna-based optimization for the given model type.
    Returns (best_mae, best_params, last_fold_preds).
    """
    if model_type == 'catboost':
        return _optimize_catboost(df, config)
    elif model_type == 'xgboost':
        return _optimize_xgboost(df, config)
    elif model_type == 'prophet':
        return _optimize_prophet(df, config, country_code)
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
    # Create a unique temp directory for this process to avoid cmdstanpy file conflicts
    # when multiple Prophet instances run in parallel.
    stan_tmpdir = tempfile.mkdtemp()
    os.environ['CMDSTAN_TMPDIR'] = stan_tmpdir
    try:
        safe_name = safe_filename(dish_name)
        os.makedirs(config.model_dir, exist_ok=True)

        # Isolate and sanitize dish data
        dish_data = shared_df[shared_df['dish'] == dish_name].copy()
        dish_data = sanitize_sparse_data(dish_data, country_code)

        # Short-data guard: < min_ml_days -> use simple average
        data_span = (dish_data['date'].max() - dish_data['date'].min()).days
        if data_span < config.min_ml_days:
            avg_sales = round(dish_data['sales'].mean())
            with open(f'{config.model_dir}/average_{safe_name}.pkl', 'wb') as f:
                pickle.dump(avg_sales, f)

            recent_sales = dish_data[['date', 'sales']].tail(28).copy()
            with open(f'{config.model_dir}/recent_sales_{safe_name}.pkl', 'wb') as f:
                pickle.dump(recent_sales, f)

            return {
                'dish': dish_name,
                'champion': 'average',
                'mae': {'prophet': None, 'catboost': None, 'xgboost': None},
                'best_params': {},
                'backtest_preds': None,
                'avg_sales': avg_sales
            }

        # Full ML pipeline
        dish_data_with_lags = add_lag_features(dish_data.copy())

        # Evaluate all 3 models
        p_mae, p_params, p_preds = evaluate_model(dish_data, 'prophet', country_code, config)
        c_mae, c_params, c_preds = evaluate_model(dish_data_with_lags, 'catboost', country_code, config)
        x_mae, x_params, x_preds = evaluate_model(dish_data_with_lags, 'xgboost', country_code, config)

        # Determine champion
        scores = {'prophet': p_mae, 'catboost': c_mae, 'xgboost': x_mae}
        champion = min(scores, key=scores.get)

        # Production training on 100% data with best params

        # Prophet
        p_df = dish_data[['date', 'sales', 'rain_lunch_vol', 'temperature']].rename(
            columns={'date': 'ds', 'sales': 'y'})
        mp = Prophet(daily_seasonality=False, **p_params)
        try:
            mp.add_country_holidays(country_name=country_code)
        except Exception:
            pass
        mp.add_regressor('rain_lunch_vol')
        mp.add_regressor('temperature')
        mp.fit(p_df)
        with open(f'{config.model_dir}/prophet_{safe_name}.pkl', 'wb') as f:
            pickle.dump(mp, f)

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
            n_estimators=1000, random_state=config.random_seed, n_jobs=-1, **xgb_params
        )
        mx.fit(dish_data_with_lags[config.tree_features], dish_data_with_lags['sales'])
        with open(f'{config.model_dir}/xgboost_{safe_name}.pkl', 'wb') as f:
            pickle.dump(mx, f)

        # Save recent sales for lag computation at prediction time
        recent_sales = dish_data[['date', 'sales']].tail(28).copy()
        with open(f'{config.model_dir}/recent_sales_{safe_name}.pkl', 'wb') as f:
            pickle.dump(recent_sales, f)

        preds_map = {'prophet': p_preds, 'catboost': c_preds, 'xgboost': x_preds}

        return {
            'dish': dish_name,
            'champion': champion,
            'mae': scores,
            'best_params': {
                'prophet': p_params,
                'catboost': c_params,
                'xgboost': x_params
            },
            'backtest_preds': preds_map,
            'champion_mae': scores[champion]
        }
    finally:
        # Ensure the temporary directory is cleaned up
        shutil.rmtree(stan_tmpdir, ignore_errors=True)
