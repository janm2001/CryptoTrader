#!/bin/bash

# CryptoTrader - Start All Services
# This script starts both the server and the Avalonia client

echo "========================================"
echo "   CryptoTrader - Starting Services    "
echo "========================================"

# Get the script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Restore and build all projects
echo ""
echo "Building solution..."
dotnet build "$SCRIPT_DIR/CryptoTrader.sln" --configuration Release

if [ $? -ne 0 ]; then
    echo "Build failed! Please fix the errors and try again."
    exit 1
fi

echo ""
echo "Starting server in background..."
dotnet run --project "$SCRIPT_DIR/CryptoExchange.Server/CryptoExchange.Server.csproj" --configuration Release &
SERVER_PID=$!

# Wait for server to start
echo "Waiting for server to initialize..."
sleep 3

echo ""
echo "Starting client application..."
dotnet run --project "$SCRIPT_DIR/CryptoTrader.App/CryptoTrader.App.csproj" --configuration Release

# When client closes, stop the server
echo ""
echo "Shutting down server..."
kill $SERVER_PID 2>/dev/null

echo "All services stopped."
