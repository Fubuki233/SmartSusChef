#!/usr/bin/env bash
# SmartSusChef - Local Dev Quick Start (macOS / Linux)
# Launches ML / Backend / Frontend in background, logs to terminal

set -e

DB_SERVER="${DB_SERVER:-oversea.zyh111.icu}"
DB_PORT="${DB_PORT:-33333}"
DB_USER="${DB_USER:-grp4}"
DB_PASSWORD="${DB_PASSWORD:-grp4}"
DB_NAME="${DB_NAME:-smartsuschef}"

ROOT="$(cd "$(dirname "$0")" && pwd)"

echo ""
echo "=========================================="
echo "  SmartSusChef - Dev Quick Start"
echo "=========================================="
echo ""

# -- Check prerequisites ---------------------------------------------------
MISSING=""
command -v python3 >/dev/null 2>&1 || MISSING="$MISSING Python3"
command -v dotnet  >/dev/null 2>&1 || MISSING="$MISSING .NET-SDK"
command -v node    >/dev/null 2>&1 || MISSING="$MISSING Node.js"

if [ -n "$MISSING" ]; then
    echo "[ERROR] Missing:$MISSING"
    exit 1
fi
echo "[OK] Python / .NET / Node.js installed"

# -- Build connection string ------------------------------------------------
CONN_STR="Server=$DB_SERVER;Port=$DB_PORT;Database=$DB_NAME;User Id=$DB_USER;Password=$DB_PASSWORD;SslMode=None;AllowPublicKeyRetrieval=true;ConnectionTimeout=30"
echo "[OK] DB: $DB_SERVER:$DB_PORT/$DB_NAME (user: $DB_USER)"
echo ""

# -- Cleanup on exit --------------------------------------------------------
cleanup() {
    echo ""
    echo "Stopping services..."
    [ -n "$ML_PID" ]       && kill "$ML_PID"       2>/dev/null
    [ -n "$BACKEND_PID" ]  && kill "$BACKEND_PID"  2>/dev/null
    [ -n "$FRONTEND_PID" ] && kill "$FRONTEND_PID" 2>/dev/null
    wait 2>/dev/null
    echo "All services stopped."
}
trap cleanup EXIT INT TERM

# -- 1) ML Service ----------------------------------------------------------
echo "[1/3] Starting ML Service (port 8000)..."
(
    cd "$ROOT/ML"
    python3 -m pip install -q -r requirements.txt
    python3 -m uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
) &
ML_PID=$!
sleep 3

# -- 2) Backend -------------------------------------------------------------
echo "[2/3] Starting Backend (port 5000)..."
(
    cd "$ROOT/backend/SmartSusChef.Api"
    export ConnectionStrings__DefaultConnection="$CONN_STR"
    dotnet run
) &
BACKEND_PID=$!
sleep 2

# -- 3) Frontend ------------------------------------------------------------
echo "[3/3] Starting Frontend (port 5173)..."
(
    cd "$ROOT/frontend"
    npm install --silent
    npm run dev
) &
FRONTEND_PID=$!

# -- Summary ----------------------------------------------------------------
echo ""
echo "=========================================="
echo "  All 3 services started"
echo "=========================================="
echo ""
echo "  Frontend   ->  http://localhost:5173"
echo "  Backend    ->  http://localhost:5000"
echo "  Swagger    ->  http://localhost:5000/swagger"
echo "  ML API     ->  http://localhost:8000"
echo "  ML Docs    ->  http://localhost:8000/docs"
echo ""
echo "  Press Ctrl+C to stop all services"
echo "=========================================="

# Wait for any child to exit
wait
