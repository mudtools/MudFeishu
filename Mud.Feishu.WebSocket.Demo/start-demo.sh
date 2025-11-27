#!/bin/bash

# 飞书WebSocket演示服务启动脚本

echo "=================================================="
echo "          飞书WebSocket演示服务启动脚本"
echo "=================================================="
echo

# 检查.NET SDK是否安装
if ! command -v dotnet &> /dev/null; then
    echo "[错误] 未检测到.NET SDK，请先安装.NET 6.0或更高版本"
    echo "下载地址: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "[信息] 检测到.NET SDK:"
dotnet --version

# 还原NuGet包
echo
echo "[信息] 正在还原NuGet包..."
dotnet restore
if [ $? -ne 0 ]; then
    echo "[错误] NuGet包还原失败"
    exit 1
fi

# 构建项目
echo
echo "[信息] 正在构建项目..."
dotnet build
if [ $? -ne 0 ]; then
    echo "[错误] 项目构建失败"
    exit 1
fi

# 设置环境变量
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS="http://localhost:5000;https://localhost:5001"

echo
echo "[信息] 环境变量设置:"
echo "        ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT"
echo "        ASPNETCORE_URLS=$ASPNETCORE_URLS"
echo

# 启动服务
echo "[信息] 正在启动飞书WebSocket演示服务..."
echo "[信息] 服务地址: http://localhost:5000"
echo "[信息] API文档: http://localhost:5000/swagger"
echo "[信息] 按 Ctrl+C 停止服务"
echo
echo "=================================================="

dotnet run