from __future__ import annotations

import threading
from contextlib import asynccontextmanager
from typing import Any, Dict, List, Optional

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

from app.inference import ModelStore, create_store_from_env, predict_dish
from app.store_manager import StoreModelManager


store: Optional[ModelStore] = None
manager: Optional[StoreModelManager] = None


@asynccontextmanager
async def lifespan(app: FastAPI):
    global store, manager
    # Legacy global store (for backward-compat /predict endpoint)
    try:
        store = create_store_from_env()
    except FileNotFoundError:
        store = None  # No global models yet, that's OK

    # Store-aware manager
    import os
    manager = StoreModelManager(base_model_dir=os.getenv("MODEL_DIR", "models"))
    yield


app = FastAPI(
    title="SmartSusChef ML Inference API",
    version="2.0.0",
    lifespan=lifespan,
)


class PredictRequest(BaseModel):
    dish: str = Field(..., description="Dish name that exists in champion_registry.pkl")
    recent_sales: List[float] = Field(..., min_length=1, description="Recent daily sales history")
    horizon_days: int = Field(14, ge=1, le=30)
    start_date: Optional[str] = Field(None, description="YYYY-MM-DD; default is tomorrow")
    address: str = Field("Shanghai, China", description="Used when lat/lon are not provided")
    latitude: Optional[float] = None
    longitude: Optional[float] = None
    country_code: Optional[str] = None
    weather_rows: Optional[List[Dict[str, Any]]] = Field(
        None,
        description="Optional custom weather list for horizon dates",
    )


class PredictResponse(BaseModel):
    dish: str
    model: str
    model_combo: str
    horizon_days: int
    start_date: str
    predictions: List[Dict[str, Any]]


@app.get("/health")
def health() -> Dict[str, Any]:
    """Health check endpoint for ALB/ECS."""
    dishes_count = 0
    if store is not None:
        dishes_count = len(store.list_dishes())
    return {
        "status": "ok",
        "dishes": dishes_count,
        "manager_ready": manager is not None,
    }


@app.get("/dishes")
def dishes() -> Dict[str, List[str]]:
    if store is None:
        raise HTTPException(status_code=503, detail="Model store not initialized")
    return {"dishes": store.list_dishes()}


@app.post("/predict", response_model=PredictResponse)
def predict(req: PredictRequest) -> Dict[str, Any]:
    if store is None:
        raise HTTPException(status_code=503, detail="Model store not initialized")

    try:
        return predict_dish(
            store=store,
            dish=req.dish,
            recent_sales=req.recent_sales,
            horizon_days=req.horizon_days,
            start_date=req.start_date,
            address=req.address,
            latitude=req.latitude,
            longitude=req.longitude,
            country_code=req.country_code,
            weather_rows=req.weather_rows,
        )
    except KeyError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e)) from e
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e)) from e


# =====================================================================
# Store-aware endpoints (called by .NET backend)
# =====================================================================

class StorePredictRequest(BaseModel):
    store_id: int = Field(..., description="Store ID from the .NET database")
    horizon_days: int = Field(14, ge=1, le=30)
    address: Optional[str] = None
    latitude: Optional[float] = None
    longitude: Optional[float] = None
    country_code: Optional[str] = None


class StorePredictResponse(BaseModel):
    store_id: int
    status: str  # "ok" | "training" | "insufficient_data" | "error"
    message: Optional[str] = None
    days_available: Optional[int] = None
    predictions: Optional[Dict[str, Any]] = None  # dish -> predictions


class TrainingProgressResponse(BaseModel):
    trained: int
    failed: int
    total: int
    current_dish: Optional[str] = None


class StoreStatusResponse(BaseModel):
    store_id: int
    has_models: bool
    is_training: bool
    dishes: Optional[List[str]] = None
    days_available: Optional[int] = None
    training_progress: Optional[TrainingProgressResponse] = None


@app.get("/store/{store_id}/status", response_model=StoreStatusResponse)
def store_status(store_id: int) -> Dict[str, Any]:
    """Check if models exist for a store, and how much data is available."""
    if manager is None:
        raise HTTPException(status_code=503, detail="Manager not initialized")

    has_models = manager.has_models(store_id)
    is_training = manager.is_training(store_id)

    dishes = None
    if has_models:
        ms = manager.get_store(store_id)
        if ms:
            dishes = ms.list_dishes()

    # Check available data — gracefully handle DB failures
    days_available = None
    try:
        _, days_available = manager.fetch_store_sales(store_id)
    except Exception as e:
        import logging
        logging.getLogger(__name__).warning(
            "Could not fetch sales data for store %d: %s", store_id, e
        )

    # Include training progress if currently training
    training_progress = None
    if is_training:
        progress = manager.get_training_progress(store_id)
        if progress:
            training_progress = progress

    return {
        "store_id": store_id,
        "has_models": has_models,
        "is_training": is_training,
        "dishes": dishes,
        "days_available": days_available,
        "training_progress": training_progress,
    }


@app.post("/store/{store_id}/train")
def store_train(store_id: int) -> Dict[str, Any]:
    """
    Trigger model training for a store.
    Returns immediately; training runs in a background thread.
    """
    if manager is None:
        raise HTTPException(status_code=503, detail="Manager not initialized")

    if manager.is_training(store_id):
        return {"status": "already_training", "store_id": store_id}

    # Quick data check
    try:
        _, days_available = manager.fetch_store_sales(store_id)
    except Exception as e:
        raise HTTPException(
            status_code=503,
            detail={"status": "error", "message": f"Cannot connect to database: {e}"},
        ) from e

    if days_available < StoreModelManager.MIN_TRAINING_DAYS:
        raise HTTPException(
            status_code=400,
            detail={
                "status": "insufficient_data",
                "message": f"Need at least {StoreModelManager.MIN_TRAINING_DAYS} days, found {days_available}.",
                "days_available": days_available,
                "min_required": StoreModelManager.MIN_TRAINING_DAYS,
            },
        )

    # Start training in background
    thread = threading.Thread(
        target=manager.train_store_models, args=(store_id,), daemon=True
    )
    thread.start()

    return {
        "status": "training_started",
        "store_id": store_id,
        "message": "Model training started in background. Poll /store/{store_id}/status to check progress.",
    }


@app.post("/store/{store_id}/predict", response_model=StorePredictResponse)
def store_predict(store_id: int, req: StorePredictRequest) -> Dict[str, Any]:
    """
    Generate predictions for ALL dishes of a store.
    - If models exist → predict immediately
    - If no models & data < 100 days → return insufficient_data
    - If no models & data >= 100 days → trigger training & return training status
    """
    if manager is None:
        raise HTTPException(status_code=503, detail="Manager not initialized")

    # Check if training is in progress
    if manager.is_training(store_id):
        return {
            "store_id": store_id,
            "status": "training",
            "message": "Model training in progress. Please retry later.",
        }

    # Check if models exist
    ms = manager.get_store(store_id)
    if ms is None:
        # No models — check data availability
        try:
            _, days_available = manager.fetch_store_sales(store_id)
        except Exception as e:
            return {
                "store_id": store_id,
                "status": "error",
                "message": f"Cannot connect to database to check sales data: {e}",
                "days_available": 0,
            }

        if days_available < StoreModelManager.MIN_TRAINING_DAYS:
            return {
                "store_id": store_id,
                "status": "insufficient_data",
                "message": f"Need at least {StoreModelManager.MIN_TRAINING_DAYS} days of sales data to train models. Currently have {days_available} days.",
                "days_available": days_available,
            }

        # Enough data — trigger training
        thread = threading.Thread(
            target=manager.train_store_models, args=(store_id,), daemon=True
        )
        thread.start()
        return {
            "store_id": store_id,
            "status": "training",
            "message": "No models found. Training started. Please retry in a few minutes.",
        }

    # Models exist — predict all dishes
    dishes = ms.list_dishes()
    lat = req.latitude
    lon = req.longitude
    cc = req.country_code

    if lat is None or lon is None or not cc:
        db_lat, db_lon, db_cc = manager.fetch_store_location(store_id)
        lat = lat or db_lat
        lon = lon or db_lon
        cc = cc or db_cc

    all_predictions: Dict[str, Any] = {}

    for dish in dishes:
        try:
            # Get recent sales from stored data
            recent_sales_path = ms.model_dir / f"recent_sales_{dish.replace(' ', '_').replace('-', '_').replace('/', '_')}.pkl"
            if recent_sales_path.exists():
                import joblib
                recent_df = joblib.load(str(recent_sales_path))
                recent_sales = recent_df["sales"].astype(float).tolist()
            else:
                # Fallback: fetch from DB
                df, _ = manager.fetch_store_sales(store_id)
                if df is not None:
                    dish_sales = df[df["dish"] == dish].sort_values("date").tail(28)
                    recent_sales = dish_sales["sales"].astype(float).tolist()
                else:
                    recent_sales = [0.0] * 14  # Last resort

            if not recent_sales:
                recent_sales = [0.0] * 14

            result = predict_dish(
                store=ms,
                dish=dish,
                recent_sales=recent_sales,
                horizon_days=req.horizon_days,
                address=req.address or "Shanghai, China",
                latitude=lat,
                longitude=lon,
                country_code=cc,
            )
            all_predictions[dish] = result
        except Exception as e:
            all_predictions[dish] = {"error": str(e)}

    return {
        "store_id": store_id,
        "status": "ok",
        "predictions": all_predictions,
    }
