# Changes to the SmartSusChef ML Repository

## Introduction

The SmartSusChef ML component has been transformed from a monolithic, script-based training pipeline into a robust, scalable service architecture. The goal was to enable:

- **Efficient real-time inference** via a FastAPI web service
- **Asynchronous, user-triggered model training** via Celery task queues
- **Zero-downtime model updates** through hot-reloading mechanisms
- **Independent scaling** of the API and training worker processes

This document details every significant change made across the repository, explaining both *what* was changed and *why*.

---

## 1. `requirements.txt` Updates

### What Changed

Three new dependencies were added:

```
python-dotenv
celery[redis]
redis
```

### Why

- **`python-dotenv`**: Enables loading environment variables from `.env` files, simplifying local development and configuration management across environments (local, Docker, AWS).
- **`celery[redis]`**: The core asynchronous task queue framework. Celery allows long-running operations (like model training) to be offloaded to background workers, preventing the FastAPI web service from blocking. The `[redis]` extra installs the Redis transport backend.
- **`redis`**: The Python client for Redis, which serves as both the message broker (task queue) and result backend (task status storage) for Celery.

---

## 2. `ML/app/inference.py` Modifications

### What Changed

#### Import Migration to `training_logic_v2`

All imports were updated from the legacy `training_logic` module to `training_logic_v2`:

```python
from training_logic_v2 import (
    PipelineConfig,
    WEATHER_COLS,
    get_location_details,
    safe_filename,
    _prophet_predict,
    compute_lag_features_from_history,
    _load_hybrid_models,
)
```

#### Removal of Hardcoded Feature Lists

The following hardcoded globals were removed:

```python
# REMOVED:
TIME_FEATURES = ["day_of_week", "month", "day", "dayofyear", "is_weekend"]
LAGS = (1, 7, 14)
ROLL_WINDOWS = (7, 14, 28)
TREE_FEATURES = [...]
```

These are now sourced dynamically from `PipelineConfig` instances (e.g., `cfg.hybrid_tree_features`, `cfg.lags`, `cfg.roll_windows`).

#### `ModelStore` Updated to Use `joblib.load`

Both `load_registry()` and `get_dish_model()` were changed from:

```python
with open(path, "rb") as f:
    data = pickle.load(f)
```

to:

```python
data = joblib.load(path)
```

#### `_compute_lag_features_from_history` Replaced

The local implementation was removed and replaced with the canonical `compute_lag_features_from_history` imported from `training_logic_v2`, which accepts a `PipelineConfig` parameter for dynamic lag/window configuration.

#### `predict_dish` Uses `PipelineConfig` Dynamically

The prediction function now:
- Creates a `PipelineConfig` instance
- Uses `cfg.hybrid_tree_features` for feature column ordering when constructing the tree model input `X_one`
- Calls `_prophet_predict` from `training_logic_v2` for consistency
- Passes `cfg` to `compute_lag_features_from_history`

#### Model Reloading Mechanism (Task 7)

A new `reload_if_updated()` method was added to `ModelStore`:

```python
def reload_if_updated(self) -> bool:
```

This method checks the `champion_registry.pkl` file's modification time (`st_mtime`) against the last known value. If the file has been updated (e.g., after a training run completes), it reloads the registry and clears the model cache.

### Why

- **Eliminating training-serving skew**: By importing feature definitions and utility functions directly from `training_logic_v2`, the inference module is guaranteed to use the exact same feature engineering, column ordering, and prediction logic as the training pipeline.
- **Consistent serialization**: Switching to `joblib.load` matches the training pipeline's use of `joblib.dump`, avoiding potential deserialization issues between `pickle` and `joblib` formats.
- **Dynamic configuration**: Sourcing feature lists from `PipelineConfig` means that if features are added or removed in the training pipeline, the inference module automatically adapts without code changes.
- **Zero-downtime updates**: The `reload_if_updated` mechanism enables the running FastAPI service to detect newly trained models and start using them without requiring a restart.

---

## 3. Creation of `ML/app/celery_app.py`

### What Changed

A new file was created to initialize and configure the Celery application:

```python
celery_app = Celery(
    "smartsuschef",
    broker=REDIS_URL,
    backend=REDIS_URL,
    include=["app.tasks.training_orchestrator"],
)
```

Key configuration:
- **JSON serialization** for task arguments and results (ensuring compatibility across services)
- **`task_track_started=True`** enabling progress monitoring
- **`result_expires=86400`** (24-hour result retention)
- **Redis** as both broker and backend, configured via `CELERY_BROKER_URL` environment variable

### Why

Centralizing Celery configuration in a single module ensures both the FastAPI web service and the Celery worker share identical settings. The `include` parameter registers task modules so the worker knows which tasks to execute. Using an environment variable for the Redis URL allows easy configuration across local development, Docker, and AWS deployments.

---

## 4. Refactoring and Relocation of `Final_model_v2.py`

### What Changed

#### Encapsulation into a Function

The entire `if __name__ == "__main__":` block (lines 528–750) was encapsulated into a single function:

```python
def train_models(
    self,
    address: str = "Shanghai, China",
    retrain_all: bool = True,
    sales_data_source: Optional[str] = None,
) -> Dict[str, Any]:
```

Parameters:
- **`address`**: Restaurant location for weather/holiday context
- **`retrain_all`**: When `False`, skips dishes that already have models in the champion registry
- **`sales_data_source`**: Optional CSV path; when `None`, uses the default DB-first-then-CSV fallback

#### Celery Task Decoration

The function was decorated with:

```python
@celery_app.task(name="train_models", bind=True)
```

The `bind=True` parameter gives the task access to `self`, enabling progress reporting:

```python
self.update_state(state="PROGRESS", meta={"current": idx, "total": total, "dish": dish_name})
```

#### Relocation

The file was moved from `ML/Final_model_v2.py` to `ML/app/tasks/training_orchestrator.py`, with a new `ML/app/tasks/__init__.py` package file created.

#### Removal of Non-Training Code

The relocated version contains only the training orchestration logic. Visualization functions (`plot_mae_comparison`, `plot_forecasts`) and the prediction function (`get_prediction`) were not included — these belong in the inference layer.

#### Return Value

Instead of displaying plots and tables, the function returns a JSON-serializable summary:

```python
return {
    "results_table": results_rows,
    "trained_dishes": len(results),
    "skipped_dishes": len(unique_dishes) - len(dishes_to_train),
    "errors": errors,
}
```

### Why

- **Modularity**: Transforming the monolithic script into a callable function allows it to be invoked programmatically — by Celery, by tests, or by any other orchestrator.
- **Async execution**: The Celery decorator enables the training pipeline to run in a background worker process, preventing API blocking.
- **Progress tracking**: The `bind=True` + `update_state` pattern lets clients poll for real-time training progress.
- **Clean architecture**: Relocating to `app/tasks/` follows standard Python project structure for Celery tasks, keeping the codebase organized.
- **Incremental training**: The `retrain_all=False` option supports adding new dishes without retraining the entire catalog.

---

## 5. `ML/app/main.py` Additions

### What Changed

#### `POST /train_models` Endpoint (Task 5)

```python
@app.post("/train_models", response_model=TrainResponse, status_code=202)
def trigger_training(req: TrainRequest):
```

- Accepts a JSON body with `address`, `retrain_all`, and `sales_data_source`
- Calls `train_models.delay(...)` to dispatch the task to the Celery worker
- Returns an immediate **HTTP 202 Accepted** response with the `task_id`

#### `GET /train_status/{task_id}` Endpoint (Task 6)

```python
@app.get("/train_status/{task_id}", response_model=TrainStatusResponse)
def train_status(task_id: str):
```

- Queries the Celery result backend using `AsyncResult`
- Returns the task's current `status` (PENDING, STARTED, PROGRESS, SUCCESS, FAILURE)
- Includes `progress` metadata during PROGRESS state (current dish, count)
- Includes `result` on SUCCESS or error details on FAILURE

#### Periodic Model Reload Integration (Task 7)

```python
def _maybe_reload_models() -> None:
```

Called on every `/predict` request (throttled to once per 60 seconds), this function invokes `store.reload_if_updated()` to detect and load newly trained models.

#### New Pydantic Models

- `TrainRequest`: Request body for `/train_models`
- `TrainResponse`: Response with `task_id` and `status`
- `TrainStatusResponse`: Full status response with optional `result` and `progress`

### Why

- **User-triggered training**: The `/train_models` endpoint provides a clean API interface for triggering retraining — no SSH access or manual script execution required.
- **Non-blocking**: By using Celery's `delay()`, the endpoint returns immediately, keeping the API responsive even during hour-long training runs.
- **Observability**: The `/train_status` endpoint gives clients (frontend dashboards, monitoring systems) real-time visibility into training progress and results.
- **Automatic model pickup**: The periodic reload mechanism ensures that once training completes and models are saved to disk, the inference service starts using them automatically.

---

## 6. `ML/Dockerfile` Updates

### What Changed

- Added `curl` to the `apt-get install` list (required for container health checks)
- No structural changes needed — the existing `COPY . .` already copies the new `app/tasks/` directory and `app/celery_app.py`
- The `CMD` remains `uvicorn app.main:app --host 0.0.0.0 --port 8000` for the web service

### Why

The same Docker image serves dual purposes:
1. **Web service**: When run with the default `CMD` (uvicorn), it serves the FastAPI inference API
2. **Celery worker**: When run with a custom command override (`celery -A app.celery_app worker`), it executes background training tasks

This single-image approach simplifies CI/CD — one build, two deployment targets. The `curl` addition enables ECS health checks that probe the `/health` endpoint.

---

## 7. Creation of `ML/aws/worker-task-definition.json`

### What Changed

A new ECS Fargate task definition was created for the Celery worker:

```json
{
  "family": "smartsuschef-ml-worker",
  "cpu": "2048",
  "memory": "4096",
  "command": ["celery", "-A", "app.celery_app", "worker", "--loglevel=info", "--concurrency=2"]
}
```

Key configuration:
- **2 vCPU / 4 GB RAM**: More resources than the API server, reflecting the compute-intensive nature of model training
- **`--concurrency=2`**: Limits parallel task execution to prevent memory exhaustion
- **Environment variables**: `MODEL_DIR` and `CELERY_BROKER_URL` for runtime configuration
- **CloudWatch logging**: Configured under `/ecs/smartsuschef-ml-worker`

The existing `ML/aws/task-definition.json` was also updated to include the `CELERY_BROKER_URL` environment variable, ensuring the API server can dispatch tasks to the broker.

### Why

- **Separation of concerns**: The API server and training worker have fundamentally different resource profiles. The API needs low latency; the worker needs high CPU/memory. Separate task definitions allow independent scaling.
- **Resource isolation**: Training runs cannot starve the inference API of resources.
- **Elastic scaling**: Workers can be scaled up during retraining periods and scaled down to zero when idle.
- **Same image, different command**: Overriding just the `command` field avoids maintaining separate Docker images.

---

## Summary of Files Changed

| File | Action | Task |
|------|--------|------|
| `ML/requirements.txt` | Modified | Task 3 |
| `ML/app/inference.py` | Modified | Tasks 2, 7 |
| `ML/app/celery_app.py` | Created | Task 3 |
| `ML/app/tasks/__init__.py` | Created | Task 4a |
| `ML/app/tasks/training_orchestrator.py` | Created | Tasks 4, 4a |
| `ML/Final_model_v2.py` | Modified | Task 4 |
| `ML/app/main.py` | Modified | Tasks 5, 6, 7 |
| `ML/Dockerfile` | Modified | Task 8 |
| `ML/aws/worker-task-definition.json` | Created | Task 9 |
| `ML/aws/task-definition.json` | Modified | Task 9 |
