#!/bin/bash
# SmartSusChef - 完整启动脚本（Linux/Mac）

echo "=========================================="
echo "SmartSusChef - 完整系统启动"
echo "=========================================="
echo ""

# 检查先决条件
check_prerequisites() {
    echo "✓ 检查先决条件..."
    
    # 检查 .NET
    if ! command -v dotnet &> /dev/null; then
        echo "✗ .NET SDK 未安装"
        echo "  请访问: https://dotnet.microsoft.com/download"
        exit 1
    fi
    echo "  ✓ .NET $(dotnet --version) 已安装"
    
    # 检查 Node.js
    if ! command -v node &> /dev/null; then
        echo "✗ Node.js 未安装"
        echo "  请访问: https://nodejs.org"
        exit 1
    fi
    echo "  ✓ Node.js $(node --version) 已安装"
    
    echo ""
}

# 启动后端
start_backend() {
    echo "🚀 启动后端服务器..."
    echo "=========================================="
    cd "$(dirname "$0")/backend/SmartSusChef.Api"
    
    # 恢复依赖
    echo "📦 恢复 NuGet 包..."
    dotnet restore
    
    echo ""
    echo "▶️  启动 .NET API 服务器..."
    echo "📍 地址: http://localhost:5000"
    echo "📚 Swagger: http://localhost:5000/swagger"
    echo "⏸  按 Ctrl+C 停止"
    echo ""
    
    dotnet run --configuration Release
}

# 启动前端
start_frontend() {
    echo ""
    echo "🚀 启动前端服务器..."
    echo "=========================================="
    cd "$(dirname "$0")/frontend"
    
    # 安装依赖
    echo "📦 安装 npm 包..."
    npm install
    
    echo ""
    echo "▶️  启动 React 开发服务器..."
    echo "📍 地址: http://localhost:5173"
    echo "⏸  按 Ctrl+C 停止"
    echo ""
    
    npm run dev
}

# 主程序
main() {
    check_prerequisites
    
    echo "📋 启动顺序:"
    echo "  1️⃣  后端服务器 (.NET API)"
    echo "  2️⃣  前端服务器 (React)"
    echo ""
    echo "💡 提示: 在另一个终端窗口启动第 2 步"
    echo ""
    
    read -p "选择启动哪个服务? (1=后端, 2=前端, 3=两个都启动): " choice
    
    case $choice in
        1)
            start_backend
            ;;
        2)
            start_frontend
            ;;
        3)
            start_backend &
            sleep 5
            start_frontend
            ;;
        *)
            echo "❌ 无效选择"
            exit 1
            ;;
    esac
}

main
