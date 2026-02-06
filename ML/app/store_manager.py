"""
Store-aware model management for multi-tenant ML inference.

Each store gets its own set of trained models stored under models/store_{id}/.
This module handles:
- Checking if models exist for a given store
- Training models for a new store from database sales data
- Loading store-specific ModelStore instances
"""

from __future__ import annotations

import os
import logging
import threading
from pathlib import Path
from typing import Any, Dict, List, Optional, Tuple

import pandas as pd
from sqlalchemy import create_engine, text

from app.inference import ModelStore

logger = logging.getLogger(__name__)


class StoreModelManager:
    """Manages per-store ML models (load / check / train)."""

    MIN_TRAINING_DAYS = 100  # Minimum days of data required to train

    def __init__(self, base_model_dir: str = "models") -> None:
        self.base_model_dir = Path(base_model_dir)
        self.base_model_dir.mkdir(parents=True, exist_ok=True)
        self._stores: Dict[int, ModelStore] = {}
        self._training_lock = threading.Lock()
        self._training_in_progress: Dict[int, bool] = {}
        self._training_progress: Dict[int, Dict[str, Any]] = {}  # {store_id: {trained, failed, total, current_dish}}
        self._engine = None  # Cached SQLAlchemy engine

    # ------------------------------------------------------------------
    # Public helpers
    # ------------------------------------------------------------------

    def store_model_dir(self, store_id: int) -> Path:
        return self.base_model_dir / f"store_{store_id}"

    def has_models(self, store_id: int) -> bool:
        """Check whether trained models exist for a given store."""
        registry_path = self.store_model_dir(store_id) / "champion_registry.pkl"
        return registry_path.exists()

    def is_training(self, store_id: int) -> bool:
        return self._training_in_progress.get(store_id, False)

    def get_training_progress(self, store_id: int) -> Optional[Dict[str, Any]]:
        """Return current training progress for a store, or None if not training."""
        return self._training_progress.get(store_id)

    def get_store(self, store_id: int) -> Optional[ModelStore]:
        """Return a loaded ModelStore for the given store, or None."""
        if store_id in self._stores:
            return self._stores[store_id]

        if not self.has_models(store_id):
            return None

        store = ModelStore(model_dir=str(self.store_model_dir(store_id)))
        store.load_registry()
        self._stores[store_id] = store
        return store

    def reload_store(self, store_id: int) -> Optional[ModelStore]:
        """Force-reload models for a store (e.g. after training)."""
        self._stores.pop(store_id, None)
        return self.get_store(store_id)

    # ------------------------------------------------------------------
    # Data fetching
    # ------------------------------------------------------------------

    @staticmethod
    def _get_db_url() -> Optional[str]:
        return os.getenv("DATABASE_URL")

    def _get_engine(self):
        """Return a cached SQLAlchemy engine (create once, reuse)."""
        if self._engine is None:
            db_url = self._get_db_url()
            if not db_url:
                return None
            # Add connect_timeout to avoid hanging on unreachable DB
            connect_args = {"connect_timeout": 5}
            self._engine = create_engine(
                db_url,
                pool_pre_ping=True,
                pool_recycle=300,
                connect_args=connect_args,
            )
        return self._engine

    def fetch_store_sales(
        self, store_id: int
    ) -> Tuple[Optional[pd.DataFrame], int]:
        """
        Fetch sales data for a specific store from the database.
        Returns (dataframe_or_none, total_unique_days).

        The SQL matches the EF Core schema:
          SalesData.Date, Recipes.Name, SalesData.Quantity
        """
        engine = self._get_engine()
        if not engine:
            logger.warning("DATABASE_URL not set — cannot fetch store sales.")
            return None, 0

        try:
            query = text("""
                SELECT s.Date   AS date,
                       r.Name   AS dish,
                       s.Quantity AS sales
                FROM SalesData s
                JOIN Recipes r ON s.RecipeId = r.Id
                WHERE s.StoreId = :store_id
                ORDER BY s.Date ASC
            """)
            df = pd.read_sql(query, engine, params={"store_id": store_id})
            df["date"] = pd.to_datetime(df["date"]).dt.normalize()
            df = (
                df.groupby(["date", "dish"])
                .agg(sales=("sales", "sum"))
                .reset_index()
                .sort_values("date")
            )
            unique_days = int(df["date"].nunique())
            logger.info(
                "Store %d: fetched %d rows, %d unique days.", store_id, len(df), unique_days
            )
            return df, unique_days
        except Exception as e:
            logger.error("Failed to fetch sales for store %d: %s", store_id, e)
            return None, 0

    def fetch_store_location(self, store_id: int) -> Tuple[Optional[float], Optional[float], Optional[str]]:
        """Fetch store lat/lon/country_code from the database."""
        engine = self._get_engine()
        if not engine:
            return None, None, None
        try:
            query = text("""
                SELECT Latitude, Longitude, CountryCode
                FROM Store
                WHERE Id = :store_id
                LIMIT 1
            """)
            row = pd.read_sql(query, engine, params={"store_id": store_id})
            if row.empty:
                return None, None, None
            lat = float(row.iloc[0]["Latitude"])
            lon = float(row.iloc[0]["Longitude"])
            cc = str(row.iloc[0]["CountryCode"]) if row.iloc[0]["CountryCode"] else None
            return lat, lon, cc
        except Exception as e:
            logger.error("Failed to fetch store location for %d: %s", store_id, e)
            return None, None, None

    # ------------------------------------------------------------------
    # Training
    # ------------------------------------------------------------------

    def train_store_models(self, store_id: int) -> Dict[str, Any]:
        """
        Train ML models for a given store.
        This is a BLOCKING call — run it in a background thread.
        Returns a summary dict.
        """
        with self._training_lock:
            if self._training_in_progress.get(store_id):
                return {"status": "already_training", "store_id": store_id}
            self._training_in_progress[store_id] = True

        try:
            logger.info("Starting training for store %d ...", store_id)

            df, unique_days = self.fetch_store_sales(store_id)
            if df is None or df.empty:
                return {"status": "error", "message": "No sales data found."}

            if unique_days < self.MIN_TRAINING_DAYS:
                return {
                    "status": "insufficient_data",
                    "message": f"Need at least {self.MIN_TRAINING_DAYS} days of data, found {unique_days}.",
                    "days_available": unique_days,
                }

            lat, lon, country_code = self.fetch_store_location(store_id)

            # Import training logic (heavy — only when needed)
            from training_logic_v2 import (
                PipelineConfig,
                process_dish,
                add_local_context,
            )
            import joblib

            config = PipelineConfig()
            model_dir = str(self.store_model_dir(store_id))
            config.model_dir = model_dir
            Path(model_dir).mkdir(parents=True, exist_ok=True)

            # Enrich with weather + holiday context
            if lat and lon and country_code:
                enriched_df, cc, _, _ = add_local_context(
                    df, address=None, latitude=lat, longitude=lon, country_code=country_code
                )
            else:
                enriched_df, cc, lat, lon = add_local_context(df, address="Shanghai, China")

            dishes = enriched_df["dish"].unique().tolist()
            dish_frames = {d: g.copy() for d, g in enriched_df.groupby("dish", sort=False)}

            champion_map: Dict[str, Dict[str, Any]] = {}
            trained = 0
            failed = 0
            total = len(dishes)

            # Initialize progress
            self._training_progress[store_id] = {
                "trained": 0,
                "failed": 0,
                "total": total,
                "current_dish": None,
            }

            for i, dish in enumerate(dishes):
                self._training_progress[store_id] = {
                    "trained": trained,
                    "failed": failed,
                    "total": total,
                    "current_dish": dish,
                }
                try:
                    result = process_dish(dish, dish_frames[dish], cc, config)
                    champion_map[dish] = {
                        "model": result["champion"],
                        "mae": result.get("champion_mae", 0.0),
                        "all_mae": result["mae"],
                    }
                    trained += 1
                except Exception as e:
                    logger.error("Store %d, dish '%s' failed: %s", store_id, dish, e)
                    failed += 1
                logger.info(
                    "Store %d training progress: %d/%d (failed: %d)",
                    store_id, trained + failed, total, failed,
                )

            # Save registry
            registry_path = Path(model_dir) / "champion_registry.pkl"
            joblib.dump(champion_map, str(registry_path))
            logger.info(
                "Store %d training complete: %d trained, %d failed.", store_id, trained, failed
            )

            # Reload into cache
            self.reload_store(store_id)

            return {
                "status": "completed",
                "store_id": store_id,
                "dishes_trained": trained,
                "dishes_failed": failed,
            }

        except Exception as e:
            logger.error("Training failed for store %d: %s", store_id, e)
            return {"status": "error", "message": str(e)}
        finally:
            self._training_in_progress[store_id] = False
            self._training_progress.pop(store_id, None)
