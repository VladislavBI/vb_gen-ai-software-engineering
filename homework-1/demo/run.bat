@echo off
REM Banking Transactions API startup script for Windows

setlocal enabledelayedexpansion

echo.
echo ===================================
echo Banking Transactions API
echo ===================================
echo.

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK is not installed or not in PATH
    echo Please install .NET 10 SDK from https://dotnet.microsoft.com/en-us/download/dotnet/10.0
    pause
    exit /b 1
)

echo Detected .NET version:
dotnet --version
echo.

REM Navigate to src directory
cd /d "%~dp0\..\src"

if not exist "Homework1.sln" (
    echo ERROR: Homework1.sln not found in %cd%
    pause
    exit /b 1
)

echo Building the solution...
dotnet build Homework1.sln
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo Build successful. Starting API...
echo.
echo The API will run on: http://localhost:5080
echo Press Ctrl+C to stop the server
echo.

dotnet run --project Homework1.Api --no-build --urls http://localhost:5080

pause
