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
