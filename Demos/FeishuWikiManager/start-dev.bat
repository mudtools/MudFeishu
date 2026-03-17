@echo off
setlocal

echo ========================================
echo  FeishuWikiManager Development Starter
echo ========================================
echo.

echo Starting Backend and Frontend servers...
echo.

start "FeishuWikiManager - Backend" cmd /k "%~dp0run-backend.bat"
timeout /t 3 /nobreak >nul

start "FeishuWikiManager - Frontend" cmd /k "%~dp0run-frontend.bat"

echo.
echo ========================================
echo  Servers are starting!
echo  Backend:  http://localhost:5000
echo  Frontend: http://localhost:5173
echo ========================================
echo.
echo Press any key to exit this window...
echo (Servers will continue running in separate windows)
pause >nul

endlocal
