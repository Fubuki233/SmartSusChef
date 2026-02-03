# 批量菜品销量预测脚本说明（`Parallel(XP_all).py`）

该脚本用于**对 `food_sales.csv` 中出现的所有菜品逐一建模并预测未来销量**：每道菜使用 **Prophet + XGBoost 残差堆叠模型**，融合**滞后/滚动统计特征 + 日期特征 + 天气特征 + 假期特征**，并以递归方式预测未来 `HORIZON_DAYS` 天的销量。

---

## 1. 脚本做了什么

对每一道菜品（`菜品` 列）：

1. 汇总为按日销量序列（`ds`, `y`），并把缺失日期补齐（缺失按 0 处理）。
2. 合并天气特征（`rain_mm`, `avg_temp_c`, `avg_humidity_pct`），并对缺失做前向填充/均值填充。
3. 生成假期特征 `is_holiday`（日期落在任意假期窗口内则为 1）。
4. 生成日期特征：`dow/month/day/dayofyear/is_weekend`。
5. 生成滞后与滚动统计特征（例如 `y_lag_1`, `y_roll_mean_7` 等），并使用 `shift(1)` 防止泄漏。
6. 训练 **Prophet**：捕捉趋势/季节性/假期，并引入天气回归变量。
7. 训练 **XGBoost**：学习残差 `resid = y - prophet_yhat`（特征包含日期/天气/假期/lag/rolling + `prophet_yhat`）。
8. 未来预测使用**递归预测**：Prophet 给出未来趋势，XGBoost 递归预测残差并叠加。

---

## 2. 输入文件与格式要求

脚本默认读取同目录下文件（可在脚本顶部常量中修改路径）：

### 2.1 `food_sales.csv`

- 必需列：
  - `日期`：可被 `pandas.to_datetime` 解析的日期
  - `菜品`：菜品名称（字符串）
  - `销量`：数值

脚本会对同一天的同一道菜**求和**。

### 2.2 `df_weather.csv`

- 必需列：
  - `ds`：日期
- 建议列（缺失时会自动补成 NaN，再按逻辑填补）：
  - `rain_mm`
  - `avg_temp_c`
  - `avg_humidity_pct`

### 2.3 `df_holiday.csv`

- 必需列：
  - `date`：日期
  - `lower_window`：窗口下界（整数天数，可为 0）
  - `upper_window`：窗口上界（整数天数，可为 0）
- 假期名称列二选一：
  - `holiday_name` 或 `holiday`

脚本会将其整理为（`ds/holiday/lower_window/upper_window`）并计算 `is_holiday`。

### 2.4 （可选）`df_weather_future.csv`

用于未来 `HORIZON_DAYS` 天的天气外生变量：

- 必需列：`ds` + `REG_COLS`（默认三列天气特征）

如果该文件不存在、或列不全、或日期覆盖不足，脚本会用历史最后一天的天气值“平推”占位（仅适合本地演示；正式使用建议替换为真实预报/规划数据）。

---

## 3. 输出文件

脚本会在运行时创建输出目录 `outputs/`：

- `outputs/summary.csv`
  - 每道菜的验证 MAE（若数据不足则为空/NA）
  - 未来 `HORIZON_DAYS` 天预测总和、均值、预测起止日期
- `outputs/forecasts/<dish>.csv`
  - 每道菜未来预测明细：`ds`, `yhat`
  - 文件名会对菜名做“安全化”处理（去除非法字符、限制长度）
- `outputs/forecast_top{N}_bar.png`
  - TopN 菜品（按未来总销量）条形图
- `outputs/forecast_top{N}_lines.png`
  - TopN 菜品历史（近 `HISTORY_PLOT_DAYS` 天）+ 未来预测折线面板

---

## 4. 可调参数（脚本顶部）

在 `Parallel(XP_all).py` 顶部可以调整：

- `HORIZON_DAYS`：预测未来多少天（默认 14）
- `MAX_DISHES`：限制最多跑多少道菜（`None` 表示全部）
- `TOP_N_PLOT`：图里展示 TopN
- `HISTORY_PLOT_DAYS`：每道菜绘图回看的历史天数

特征相关：

- `REG_COLS`：天气特征列名列表
- `LAGS`：滞后阶数（如 1/7/14）
- `ROLL_WINDOWS`：滚动窗口大小（如 7/14/28）

输入输出路径：

- `DATA_FOOD/DATA_WEATHER/DATA_HOLIDAY/DATA_WEATHER_FUTURE`
- `OUT_DIR/OUT_FORECASTS_DIR`

### 4.1 固定参数组合（当前脚本内置）

脚本内已固定使用以下组合：

**Prophet**

```python
{
  "changepoint_prior_scale": 0.5,
  "daily_seasonality": False,
  "holidays_prior_scale": 10.0,
  "seasonality_mode": "additive",
  "seasonality_prior_scale": 10.0,
  "weekly_seasonality": True,
  "yearly_seasonality": False
}
```

**XGBoost**

```python
{
  "colsample_bytree": 0.9,
  "learning_rate": 0.05,
  "max_depth": 6,
  "n_estimators": 600,
  "reg_lambda": 1.0,
  "subsample": 0.9
}
```

---

## 5. 运行方式

在当前目录执行：

```bash
python3 "Parallel(XP_all).py"
```

依赖包（按脚本 import）：

- `numpy`
- `pandas`
- `scikit-learn`
- `xgboost`
- `matplotlib`
- `prophet`
- `seaborn`（可选；缺失也能运行）

---

## 6. 重要实现细节（理解结果用）

- **缺失日期如何处理**：单道菜的日序列会补齐所有日期，缺失销量视为 `0.0`。
- **天气缺失如何处理**：按列前向填充（`ffill`）→ 均值填充 → 仍缺则填 0。
- **假期特征**：只生成一个 `is_holiday`（0/1），只要日期落在任意假期窗口区间内即为 1。
- **滚动特征防泄漏**：滚动统计基于 `y.shift(1)` 计算，避免用到当天真实销量。
- **残差堆叠**：先用 Prophet 预测趋势，再由 XGBoost 预测残差，最终 `yhat = prophet_yhat + resid_hat`。
- **递归预测**：未来第 t 天的 lag/rolling 使用历史 + 已预测 `yhat` 迭代得到，所以未来误差会逐步累积（这是带自回归特征时的常见做法）。
- **验证方式**：当数据量足够时，用末尾 `HORIZON_DAYS` 天做简单验证，输出 `val_mae`；否则 `val_mae=None`。

---

## 7. 常见问题排查

- 报错“找不到数据文件”：确认 `food_sales.csv/df_weather.csv/df_holiday.csv` 位于脚本同目录，或修改脚本顶部 `DATA_*` 路径。
- 报错“缺少列”：按“输入文件与格式要求”检查列名（注意中文列名与大小写）。
- 未来天气结果看起来“太平”：若没有提供 `df_weather_future.csv`，脚本会用最后一天天气平推，占位性质较强。
- 图表中文显示异常：与本机 matplotlib 字体配置有关，可自行配置中文字体或改用英文标题。

---

## 8. 可选改进建议（按需）

- 用更可靠的时间序列验证方式（滚动验证/多折验证）替代单次“末尾切分”。
- 引入价格、促销、节令、店铺营业等更多外生特征。
- 未来天气使用真实预报/规划数据，避免“平推”导致特征不变。
