@echo off
setlocal

echo ========================================
echo  FeishuWikiManager Build All Script
echo ========================================
echo.

echo [1/2] Building Backend...
call "%~dp0build-backend.bat"
if %errorlevel% neq 0 (
    echo ERROR: Backend build failed!
    exit /b %errorlevel%
)

echo.
echo [2/2] Building Frontend...
call "%~dp0build-frontend.bat"
if %errorlevel% neq 0 (
    echo ERROR: Frontend build failed!
    exit /b %errorlevel%
)

echo.
echo ========================================
echo  All builds completed successfully!
echo ========================================

endlocal
