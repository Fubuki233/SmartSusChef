"""
SmartSusChef Training Orchestrator — Celery Task.

Encapsulates the full training pipeline (formerly Final_model_v2.py) as an
async Celery task that can be triggered via the FastAPI /train_models endpoint.
"""

from __future__ import annotations

import os

os.environ.setdefault("CMDSTANPY_LOG_LEVEL", "WARNING")

import logging
import traceback
from concurrent.futures import ProcessPoolExecutor, as_completed
from typing import Any, Dict, List, Optional

import joblib
import pandas as pd

from app.celery_app import celery_app
from training_logic_v2 import (
    PipelineConfig,
    process_dish,
    fetch_training_data,
    add_local_context,
    _silence_logs,
)

logger = logging.getLogger(__name__)

_silence_logs()


@celery_app.task(name="train_models", bind=True)
def train_models(
    self,
    address: str = "Shanghai, China",
    retrain_all: bool = True,
    sales_data_source: Optional[str] = None,
) -> Dict[str, Any]:
    """
    Execute the full training pipeline for all dishes.

    Parameters
    ----------
    address : str
        Restaurant location address for weather/holiday context.
    retrain_all : bool
        If True, retrain all dishes. If False, skip dishes that already
        have an entry in the champion registry.
    sales_data_source : Optional[str]
        Path to a CSV file for sales data. If None, uses the default
        behavior (database first, then 'food_sales_eng.csv' fallback).

    Returns
    -------
    dict
        Summary containing 'results_table' (list of row dicts),
        'trained_dishes' count, 'skipped_dishes' count, and 'errors' list.
    """
    config = PipelineConfig()
    os.makedirs(config.model_dir, exist_ok=True)

    # 1. Load sales data
    if sales_data_source is not None:
        logger.info("Loading sales data from provided source: %s", sales_data_source)
        raw_df = pd.read_csv(sales_data_source)
        raw_df["date"] = pd.to_datetime(raw_df["date"], format="%m/%d/%Y")
        raw_df["date"] = raw_df["date"].dt.normalize()
        raw_df = raw_df.groupby(["date", "dish"]).agg(sales=("sales", "sum")).reset_index()
        raw_df = raw_df.sort_values("date")
    else:
        raw_df = fetch_training_data()

    # 2. Enrich with location context (holidays + weather)
    enriched_df, country, lat, lon = add_local_context(raw_df, address)

    # 3. Determine which dishes to train
    unique_dishes = enriched_df["dish"].unique()
    dishes_to_train = list(unique_dishes)

    existing_registry: Dict[str, Any] = {}
    registry_path = f"{config.model_dir}/champion_registry.pkl"
    if not retrain_all:
        try:
            existing_registry = joblib.load(registry_path)
            dishes_to_train = [d for d in unique_dishes if d not in existing_registry]
            logger.info(
                "retrain_all=False: skipping %d dishes with existing models, training %d new dishes.",
                len(unique_dishes) - len(dishes_to_train),
                len(dishes_to_train),
            )
        except FileNotFoundError:
            logger.info(
                "No existing champion registry found. Training all %d dishes.",
                len(unique_dishes),
            )

    if len(dishes_to_train) == 0:
        logger.info("No dishes to train. All models are up to date.")
        return {
            "results_table": [],
            "trained_dishes": 0,
            "skipped_dishes": len(unique_dishes),
            "errors": [],
        }

    logger.info("=" * 95)
    logger.info(
        "STARTING HYBRID PROPHET + TREE TRAINING FOR %d DISHES (%d workers, %d Optuna trials each)",
        len(dishes_to_train),
        config.max_workers,
        config.n_optuna_trials,
    )
    logger.info("=" * 95)

    # 4. Run the full training pipeline in parallel for each dish
    results: List[Dict[str, Any]] = []
    errors: List[Dict[str, str]] = []
    total = len(dishes_to_train)

    with ProcessPoolExecutor(max_workers=config.max_workers) as executor:
        futures = {
            executor.submit(process_dish, dish, enriched_df, country, config): dish
            for dish in dishes_to_train
        }

        for idx, future in enumerate(as_completed(futures), 1):
            dish_name = futures[future]
            # Update Celery task progress metadata
            self.update_state(
                state="PROGRESS",
                meta={"current": idx, "total": total, "dish": dish_name},
            )
            try:
                result = future.result()
                results.append(result)
                mae = result["mae"]
                logger.info(
                    "  %s | X=%s C=%s L=%s -> Prophet+%s",
                    dish_name,
                    mae["xgboost"],
                    mae["catboost"],
                    mae["lightgbm"],
                    result["champion"].upper(),
                )
            except Exception as e:
                logger.error("  %s | FAILED: %s", dish_name, e)
                logger.error(traceback.format_exc())
                errors.append({"dish": dish_name, "error": str(e)})

    # 5. Aggregate results and build champion registry
    champion_map = dict(existing_registry)

    results_rows: List[Dict[str, Any]] = []
    for r in results:
        dish = r["dish"]
        champion_map[dish] = {
            "model": r["champion"],
            "mae": r.get("champion_mae", 0.0),
            "all_mae": r["mae"],
            "best_params": r["best_params"],
            "model_type": r.get("model_type", "hybrid"),
        }
        results_rows.append(
            {
                "Dish": dish,
                "XGBoost MAE": r["mae"]["xgboost"],
                "CatBoost MAE": r["mae"]["catboost"],
                "LightGBM MAE": r["mae"]["lightgbm"],
                "Winner": f"Prophet+{r['champion'].upper()}",
            }
        )

    # Save champion registry
    joblib.dump(champion_map, registry_path)

    logger.info("=" * 60)
    logger.info("MODEL LEADERBOARD (Lower MAE is Better)")
    logger.info("=" * 60)
    logger.info(
        "Training complete. %d dishes trained, %d errors.", len(results), len(errors)
    )

    return {
        "results_table": results_rows,
        "trained_dishes": len(results),
        "skipped_dishes": len(unique_dishes) - len(dishes_to_train),
        "errors": errors,
    }
