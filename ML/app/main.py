from __future__ import annotations

import logging
from contextlib import asynccontextmanager
from typing import Any, Dict, List, Optional

from celery.result import AsyncResult
from fastapi import FastAPI, HTTPException
from fastapi.responses import JSONResponse
from pydantic import BaseModel, Field

from app.celery_app import celery_app
from app.inference import ModelStore, create_store_from_env, predict_dish

logger = logging.getLogger(__name__)

store: Optional[ModelStore] = None

_RELOAD_CHECK_INTERVAL = 60  # seconds between registry mtime checks
_last_reload_check = 0.0


@asynccontextmanager
async def lifespan(app: FastAPI):
    global store
    store = create_store_from_env()
    yield


app = FastAPI(
    title="SmartSusChef ML Inference API",
    version="2.0.0",
    lifespan=lifespan,
)


# ---------------------------------------------------------------------------
# Request / Response models
# ---------------------------------------------------------------------------
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


class TrainRequest(BaseModel):
    address: str = Field("Shanghai, China", description="Restaurant location address")
    retrain_all: bool = Field(True, description="Retrain all dishes or only new ones")
    sales_data_source: Optional[str] = Field(
        None, description="Path to CSV file; if omitted, uses DB/default CSV"
    )


class TrainResponse(BaseModel):
    task_id: str
    status: str


class TrainStatusResponse(BaseModel):
    task_id: str
    status: str
    result: Optional[Dict[str, Any]] = None
    progress: Optional[Dict[str, Any]] = None


# ---------------------------------------------------------------------------
# Helper: periodic model reload check
# ---------------------------------------------------------------------------
def _maybe_reload_models() -> None:
    """Check if models were updated on disk and reload if needed."""
    import time

    global _last_reload_check
    now = time.time()
    if now - _last_reload_check < _RELOAD_CHECK_INTERVAL:
        return
    _last_reload_check = now
    if store is not None:
        store.reload_if_updated()


# ---------------------------------------------------------------------------
# Endpoints
# ---------------------------------------------------------------------------
@app.get("/health")
def health() -> Dict[str, Any]:
    if store is None:
        raise HTTPException(status_code=503, detail="Model store not initialized")
    return {
        "status": "ok",
        "dishes": len(store.list_dishes()),
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

    _maybe_reload_models()

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


@app.post("/train_models", response_model=TrainResponse, status_code=202)
def trigger_training(req: TrainRequest):
    """Trigger an asynchronous model training run via Celery."""
    from app.tasks.training_orchestrator import train_models

    task = train_models.delay(
        address=req.address,
        retrain_all=req.retrain_all,
        sales_data_source=req.sales_data_source,
    )
    return JSONResponse(
        status_code=202,
        content={"task_id": task.id, "status": "ACCEPTED"},
    )


@app.get("/train_status/{task_id}", response_model=TrainStatusResponse)
def train_status(task_id: str):
    """Query the status of an async training task."""
    result = AsyncResult(task_id, app=celery_app)

    response: Dict[str, Any] = {
        "task_id": task_id,
        "status": result.status,
        "result": None,
        "progress": None,
    }

    if result.state == "PROGRESS":
        response["progress"] = result.info
    elif result.state == "SUCCESS":
        response["result"] = result.result
    elif result.state == "FAILURE":
        response["result"] = {"error": str(result.result)}

    return response
