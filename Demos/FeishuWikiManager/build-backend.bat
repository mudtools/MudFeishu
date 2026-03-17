@echo off
setlocal

echo ========================================
echo  FeishuWikiManager Backend Build Script
echo ========================================
echo.

cd /d "%~dp0backend"

echo [1/3] Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Package restore failed!
    exit /b %errorlevel%
)

echo.
echo [2/3] Building project (Debug)...
dotnet build --configuration Debug --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Build failed!
    exit /b %errorlevel%
)

echo.
echo [3/3] Building project (Release)...
dotnet build --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Release build failed!
    exit /b %errorlevel%
)

echo.
echo ========================================
echo  Build completed successfully!
echo  Debug:   bin\Debug\net10.0\
echo  Release: bin\Release\net10.0\
echo ========================================

endlocal
