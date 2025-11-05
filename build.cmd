@echo off
REM Build script for Emergent Computing Simulator

echo ================================================
echo Emergent Computing Simulator - Build Script
echo ================================================
echo.

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found!
    echo Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download
    exit /b 1
)

echo [1/3] Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore packages
    exit /b 1
)
echo.

echo [2/3] Building project...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    exit /b 1
)
echo.

echo [3/3] Build complete!
echo.
echo ================================================
echo Build successful! 
echo.
echo To run the application:
echo   1. Run: dotnet run
echo   2. Or execute: bin\Release\net8.0-windows\EmergentComputing.exe
echo ================================================
echo.

pause

