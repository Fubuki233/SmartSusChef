# AWS Infrastructure for SmartSusChef
# Terraform configuration for ECS Fargate deployment

terraform {
  required_version = ">= 1.0"
  
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  # backend "s3" {
  #   bucket         = "smartsuschef-terraform-state"
  #   key            = "infrastructure/terraform.tfstate"
  #   region         = "ap-southeast-1"
  #   encrypt        = true
  #   use_lockfile   = true
  # }
}

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = "SmartSusChef"
      Environment = var.environment
      ManagedBy   = "Terraform"
    }
  }
}

# ==========================================
# Variables
# ==========================================
variable "aws_region" {
  description = "AWS region"
  default     = "ap-southeast-1"
}

variable "environment" {
  description = "Environment name"
  type        = string
}

variable "db_password" {
  description = "Database password"
  type        = string
  sensitive   = true
}

variable "jwt_secret" {
  description = "JWT secret key"
  type        = string
  sensitive   = true
}

# ==========================================
# VPC Configuration
# ==========================================
module "vpc" {
  source  = "terraform-aws-modules/vpc/aws"
  version = "~> 5.0"

  name = "smartsuschef-${var.environment}-vpc"
  cidr = "10.0.0.0/16"

  azs             = ["${var.aws_region}a", "${var.aws_region}b"]
  private_subnets = ["10.0.1.0/24", "10.0.2.0/24"]
  public_subnets  = ["10.0.101.0/24", "10.0.102.0/24"]

  enable_nat_gateway     = true
  single_nat_gateway     = var.environment == "uat" ? true : false
  enable_dns_hostnames   = true
  enable_dns_support     = true

  tags = {
    Environment = var.environment
  }
}

# ==========================================
# Security Groups
# ==========================================
resource "aws_security_group" "alb" {
  name        = "smartsuschef-${var.environment}-alb-sg"
  description = "Security group for ALB"
  vpc_id      = module.vpc.vpc_id

  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_security_group" "ecs" {
  name        = "smartsuschef-${var.environment}-ecs-sg"
  description = "Security group for ECS tasks"
  vpc_id      = module.vpc.vpc_id

  ingress {
    from_port       = 0
    to_port         = 0
    protocol        = "-1"
    security_groups = [aws_security_group.alb.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_security_group" "rds" {
  name        = "smartsuschef-${var.environment}-rds-sg"
  description = "Security group for RDS"
  vpc_id      = module.vpc.vpc_id

  ingress {
    from_port       = 3306
    to_port         = 3306
    protocol        = "tcp"
    security_groups = [aws_security_group.ecs.id]
  }
}

# ==========================================
# RDS MySQL Database
# ==========================================
resource "aws_db_subnet_group" "main" {
  name       = "smartsuschef-${var.environment}-db-subnet"
  subnet_ids = module.vpc.private_subnets
}

resource "aws_db_instance" "mysql" {
  identifier     = "smartsuschef-${var.environment}-db"
  engine         = "mysql"
  engine_version = "8.0"
  instance_class = var.environment == "uat" ? "db.t3.micro" : "db.t3.small"

  allocated_storage     = 20
  max_allocated_storage = 100
  storage_type          = "gp3"
  storage_encrypted     = true

  db_name  = "smartsuschef"
  username = "smartsuschef"
  password = var.db_password

  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = [aws_security_group.rds.id]

  backup_retention_period = var.environment == "production" ? 7 : 1
  skip_final_snapshot     = var.environment == "uat" ? true : false
  deletion_protection     = var.environment == "production" ? true : false

  tags = {
    Name = "smartsuschef-${var.environment}-db"
  }
}

# ==========================================
# ElastiCache (Redis)
# ==========================================
resource "aws_security_group" "redis" {
  name        = "smartsuschef-${var.environment}-redis-sg"
  description = "Security group for ElastiCache Redis"
  vpc_id      = module.vpc.vpc_id

  ingress {
    from_port       = 6379
    to_port         = 6379
    protocol        = "tcp"
    security_groups = [aws_security_group.ecs.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_elasticache_subnet_group" "main" {
  name       = "smartsuschef-${var.environment}-redis-subnet-group"
  subnet_ids = module.vpc.private_subnets
}

resource "aws_elasticache_replication_group" "main" {
  replication_group_id       = "smartsuschef-${var.environment}-redis-rg"
  description                = "SmartSusChef Redis Replication Group"
  node_type                  = var.environment == "production" ? "cache.t3.medium" : "cache.t3.micro"
  num_cache_clusters         = var.environment == "production" ? 2 : 1
  engine                     = "redis"
  engine_version             = "6.x"
  port                       = 6379
  subnet_group_name          = aws_elasticache_subnet_group.main.name
  security_group_ids         = [aws_security_group.redis.id]
  parameter_group_name       = "default.redis6.x"
  automatic_failover_enabled = true
  transit_encryption_enabled = true
  at_rest_encryption_enabled = true
  snapshot_retention_limit   = var.environment == "production" ? 7 : 0
}

# ==========================================
# EFS for ML Models
# ==========================================
resource "aws_security_group" "efs" {
  name        = "smartsuschef-${var.environment}-efs-sg"
  description = "Security group for EFS"
  vpc_id      = module.vpc.vpc_id

  ingress {
    from_port       = 2049
    to_port         = 2049
    protocol        = "tcp"
    security_groups = [aws_security_group.ecs.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_efs_file_system" "models" {
  creation_token   = "smartsuschef-${var.environment}-ml-models"
  performance_mode = "generalPurpose"
  throughput_mode  = "bursting"
  encrypted        = true

  tags = {
    Name = "smartsuschef-${var.environment}-ml-models"
  }
}

resource "aws_efs_mount_target" "models_mount_targets" {
  for_each        = toset(module.vpc.private_subnets)
  file_system_id  = aws_efs_file_system.models.id
  subnet_id       = each.key
  security_groups = [aws_security_group.efs.id]
}

resource "aws_efs_access_point" "models_access_point" {
  file_system_id = aws_efs_file_system.models.id

  posix_user {
    uid = 1000
    gid = 1000
  }

  root_directory {
    path = "/models"
    creation_info {
      owner_uid   = 1000
      owner_gid   = 1000
      permissions = "0755"
    }
  }
}

# ==========================================
# ECR Repositories
# ==========================================
resource "aws_ecr_repository" "backend" {
  name                 = "smartsuschef-backend"
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }
}

resource "aws_ecr_repository" "frontend" {
  name                 = "smartsuschef-frontend"
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }
}

resource "aws_ecr_repository" "ml_api" {
  name                 = "smartsuschef-ml-api"
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }
}

# ==========================================
# ECS Cluster
# ==========================================
resource "aws_ecs_cluster" "main" {
  name = "smartsuschef-${var.environment}-cluster"

  setting {
    name  = "containerInsights"
    value = "enabled"
  }
}

resource "aws_ecs_cluster_capacity_providers" "main" {
  cluster_name = aws_ecs_cluster.main.name

  capacity_providers = ["FARGATE", "FARGATE_SPOT"]

  default_capacity_provider_strategy {
    base              = 1
    weight            = 100
    capacity_provider = var.environment == "uat" ? "FARGATE_SPOT" : "FARGATE"
  }
}

# ==========================================
# Application Load Balancer
# ==========================================
resource "aws_lb" "main" {
  name               = "smartsuschef-${var.environment}-alb"
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb.id]
  subnets            = module.vpc.public_subnets

  enable_deletion_protection = var.environment == "production" ? true : false
}

resource "aws_lb_target_group" "backend" {
  name        = "ssc-${var.environment}-be-tg"
  port        = 8080
  protocol    = "HTTP"
  vpc_id      = module.vpc.vpc_id
  target_type = "ip"

  health_check {
    enabled             = true
    healthy_threshold   = 2
    interval            = 30
    matcher             = "200"
    path                = "/health"
    port                = "traffic-port"
    protocol            = "HTTP"
    timeout             = 5
    unhealthy_threshold = 3
  }
}

resource "aws_lb_target_group" "frontend" {
  name        = "ssc-${var.environment}-fe-tg"
  port        = 80
  protocol    = "HTTP"
  vpc_id      = module.vpc.vpc_id
  target_type = "ip"

  health_check {
    enabled             = true
    healthy_threshold   = 2
    interval            = 30
    matcher             = "200"
    path                = "/"
    port                = "traffic-port"
    protocol            = "HTTP"
    timeout             = 5
    unhealthy_threshold = 3
  }
}

# HTTPS Listener - Disabled until ACM certificate is validated
# To enable HTTPS:
# 1. Validate the ACM certificate by adding DNS records to your domain
# 2. Uncomment the https listener and update api rule to use it
#
# resource "aws_lb_listener" "https" {
#   load_balancer_arn = aws_lb.main.arn
#   port              = "443"
#   protocol          = "HTTPS"
#   ssl_policy        = "ELBSecurityPolicy-TLS13-1-2-2021-06"
#   certificate_arn   = aws_acm_certificate.main.arn
#
#   default_action {
#     type             = "forward"
#     target_group_arn = aws_lb_target_group.frontend.arn
#   }
# }

# HTTP Listener - Direct forwarding (use this until HTTPS is configured)
resource "aws_lb_listener" "http" {
  load_balancer_arn = aws_lb.main.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.frontend.arn
  }
}

resource "aws_lb_listener_rule" "api" {
  listener_arn = aws_lb_listener.http.arn
  priority     = 100

  action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.backend.arn
  }

  condition {
    path_pattern {
      values = ["/api/*"]
    }
  }
}

# ==========================================
# ACM Certificate
# ==========================================
resource "aws_acm_certificate" "main" {
  domain_name               = var.environment == "production" ? "smartsuschef.com" : "uat.smartsuschef.com"
  subject_alternative_names = var.environment == "production" ? ["*.smartsuschef.com"] : []
  validation_method         = "DNS"

  lifecycle {
    create_before_destroy = true
  }
}

# ==========================================
# IAM Roles for ECS
# ==========================================
resource "aws_iam_role" "ecs_task_execution" {
  name = "smartsuschef-${var.environment}-ecs-task-execution"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "ecs-tasks.amazonaws.com"
      }
    }]
  })
}

resource "aws_iam_role_policy_attachment" "ecs_task_execution" {
  role       = aws_iam_role.ecs_task_execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

resource "aws_iam_role" "ecs_task" {
  name = "smartsuschef-${var.environment}-ecs-task"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "ecs-tasks.amazonaws.com"
      }
    }]
  })
}

# ==========================================
# CloudWatch Log Groups
# ==========================================
resource "aws_cloudwatch_log_group" "backend" {
  name              = "/ecs/smartsuschef-${var.environment}-backend"
  retention_in_days = var.environment == "production" ? 30 : 7
}

resource "aws_cloudwatch_log_group" "frontend" {
  name              = "/ecs/smartsuschef-${var.environment}-frontend"
  retention_in_days = var.environment == "production" ? 30 : 7
}

# ==========================================
# ECS Task Definitions
# ==========================================
resource "aws_ecs_task_definition" "backend" {
  family                   = "smartsuschef-${var.environment}-backend"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.environment == "uat" ? "256" : "512"
  memory                   = var.environment == "uat" ? "512" : "1024"
  execution_role_arn       = aws_iam_role.ecs_task_execution.arn
  task_role_arn            = aws_iam_role.ecs_task.arn

  container_definitions = jsonencode([{
    name  = "smartsuschef-backend"
    image = "${aws_ecr_repository.backend.repository_url}:latest"
    
    portMappings = [{
      containerPort = 8080
      protocol      = "tcp"
    }]

    environment = [
      {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment == "production" ? "Production" : "Staging"
      },
      {
        name  = "ConnectionStrings__DefaultConnection"
        value = "Server=${aws_db_instance.mysql.endpoint};Database=smartsuschef;User=smartsuschef;Password=${var.db_password};"
      },
      {
        name  = "Jwt__Secret"
        value = var.jwt_secret
      },
      {
        name  = "MLService__BaseUrl"
        value = "http://smartsuschef-${var.environment}-ml-api-service:8000"
      }
    ]

    logConfiguration = {
      logDriver = "awslogs"
      options = {
        "awslogs-group"         = aws_cloudwatch_log_group.backend.name
        "awslogs-region"        = var.aws_region
        "awslogs-stream-prefix" = "ecs"
      }
    }

    healthCheck = {
      command     = ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
      interval    = 30
      timeout     = 5
      retries     = 3
      startPeriod = 60
    }
  }])
}

resource "aws_ecs_task_definition" "frontend" {
  family                   = "smartsuschef-${var.environment}-frontend"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = "256"
  memory                   = "512"
  execution_role_arn       = aws_iam_role.ecs_task_execution.arn
  task_role_arn            = aws_iam_role.ecs_task.arn

  container_definitions = jsonencode([{
    name  = "smartsuschef-frontend"
    image = "${aws_ecr_repository.frontend.repository_url}:latest"
    
    portMappings = [{
      containerPort = 80
      protocol      = "tcp"
    }]

    logConfiguration = {
      logDriver = "awslogs"
      options = {
        "awslogs-group"         = aws_cloudwatch_log_group.frontend.name
        "awslogs-region"        = var.aws_region
        "awslogs-stream-prefix" = "ecs"
      }
    }

    healthCheck = {
      command     = ["CMD-SHELL", "curl -f http://localhost:80 || exit 1"]
      interval    = 30
      timeout     = 5
      retries     = 3
      startPeriod = 30
    }
  }])
}

# ==========================================
# ECS Services
# ==========================================
resource "aws_ecs_service" "backend" {
  name            = "smartsuschef-${var.environment}-backend-service"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.backend.arn
  desired_count   = var.environment == "uat" ? 1 : 2
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = module.vpc.private_subnets
    security_groups  = [aws_security_group.ecs.id]
    assign_public_ip = false
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.backend.arn
    container_name   = "smartsuschef-backend"
    container_port   = 8080
  }

  deployment_circuit_breaker {
    enable   = true
    rollback = true
  }

  depends_on = [aws_lb_listener.http]
}

resource "aws_ecs_service" "frontend" {
  name            = "smartsuschef-${var.environment}-frontend-service"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.frontend.arn
  desired_count   = var.environment == "uat" ? 1 : 2
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = module.vpc.private_subnets
    security_groups  = [aws_security_group.ecs.id]
    assign_public_ip = false
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.frontend.arn
    container_name   = "smartsuschef-frontend"
    container_port   = 80
  }

  deployment_circuit_breaker {
    enable   = true
    rollback = true
  }

  depends_on = [aws_lb_listener.http]
}

# ==========================================
# ML API - CloudWatch, Task Definition, ALB, Service
# ==========================================
resource "aws_cloudwatch_log_group" "ml_api" {
  name              = "/ecs/smartsuschef-${var.environment}-ml-api"
  retention_in_days = var.environment == "production" ? 30 : 7
}

resource "aws_ecs_task_definition" "ml_api" {
  family                   = "smartsuschef-${var.environment}-ml-api"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.environment == "uat" ? "512" : "1024"
  memory                   = var.environment == "uat" ? "1024" : "2048"
  execution_role_arn       = aws_iam_role.ecs_task_execution.arn
  task_role_arn            = aws_iam_role.ecs_task.arn

  volume {
    name = "ml-models-volume"
    efs_volume_configuration {
      file_system_id     = aws_efs_file_system.models.id
      transit_encryption = "ENABLED"
      authorization_config {
        access_point_id = aws_efs_access_point.models_access_point.id
        iam             = "ENABLED"
      }
    }
  }

  container_definitions = jsonencode([{
    name  = "smartsuschef-ml-api"
    image = "${aws_ecr_repository.ml_api.repository_url}:latest"

    portMappings = [{
      containerPort = 8000
      protocol      = "tcp"
    }]

    environment = [
      {
        name  = "CELERY_BROKER_URL"
        value = "redis://${aws_elasticache_replication_group.main.primary_endpoint_address}:6379/0"
      },
      {
        name  = "DATABASE_URL"
        value = "mysql+pymysql://smartsuschef:${var.db_password}@${aws_db_instance.mysql.address}/smartsuschef"
      },
      {
        name  = "MODEL_DIR"
        value = "/app/models"
      }
    ]

    mountPoints = [{
      sourceVolume  = "ml-models-volume"
      containerPath = "/app/models"
    }]

    logConfiguration = {
      logDriver = "awslogs"
      options = {
        "awslogs-group"         = "/ecs/smartsuschef-${var.environment}-ml-api"
        "awslogs-region"        = var.aws_region
        "awslogs-stream-prefix" = "ecs"
      }
    }

    healthCheck = {
      command     = ["CMD-SHELL", "curl -f http://localhost:8000/health || exit 1"]
      interval    = 30
      timeout     = 5
      retries     = 3
      startPeriod = 60
    }
  }])
}

resource "aws_lb_target_group" "ml_api" {
  name        = "ssc-${var.environment}-ml-tg"
  port        = 8000
  protocol    = "HTTP"
  vpc_id      = module.vpc.vpc_id
  target_type = "ip"

  health_check {
    enabled             = true
    healthy_threshold   = 2
    interval            = 30
    matcher             = "200"
    path                = "/health"
    port                = "traffic-port"
    protocol            = "HTTP"
    timeout             = 5
    unhealthy_threshold = 3
  }
}

resource "aws_lb_listener_rule" "ml_api" {
  listener_arn = aws_lb_listener.http.arn
  priority     = 99

  action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.ml_api.arn
  }

  condition {
    path_pattern {
      values = ["/ml/*"]
    }
  }
}

resource "aws_ecs_service" "ml_api" {
  name            = "smartsuschef-${var.environment}-ml-api-service"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.ml_api.arn
  desired_count   = var.environment == "uat" ? 1 : 2
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = module.vpc.private_subnets
    security_groups  = [aws_security_group.ecs.id, aws_security_group.efs.id, aws_security_group.redis.id]
    assign_public_ip = false
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.ml_api.arn
    container_name   = "smartsuschef-ml-api"
    container_port   = 8000
  }

  deployment_circuit_breaker {
    enable   = true
    rollback = true
  }

  depends_on = [aws_lb_listener_rule.ml_api]
}

# ==========================================
# ML Worker - CloudWatch, Task Definition, Service
# ==========================================
resource "aws_cloudwatch_log_group" "ml_worker" {
  name              = "/ecs/smartsuschef-${var.environment}-ml-worker"
  retention_in_days = var.environment == "production" ? 30 : 7
}

resource "aws_ecs_task_definition" "ml_worker" {
  family                   = "smartsuschef-${var.environment}-ml-worker"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.environment == "uat" ? "1024" : "2048"
  memory                   = var.environment == "uat" ? "2048" : "4096"
  execution_role_arn       = aws_iam_role.ecs_task_execution.arn
  task_role_arn            = aws_iam_role.ecs_task.arn

  volume {
    name = "ml-models-volume"
    efs_volume_configuration {
      file_system_id     = aws_efs_file_system.models.id
      transit_encryption = "ENABLED"
      authorization_config {
        access_point_id = aws_efs_access_point.models_access_point.id
        iam             = "ENABLED"
      }
    }
  }

  container_definitions = jsonencode([{
    name  = "smartsuschef-ml-worker"
    image = "${aws_ecr_repository.ml_api.repository_url}:latest"

    command = ["celery", "-A", "app.celery_app", "worker", "--loglevel=info"]

    environment = [
      {
        name  = "CELERY_BROKER_URL"
        value = "redis://${aws_elasticache_replication_group.main.primary_endpoint_address}:6379/0"
      },
      {
        name  = "DATABASE_URL"
        value = "mysql+pymysql://smartsuschef:${var.db_password}@${aws_db_instance.mysql.address}/smartsuschef"
      },
      {
        name  = "MODEL_DIR"
        value = "/app/models"
      }
    ]

    mountPoints = [{
      sourceVolume  = "ml-models-volume"
      containerPath = "/app/models"
    }]

    logConfiguration = {
      logDriver = "awslogs"
      options = {
        "awslogs-group"         = "/ecs/smartsuschef-${var.environment}-ml-worker"
        "awslogs-region"        = var.aws_region
        "awslogs-stream-prefix" = "ecs"
      }
    }
  }])
}

resource "aws_ecs_service" "ml_worker" {
  name            = "smartsuschef-${var.environment}-ml-worker-service"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.ml_worker.arn
  desired_count   = var.environment == "uat" ? 1 : 0
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = module.vpc.private_subnets
    security_groups  = [aws_security_group.ecs.id, aws_security_group.efs.id, aws_security_group.redis.id]
    assign_public_ip = false
  }

  deployment_circuit_breaker {
    enable   = true
    rollback = true
  }

  depends_on = [
    aws_elasticache_replication_group.main
  ]
}

# ==========================================
# ECS Task IAM Permissions (EFS + ElastiCache)
# ==========================================
resource "aws_iam_role_policy" "ecs_task_permissions" {
  name = "smartsuschef-${var.environment}-ecs-task-permissions"
  role = aws_iam_role.ecs_task.id

  policy = jsonencode({
    Version = "2012-10-17",
    Statement = [
      {
        Effect = "Allow",
        Action = [
          "elasticache:Connect"
        ],
        Resource = aws_elasticache_replication_group.main.arn
      },
      {
        Effect = "Allow",
        Action = [
          "elasticache:Connect"
        ],
        Resource = "${aws_elasticache_replication_group.main.arn}:*"
      },
      {
        Effect = "Allow",
        Action = [
          "elasticache:DescribeCacheClusters"
        ],
        Resource = "*"
      },
      {
        Effect = "Allow",
        Action = [
          "efs:ClientMount",
          "efs:ClientWrite",
          "efs:ClientRootAccess"
        ],
        Resource = aws_efs_file_system.models.arn
      },
      {
        Effect   = "Allow",
        Action   = "elasticfilesystem:DescribeMountTargets",
        Resource = "*"
      }
    ]
  })
}

# ==========================================
# Outputs
# ==========================================
output "alb_dns_name" {
  description = "ALB DNS name"
  value       = aws_lb.main.dns_name
}

output "ecr_backend_url" {
  description = "ECR Backend repository URL"
  value       = aws_ecr_repository.backend.repository_url
}

output "ecr_frontend_url" {
  description = "ECR Frontend repository URL"
  value       = aws_ecr_repository.frontend.repository_url
}

output "ecr_ml_api_url" {
  description = "ECR ML API repository URL"
  value       = aws_ecr_repository.ml_api.repository_url
}

output "rds_endpoint" {
  description = "RDS endpoint"
  value       = aws_db_instance.mysql.endpoint
}

output "efs_id" {
  description = "EFS File System ID for ML models"
  value       = aws_efs_file_system.models.id
}

output "elasticache_primary_endpoint" {
  description = "ElastiCache Redis Primary Endpoint"
  value       = aws_elasticache_replication_group.main.primary_endpoint_address
}
