# SmartSusChef - Local Dev Quick Start (Windows PowerShell)
# Launches ML / Backend / Frontend in 3 separate windows

param(
    [string]$DbServer = "oversea.zyh111.icu",
    [int]$DbPort = 33333,
    [string]$DbUser = "grp4",
    [string]$DbPassword = "grp4",
    [string]$DbName = "smartsuschef"
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  SmartSusChef - Dev Quick Start" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""

# -- Check prerequisites ---------------------------------------------------
$missing = @()
if (-not (Get-Command python -ErrorAction SilentlyContinue)) { $missing += "Python" }
if (-not (Get-Command dotnet  -ErrorAction SilentlyContinue)) { $missing += ".NET SDK" }
if (-not (Get-Command node    -ErrorAction SilentlyContinue)) { $missing += "Node.js" }

if ($missing.Count -gt 0) {
    Write-Host "[ERROR] Missing: $($missing -join ', ')" -ForegroundColor Red
    exit 1
}
Write-Host "[OK] Python / .NET / Node.js installed" -ForegroundColor Green

# -- Build connection string ------------------------------------------------
$connStr = "Server=$DbServer;Port=$DbPort;Database=$DbName;User Id=$DbUser;Password=$DbPassword;SslMode=None;AllowPublicKeyRetrieval=true;ConnectionTimeout=30"
Write-Host "[OK] DB: $DbServer`:$DbPort/$DbName (user: $DbUser)" -ForegroundColor Green
Write-Host ""

# -- 1) ML Service ----------------------------------------------------------
Write-Host "[1/3] Starting ML Service (port 8000)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", @"
    `$Host.UI.RawUI.WindowTitle = 'SmartSusChef - ML (8000)'
    Set-Location '$Root\ML'
    Write-Host 'Installing Python dependencies...' -ForegroundColor Yellow
    python -m pip install -q -r requirements.txt
    Write-Host 'ML service starting...' -ForegroundColor Green
    python -m uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
"@

Start-Sleep -Seconds 3

# -- 2) Backend -------------------------------------------------------------
Write-Host "[2/3] Starting Backend (port 5000)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", @"
    `$Host.UI.RawUI.WindowTitle = 'SmartSusChef - Backend (5000)'
    Set-Location '$Root\backend\SmartSusChef.Api'
    `$env:ConnectionStrings__DefaultConnection = '$connStr'
    Write-Host 'Backend starting...' -ForegroundColor Green
    dotnet run
"@

Start-Sleep -Seconds 2

# -- 3) Frontend ------------------------------------------------------------
Write-Host "[3/3] Starting Frontend (port 5173)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", @"
    `$Host.UI.RawUI.WindowTitle = 'SmartSusChef - Frontend (5173)'
    Set-Location '$Root\frontend'
    Write-Host 'Installing npm dependencies...' -ForegroundColor Yellow
    npm install --silent
    Write-Host 'Frontend starting...' -ForegroundColor Green
    npm run dev
"@

# -- Summary ----------------------------------------------------------------
Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  All 3 service windows launched" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Frontend   ->  http://localhost:5173" -ForegroundColor White
Write-Host "  Backend    ->  http://localhost:5000" -ForegroundColor White
Write-Host "  Swagger    ->  http://localhost:5000/swagger" -ForegroundColor White
Write-Host "  ML API     ->  http://localhost:8000" -ForegroundColor White
Write-Host "  ML Docs    ->  http://localhost:8000/docs" -ForegroundColor White
Write-Host ""
Write-Host "  To stop: close each PowerShell window" -ForegroundColor Gray
Write-Host "==========================================" -ForegroundColor Green
