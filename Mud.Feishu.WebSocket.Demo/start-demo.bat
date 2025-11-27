@echo off
echo ==================================================
echo          飞书WebSocket演示服务启动脚本
echo ==================================================
echo.

REM 检查.NET SDK是否安装
dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo [错误] 未检测到.NET SDK，请先安装.NET 6.0或更高版本
    echo 下载地址: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo [信息] 检测到.NET SDK:
dotnet --version

REM 还原NuGet包
echo.
echo [信息] 正在还原NuGet包...
dotnet restore
if %ERRORLEVEL% neq 0 (
    echo [错误] NuGet包还原失败
    pause
    exit /b 1
)

REM 构建项目
echo.
echo [信息] 正在构建项目...
dotnet build
if %ERRORLEVEL% neq 0 (
    echo [错误] 项目构建失败
    pause
    exit /b 1
)

REM 设置环境变量
set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://localhost:5000;https://localhost:5001

echo.
echo [信息] 环境变量设置:
echo        ASPNETCORE_ENVIRONMENT=%ASPNETCORE_ENVIRONMENT%
echo        ASPNETCORE_URLS=%ASPNETCORE_URLS%
echo.

REM 启动服务
echo [信息] 正在启动飞书WebSocket演示服务...
echo [信息] 服务地址: http://localhost:5000
echo [信息] API文档: http://localhost:5000/swagger
echo [信息] 按 Ctrl+C 停止服务
echo.
echo ==================================================

dotnet run