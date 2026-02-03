# SmartSus Chef Predictive Engine: A Technical Explanation

## 1. High-Level Goal

The primary goal of this project is to create a robust, automated, and high-performance forecasting engine for predicting daily food sales. The system is designed to handle multiple dishes, automatically select the best model for each one, and run the entire training process in parallel to ensure speed and efficiency.

Key technologies used:
- **Orchestration:** Python & Jupyter Notebook
- **Data Handling:** `pandas`
- **Modeling:** `Prophet`, `CatBoost`, `XGBoost`
- **Hyperparameter Optimization:** `Optuna`
- **Parallel Processing:** `concurrent.futures.ProcessPoolExecutor`

---

## 2. Architectural Overview

The project is intentionally split into two key files, following a "separation of concerns" principle.

-   `model.ipynb`: The **Orchestrator**. This notebook is the user-facing entry point. Its role is to manage the high-level workflow, run the pipeline, and visualize the results. It tells the *story* of the data and the model performance.
-   `training_logic.py`: The **Engine**. This Python script contains all the core logic for data processing, feature engineering, model evaluation, and training. It is designed as an importable module with no side effects.

### Architectural Rationale: Why This Structure?

The decision to separate the notebook from the core logic was driven by one critical requirement: **parallelism**.

1.  **The `multiprocessing` Challenge:** Python's `multiprocessing` library (which `ProcessPoolExecutor` uses) needs to "pickle" (serialize) functions and their arguments to send them to different CPU cores. Functions defined in the main scope of a Jupyter Notebook (`__main__`) cannot be reliably pickled. By moving all core functions to an importable `.py` file, we make them easily picklable, unlocking true parallel execution.
2.  **Maintainability & Reusability:** Code in a standalone `.py` file is significantly easier to version control with Git, lint for quality, and reuse in other parts of a larger application (e.g., a production API that imports these functions directly).
3.  **Clarity and Focus:** This separation keeps the notebook clean and focused on high-level orchestration and analysis. The notebook answers "what are we doing?", while the script answers "how are we doing it?".

### Pros and Cons of this Architecture

**Pros:**
-   **Performance:** Enables true parallel processing, reducing a potentially hours-long sequential run into minutes.
-   **Scalability:** New models or complex features can be added to the `.py` engine without cluttering the main notebook.
-   **Robustness:** Isolates the core logic from the notebook's execution state, preventing unexpected behavior.
-   **Developer-Friendly:** A clean separation of concerns makes the codebase easier to understand and maintain.

**Cons:**
-   **Slightly Higher Complexity:** Requires managing two files instead of putting everything in one place.
-   **Debugging:** Can require tracing execution flow between the notebook and the script, though this is a standard and valuable skill in software development.

---

## 3. Deep Dive: The End-to-End Workflow

The pipeline is executed by the `model.ipynb` notebook, but the logic resides in `training_logic.py`. Here is a step-by-step breakdown.

### Step 1: Configuration (`PipelineConfig` dataclass)

-   All key parameters (e.g., number of cross-validation folds, Optuna trials, model feature lists) are centralized in the `PipelineConfig` dataclass. This makes it easy to tune the entire pipeline's behavior from one place without digging through the code.

### Step 2: Data Ingestion & Context (`fetch_training_data`, `add_local_context`)

-   `fetch_training_data`: The system first attempts to connect to a production database. If that fails, it gracefully falls back to a local CSV file, ensuring the pipeline can always run. A critical step here is `df.groupby(['date', 'dish']).agg(sales=('sales', 'sum'))`, which ensures data integrity by aggregating records so there is only one row per dish, per day.
-   `add_local_context`: Forecasting models are highly sensitive to external factors. This function enriches the data by detecting the restaurant's location (and thus the correct public holidays) and simulating realistic weather data.

### Step 3: Data Sanitation & Feature Engineering (`sanitize_sparse_data`, `add_lag_features`)

-   `sanitize_sparse_data`: This function implements the "Anti-Lazy Employee" logic. If a dish wasn't sold on a particular day, it won't appear in the database. Instead of incorrectly assuming sales were zero, we reindex the data to create a complete timeline and use `interpolate()` to fill in the gaps. This provides a more realistic sales history.
-   `add_lag_features`: While Prophet handles time-series features internally, tree-based models like XGBoost and CatBoost require them to be explicitly engineered. This function creates dozens of powerful features, such as:
    -   **Lags:** Sales from 1, 7, 14, 21, and 28 days ago.
    -   **Rolling Averages:** Mean sales over the last 7 and 14 days.
    -   **Trend Indicators:** The ratio of the 7-day rolling mean to the 14-day rolling mean.

### Step 4: Backtesting & Hyperparameter Optimization

This is the core of the evaluation logic, where we determine the best model for each dish.

-   **`_generate_cv_folds`**: This function implements **expanding-window cross-validation**. For time-series data, random splits are invalid as they leak future information into the training set. This method creates multiple folds, where for each fold, the training set always comes before the test set. It's the gold standard for robustly evaluating forecasting models.
-   **`_optimize_*` functions**: Each model (`catboost`, `xgboost`, `prophet`) has its own optimization function.
    -   Inside each, an `objective` function is defined for **Optuna**.
    -   Optuna, a Bayesian optimization framework, intelligently searches the hyperparameter space to find the combination that yields the lowest Mean Absolute Error (MAE) during cross-validation. This is far more efficient than manual tuning.
-   **`evaluate_model`**: This is a simple dispatcher function that calls the appropriate optimizer based on the model type.

### Step 5: The Main Worker (`process_dish`)

This function is the heart of the parallel engine. It encapsulates the entire pipeline for a single dish.

1.  **Concurrency Fix:** It starts by creating a unique temporary directory and setting the `CMDSTAN_TMPDIR` environment variable. This is the critical fix that prevents `cmdstanpy` (Prophet's backend) from causing file-access errors when multiple instances are run in parallel.
2.  **Short-Data Guard:** It checks if the dish has enough historical data. If not, it skips the expensive ML training and defaults to a simple average.
3.  **Model Evaluation:** It calls `evaluate_model` for each of the three model types.
4.  **Champion Selection:** It compares the MAE from all three models and selects the best-performing one as the "champion".
5.  **Production Training:** It retrains all three models (Prophet, CatBoost, XGBoost) on 100% of the available data using the best hyperparameters found by Optuna and saves the final, trained model objects to disk as `.pkl` files.
6.  **Cleanup:** In a `finally` block, it safely removes the temporary directory created in step 1.

### Step 6: Orchestration (`model.ipynb`)

The main notebook kicks off the process within an `if __name__ == "__main__":` block.

-   It uses `ProcessPoolExecutor` to create a pool of worker processes (one for each CPU core).
-   It submits a `process_dish` job to the pool for every unique dish in the dataset.
-   `as_completed` is used to gracefully handle results as they finish, allowing for real-time progress updates in the console.
-   Finally, it aggregates the results from all the completed jobs into a summary table and creates visualizations.
