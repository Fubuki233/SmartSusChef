@echo off
REM SmartSusChef - 完整启动脚本 (Windows PowerShell)

echo.
echo ==========================================
echo SmartSusChef - 完整系统启动
echo ==========================================
echo.

REM 检查先决条件
echo Checking prerequisites...

REM 检查 .NET
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo Error: .NET SDK not found
    echo Please install from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

REM 检查 Node.js
node --version >nul 2>&1
if errorlevel 1 (
    echo Error: Node.js not found
    echo Please install from: https://nodejs.org
    pause
    exit /b 1
)

echo.
echo Available options:
echo 1 = Start Backend Server only
echo 2 = Start Frontend Server only  
echo 3 = Start Both (opens new windows)
echo.

set /p choice="Choose (1-3): "

if "%choice%"=="1" goto backend
if "%choice%"=="2" goto frontend
if "%choice%"=="3" goto both
echo Invalid choice
exit /b 1

:backend
echo.
echo Starting Backend Server...
echo ==========================================
cd /d "%~dp0backend\SmartSusChef.Api"
echo.
echo Restoring NuGet packages...
dotnet restore
echo.
echo Starting .NET API Server...
echo Address: http://localhost:5000
echo Swagger: http://localhost:5000/swagger
echo Press Ctrl+C to stop
echo.
dotnet run --configuration Release
goto end

:frontend
echo.
echo Starting Frontend Server...
echo ==========================================
cd /d "%~dp0frontend"
echo.
echo Installing npm packages...
call npm install
echo.
echo Starting React development server...
echo Address: http://localhost:5173
echo Press Ctrl+C to stop
echo.
call npm run dev
goto end

:both
echo.
echo Starting Backend in new window...
start "" cmd /k "cd /d %~dp0backend\SmartSusChef.Api && dotnet run --configuration Release"
timeout /t 3
echo.
echo Starting Frontend in new window...
cd /d "%~dp0frontend"
call npm install
start "" cmd /k "cd /d %~dp0frontend && npm run dev"
echo.
echo Both servers starting...
echo Backend: http://localhost:5000
echo Frontend: http://localhost:5173
goto end

:end
pause
