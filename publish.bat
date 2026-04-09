@echo off

REM ===============================================================
REM Auto-publish Mud.Feishu project batch file
REM Author: Mud Studio
REM Date: 2026-01-25
REM ===============================================================

echo ===============================================================
echo Starting Mud.Feishu project publish process
echo Current directory: %cd%
echo Execution time: %date% %time%
echo ===============================================================

REM Check if we're in the project root directory
if not exist "Mud.Feishu.Abstractions" (
    echo Error: Current directory is not the project root. Please run this script in the MudFeishu directory
    pause
    exit /b 1
)

REM Check if dotnet command is available
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo Error: dotnet command not found. Please ensure .NET SDK is installed
    pause
    exit /b 1
)

echo 1. Restoring dependencies...
echo ===============================================================
dotnet restore
if %errorlevel% neq 0 (
    echo Error: Failed to restore dependencies
    pause
    exit /b 1
)

echo.
echo 2. Building core projects...
echo ===============================================================

echo Building Mud.Feishu...
dotnet build Mud.Feishu --configuration Release
if %errorlevel% neq 0 (
    echo Error: Failed to build Mud.Feishu
    pause
    exit /b 1
)

echo Building Mud.Feishu.Abstractions...
dotnet build Mud.Feishu.Abstractions --configuration Release
if %errorlevel% neq 0 (
    echo Error: Failed to build Mud.Feishu.Abstractions
    pause
    exit /b 1
)

echo Building Mud.Feishu.WebSocket...
dotnet build Mud.Feishu.WebSocket --configuration Release
if %errorlevel% neq 0 (
    echo Error: Failed to build Mud.Feishu.WebSocket
    pause
    exit /b 1
)

echo Building Mud.Feishu.Webhook...
dotnet build Mud.Feishu.Webhook --configuration Release
if %errorlevel% neq 0 (
    echo Error: Failed to build Mud.Feishu.Webhook
    pause
    exit /b 1
)

echo Building Mud.Feishu.Authentication...
dotnet build Mud.Feishu.Authentication --configuration Release
if %errorlevel% neq 0 (
    echo Error: Failed to build Mud.Feishu.Authentication
    pause
    exit /b 1
)

echo Building Mud.Feishu.EventCallback...
dotnet build Mud.Feishu.EventCallback --configuration Release
if %errorlevel% neq 0 (
    echo Error: Failed to build Mud.Feishu.EventCallback
    pause
    exit /b 1
)

REM Build Redis project if exists
if exist "Mud.Feishu.Redis" (
    echo Building Mud.Feishu.Redis...
    dotnet build Mud.Feishu.Redis --configuration Release
    if %errorlevel% neq 0 (
        echo Warning: Failed to build Mud.Feishu.Redis. Continue publishing...
    )
)

echo.
echo 3. Running critical tests...
echo ===============================================================

echo Running WebSocket tests...
dotnet test Tests\Mud.Feishu.WebSocket.Tests --configuration Release
if %errorlevel% neq 0 (
    echo Warning: WebSocket tests failed! Continue publishing...
)

echo.
echo Running Webhook tests...
dotnet test Tests\Mud.Feishu.Webhook.Tests --configuration Release
if %errorlevel% neq 0 (
    echo Warning: Webhook tests failed! Continue publishing...
)

echo.
echo Running Authentication tests...
dotnet test Tests\Mud.Feishu.Authentication.Tests --configuration Release
if %errorlevel% neq 0 (
    echo Warning: Authentication tests failed! Continue publishing...
)

echo.
echo 4. Publishing packages...
echo ===============================================================

REM Define publish parameters
set VERSION_SUFFIX=-preview-%date:~0,4%%date:~5,2%%date:~8,2%-%time:~0,2%%time:~3,2%
set OUTPUT_DIR=artifacts

REM Create output directory
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

echo Publishing Mud.Feishu...
dotnet pack Mud.Feishu --configuration Release --output "%OUTPUT_DIR%" --version-suffix %VERSION_SUFFIX%
if %errorlevel% neq 0 (
    echo Error: Failed to publish Mud.Feishu
    pause
    exit /b 1
)

echo.
echo Publishing Mud.Feishu.Abstractions...
dotnet pack Mud.Feishu.Abstractions --configuration Release --output "%OUTPUT_DIR%" --version-suffix %VERSION_SUFFIX%
if %errorlevel% neq 0 (
    echo Error: Failed to publish Mud.Feishu.Abstractions
    pause
    exit /b 1
)

echo.
echo Publishing Mud.Feishu.WebSocket...
dotnet pack Mud.Feishu.WebSocket --configuration Release --output "%OUTPUT_DIR%" --version-suffix %VERSION_SUFFIX%
if %errorlevel% neq 0 (
    echo Error: Failed to publish Mud.Feishu.WebSocket
    pause
    exit /b 1
)

echo.
echo Publishing Mud.Feishu.Webhook...
dotnet pack Mud.Feishu.Webhook --configuration Release --output "%OUTPUT_DIR%" --version-suffix %VERSION_SUFFIX%
if %errorlevel% neq 0 (
    echo Error: Failed to publish Mud.Feishu.Webhook
    pause
    exit /b 1
)

echo.
echo Publishing Mud.Feishu.Authentication...
dotnet pack Mud.Feishu.Authentication --configuration Release --output "%OUTPUT_DIR%" --version-suffix %VERSION_SUFFIX%
if %errorlevel% neq 0 (
    echo Error: Failed to publish Mud.Feishu.Authentication
    pause
    exit /b 1
)

echo.
echo Publishing Mud.Feishu.EventCallback...
dotnet pack Mud.Feishu.EventCallback --configuration Release --output "%OUTPUT_DIR%" --version-suffix %VERSION_SUFFIX%
if %errorlevel% neq 0 (
    echo Error: Failed to publish Mud.Feishu.EventCallback
    pause
    exit /b 1
)

REM Check if Redis project exists
if exist "Mud.Feishu.Redis" (
    echo.
    echo Publishing Mud.Feishu.Redis...
    dotnet pack Mud.Feishu.Redis --configuration Release --output "%OUTPUT_DIR%" --version-suffix %VERSION_SUFFIX%
    if %errorlevel% neq 0 (
        echo Warning: Failed to publish Mud.Feishu.Redis. Continue...
    )
)

echo.
echo 5. Verifying published packages...
echo ===============================================================

REM List published packages
dir "%OUTPUT_DIR%" /b
if %errorlevel% neq 0 (
    echo Error: Failed to list published packages
    pause
    exit /b 1
)

REM Count published packages
set PACKAGE_COUNT=0
for /f %%i in ('dir "%OUTPUT_DIR%" /b ^| find /c ".nupkg"') do set PACKAGE_COUNT=%%i

echo Total published packages: %PACKAGE_COUNT%

if %PACKAGE_COUNT% equ 0 (
    echo Error: No packages were published
    pause
    exit /b 1
)

echo ===============================================================
echo Publish process completed successfully!
echo Output directory: %cd%\%OUTPUT_DIR%
echo Execution time: %date% %time%
echo ===============================================================

pause
