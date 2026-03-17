@echo off
setlocal

echo ========================================
echo  FeishuWikiManager Backend Run Script
echo ========================================
echo.

cd /d "%~dp0backend"

set ASPNETCORE_ENVIRONMENT=Development

echo Starting backend server...
echo Environment: %ASPNETCORE_ENVIRONMENT%
echo URL: http://localhost:5000
echo.

dotnet run --configuration Debug --urls "http://localhost:5000"

endlocal
