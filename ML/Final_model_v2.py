#!/usr/bin/env python
# coding: utf-8

# # SmartSus Chef: The Universal Predictive Engine
# **Version:** 4.0 (Hybrid Prophet + Tree) | **Architecture:** Prophet + Tree Residual Stacking
#
# ## How to read this Notebook
# This notebook runs a robust, parallelized ML pipeline to predict food demand using a
# Prophet + Tree Residual Stacking methodology. It acts as a high-level **orchestrator**,
# delegating the complex implementation details to the `training_logic_v2.py` module.
#
# ### The End-to-End Workflow:
# 1.  **Context Detection:** Automatically determines the restaurant's location to load correct holiday/weather data.
# 2.  **Data Ingestion:** Fetches the raw sales history from a database or a fallback CSV file.
# 3.  **Data Cleaning & Sanitation:** Aggregates data to have one record per dish-day and fills gaps.
# 4.  **Hybrid Model Training:** For each dish:
#     - Prophet models trend/seasonality/holidays with weather regressors
#     - Tree models (XGBoost, CatBoost, LightGBM) learn the Prophet residuals
#     - Optuna optimizes hyperparameters for each tree model
#     - The champion is selected based on lowest CV MAE
# 5.  **Parallel Execution:** Each dish is processed on a separate CPU core.
# 6.  **Forecasting:** Multi-day rolling forecast combines Prophet trend + tree residual predictions.
# 7.  **Explainability:** SHAP-based explanations show contribution of each feature group.

# In[ ]:

# --- IMPORTS & SETUP ---
from __future__ import annotations

import os
os.environ.setdefault("CMDSTANPY_LOG_LEVEL", "WARNING")

import pandas as pd
import joblib
import warnings
import logging
import traceback
from typing import Optional, Dict, Any, List

# Set up logging for this module
logger = logging.getLogger(__name__)
logger.setLevel(logging.INFO)
if not logger.handlers:
    _handler = logging.StreamHandler()
    _handler.setFormatter(logging.Formatter(
        "%(asctime)s [%(levelname)s] %(name)s: %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S"
    ))
    logger.addHandler(_handler)

from concurrent.futures import ProcessPoolExecutor, as_completed

try:
    from tqdm.notebook import tqdm
except Exception:
    try:
        from tqdm import tqdm
    except Exception:
        tqdm = None

# Import core pipeline logic from our module
from training_logic_v2 import (
    PipelineConfig,
    process_dish,
    fetch_training_data,
    add_local_context,
    _silence_logs,
)

# Silence verbose logging (centralized in training_logic_v2)
warnings.filterwarnings('ignore')
_silence_logs()

logger.info("Libraries loaded successfully.")


# ## Parallel Execution & Results
# Run all dishes in parallel using `ProcessPoolExecutor`. Each dish is independently
# processed across CPU cores using the hybrid Prophet + Tree methodology.


# --- CELERY TASK: TRAINING ORCHESTRATOR ---
from app.celery_app import celery_app


@celery_app.task(name="train_models")
def train_models(
    address: str = "Shanghai, China",
    retrain_all: bool = True,
    sales_data_source: Optional[str] = None,
) -> Dict[str, Any]:
    """
    Execute the full training pipeline for all dishes.

    This function encapsulates the complete training workflow and is designed
    to be called as a Celery task for asynchronous execution.

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
        Summary containing 'champion_registry', 'results_table' (list of row dicts),
        'trained_dishes' count, 'skipped_dishes' count, and 'errors' list.
    """
    config = PipelineConfig()
    os.makedirs(config.model_dir, exist_ok=True)

    # 1. Load sales data
    if sales_data_source is not None:
        logger.info("Loading sales data from provided source: %s", sales_data_source)
        raw_df = pd.read_csv(sales_data_source)
        raw_df['date'] = pd.to_datetime(raw_df['date'], format='%m/%d/%Y')
        raw_df['date'] = raw_df['date'].dt.normalize()
        raw_df = raw_df.groupby(['date', 'dish']).agg(sales=('sales', 'sum')).reset_index()
        raw_df = raw_df.sort_values('date')
    else:
        raw_df = fetch_training_data()

    # 2. Enrich with location context (holidays + weather)
    enriched_df, country, lat, lon = add_local_context(raw_df, address)

    # 3. Determine which dishes to train
    unique_dishes = enriched_df['dish'].unique()
    dishes_to_train = list(unique_dishes)

    existing_registry: Dict[str, Any] = {}
    registry_path = f'{config.model_dir}/champion_registry.pkl'
    if not retrain_all:
        try:
            existing_registry = joblib.load(registry_path)
            dishes_to_train = [d for d in unique_dishes if d not in existing_registry]
            logger.info(
                "retrain_all=False: skipping %d dishes with existing models, training %d new dishes.",
                len(unique_dishes) - len(dishes_to_train), len(dishes_to_train)
            )
        except FileNotFoundError:
            logger.info("No existing champion registry found. Training all %d dishes.", len(unique_dishes))

    if len(dishes_to_train) == 0:
        logger.info("No dishes to train. All models are up to date.")
        return {
            'champion_registry': existing_registry,
            'results_table': [],
            'trained_dishes': 0,
            'skipped_dishes': len(unique_dishes),
            'errors': [],
        }

    logger.info("=" * 95)
    logger.info(
        "STARTING HYBRID PROPHET + TREE TRAINING FOR %d DISHES (%d workers, %d Optuna trials each)",
        len(dishes_to_train), config.max_workers, config.n_optuna_trials
    )
    logger.info("=" * 95)

    # 4. Run the full training pipeline in parallel for each dish
    results: List[Dict[str, Any]] = []
    errors: List[Dict[str, str]] = []

    with ProcessPoolExecutor(max_workers=config.max_workers) as executor:
        futures = {
            executor.submit(process_dish, dish, enriched_df, country, config): dish
            for dish in dishes_to_train
        }

        if tqdm is not None:
            pbar = tqdm(as_completed(futures), total=len(dishes_to_train), desc="Training Dishes")
        else:
            pbar = as_completed(futures)

        for future in pbar:
            dish_name = futures[future]
            try:
                result = future.result()
                results.append(result)
                mae = result['mae']
                msg = (f"  {dish_name:<35} | X={mae['xgboost']:<7} C={mae['catboost']:<7} "
                       f"L={mae['lightgbm']:<7} -> Prophet+{result['champion'].upper()}")
                if tqdm is not None:
                    tqdm.write(msg)
                else:
                    logger.info(msg)
            except Exception as e:
                msg = f"  {dish_name:<35} | FAILED: {e}"
                if tqdm is not None:
                    tqdm.write(msg)
                else:
                    logger.error(msg)
                logger.error(traceback.format_exc())
                errors.append({'dish': dish_name, 'error': str(e)})

    # 5. Aggregate results and build champion registry
    champion_map = dict(existing_registry)  # Preserve existing entries if retrain_all=False

    results_rows: List[Dict[str, Any]] = []
    for r in results:
        dish = r['dish']
        champion_map[dish] = {
            'model': r['champion'],
            'mae': r.get('champion_mae', 0.0),
            'all_mae': r['mae'],
            'best_params': r['best_params'],
            'model_type': r.get('model_type', 'hybrid'),
        }
        results_rows.append({
            'Dish': dish,
            'XGBoost MAE': r['mae']['xgboost'],
            'CatBoost MAE': r['mae']['catboost'],
            'LightGBM MAE': r['mae']['lightgbm'],
            'Winner': f"Prophet+{r['champion'].upper()}"
        })

    # Save champion registry
    joblib.dump(champion_map, registry_path)

    logger.info("=" * 60)
    logger.info("MODEL LEADERBOARD (Lower MAE is Better)")
    logger.info("Model Type: Prophet + Tree Residual Stacking")
    logger.info("=" * 60)

    if results_rows:
        results_table = pd.DataFrame(results_rows)
        logger.info("\n%s", results_table.to_string(index=False))
    else:
        logger.warning("No results to display. Check for errors above.")

    logger.info("Training complete. %d dishes trained, %d errors.", len(results), len(errors))

    return {
        'champion_registry': champion_map,
        'results_table': results_rows,
        'trained_dishes': len(results),
        'skipped_dishes': len(unique_dishes) - len(dishes_to_train),
        'errors': errors,
    }
