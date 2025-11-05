@echo off
REM Run script for Emergent Computing Simulator

echo ================================================
echo Emergent Computing Simulator
echo ================================================
echo.

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found!
    echo Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download
    exit /b 1
)

echo Starting application...
echo.

dotnet run

if %errorlevel% neq 0 (
    echo.
    echo ERROR: Application failed to start
    pause
    exit /b 1
)

