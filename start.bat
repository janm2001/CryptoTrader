@echo off
REM CryptoTrader - Start All Services
REM This script starts both the server and the Avalonia client

echo ========================================
echo    CryptoTrader - Starting Services    
echo ========================================

REM Get script directory
set SCRIPT_DIR=%~dp0

REM Build solution
echo.
echo Building solution...
dotnet build "%SCRIPT_DIR%CryptoTrader.sln" --configuration Release

if %ERRORLEVEL% neq 0 (
    echo Build failed! Please fix the errors and try again.
    pause
    exit /b 1
)

echo.
echo Starting server in new window...
start "CryptoTrader Server" cmd /c "dotnet run --project "%SCRIPT_DIR%CryptoExchange.Server\CryptoExchange.Server.csproj" --configuration Release"

REM Wait for server to start
echo Waiting for server to initialize...
timeout /t 3 /nobreak > nul

echo.
echo Starting client application...
dotnet run --project "%SCRIPT_DIR%CryptoTrader.App\CryptoTrader.App.csproj" --configuration Release

echo.
echo Client closed. Remember to close the server window manually.
pause
