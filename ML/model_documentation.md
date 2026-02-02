# SmartSus Chef: Predictive Engine Documentation

## Overview

SmartSus Chef is a food demand prediction system for F&B operators. Given a restaurant's historical sales data and location, it automatically selects the best forecasting model per dish and serves daily quantity predictions with confidence intervals and explainability breakdowns.

The system supports two markets — **Singapore (SG)** and **China (CN)** — with location-specific holidays, weather simulation, and seasonal patterns.

---

## Architecture

The engine follows a **Champion-Challenger** pattern:

1. Three models compete per dish: **Prophet**, **CatBoost**, **XGBoost**
2. Each is evaluated via time-series cross-validation
3. The lowest-MAE model becomes the **champion** for that dish
4. All three are saved — the champion is auto-selected at prediction time, but any model can be manually specified

---

## Pipeline

```
CSV/MySQL --> Context Enrichment --> Sanitization --> Lag Features
    --> Cross-Validation (3 models x 3 folds x depth grid)
    --> Production Training (full data) --> Save Models + Registry
    --> Prediction API (load model, rebuild features, predict + explain)
```

---

## Cell-by-Cell Breakdown

### cell-1: Imports

Loads all dependencies:

| Library | Purpose |
|---------|---------|
| `pandas` / `numpy` | Data manipulation |
| `Prophet` | Facebook's time-series model (trend + seasonality + regressors) |
| `CatBoostRegressor` | Gradient-boosted trees with native categorical support |
| `XGBRegressor` | Gradient-boosted trees (general-purpose) |
| `holidays` | Country-specific public holiday calendars |
| `shap` | Model explainability (SHAP values for tree models) |
| `sqlalchemy` | Database connectivity |
| `pickle` | Model serialization |

---

### cell-2: Configuration

Central constants that control the entire pipeline:

```python
N_CV_FOLDS = 3            # Number of expanding-window CV folds
TEST_WINDOW_DAYS = 30     # Days per test fold
MIN_TRAIN_DAYS = 60       # Minimum training history before first fold
RANDOM_SEED = 42          # Reproducibility seed
TREE_DEPTH_GRID = [3, 4, 6]  # Hyperparameter search space for tree depth
HOLIDAY_YEARS = [2024, 2025, 2026]  # Years to generate holiday calendars for
```

**Feature lists** (single source of truth for tree models):

```python
TREE_FEATURES = ['day_of_week', 'month', 'is_public_holiday',
                 'rain_lunch_vol', 'temperature',
                 'lag_1', 'lag_7', 'rolling_mean_7', 'rolling_mean_14', 'lag_same_weekday_avg']
TREE_CAT_FEATURES = ['day_of_week', 'month', 'is_public_holiday']
```

The feature order matters because SHAP grouping in the prediction API uses index positions:

| Index | Feature | SHAP Group |
|-------|---------|------------|
| 0 | day_of_week | Seasonality |
| 1 | month | Seasonality |
| 2 | is_public_holiday | Holiday |
| 3 | rain_lunch_vol | Weather |
| 4 | temperature | Weather |
| 5 | lag_1 | Lags (rolled into Trend) |
| 6 | lag_7 | Lags (rolled into Trend) |
| 7 | rolling_mean_7 | Lags (rolled into Trend) |
| 8 | rolling_mean_14 | Lags (rolled into Trend) |
| 9 | lag_same_weekday_avg | Lags (rolled into Trend) |

`safe_filename()` sanitizes dish names (spaces, hyphens, slashes) for use as file paths when saving models.

---

### cell-4: Context Awareness

#### `get_country_code(lat, lon)`

Uses a bounding box to classify coordinates as Singapore or China:
- Latitude 1.1-1.5, Longitude 103.5-104.1 = **SG**
- Everything else = **CN**

#### `estimate_temperature(date, country_code)`

Simulates daily temperature using **per-date seeding**:

```python
rng = np.random.default_rng(RANDOM_SEED + date.toordinal())
```

This ensures:
- The same date always produces the same temperature (reproducible)
- Different dates produce different temperatures (no train-serve skew)
- Values follow seasonal ranges:

| Country | Season | Range |
|---------|--------|-------|
| SG | Year-round | 25-34 C |
| CN | Winter (Dec-Feb) | 2-10 C |
| CN | Spring (Mar-May) | 12-25 C |
| CN | Summer (Jun-Aug) | 26-38 C |
| CN | Fall (Sep-Nov) | 12-25 C |

This function is the single swap point for a real weather API.

#### `add_local_context(df, lat, lon)`

Enriches the raw sales DataFrame with:

1. **Time features**: `day_of_week` (0-6), `month` (1-12)
2. **Holidays**: `is_public_holiday` (0/1) using the `holidays` library for SG or CN
3. **Rain**: `rain_lunch_vol` — simulated rainfall volume, seasonal by country (placeholder for real weather data)
4. **Temperature**: `temperature` — via `estimate_temperature()`

Returns the enriched DataFrame and the detected country code.

---

### cell-6: Data Ingestion & Sanitization

#### `fetch_training_data()`

Attempts MySQL connection first. On failure, falls back to `food_sales_eng.csv`. Returns a DataFrame with columns: `date`, `dish`, `sales`.

#### `sanitize_sparse_data(df, country_code)`

The "Anti-Lazy Employee" fix. Real-world sales data often has missing days (weekends, holidays, staff forgot to log). The model would interpret gaps as zero sales, skewing predictions downward.

**Logic:**

1. **Reindex** to a complete daily date range (fill gaps with NaN)
2. **Detect weak days**: If any day-of-week has less than 50% of the average observation count, its sales are set to NaN (likely unreported, not actually zero)
3. **Interpolate** sales using time-based interpolation, remaining NaN filled with 0
4. **Fill dish name**: Propagate the dish name to newly created rows
5. **Fill weather features**: Interpolate `rain_lunch_vol` and `temperature` for gap-filled dates (with backfill/forwardfill for edges)
6. **Recalculate derived features**: Holidays, `day_of_week`, `month` are recomputed for all dates (including newly inserted ones)

#### `add_lag_features(df)`

Computes lag and rolling features for tree-based models:

| Feature | Definition | Purpose |
|---------|-----------|---------|
| `lag_1` | Sales 1 day ago | Short-term momentum |
| `lag_7` | Sales 7 days ago | Same weekday last week |
| `rolling_mean_7` | Mean of last 7 days (shifted by 1) | Weekly trend |
| `rolling_mean_14` | Mean of last 14 days (shifted by 1) | Bi-weekly trend |
| `lag_same_weekday_avg` | Average of same weekday over past 4 weeks | Weekday-specific baseline |

The shift-by-1 on rolling means prevents data leakage (today's sales excluded). `dropna()` at the end removes the 28-day warmup period where lags are undefined.

---

### cell-8: Model Evaluation

#### `_evaluate_tree(df, end_date, features, make_model, param_key)`

Shared evaluation logic for CatBoost and XGBoost. Runs a **nested loop**:

- **Outer loop**: Iterates over `TREE_DEPTH_GRID` (hyperparameter tuning)
- **Inner loop**: Expanding-window time-series CV with `N_CV_FOLDS` folds

```
|--- Train (expands) ---|--- Test (30 days) ---|--- Future (not used) ---|
                        ^                      ^
                   test_start              test_end
```

Each fold moves the test window forward by 30 days. The training set always starts from the beginning of the data (expanding window, not sliding).

The depth with the lowest average MAE across folds wins. Returns `(avg_mae, last_fold_predictions, best_params)`.

#### `evaluate_model(df, model_type, country_code)`

Dispatches to the appropriate evaluation logic:

**Prophet path:**
- Manual CV loop (same expanding window structure)
- `daily_seasonality=False` (data is daily aggregates, not intra-day — daily seasonality adds noise)
- Regressors: `rain_lunch_vol`, `temperature`
- Country holidays added via `add_country_holidays()`
- No depth tuning (Prophet has no equivalent hyperparameter in this setup)

**CatBoost/XGBoost path:**
- Delegates to `_evaluate_tree()` with a model factory closure
- CatBoost tunes `depth`, XGBoost tunes `max_depth`
- Both use 300 iterations/estimators

---

### cell-10: Training Pipeline

`train_and_evaluate(df, country_code)` is the main orchestrator. For each dish:

1. **Isolate**: Filter to single dish
2. **Sanitize**: Fill gaps via `sanitize_sparse_data()`
3. **Add lags**: Create lag features for tree models
4. **Evaluate all three models**: Get MAE scores and best hyperparameters
5. **Select champion**: Lowest MAE wins
6. **Train production models**: Retrain all three on 100% of data (no holdout) using tuned depths
7. **Save**:
   - `models/prophet_{dish}.pkl`
   - `models/catboost_{dish}.pkl`
   - `models/xgboost_{dish}.pkl`
   - `models/recent_sales_{dish}.pkl` (last 28 days, for lag computation at prediction time)
8. **Update registry**: `models/champion_registry.pkl`

**Registry format:**
```python
{
    "Grilled lamb skewers": {
        "model": "catboost",        # Champion model name
        "mae": 23.15,               # Champion's MAE
        "all_mae": {                # All models' MAEs (for CI calculation)
            "prophet": 28.4,
            "catboost": 23.15,
            "xgboost": 24.01
        }
    },
    ...
}
```

After saving, `clear_model_cache()` is called to ensure stale models aren't served.

The function prints a leaderboard table showing MAE and tuned depth per dish, and returns a results DataFrame + predictions dict for visualization.

---

### cell-12: Prediction API

#### Model Cache

```python
_model_cache = {}        # filepath -> loaded object
_load_cached(filepath)   # Load once, return from cache on subsequent calls
clear_model_cache()      # Invalidate after retraining
```

Avoids repeated `pickle.load()` for the same model file across multiple predictions.

#### `_predict_tree(model_obj, future, cols, dish_mae)`

Shared prediction + SHAP logic for CatBoost and XGBoost:

1. **Predict**: Get raw prediction, clip to >= 0
2. **Confidence interval**: `[prediction - MAE, prediction + MAE]` (lower clipped to 0)
3. **SHAP explanation**: Uses `TreeExplainer` to decompose the prediction into:
   - **Trend**: Base value (expected value) + lag contributions
   - **Seasonality**: day_of_week + month effects
   - **Holiday**: is_public_holiday effect
   - **Weather**: rain + temperature effects

#### `get_prediction(dish, date_str, lat, lon, rain_forecast=0, model='auto')`

The main prediction entry point.

**Step 1 — Registry lookup:**
- Always loads the registry (even for manual model override) to get the MAE for confidence intervals
- If `model='auto'`, selects the champion; otherwise uses the specified model

**Step 2 — Rebuild context:**
- Determines country from coordinates
- Checks if the date is a public holiday
- Computes temperature via `estimate_temperature()`
- Rain is provided by the caller (`rain_forecast` parameter)

**Step 3 — Compute lag features (tree models only):**
- Loads `recent_sales_{dish}.pkl` (last 28 days from training data)
- Computes `lag_1`, `lag_7`, `rolling_mean_7`, `rolling_mean_14`, `lag_same_weekday_avg` from the stored history
- Falls back to zeros if the file is missing

**Step 4 — Predict:**
- **Prophet**: Passes `ds`, `rain_lunch_vol`, `temperature` to the fitted model. Decomposes forecast into trend, holidays, weather (extra regressors), and seasonality (residual).
- **Tree models**: Delegates to `_predict_tree()` for prediction + SHAP.

**Return format:**
```python
{
    "Dish": "Grilled lamb skewers",
    "Date": "2026-05-20",
    "Model Used": "CATBOOST",
    "Prediction": 70,
    "Prediction_Lower": 47,
    "Prediction_Upper": 93,
    "Explanation": {
        "Trend": 74.4,
        "Seasonality": -4.5,
        "Holiday": -2.3,
        "Weather": 3.2
    }
}
```

---

### cell-14: Execution

Runs the full pipeline:
1. Load data (CSV fallback)
2. Set location (currently Shanghai: 31.23, 121.47)
3. Enrich with context features
4. Train and evaluate all dishes
5. Display leaderboard

---

### cell-15: MAE Comparison Chart

Grouped bar chart showing Prophet vs CatBoost vs XGBoost MAE per dish. The champion for each dish is marked with a gold star.

---

### cell-16: Actual vs Predicted

Grid of time-series plots (one per dish) showing the last CV fold's actual vs predicted values for the winning model. The shaded area between the lines represents prediction error.

---

### cell-17: Predictions Table & Chart

Generates forecasts for all dishes on a specified future date using `get_prediction()`. Displays:

- **Table**: Dish, predicted quantity, lower/upper confidence bounds, model used, and SHAP explanation breakdown (Trend, Seasonality, Holiday, Weather)
- **Bar chart**: Predicted quantities with error bars representing confidence intervals, color-coded by model type

---

## Confidence Intervals

All models use the same CI method for consistency:

```
Lower = max(0, prediction - MAE)
Upper = prediction + MAE
```

Where MAE is the model's cross-validation MAE for that specific dish. This provides a practical interpretation: "on average, the model is off by this many plates."

---

## Feature Summary

| Feature | Source | Used By | Description |
|---------|--------|---------|-------------|
| `day_of_week` | Derived from date | Tree models (categorical) | 0 (Monday) to 6 (Sunday) |
| `month` | Derived from date | Tree models (categorical) | 1-12 |
| `is_public_holiday` | `holidays` library | Tree models (categorical) | 0 or 1 |
| `rain_lunch_vol` | Simulated / user input | All models (regressor) | Rainfall volume in mm |
| `temperature` | `estimate_temperature()` | All models (regressor) | Temperature in Celsius |
| `lag_1` | Sales shift(1) | Tree models | Yesterday's sales |
| `lag_7` | Sales shift(7) | Tree models | Same weekday last week |
| `rolling_mean_7` | 7-day rolling mean (shifted) | Tree models | Weekly average trend |
| `rolling_mean_14` | 14-day rolling mean (shifted) | Tree models | Bi-weekly average trend |
| `lag_same_weekday_avg` | Avg of shift(7,14,21,28) | Tree models | 4-week same-weekday baseline |

Prophet uses its own internal trend/seasonality decomposition and receives `rain_lunch_vol` and `temperature` as external regressors.

---

## File Outputs

After training, the `models/` directory contains:

```
models/
  champion_registry.pkl          # {dish: {model, mae, all_mae}}
  prophet_{dish}.pkl             # Fitted Prophet model
  catboost_{dish}.pkl            # Fitted CatBoost model
  xgboost_{dish}.pkl             # Fitted XGBoost model
  recent_sales_{dish}.pkl        # Last 28 days of sales (for lag features)
```

---

## Known Limitations & Future Work

1. **Weather data is simulated.** `estimate_temperature()` and `estimate_rain()` produce plausible seasonal values but are not real weather. Replace with a weather API (e.g., OpenWeatherMap) for production use. `estimate_temperature()` is the single swap point.

2. **Lag features are static at prediction time.** The saved `recent_sales` snapshot is from the last training run. If predicting several days into the future, lags do not update day-by-day. In production, retrain daily or implement rolling lag updates.

3. **Two-country scope.** The bounding box logic defaults to CN for anything outside Singapore. Extending to additional countries requires updating `get_country_code()`, holiday calendars, and weather ranges.

4. **Depth is the only tuned hyperparameter.** Tree iterations (300), learning rate (0.05 for XGBoost), and other parameters are fixed. Expanding the tuning grid would improve accuracy at the cost of longer training time.
