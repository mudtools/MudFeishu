@echo off
setlocal

echo ========================================
echo  FeishuWikiManager Frontend Build Script
echo ========================================
echo.

cd /d "%~dp0frontend"

echo [1/3] Checking Node.js version...
node --version
if %errorlevel% neq 0 (
    echo ERROR: Node.js is not installed!
    exit /b %errorlevel%
)

echo.
echo [2/3] Installing dependencies...
if not exist "node_modules" (
    echo Running npm install...
    npm install
) else (
    echo node_modules exists, skipping install.
    echo Run "npm install" manually if needed.
)

echo.
echo [3/3] Building project...
npm run build
if %errorlevel% neq 0 (
    echo ERROR: Build failed!
    exit /b %errorlevel%
)

echo.
echo ========================================
echo  Build completed successfully!
echo  Output: dist\
echo ========================================

endlocal
