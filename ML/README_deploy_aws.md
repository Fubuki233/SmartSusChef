# FastAPI + AWS 部署说明（ML）

本文档对应 `ML` 目录下的部署文件：

- `app/main.py` FastAPI 入口
- `app/inference.py` 模型加载与推理
- `Dockerfile` 容器构建
- `aws/task-definition.json` ECS 任务定义模板
- `.github/workflows/deploy-ecs.yml` GitHub Actions CI/CD

## 1. 先决条件

1. 你已经有可用模型文件：
   - `models/champion_registry.pkl`
   - `models/prophet_<dish>.pkl`
   - `models/<champion>_<dish>.pkl`
2. AWS 侧已创建：
   - ECR 仓库
   - ECS Fargate 集群与服务
   - 任务执行角色（含 ECR 拉取与 CloudWatch Logs 权限）

## 2. 本地启动验证

```bash
pip install -r requirements.txt
uvicorn app.main:app --host 0.0.0.0 --port 8000
```

验证接口：

- `GET /health`
- `GET /dishes`
- `POST /predict`

## 3. /predict 请求示例

```json
{
  "dish": "Kung Pao Chicken",
  "recent_sales": [30, 28, 35, 40, 38, 42, 36, 39, 41, 37],
  "horizon_days": 14,
  "address": "Shanghai, China"
}
```

可选字段：`start_date`, `latitude`, `longitude`, `country_code`, `weather_rows`。

## 4. AWS 文件需要替换的占位符

### 4.1 `aws/task-definition.json`

替换：

- `<ACCOUNT_ID>`
- `<REGION>`
- ECR 仓库名
- 日志组名（可沿用默认）

### 4.2 GitHub Secrets（Actions）

在仓库里配置：

- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`
- `AWS_REGION`
- `ECR_REPOSITORY`
- `ECS_CLUSTER`
- `ECS_SERVICE`

## 5. CI/CD 触发方式

- push 到 `main` 自动触发
- 或手动触发 `workflow_dispatch`

流程：

1. 构建 Docker 镜像
2. 推送到 ECR
3. 渲染 ECS task definition（注入镜像 tag）
4. 更新 ECS service 并等待稳定

## 6. 运行时配置

默认读取环境变量：

- `MODEL_DIR`（默认 `models`）

如果模型放在其他路径，给容器设置 `MODEL_DIR`。

## 7. 生产建议

1. 把模型文件放在镜像内（简单）或挂载 EFS/S3 拉取（更灵活）。
2. 为 ECS Service 配置 ALB + 健康检查 `/health`。
3. 设置 CloudWatch 告警（5xx、CPU、内存）。
4. 给 `POST /predict` 增加鉴权（API Gateway 或应用层 token）。
