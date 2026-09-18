@echo off
setlocal

rem ============================================================
rem  Mud.Feishu.WebSocket.Demo  AOT 发布脚本
rem  前置条件:
rem    1. .NET 10 SDK
rem    2. Visual Studio "使用 C++ 的桌面开发" 工作负载(MSVC 链接器)
rem  产物:
rem    bin\Release\net10.0\win-x64\publish\Mud.Feishu.WebSocket.Demo.exe
rem ============================================================

cd /d "%~dp0"

echo 正在执行 AOT 发布 (Release / win-x64)...
dotnet publish Mud.Feishu.WebSocket.Demo.csproj -c Release -r win-x64 %*

if errorlevel 1 (
    echo.
    echo [失败] AOT 发布未成功, 请检查上方错误输出。
    pause
    exit /b 1
)

echo.
echo [成功] 发布产物位于 bin\Release\net10.0\win-x64\publish\
pause
endlocal
