from __future__ import annotations

from contextlib import asynccontextmanager
from typing import Any, Dict, List, Optional

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

from app.inference import ModelStore, create_store_from_env, predict_dish


store: Optional[ModelStore] = None


@asynccontextmanager
async def lifespan(app: FastAPI):
    global store
    store = create_store_from_env()
    yield


app = FastAPI(
    title="SmartSusChef ML Inference API",
    version="1.0.0",
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
