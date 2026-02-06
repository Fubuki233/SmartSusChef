# SmartSusChef ML — Complete System Explanation

## 1. Project Overview

### What is SmartSusChef ML?

SmartSusChef ML is a machine learning system designed to forecast daily food demand for restaurants. Given a restaurant's historical sales data, location, and contextual signals (weather, holidays, day-of-week patterns), it predicts how many servings of each dish will be needed over the next 14 days.

### The Problem It Solves

Restaurants face a fundamental tension: prepare too much food and you waste ingredients and money; prepare too little and you lose revenue and disappoint customers. Manual forecasting — a chef estimating tomorrow's orders based on intuition — is inconsistent and doesn't scale across large menus or multiple locations.

SmartSusChef ML automates this process by learning from historical patterns and external signals to produce accurate, dish-level demand forecasts with confidence intervals and explainable breakdowns.

### High-Level Goal

Provide accurate, scalable, and dynamic food demand predictions through:
- A **real-time prediction API** that returns forecasts in milliseconds
- An **asynchronous training pipeline** that can retrain models without disrupting the prediction service
- **Automatic model updates** that take effect without downtime

---

## 2. Core ML Model Architecture

### Name: Hybrid Prophet + Tree Residual Stacking Model

The forecasting engine uses a two-stage stacking approach where each model handles what it does best.

### Stage 1: Prophet (Baseline Prediction)

[Prophet](https://facebook.github.io/prophet/) is a time-series forecasting model developed by Meta. In SmartSusChef, Prophet serves as the **baseline predictor**, modeling:

- **Trend**: The long-term direction of sales (e.g., a gradually increasing customer base)
- **Weekly seasonality**: Day-of-week patterns (e.g., higher sales on Fridays)
- **Holiday effects**: Country-specific public holidays that affect dining behavior
- **Weather regressors**: Daily temperature (max/min), humidity, and precipitation as additional signals

**Why Prophet?**
- Robust to missing data and outliers (common in restaurant sales)
- Built-in handling of holidays and multiple seasonality patterns
- Interpretable decomposition (you can see exactly how much trend vs. seasonality contributes)
- Performs well even with limited data history

Prophet outputs a prediction (`yhat`) for each future date. However, it cannot capture complex non-linear interactions between features — that's where the tree models come in.

### Stage 2: Tree Models (Residual Refinement)

Three gradient-boosted tree algorithms compete to model the **residuals** (errors) left over after Prophet's prediction:

| Model | Library | Key Strengths |
|-------|---------|---------------|
| **XGBoost** | `xgboost` | Fast, regularized, widely used |
| **CatBoost** | `catboost` | Handles categorical features natively, robust to overfitting |
| **LightGBM** | `lightgbm` | Memory-efficient, fastest training on large datasets |

Each tree model receives a rich feature set:

- **Seasonality features**: `day_of_week`, `month`, `day`, `dayofyear`, `is_weekend`
- **Holiday features**: `is_public_holiday`
- **Weather features**: `temperature_2m_max`, `temperature_2m_min`, `relative_humidity_2m_mean`, `precipitation_sum`
- **Lag features**: `y_lag_1`, `y_lag_7`, `y_lag_14` (sales 1, 7, and 14 days ago)
- **Rolling statistics**: `y_roll_mean_7`, `y_roll_std_7`, `y_roll_mean_14`, etc. (rolling averages and standard deviations over 7, 14, and 28-day windows)
- **Prophet's prediction**: `prophet_yhat` (the baseline forecast itself becomes a feature)

The tree model's target is the **residual**: `actual_sales - prophet_yhat`. By learning patterns in these errors, the tree model corrects systematic biases in Prophet's predictions.

**Why tree models?**
- Excel at capturing non-linear interactions (e.g., "cold rainy Mondays in winter" being systematically different)
- Handle heterogeneous features (dates, weather, lags) naturally
- Fast to train and predict
- Well-understood regularization to prevent overfitting

### How Residual Stacking Works

At prediction time, the final forecast for each day is:

```
final_prediction = max(0, prophet_yhat + tree_residual_prediction)
```

The `max(0, ...)` clamp ensures predictions never go negative (you can't sell negative dishes).

This stacking approach consistently outperforms either model alone because:
- Prophet captures the smooth baseline (trend + seasonality)
- The tree model captures the remaining complex patterns
- Together, they cover both the "big picture" and the "fine details"

### Hyperparameter Optimization (Optuna)

Each tree model's hyperparameters (learning rate, tree depth, regularization strength, etc.) are optimized using [Optuna](https://optuna.org/), a Bayesian hyperparameter search framework.

For each dish, Optuna runs 30 trials per model type (configurable), evaluating each parameter combination using expanding-window time-series cross-validation. This prevents data leakage — the model is always validated on future data it hasn't seen.

**Cross-validation strategy:**
- 3 folds with expanding training windows
- 30-day test windows
- Minimum 60 days of training data required
- Data sanitation (interpolation of missing days) is applied per-fold independently to prevent leakage

### Champion Model Selection

After optimization, the three models (XGBoost, CatBoost, LightGBM) are compared on their cross-validated **Mean Absolute Error (MAE)** — the average number of servings the prediction is off by. The model with the lowest MAE becomes the **champion** for that dish.

This is done per-dish because different dishes may have different optimal models. A dish with strong weekly patterns might favor LightGBM, while one with irregular spikes might favor CatBoost.

The champion selection is stored in `champion_registry.pkl` — a dictionary mapping each dish name to its winning model type, MAE scores, and optimized hyperparameters.

---

## 3. System Architecture and Flow

```
┌─────────────┐         ┌─────────────┐         ┌──────────────────┐
│   Client /   │  HTTP   │   FastAPI    │  Celery  │  Celery Worker   │
│   Frontend   │ ──────> │  Web Server  │ ──────> │  (Training)      │
│              │ <────── │  (Inference) │ <────── │                  │
└─────────────┘         └──────┬──────┘         └────────┬─────────┘
                               │                         │
                               │    ┌──────────┐         │
                               │    │  Redis    │         │
                               │    │  Broker   │ <───────┘
                               │    └──────────┘
                               │
                          ┌────┴────┐
                          │ models/ │
                          │  .pkl   │
                          └─────────┘
```

### FastAPI (API Gateway) — `app/main.py`

FastAPI serves as the front-facing web service, exposing three categories of endpoints:

#### Inference Endpoints

- **`GET /health`**: Returns service status and number of loaded dishes
- **`GET /dishes`**: Lists all dishes in the champion registry
- **`POST /predict`**: Accepts a dish name, recent sales history, forecast horizon, and location; returns multi-day predictions with Prophet trend and tree residual breakdowns

#### Training Endpoints

- **`POST /train_models`**: Triggers an asynchronous training run. Accepts parameters:
  - `address`: Restaurant location (for weather/holiday context)
  - `retrain_all`: Whether to retrain all dishes or only new ones
  - `sales_data_source`: Optional CSV file path

  Returns immediately with HTTP 202 and a `task_id`.

- **`GET /train_status/{task_id}`**: Polls the status of a training task. Returns:
  - `PENDING`: Task is queued
  - `STARTED`: Worker has picked it up
  - `PROGRESS`: Training is running (includes current dish and count)
  - `SUCCESS`: Training complete (includes results summary)
  - `FAILURE`: Training failed (includes error details)

**Benefit**: The API remains responsive at all times. Prediction requests are served in milliseconds even while a multi-hour training job runs in the background.

### Celery (Asynchronous Task Queue) — `app/celery_app.py`

Celery is a distributed task queue that decouples "requesting work" from "doing work."

**Components:**
- **Message Broker (Redis)**: A queue where tasks are placed by the FastAPI server and consumed by workers
- **Result Backend (Redis)**: Stores task status and results so clients can check progress
- **Celery Workers**: Separate processes (or containers) that execute tasks

**Flow:**
1. User sends `POST /train_models` to FastAPI
2. FastAPI calls `train_models.delay(...)`, which serializes the task arguments and pushes a message to Redis
3. FastAPI immediately returns HTTP 202 with the `task_id`
4. A Celery worker picks the message from Redis and begins executing the `train_models` function
5. The worker periodically updates its state (PROGRESS) in Redis
6. When complete, the worker saves the result to Redis and writes models to disk
7. Client polls `GET /train_status/{task_id}` to check progress

**Benefit**: Training can take minutes to hours depending on the number of dishes and Optuna trials. Without Celery, the API server would be blocked for the entire duration. With Celery, the API continues serving predictions while training runs on a separate process (or entirely separate machine).

### Training Orchestrator — `app/tasks/training_orchestrator.py`

This module contains the `train_models` Celery task — the heart of the training pipeline. It orchestrates the end-to-end workflow:

1. **Data loading**: From a MySQL database (via `DATABASE_URL` env var) or a CSV fallback
2. **Context enrichment**: Geocoding the restaurant address, fetching historical weather data, adding holiday flags
3. **Dish filtering**: If `retrain_all=False`, skips dishes that already have models
4. **Parallel training**: Uses `ProcessPoolExecutor` to train each dish on a separate CPU core
5. **Per-dish pipeline** (via `process_dish` from `training_logic_v2`):
   - Data sanitation and feature engineering
   - Prophet fitting and residual computation
   - Optuna optimization for XGBoost, CatBoost, and LightGBM
   - Champion selection based on CV MAE
   - Final model retraining on full data
   - Model saving to disk
6. **Registry update**: Saves the champion registry (`champion_registry.pkl`)
7. **Progress reporting**: Updates Celery task state with current dish and count

**How it interacts with Celery**: The function is decorated with `@celery_app.task(name="train_models", bind=True)`. The `bind=True` gives access to `self`, which is used to call `self.update_state()` for progress reporting. Arguments are JSON-serialized, and the return value is stored in Redis.

### Inference Module — `app/inference.py`

This module handles all prediction-related logic:

#### `ModelStore` Class

The central model management class:
- **`load_registry()`**: Loads `champion_registry.pkl` using `joblib.load`
- **`get_dish_model(dish)`**: Loads Prophet and tree model files for a specific dish, with in-memory caching
- **`reload_if_updated()`**: Checks if the registry file has been modified on disk (by comparing `st_mtime`) and reloads if so, clearing the model cache

#### `predict_dish()` Function

The main prediction function:
1. Resolves the restaurant's location (geocoding, cached)
2. Fetches weather forecast data from Open-Meteo API
3. Loads the dish's Prophet and tree models via `ModelStore`
4. For each day in the forecast horizon:
   - Gets Prophet's baseline prediction
   - Computes lag/rolling features from sales history
   - Builds the full feature vector using `PipelineConfig.hybrid_tree_features`
   - Gets the tree model's residual prediction
   - Combines: `final = max(0, prophet_yhat + residual)`
   - Appends the prediction to history (for recursive forecasting)
5. Returns predictions with Prophet trend and residual components

**Key features:**
- **Dynamic configuration**: All feature names and model parameters come from `PipelineConfig`, not hardcoded values
- **Model caching**: Models are loaded once and cached in memory for fast subsequent predictions
- **Hot-reloading**: The `reload_if_updated` mechanism detects new models written by the training pipeline

### Model Storage — `models/` Directory

All trained artifacts are saved as `.pkl` files using `joblib`:

| File | Contents |
|------|----------|
| `champion_registry.pkl` | Dict mapping dish names to champion model info (model type, MAE, hyperparams) |
| `prophet_{dish_name}.pkl` | Fitted Prophet model for each dish |
| `{champion}_{dish_name}.pkl` | Fitted tree model (e.g., `lightgbm_Kung_Pao_Chicken.pkl`) |
| `recent_sales_{dish_name}.pkl` | Last 28 days of sales data for lag feature computation at prediction time |

---

## 4. Key Benefits of This Architecture

### Responsiveness

FastAPI handles prediction requests with sub-second latency. Model training is fully decoupled — even a training job processing 50 dishes with 30 Optuna trials each runs in the background without affecting prediction performance.

### Scalability

The API server and training workers are separate processes (and separate ECS tasks in production). This means:
- **API**: Scale horizontally to handle more prediction requests
- **Workers**: Scale up during retraining windows, scale to zero when idle
- **Independent resource allocation**: Workers get more CPU/memory for compute-heavy training

### Maintainability

Clear separation of concerns across modules:
- `training_logic_v2.py` — Core ML algorithms (portable, testable)
- `app/tasks/training_orchestrator.py` — Training workflow orchestration
- `app/inference.py` — Prediction logic and model management
- `app/main.py` — HTTP API layer
- `app/celery_app.py` — Task queue configuration

Each module has a single responsibility and can be modified, tested, or replaced independently.

### Robustness

The system handles real-world data challenges gracefully:
- **Missing sales days**: Interpolated during sanitation
- **Geocoding failures**: Falls back to configurable default location
- **Weather API failures**: Falls back to database cache, then to zero-filled features
- **Unsupported holiday countries**: Falls back to default country code
- **Sparse dish data**: Dishes with insufficient history are skipped with clear error messages
- **Per-fold sanitation**: Data interpolation happens independently per CV fold to prevent data leakage

### Dynamic Updates

When training completes:
1. New model files are written to `models/`
2. The champion registry is updated
3. The inference service detects the file change on the next periodic check (every 60 seconds)
4. Model cache is cleared and new models are loaded on demand
5. No restart required — zero downtime

### Transparency

- Training progress is reported in real-time via Celery state updates
- The `/train_status` endpoint provides visibility into training jobs
- Each prediction includes component breakdowns (Prophet trend + residual)
- SHAP-based feature group explanations are available (Seasonality, Holiday, Weather, Lags/Trend)
- Champion selection is based on transparent, logged MAE comparisons

---

## 5. How to Run/Deploy the System

### Local Development

#### Prerequisites

```bash
# Install Python dependencies
pip install -r requirements.txt

# Start a local Redis instance (for Celery)
# Using Docker:
docker run -d -p 6379:6379 redis:alpine

# Or install natively on macOS:
brew install redis && brew services start redis
```

#### Environment Variables

Create a `.env` file in the `ML/` directory:

```env
DATABASE_URL=mysql+pymysql://user:pass@host:3306/dbname   # Optional, falls back to CSV
CELERY_BROKER_URL=redis://localhost:6379/0                  # Required for Celery
MODEL_DIR=models                                            # Default
```

#### Running the System

```bash
# Terminal 1: Start the Celery worker
celery -A app.celery_app worker --loglevel=info

# Terminal 2: Start the FastAPI server
uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
```

#### Testing

```bash
# Trigger training
curl -X POST http://localhost:8000/train_models \
  -H "Content-Type: application/json" \
  -d '{"address": "Shanghai, China", "retrain_all": true}'

# Check training status (use the task_id from the response above)
curl http://localhost:8000/train_status/{task_id}

# Get a prediction
curl -X POST http://localhost:8000/predict \
  -H "Content-Type: application/json" \
  -d '{"dish": "Kung Pao Chicken", "recent_sales": [10,12,8,15,20,18,14,11,13,16,19,22,17,15,12,10,14,18,20,16,13,11,15,19,21,17,14,12], "horizon_days": 14, "address": "Shanghai, China"}'
```

### Deployment on AWS ECS Fargate

#### Docker Image

The `Dockerfile` packages the entire ML application:

```dockerfile
FROM python:3.11-slim
WORKDIR /app
COPY requirements.txt ./
RUN pip install -r requirements.txt
COPY . .
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
```

A single image is built and pushed to **Amazon ECR**. Both the web service and worker use the same image, differentiated only by their startup command.

#### ECS Task Definitions

Two separate ECS task definitions deploy the same image with different roles:

**`task-definition.json` (Web Service)**
- **CPU/Memory**: 1 vCPU / 2 GB
- **Command**: Default (`uvicorn app.main:app ...`)
- **Port mapping**: 8000
- **Health check**: `curl -f http://localhost:8000/health`
- **Scaling**: Based on request volume

**`worker-task-definition.json` (Celery Worker)**
- **CPU/Memory**: 2 vCPU / 4 GB (more resources for training)
- **Command**: `celery -A app.celery_app worker --loglevel=info --concurrency=2`
- **No port mapping** (workers don't serve HTTP)
- **Scaling**: Based on queue depth (scale up when training tasks are queued)

#### Infrastructure Components

| Component | AWS Service | Purpose |
|-----------|-------------|---------|
| Container registry | ECR | Stores the Docker image |
| Web service | ECS Fargate | Runs FastAPI for inference |
| Training workers | ECS Fargate | Runs Celery workers for training |
| Message broker | ElastiCache (Redis) | Task queue and result storage |
| Database | RDS (MySQL) | Sales data storage |
| Logging | CloudWatch | Centralized log aggregation |

The separate task definitions enable **independent scaling**:
- The web service scales horizontally based on prediction request volume
- Workers can be scaled to zero when no training is happening, and spun up on-demand when `/train_models` is called
- This optimizes cost — training resources are only consumed when needed

---

## 6. Conclusion

The SmartSusChef ML system combines proven forecasting techniques (Prophet for time-series decomposition, gradient-boosted trees for residual learning) with modern software engineering practices (async task queues, containerized microservices, hot-reloading) to deliver a production-ready food demand forecasting platform.

The architecture is designed for the real world:
- **Accurate**: Hybrid stacking with per-dish champion selection ensures optimal performance for each dish
- **Scalable**: Independent scaling of inference and training workloads
- **Resilient**: Graceful fallbacks for geocoding, weather, and data issues
- **Operational**: Real-time training monitoring, automatic model updates, health checks
- **Maintainable**: Clean module boundaries with a single source of truth for ML configuration

The system can be extended to support multi-restaurant deployments, A/B testing of model versions, automated retraining schedules, and integration with inventory management systems — all without fundamental architectural changes.
