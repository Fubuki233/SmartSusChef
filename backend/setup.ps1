# Run this script from the backend/SmartSusChef.Api directory
# Prerequisites: .NET 8 SDK and MySQL must be installed

Write-Host "SmartSusChef Backend Setup Script" -ForegroundColor Green
Write-Host "===================================" -ForegroundColor Green
Write-Host ""

# Check if .NET 8 is installed
Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ .NET SDK found: $dotnetVersion" -ForegroundColor Green
} else {
    Write-Host "✗ .NET SDK not found. Please install .NET 8 SDK" -ForegroundColor Red
    exit 1
}

# Check if EF Core tools are installed
Write-Host "`nChecking EF Core tools..." -ForegroundColor Yellow
$efVersion = dotnet ef --version 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ EF Core tools found: $efVersion" -ForegroundColor Green
} else {
    Write-Host "Installing EF Core tools..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
}

# Restore NuGet packages
Write-Host "`nRestoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Packages restored" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to restore packages" -ForegroundColor Red
    exit 1
}

# Build the project
Write-Host "`nBuilding project..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Build successful" -ForegroundColor Green
} else {
    Write-Host "✗ Build failed" -ForegroundColor Red
    exit 1
}

# Create initial migration if it doesn't exist
Write-Host "`nChecking migrations..." -ForegroundColor Yellow
$migrationsFolder = "Migrations"
if (Test-Path $migrationsFolder) {
    Write-Host "✓ Migrations folder exists" -ForegroundColor Green
} else {
    Write-Host "Creating initial migration..." -ForegroundColor Yellow
    dotnet ef migrations add InitialCreate
}

# Prompt for database update
Write-Host "`n" -ForegroundColor Yellow
$updateDb = Read-Host "Do you want to update the database now? (y/n)"
if ($updateDb -eq 'y') {
    Write-Host "`nUpdating database..." -ForegroundColor Yellow
    dotnet ef database update
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Database updated successfully" -ForegroundColor Green
    } else {
        Write-Host "✗ Database update failed. Please check your connection string in appsettings.json" -ForegroundColor Red
    }
}

Write-Host "`n===================================" -ForegroundColor Green
Write-Host "Setup completed!" -ForegroundColor Green
Write-Host "`nTo run the application:" -ForegroundColor Cyan
Write-Host "  dotnet run" -ForegroundColor White
Write-Host "`nSwagger UI will be available at:" -ForegroundColor Cyan
Write-Host "  http://localhost:5000/swagger" -ForegroundColor White
Write-Host "`nDefault credentials:" -ForegroundColor Cyan
Write-Host "  Manager: admin / admin123" -ForegroundColor White
Write-Host "  Employee: employee / employee123" -ForegroundColor White
Write-Host "===================================" -ForegroundColor Green
