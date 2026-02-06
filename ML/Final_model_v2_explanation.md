# SmartSus Chef: Predictive Demand Forecasting Engine

## Project Overview

SmartSus Chef is an intelligent demand forecasting system designed for restaurants and food service businesses. It predicts how many portions of each dish will be sold over the next 14 days, enabling better inventory management, reduced food waste, and optimized staffing.

The system uses a **Hybrid Prophet + Tree Residual Stacking** methodology that combines the strengths of time-series forecasting with modern machine learning to deliver accurate, explainable predictions.

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Project Setup](#project-setup)
3. [Architecture Overview](#architecture-overview)
4. [The Hybrid Model Explained](#the-hybrid-model-explained)
5. [Module Breakdown](#module-breakdown)
6. [Data Flow](#data-flow)
7. [Key Features](#key-features)
8. [Output and Visualizations](#output-and-visualizations)
9. [Configuration Options](#configuration-options)

---

## Quick Start

```bash
# 1. Install dependencies
pip install pandas numpy prophet xgboost catboost lightgbm optuna shap \
            joblib holidays geopy openmeteo-requests retry-requests \
            python-dotenv matplotlib tqdm sqlalchemy pymysql

# 2. (Optional) Configure database connection
# Edit .env file with your MySQL credentials, or skip to use CSV fallback

# 3. Run the forecasting engine
python Final_model_v2.py --address "Your City, Country"
```

---

## Project Setup

### Prerequisites

- **Python 3.9+** (recommended: 3.10 or 3.11)
- **pip** package manager

### Installation

1. **Clone or download the project** to your local machine.

2. **Create a virtual environment** (recommended):
   ```bash
   python -m venv venv
   source venv/bin/activate  # On Windows: venv\Scripts\activate
   ```

3. **Install dependencies**:
   ```bash
   pip install pandas numpy prophet xgboost catboost lightgbm optuna shap \
               joblib holidays geopy openmeteo-requests retry-requests \
               python-dotenv matplotlib tqdm sqlalchemy pymysql
   ```

### External Dependencies

| Dependency | Purpose | Required? |
|------------|---------|-----------|
| **MySQL Database** | Primary data source for sales/weather data | Optional (CSV fallback available) |
| **Open-Meteo API** | Historical and forecast weather data | Optional (free, no API key needed) |
| **Nominatim (OpenStreetMap)** | Geocoding addresses to coordinates | Optional (used for location detection) |

### Environment Configuration

The project uses a `.env` file for sensitive configuration:

```bash
# .env file
DATABASE_URL=mysql+pymysql://username:password@localhost:3306/SmartSusChef
```

If `DATABASE_URL` is not set, the system automatically falls back to using `food_sales_eng.csv` for training data.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     Final_model_v2.py                           │
│                      (Orchestrator)                             │
│  - Command-line interface                                       │
│  - Parallel dish processing                                     │
│  - Prediction API                                               │
│  - Visualizations                                               │
└─────────────────────┬───────────────────────────────────────────┘
                      │ imports
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                   training_logic_v2.py                          │
│                    (Core ML Engine)                             │
│  - Data ingestion & sanitation                                  │
│  - Feature engineering                                          │
│  - Prophet + Tree model training                                │
│  - Cross-validation with Optuna                                 │
│  - Model persistence                                            │
└─────────────────────────────────────────────────────────────────┘
                      │ uses
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                    External Services                            │
│  - MySQL Database (sales data)                                  │
│  - Open-Meteo API (weather data)                                │
│  - Nominatim (geocoding)                                        │
│  - holidays library (public holidays)                           │
└─────────────────────────────────────────────────────────────────┘
```

---

## The Hybrid Model Explained

### Why a Hybrid Approach?

Traditional time-series models (like ARIMA or Prophet) excel at capturing **trends and seasonality** but struggle with complex non-linear relationships. Machine learning models (like XGBoost) are great at learning **complex patterns** but don't inherently understand time-series concepts.

Our hybrid approach combines the best of both worlds:

```
Final Prediction = Prophet Trend + Tree Residual Correction
```

### Step 1: Prophet Models the Big Picture

[Prophet](https://facebook.github.io/prophet/) (developed by Meta) handles:

- **Trend**: Long-term increase or decrease in sales
- **Weekly Seasonality**: Day-of-week patterns (e.g., weekends are busier)
- **Holiday Effects**: Public holidays impact demand
- **Weather Regressors**: Temperature, humidity, and precipitation affect dining behavior

Prophet produces a baseline prediction (`prophet_yhat`) that captures these macro-level patterns.

### Step 2: Tree Models Learn the Residuals

After Prophet makes its prediction, there's always some error (the "residual"):

```
Residual = Actual Sales - Prophet Prediction
```

We train tree-based models (XGBoost, CatBoost, LightGBM) to predict these residuals. Trees are excellent at capturing:

- Non-linear interactions between features
- Complex lag patterns (how yesterday's sales affect today)
- Subtle weather-demand relationships

### Step 3: Optuna Finds the Best Hyperparameters

[Optuna](https://optuna.org/) automatically searches for the optimal hyperparameters for each tree model using Bayesian optimization. It runs 30 trials per model type, evaluating each on cross-validation performance.

### Step 4: Champion Selection

After optimization, we compare the three tree models (XGBoost, CatBoost, LightGBM) and select the **champion** based on the lowest Mean Absolute Error (MAE) on cross-validation.

### Why This Works

| Component | Strength | Weakness Addressed |
|-----------|----------|-------------------|
| Prophet | Interpretable trends/seasonality | Can't capture complex interactions |
| Tree Models | Complex pattern learning | No time-series awareness |
| **Combined** | Best of both | Compensates for each other's weaknesses |

---

## Module Breakdown

### `Final_model_v2.py` - The Orchestrator

**Purpose**: Main entry point that coordinates the entire pipeline.

**Key Responsibilities**:
- **Command-line interface**: Accepts `--forecast_date`, `--address`, and `--debug` flags
- **Parallel execution**: Uses `ProcessPoolExecutor` to train models for multiple dishes simultaneously
- **Prediction API**: `get_prediction()` function for generating forecasts
- **Caching**: Caches models, geocoding results, and weather forecasts for efficiency
- **Visualizations**: MAE comparison charts, 14-day forecast tables, and per-dish forecast plots

**Key Functions**:
```python
get_prediction(dish, date_str, address, model='auto', config=CFG)
# Returns 14-day forecast with predictions, confidence intervals, and explanations

get_weather_forecast(latitude, longitude)
# Fetches 14-day weather forecast from Open-Meteo

plot_mae_comparison(results_table)
# Visualizes model performance across dishes

plot_forecasts(all_forecasts, forecast_horizon)
# Plots individual dish forecasts
```

### `training_logic_v2.py` - The ML Engine

**Purpose**: Contains all core machine learning logic.

**Key Responsibilities**:
- **Data ingestion**: Loads from MySQL or CSV fallback
- **Data sanitation**: Fills missing days, interpolates gaps
- **Feature engineering**: Creates lag features, rolling statistics, date features
- **Prophet training**: Fits Prophet models with weather regressors
- **Tree model training**: Trains XGBoost, CatBoost, LightGBM on residuals
- **Cross-validation**: Time-series CV with expanding window
- **Hyperparameter optimization**: Optuna-based search
- **Model persistence**: Saves/loads models using joblib

**Key Functions**:
```python
process_dish(dish_name, shared_df, country_code, config)
# Main worker function - trains hybrid model for one dish

fetch_training_data()
# Loads sales data from database or CSV

add_local_context(df, address, config)
# Enriches data with holidays and weather

_prepare_cv_fold_cache(df_feat, country_code, config)
# Pre-computes Prophet predictions for each CV fold

_optimize_hybrid(model_type, fold_cache, config)
# Runs Optuna optimization for a tree model type
```

### `CLAUDE.md` - Agent Instructions

**Purpose**: Contains instructions for AI assistants (like Claude) working on this codebase. It specifies tasks, implementation plans, and verification steps.

---

## Data Flow

```
┌──────────────────┐
│   Data Sources   │
│  - MySQL DB      │
│  - CSV fallback  │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  fetch_training  │  Loads raw sales data
│      _data()     │  (date, dish, sales)
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  add_local       │  Enriches with:
│    _context()    │  - Holidays (via holidays library)
└────────┬─────────┘  - Weather (via Open-Meteo API)
         │
         ▼
┌──────────────────┐
│  process_dish()  │  For each dish:
│                  │  1. Add features (lags, rolling stats)
└────────┬─────────┘  2. Generate CV folds
         │            3. Train Prophet per fold
         │            4. Optimize tree models
         │            5. Select champion
         │            6. Retrain on full data
         │            7. Save models
         ▼
┌──────────────────┐
│  models/ folder  │  Saved artifacts:
│                  │  - prophet_{dish}.pkl
│                  │  - {champion}_{dish}.pkl
│                  │  - recent_sales_{dish}.pkl
│                  │  - champion_registry.pkl
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ get_prediction() │  At inference time:
│                  │  1. Load Prophet + Tree models
│                  │  2. Get weather forecast
│                  │  3. Generate 14-day predictions
│                  │  4. Compute SHAP explanations
└──────────────────┘
```

---

## Key Features

### 1. Dynamic Context Awareness

The system automatically adapts to any location:

- **Geocoding**: Converts addresses to coordinates using Nominatim
- **Local Holidays**: Fetches country-specific public holidays
- **Real Weather Data**: Integrates actual historical and forecast weather

```python
# Example: Just provide an address
python Final_model_v2.py --address "Paris, France"
# System automatically detects FR holidays and Paris weather
```

### 2. Data Leakage Prevention

A critical concern in time-series ML is **data leakage** - accidentally using future information during training. We prevent this by:

- **Per-fold sanitation**: Data interpolation happens separately for train and test sets in each CV fold
- **Proper time splits**: Training data always precedes test data chronologically
- **Lag feature computation**: Only uses past values, never future

### 3. Graceful Error Handling

The system continues operating even when external services fail:

```python
# Weather API fails? Use zeros and warn
if weather_df is None:
    logger.warning("Proceeding with weather features set to 0.")
    for col in WEATHER_COLS:
        df[col] = 0.0

# Database unavailable? Fall back to CSV
if DB_URL is None:
    df = pd.read_csv('food_sales_eng.csv')
```

### 4. Production-Ready Security

- **No hardcoded credentials**: Database URL loaded from environment variable
- **Safe serialization**: Uses `joblib` instead of `pickle` (reduces arbitrary code execution risk)
- **Proper logging**: Structured logging with levels (INFO, WARNING, ERROR)

### 5. Explainability with SHAP

Every prediction includes feature contribution explanations:

```python
{
    "ProphetTrend": 45.2,      # Prophet's baseline prediction
    "Seasonality": 3.1,        # Day-of-week, month effects
    "Holiday": 0.0,            # Holiday impact
    "Weather": -2.5,           # Temperature, rain effects
    "Lags/Trend": 5.8,         # Recent sales momentum
    "ResidualBase": 2.1        # Tree model baseline
}
```

### 6. Parallel Processing

Dishes are trained in parallel using `ProcessPoolExecutor`:

```python
with ProcessPoolExecutor(max_workers=config.max_workers) as executor:
    futures = {
        executor.submit(process_dish, dish, df, country, config): dish
        for dish in dishes
    }
```

This dramatically reduces training time on multi-core machines.

---

## Output and Visualizations

### 1. Model Leaderboard

Shows MAE (Mean Absolute Error) for each dish across all three tree models:

| Dish | XGBoost MAE | CatBoost MAE | LightGBM MAE | Winner |
|------|-------------|--------------|--------------|--------|
| Kung Pao Chicken | 3.24 | 3.18 | 3.45 | Prophet+CATBOOST |
| Fried Rice | 5.67 | 5.89 | 5.42 | Prophet+LIGHTGBM |

### 2. 14-Day Forecast Table

Displays predicted quantities for each dish over the forecast horizon:

| Dish | 2026-02-07 | 2026-02-08 | ... | 2026-02-20 |
|------|------------|------------|-----|------------|
| Kung Pao Chicken | 45 | 42 | ... | 48 |
| Fried Rice | 120 | 115 | ... | 125 |

This table is shown both in the terminal and as a graphical matplotlib window.

### 3. Per-Dish Forecast Plots

Line charts showing the 14-day forecast for each dish with confidence intervals.

---

## Configuration Options

### PipelineConfig Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `n_cv_folds` | 3 | Number of cross-validation folds |
| `test_window_days` | 30 | Days in each test window |
| `min_train_days` | 60 | Minimum training data required |
| `forecast_horizon` | 14 | Days to forecast ahead |
| `n_optuna_trials` | 30 | Hyperparameter search iterations |
| `max_workers` | 4 | Parallel processing workers |
| `model_dir` | "models" | Where to save trained models |
| `default_fallback_address` | "Shanghai, China" | Fallback location if geocoding fails |

### Command-Line Arguments

```bash
python Final_model_v2.py [OPTIONS]

Options:
  --forecast_date DATE   Start date for forecasts (YYYY-MM-DD), default: tomorrow
  --address ADDRESS      Restaurant location for weather/holidays, default: "Shanghai, China"
  --debug                Run sequentially for debugging (no parallel processing)
```

---

## Troubleshooting

### Common Issues

1. **"DATABASE_URL not set"**: This is normal if you don't have MySQL. The system uses CSV fallback.

2. **"Could not geocode address"**: Check internet connection or try a more specific address.

3. **"Failed to fetch weather"**: Open-Meteo API may be temporarily unavailable. System continues with weather features set to 0.

4. **Import errors**: Ensure all dependencies are installed: `pip install -r requirements.txt`

---

## Summary

SmartSus Chef combines the interpretability of Prophet with the power of gradient boosting trees to deliver accurate, explainable demand forecasts. Its production-ready architecture handles real-world challenges like missing data, API failures, and security concerns while remaining easy to configure and extend.

For questions or issues, refer to the code comments or the `CLAUDE.md` instruction file.
