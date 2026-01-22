# SmartSusChef - Quick Start with Docker
# This script builds and starts all services using Docker Compose

Write-Host "🚀 SmartSusChef - Quick Start" -ForegroundColor Green
Write-Host "==============================" -ForegroundColor Green
Write-Host ""

# Check if Docker is installed
Write-Host "Checking Docker..." -ForegroundColor Yellow
try {
    $dockerVersion = docker --version
    Write-Host "✓ Docker found: $dockerVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ Docker not found. Please install Docker Desktop" -ForegroundColor Red
    exit 1
}

# Check if Docker Compose is installed
Write-Host "Checking Docker Compose..." -ForegroundColor Yellow
try {
    $composeVersion = docker-compose --version
    Write-Host "✓ Docker Compose found: $composeVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ Docker Compose not found" -ForegroundColor Red
    exit 1
}

# Stop and remove existing containers
Write-Host "`nStopping existing containers..." -ForegroundColor Yellow
docker-compose down

# Build and start all services
Write-Host "`nBuilding and starting services..." -ForegroundColor Yellow
docker-compose up -d --build

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Services started successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to start services" -ForegroundColor Red
    exit 1
}

# Wait for MySQL to be ready
Write-Host "`nWaiting for MySQL to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Run database migrations
Write-Host "`nRunning database migrations..." -ForegroundColor Yellow
docker exec smartsuschef_backend dotnet ef database update

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Database migrations completed" -ForegroundColor Green
} else {
    Write-Host "✗ Database migrations failed" -ForegroundColor Red
}

# Display service URLs
Write-Host "`n==============================" -ForegroundColor Green
Write-Host "✅ SmartSusChef is running!" -ForegroundColor Green
Write-Host "==============================" -ForegroundColor Green
Write-Host "`nServices:" -ForegroundColor Cyan
Write-Host "  Frontend:    http://localhost:3000" -ForegroundColor White
Write-Host "  Backend API: http://localhost:5000" -ForegroundColor White
Write-Host "  Swagger UI:  http://localhost:5000/swagger" -ForegroundColor White
Write-Host "  MySQL:       localhost:3306" -ForegroundColor White
Write-Host "`nDefault credentials:" -ForegroundColor Cyan
Write-Host "  Manager:  admin / admin123" -ForegroundColor White
Write-Host "  Employee: employee / employee123" -ForegroundColor White
Write-Host "`nTo view logs:" -ForegroundColor Cyan
Write-Host "  docker-compose logs -f" -ForegroundColor White
Write-Host "`nTo stop services:" -ForegroundColor Cyan
Write-Host "  docker-compose down" -ForegroundColor White
Write-Host "==============================" -ForegroundColor Green
