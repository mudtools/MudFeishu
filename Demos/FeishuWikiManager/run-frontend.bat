@echo off
setlocal

echo ========================================
echo  FeishuWikiManager Frontend Dev Server
echo ========================================
echo.

cd /d "%~dp0frontend"

echo Checking Node.js version...
node --version
if %errorlevel% neq 0 (
    echo ERROR: Node.js is not installed!
    exit /b %errorlevel%
)

if not exist "node_modules" (
    echo Installing dependencies...
    npm install
)

echo.
echo Starting development server...
echo URL: http://localhost:5173
echo.

npm run dev

endlocal
