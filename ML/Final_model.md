# Final_model 工作流说明

本文档解释 `Final_model.py` 的整体工作流、关键实现逻辑，以及并行机制是如何组织的。

## 1. 目标与产出

`Final_model.py` 的目标是对每道菜做混合建模并输出未来预测：

1. Prophet 建模主趋势（含天气回归变量）。
2. Tree 模型（XGBoost / CatBoost / LightGBM）学习残差。
3. Optuna 为每个树模型调参并选冠军模型。
4. 保存每道菜的 Prophet + 冠军树模型。
5. 生成未来 14 天预测、汇总表和图表。

输出主要在：

- `models/`：每道菜的模型文件、冠军注册表
- `outputs/summary.csv`：汇总预测结果
- `outputs/forecasts/*.csv`：每道菜未来预测
- `outputs/*.png`：Top 菜品图表

## 2. 工作流实现（端到端）

### 2.1 数据准备

1. 读取销售数据（MySQL，失败则回退 CSV）。
2. 增强上下文特征：
   - 节假日特征
   - 天气特征（优先 DB，失败回退 Open-Meteo）
3. 在 `main()` 中一次性获取未来天气预测（避免每道菜重复请求）。

### 2.2 单菜训练 `process_dish`

每个菜品独立执行以下步骤：

1. 稀疏数据修复（补全日期、插值等）。
2. 构建日期特征 + 滞后/滚动特征。
3. 预计算 CV fold 缓存：
   - 每个 fold 只拟合一次 Prophet
   - 缓存残差学习所需的 `X_train/X_test/y_train/y_test` 与 `prophet_yhat`
4. 对三类树模型分别做 Optuna 调参。
5. 选择 MAE 最小的冠军模型。
6. 用全量样本重训冠军模型并保存。

### 2.3 未来预测 `_predict_future`

对每道菜进行逐日递归预测（horizon=14）：

1. 用已训练 Prophet 先得到未来趋势项 `prophet_yhat`。
2. 使用历史销量递归构造 lag/rolling 特征。
3. 冠军树模型预测残差。
4. 最终预测 = `max(0, prophet_yhat + residual_hat)`。
5. 将当天预测回写到历史，供下一天 lag 特征使用。

## 3. 我们的并行链接（Parallel Pipeline）

这套并行不是单点并行，而是“多层协调”：

### 3.1 外层并行：按菜品多进程

- 使用 `ProcessPoolExecutor(max_workers=CFG.max_workers)`。
- 一个进程处理一个菜品的完整训练流程。
- 这能把“菜品间相互独立”最大化利用起来。

### 3.2 内层线程策略：避免过度抢核

- 外层已经多进程并行时，XGBoost/LightGBM 的 `n_jobs` 收敛到 1。
- 避免“每个进程都再吃满全核”导致 oversubscription。
- 这样通常更稳定，整体吞吐更高。

### 3.3 调参阶段去冗余并行

- 过去每个 trial/fold 会重复拟合 Prophet，成本极高。
- 现在先做 fold 缓存（每 fold 仅一次 Prophet），Optuna trial 只训练树模型。
- 本质是把可复用计算从“trial 内部”提到“trial 外部”。

### 3.4 运行可观测性：进度条 + 心跳

- 训练阶段：`Train Dishes` 进度条。
- 预测阶段：`Forecast Dishes` 进度条。
- 若训练长时间无任务完成，会输出心跳：
  - `[TRAIN] still running... x/y done | elapsed=...s`
- 这样可以避免“控制台看起来卡住不动”。

## 4. 你如何使用这套流程

## 4.1 直接运行

```bash
python3 Final_model.py
```

## 4.2 运行时你会看到

1. 启动信息（菜品数、trial 数）。
2. 训练进度条 + 每个菜品完成后的 MAE/冠军模型日志。
3. 预测进度条。
4. 完成信息 `Done.`

## 4.3 你应重点查看

1. `models/champion_registry.pkl`：每道菜最终模型及参数。
2. `outputs/summary.csv`：未来 14 天销量汇总。
3. `outputs/forecasts/*.csv`：单菜品逐日预测。

## 5. 一句话总结

当前版本通过“按菜品并行 + Prophet fold 缓存 + 线程收敛 + 进度心跳”实现了更快、更稳定、可观测的混合预测工作流。

## 6. 链接原理（TS + ML）

我们的 TS（Prophet）与 ML（树模型）采用“残差堆叠”链接：

1. TS 先建趋势  
   - `Prophet` 训练并输出趋势预测 `prophet_yhat`（含天气回归变量）。
2. 构造残差标签  
   - `resid = sales - prophet_yhat`。
   - 同时把 `prophet_yhat` 写入特征，作为 ML 侧可用输入。
3. ML 学习残差  
   - XGBoost / CatBoost / LightGBM 以 `resid` 为目标学习误差结构。
   - 特征包含：时间/节假日/天气/滞后滚动特征 + `prophet_yhat`。
4. 预测时相加  
   - 先用 Prophet 得到未来 `prophet_yhat_future`；
   - 树模型预测残差 `resid_hat`；
   - 最终 `yhat = max(0, prophet_yhat + resid_hat)`。

一句话：TS 负责趋势与季节性，ML 负责补残差，两者相加得到最终预测。
