#!/usr/bin/env bash
# SmartSusChef - PowerShell 启动脚本 (Windows)

Write-Host ""
Write-Host "=========================================="
Write-Host "SmartSusChef - 完整系统启动"
Write-Host "=========================================="
Write-Host ""

# 检查先决条件
function Test-Prerequisites {
    Write-Host "✓ 检查先决条件..." -ForegroundColor Green
    
    # 检查 .NET
    $dotnetPath = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnetPath) {
        Write-Host "✗ .NET SDK 未安装" -ForegroundColor Red
        Write-Host "  请访问: https://dotnet.microsoft.com/download"
        exit 1
    }
    Write-Host "  ✓ .NET 已安装" -ForegroundColor Green
    
    # 检查 Node.js
    $nodePath = Get-Command node -ErrorAction SilentlyContinue
    if (-not $nodePath) {
        Write-Host "✗ Node.js 未安装" -ForegroundColor Red
        Write-Host "  请访问: https://nodejs.org"
        exit 1
    }
    Write-Host "  ✓ Node.js 已安装" -ForegroundColor Green
    
    Write-Host ""
}

# 启动后端
function Start-Backend {
    Write-Host "🚀 启动后端服务器..." -ForegroundColor Cyan
    Write-Host "=========================================="
    
    $backendPath = Join-Path $PSScriptRoot "backend\SmartSusChef.Api"
    Push-Location $backendPath
    
    Write-Host "📦 恢复 NuGet 包..."
    dotnet restore
    
    Write-Host ""
    Write-Host "▶️  启动 .NET API 服务器..." -ForegroundColor Yellow
    Write-Host "📍 地址: http://localhost:5000"
    Write-Host "📚 Swagger: http://localhost:5000/swagger"
    Write-Host "⏸  按 Ctrl+C 停止"
    Write-Host ""
    
    dotnet run --configuration Debug
    Pop-Location
}

# 启动前端
function Start-Frontend {
    Write-Host ""
    Write-Host "🚀 启动前端服务器..." -ForegroundColor Cyan
    Write-Host "=========================================="
    
    $frontendPath = Join-Path $PSScriptRoot "frontend"
    Push-Location $frontendPath
    
    Write-Host "📦 安装 npm 包..."
    npm install
    
    Write-Host ""
    Write-Host "▶️  启动 React 开发服务器..." -ForegroundColor Yellow
    Write-Host "📍 地址: http://localhost:5173"
    Write-Host "⏸  按 Ctrl+C 停止"
    Write-Host ""
    
    npm run dev
    Pop-Location
}

# 主程序
function Main {
    Test-Prerequisites
    
    Write-Host "📋 启动顺序:" -ForegroundColor Cyan
    Write-Host "  1️⃣  后端服务器 (.NET API)"
    Write-Host "  2️⃣  前端服务器 (React)"
    Write-Host ""
    Write-Host "💡 提示: 在另一个 PowerShell 窗口启动第 2 步"
    Write-Host ""
    
    $choice = Read-Host "选择启动哪个服务? (1=后端, 2=前端, 3=两个都启动)"
    
    switch ($choice) {
        "1" {
            Start-Backend
        }
        "2" {
            Start-Frontend
        }
        "3" {
            # 后端在新窗口
            $backendPath = Join-Path $PSScriptRoot "backend\SmartSusChef.Api"
            Start-Process pwsh -ArgumentList "-NoExit", "-Command", "cd '$backendPath'; dotnet run --configuration Debug"
            
            # 等待 3 秒
            Start-Sleep -Seconds 3
            
            # 前端在新窗口
            $frontendPath = Join-Path $PSScriptRoot "frontend"
            Start-Process pwsh -ArgumentList "-NoExit", "-Command", "cd '$frontendPath'; npm install; npm run dev"
            
            Write-Host ""
            Write-Host "✅ 两个服务器都已启动!" -ForegroundColor Green
            Write-Host ""
            Write-Host "📍 后端: http://localhost:5000" -ForegroundColor Cyan
            Write-Host "📍 前端: http://localhost:5173" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "🌐 打开浏览器访问: http://localhost:5173" -ForegroundColor Yellow
        }
        default {
            Write-Host "❌ 无效选择" -ForegroundColor Red
            exit 1
        }
    }
}

Main
