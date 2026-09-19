@echo off
setlocal

rem ============================================================
rem  Mud.Feishu.Webhook.Demo  AOT publish script
rem  Prerequisites:
rem    1. .NET 10 SDK
rem    2. Visual Studio "Desktop development with C++" workload (MSVC toolchain)
rem  Output:
rem    bin\Release\net10.0\win-x64\publish\Mud.Feishu.Webhook.Demo.exe
rem ============================================================

cd /d "%~dp0"

echo Publishing Native AOT (Release / win-x64)...
dotnet publish Mud.Feishu.Webhook.Demo.csproj -c Release -r win-x64 %*

if errorlevel 1 (
    echo.
    echo [FAILED] AOT publish did not succeed. Check the output above for errors.
    pause
    exit /b 1
)

echo.
echo [OK] Publish output: bin\Release\net10.0\win-x64\publish\
pause
endlocal
