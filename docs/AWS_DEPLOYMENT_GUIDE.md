# SmartSusChef AWS 部署指南

## 📋 目录

1. [架构概览](#架构概览)
2. [前置条件](#前置条件)
3. [AWS 资源配置](#aws-资源配置)
4. [GitHub 配置](#github-配置)
5. [部署流程](#部署流程)
6. [UAT 测试指南](#uat-测试指南)
7. [生产部署](#生产部署)
8. [监控与告警](#监控与告警)

---

## 🏗️ 架构概览

```
┌─────────────────────────────────────────────────────────────────┐
│                         AWS Cloud                                │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐    ┌─────────────────────────────────────┐     │
│  │   Route 53  │───▶│     Application Load Balancer       │     │
│  └─────────────┘    │  (HTTPS: 443, HTTP→HTTPS redirect)  │     │
│                     └──────────────┬──────────────────────┘     │
│                                    │                             │
│            ┌───────────────────────┴───────────────────────┐    │
│            │                                               │    │
│            ▼                                               ▼    │
│  ┌─────────────────┐                         ┌─────────────────┐│
│  │   Frontend      │                         │    Backend      ││
│  │   (ECS Fargate) │                         │   (ECS Fargate) ││
│  │   Port: 80      │                         │   Port: 8080    ││
│  └─────────────────┘                         └────────┬────────┘│
│                                                       │         │
│                                                       ▼         │
│                                              ┌─────────────────┐│
│                                              │   RDS MySQL     ││
│                                              │   (Private)     ││
│                                              └─────────────────┘│
│                                                                  │
│  ┌─────────────────┐    ┌─────────────────┐                     │
│  │  ECR Registry   │    │   CloudWatch    │                     │
│  │  (Container     │    │   (Logs &       │                     │
│  │   Images)       │    │    Metrics)     │                     │
│  └─────────────────┘    └─────────────────┘                     │
└─────────────────────────────────────────────────────────────────┘
```

### 环境说明

| 环境 | 分支 | URL | 用途 |
|------|------|-----|------|
| UAT | `develop` | https://uat.smartsuschef.com | 用户验收测试 |
| Production | `main` | https://smartsuschef.com | 生产环境 |

---

## 🔧 前置条件

### 1. 安装必要工具

```bash
# AWS CLI
curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
unzip awscliv2.zip
sudo ./aws/install

# Terraform
wget https://releases.hashicorp.com/terraform/1.6.0/terraform_1.6.0_linux_amd64.zip
unzip terraform_1.6.0_linux_amd64.zip
sudo mv terraform /usr/local/bin/

# Docker
sudo apt-get update
sudo apt-get install docker.io docker-compose
```

### 2. AWS 账户设置

1. 创建 AWS 账户（如果没有）
2. 创建 IAM 用户用于 Terraform
3. 配置 AWS CLI：

```bash
aws configure
# AWS Access Key ID: <your-access-key>
# AWS Secret Access Key: <your-secret-key>
# Default region name: ap-southeast-1
# Default output format: json
```

---

## ☁️ AWS 资源配置

### Step 1: 创建 Terraform State Backend

```bash
# 创建 S3 bucket 存储 Terraform state
aws s3 mb s3://smartsuschef-terraform-state --region ap-southeast-1

# 启用版本控制
aws s3api put-bucket-versioning \
  --bucket smartsuschef-terraform-state \
  --versioning-configuration Status=Enabled

# 创建 DynamoDB table 用于 state locking
aws dynamodb create-table \
  --table-name smartsuschef-terraform-lock \
  --attribute-definitions AttributeName=LockID,AttributeType=S \
  --key-schema AttributeName=LockID,KeyType=HASH \
  --billing-mode PAY_PER_REQUEST \
  --region ap-southeast-1
```

### Step 2: 创建 GitHub Actions OIDC Provider

```bash
# 获取 GitHub OIDC provider thumbprint
THUMBPRINT=$(echo | openssl s_client -servername token.actions.githubusercontent.com -connect token.actions.githubusercontent.com:443 2>/dev/null | openssl x509 -fingerprint -noout | cut -d'=' -f2 | tr -d ':' | tr '[:upper:]' '[:lower:]')

# 创建 OIDC provider
aws iam create-open-id-connect-provider \
  --url https://token.actions.githubusercontent.com \
  --client-id-list sts.amazonaws.com \
  --thumbprint-list $THUMBPRINT
```

### Step 3: 创建 IAM Role for GitHub Actions

创建文件 `trust-policy.json`:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Federated": "arn:aws:iam::<AWS_ACCOUNT_ID>:oidc-provider/token.actions.githubusercontent.com"
      },
      "Action": "sts:AssumeRoleWithWebIdentity",
      "Condition": {
        "StringEquals": {
          "token.actions.githubusercontent.com:aud": "sts.amazonaws.com"
        },
        "StringLike": {
          "token.actions.githubusercontent.com:sub": "repo:Fubuki233/SmartSusChef:*"
        }
      }
    }
  ]
}
```

```bash
# 创建 IAM role
aws iam create-role \
  --role-name GitHubActionsSmartSusChef \
  --assume-role-policy-document file://trust-policy.json

# 附加必要权限
aws iam attach-role-policy \
  --role-name GitHubActionsSmartSusChef \
  --policy-arn arn:aws:iam::aws:policy/AmazonEC2ContainerRegistryFullAccess

aws iam attach-role-policy \
  --role-name GitHubActionsSmartSusChef \
  --policy-arn arn:aws:iam::aws:policy/AmazonECS_FullAccess
```

### Step 4: 部署 UAT 环境基础设施

```bash
cd infrastructure/terraform

# 初始化 Terraform
terraform init

# 创建 UAT 环境
terraform workspace new uat
terraform workspace select uat

# 创建变量文件
cat > terraform.tfvars <<EOF
environment = "uat"
db_password = "your-secure-password-here"
jwt_secret  = "your-jwt-secret-key-here"
EOF

# 预览变更
terraform plan -var-file=terraform.tfvars

# 应用变更
terraform apply -var-file=terraform.tfvars
```

### Step 5: 部署 Production 环境基础设施

```bash
# 创建 Production 环境
terraform workspace new production
terraform workspace select production

# 创建变量文件
cat > terraform-prod.tfvars <<EOF
environment = "production"
db_password = "your-secure-prod-password"
jwt_secret  = "your-jwt-secret-key"
EOF

# 应用变更
terraform apply -var-file=terraform-prod.tfvars
```

---

## 🔐 GitHub 配置

### Step 1: 配置 Repository Secrets

在 GitHub 仓库中配置以下 Secrets（Settings → Secrets and variables → Actions）：

| Secret Name | 描述 | 示例值 |
|-------------|------|--------|
| `AWS_ROLE_ARN` | GitHub Actions IAM Role ARN | `arn:aws:iam::123456789:role/GitHubActionsSmartSusChef` |
| `SLACK_WEBHOOK_URL` | Slack 通知 Webhook（可选）| `https://hooks.slack.com/...` |

### Step 2: 配置 Environments

1. 进入 Repository Settings → Environments
2. 创建 `uat` 环境
3. 创建 `production` 环境，配置：
   - ✅ Required reviewers（需要审批）
   - ✅ Wait timer: 5 minutes

### Step 3: 配置分支保护规则

Settings → Branches → Add branch protection rule:

**For `main` branch:**
- ✅ Require a pull request before merging
- ✅ Require approvals (1)
- ✅ Require status checks to pass
- ✅ Require branches to be up to date

**For `develop` branch:**
- ✅ Require status checks to pass

---

## 🚀 部署流程

### 开发流程

```
feature/* branch → develop branch → main branch
     │                  │                │
     │                  ▼                ▼
     │            Deploy to UAT    Deploy to Production
     │
     └── CI: Build & Test only
```

### CI/CD Pipeline 阶段

```
┌─────────────────┐
│  Security Scan  │  ← CodeQL + Trivy 安全扫描
└────────┬────────┘
         │
    ┌────┴────┐
    ▼         ▼
┌───────┐ ┌───────┐
│Backend│ │Frontend│  ← 并行构建 & 测试
│ Build │ │ Build │
└───┬───┘ └───┬───┘
    └────┬────┘
         ▼
┌─────────────────┐
│  Docker Build   │  ← 构建镜像 & 推送到 ECR
│  & Push to ECR  │
└────────┬────────┘
         │
    ┌────┴────────┐
    ▼             ▼
┌───────┐   ┌──────────┐
│ UAT   │   │Production│  ← 根据分支部署
│Deploy │   │  Deploy  │
└───────┘   └──────────┘
```

### 手动部署（如需要）

```bash
# 登录 ECR
aws ecr get-login-password --region ap-southeast-1 | docker login --username AWS --password-stdin <account-id>.dkr.ecr.ap-southeast-1.amazonaws.com

# 构建并推送 Backend
docker build -t smartsuschef-backend ./backend
docker tag smartsuschef-backend:latest <account-id>.dkr.ecr.ap-southeast-1.amazonaws.com/smartsuschef-backend:latest
docker push <account-id>.dkr.ecr.ap-southeast-1.amazonaws.com/smartsuschef-backend:latest

# 构建并推送 Frontend
docker build -t smartsuschef-frontend ./frontend
docker tag smartsuschef-frontend:latest <account-id>.dkr.ecr.ap-southeast-1.amazonaws.com/smartsuschef-frontend:latest
docker push <account-id>.dkr.ecr.ap-southeast-1.amazonaws.com/smartsuschef-frontend:latest

# 更新 ECS 服务
aws ecs update-service --cluster smartsuschef-uat-cluster --service smartsuschef-uat-backend-service --force-new-deployment
aws ecs update-service --cluster smartsuschef-uat-cluster --service smartsuschef-uat-frontend-service --force-new-deployment
```

---

## 🧪 UAT 测试指南

### 测试清单

| 功能 | 测试步骤 | 预期结果 |
|------|---------|---------|
| 登录 | 输入凭据点击登录 | 跳转到 Dashboard |
| 销售数据 | 添加/编辑/删除销售记录 | 数据正确保存 |
| 食材管理 | CRUD 操作 | 数据正确处理 |
| 菜谱管理 | 创建带食材的菜谱 | 关联正确 |
| 天气显示 | 查看 Dashboard | 显示当前天气 |
| 预测功能 | 查看销售预测 | 显示预测数据 |
| 导出功能 | 导出 CSV/PDF | 文件正确下载 |

### UAT 环境访问

- **前端**: https://uat.smartsuschef.com
- **API**: https://uat.smartsuschef.com/api
- **Health Check**: https://uat.smartsuschef.com/api/health

### UAT 测试账号

| 角色 | 用户名 | 密码 |
|------|--------|------|
| Manager | manager@test.com | Test123! |
| Employee | employee@test.com | Test123! |

---

## 🏭 生产部署

### 部署检查清单

- [ ] UAT 测试全部通过
- [ ] 代码审查完成
- [ ] 数据库迁移脚本准备好
- [ ] 回滚计划准备好
- [ ] 通知相关人员

### 部署步骤

1. 创建 PR: `develop` → `main`
2. 完成代码审查
3. 合并 PR
4. CI/CD 自动触发 Production 部署
5. 等待 Environment 审批（如配置）
6. 验证 Production 环境

### 回滚步骤

```bash
# 获取上一个稳定版本的 task definition
aws ecs describe-task-definition --task-definition smartsuschef-prod-backend:<previous-version>

# 更新服务使用旧版本
aws ecs update-service \
  --cluster smartsuschef-prod-cluster \
  --service smartsuschef-prod-backend-service \
  --task-definition smartsuschef-prod-backend:<previous-version>
```

---

## 📊 监控与告警

### CloudWatch Dashboard

1. 进入 AWS Console → CloudWatch → Dashboards
2. 创建 "SmartSusChef-Monitoring" dashboard
3. 添加以下 widgets:
   - ECS CPU/Memory 使用率
   - ALB 请求数和延迟
   - RDS 连接数和 CPU
   - Error 日志计数

### 配置告警

```bash
# CPU 使用率告警
aws cloudwatch put-metric-alarm \
  --alarm-name "SmartSusChef-Backend-HighCPU" \
  --metric-name CPUUtilization \
  --namespace AWS/ECS \
  --statistic Average \
  --period 300 \
  --threshold 80 \
  --comparison-operator GreaterThanThreshold \
  --evaluation-periods 2 \
  --dimensions Name=ClusterName,Value=smartsuschef-prod-cluster Name=ServiceName,Value=smartsuschef-prod-backend-service \
  --alarm-actions <SNS_TOPIC_ARN>
```

### 日志查看

```bash
# 查看 Backend 日志
aws logs tail /ecs/smartsuschef-prod-backend --follow

# 查看 Frontend 日志
aws logs tail /ecs/smartsuschef-prod-frontend --follow
```

---

## 💰 成本估算

### UAT 环境（月）

| 资源 | 规格 | 预估成本 |
|------|------|---------|
| ECS Fargate | 2 tasks × 0.25 vCPU | ~$15 |
| RDS MySQL | db.t3.micro | ~$15 |
| ALB | 1 个 | ~$20 |
| NAT Gateway | 1 个 | ~$35 |
| ECR | 存储 | ~$5 |
| **总计** | | **~$90/月** |

### Production 环境（月）

| 资源 | 规格 | 预估成本 |
|------|------|---------|
| ECS Fargate | 4 tasks × 0.5 vCPU | ~$60 |
| RDS MySQL | db.t3.small (Multi-AZ) | ~$50 |
| ALB | 1 个 | ~$20 |
| NAT Gateway | 2 个 | ~$70 |
| ECR | 存储 | ~$5 |
| CloudWatch | Logs & Metrics | ~$10 |
| **总计** | | **~$215/月** |

---

## 🆘 常见问题

### Q: ECS 服务启动失败？

检查 CloudWatch Logs 查看具体错误：
```bash
aws logs tail /ecs/smartsuschef-uat-backend --since 1h
```

### Q: 数据库连接失败？

确认安全组规则允许 ECS 访问 RDS：
```bash
aws ec2 describe-security-groups --group-ids <rds-security-group-id>
```

### Q: GitHub Actions 权限错误？

确认 IAM Role 的 trust policy 正确配置了仓库名。

---

## 📞 联系方式

如有问题，请联系 DevOps 团队或在 GitHub Issues 中提出。
