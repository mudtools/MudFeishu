@echo off

REM ===============================================================
REM Auto-run Mud.Feishu project tests batch file
REM Author: Mud Studio
REM Date: 2026-01-25
REM ===============================================================

echo ===============================================================
echo Starting Mud.Feishu project tests
echo Current directory: %cd%
echo Execution time: %date% %time%
echo ===============================================================

REM Check if we're in the project root directory
if not exist "Tests" (
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

echo 1. Running all tests with reports...
echo ===============================================================

REM Define test report directory
set TEST_REPORT_DIR=test-reports

REM Create test report directory
if not exist "%TEST_REPORT_DIR%" mkdir "%TEST_REPORT_DIR%"

REM Run the entire test suite with XML logger
dotnet test --logger "trx;LogFileName=all-tests.trx" --results-directory "%TEST_REPORT_DIR%"

REM Check test results
if %errorlevel% neq 0 (
    echo ===============================================================
    echo Test execution failed! Please check the error messages above
    echo ===============================================================
    echo Test report generated at: %cd%\%TEST_REPORT_DIR%\all-tests.trx
    pause
    exit /b 1
) else (
    echo ===============================================================
    echo All tests executed successfully!
    echo ===============================================================
    echo Test report generated at: %cd%\%TEST_REPORT_DIR%\all-tests.trx
)

REM Check if ReportGenerator is available
where ReportGenerator >nul 2>nul
if %errorlevel% equ 0 (
    echo.
    echo Generating HTML test report...
    echo ===============================================================
    
    REM Generate HTML report
    ReportGenerator "-reports:%TEST_REPORT_DIR%\*.trx" "-targetdir:%TEST_REPORT_DIR%\html" "-reporttypes:Html"
    
    if %errorlevel% neq 0 (
        echo Warning: Failed to generate HTML report
    ) else (
        echo HTML test report generated at: %cd%\%TEST_REPORT_DIR%\html\index.htm
    )
) else (
    echo.
    echo Info: ReportGenerator not found. To generate HTML reports, install it with: dotnet tool install -g dotnet-reportgenerator-globaltool
)

REM Run specific project tests with reports (optional)
echo.
echo 2. Running specific project tests with reports...
echo ===============================================================

echo Running WebSocket tests...
dotnet test Tests\Mud.Feishu.WebSocket.Tests --logger "trx;LogFileName=websocket-tests.trx" --results-directory "%TEST_REPORT_DIR%"
if %errorlevel% neq 0 (
    echo WebSocket tests failed!
    echo Test report generated at: %cd%\%TEST_REPORT_DIR%\websocket-tests.trx
) else (
    echo WebSocket tests passed!
    echo Test report generated at: %cd%\%TEST_REPORT_DIR%\websocket-tests.trx
)

echo.
echo Running Webhook tests...
dotnet test Tests\Mud.Feishu.Webhook.Tests --logger "trx;LogFileName=webhook-tests.trx" --results-directory "%TEST_REPORT_DIR%"
if %errorlevel% neq 0 (
    echo Webhook tests failed!
    echo Test report generated at: %cd%\%TEST_REPORT_DIR%\webhook-tests.trx
) else (
    echo Webhook tests passed!
    echo Test report generated at: %cd%\%TEST_REPORT_DIR%\webhook-tests.trx
)

echo.
echo Running Authentication tests...
dotnet test Tests\Mud.Feishu.Authentication.Tests --logger "trx;LogFileName=authentication-tests.trx" --results-directory "%TEST_REPORT_DIR%"
if %errorlevel% neq 0 (
    echo Authentication tests failed!
    echo Test report generated at: %cd%\%TEST_REPORT_DIR%\authentication-tests.trx
) else (
    echo Authentication tests passed!
    echo Test report generated at: %cd%\%TEST_REPORT_DIR%\authentication-tests.trx
)

REM Generate combined HTML report if ReportGenerator is available
where ReportGenerator >nul 2>nul
if %errorlevel% equ 0 (
    echo.
    echo Generating combined HTML test report...
    echo ===============================================================
    
    REM Generate combined HTML report
    ReportGenerator "-reports:%TEST_REPORT_DIR%\*.trx" "-targetdir:%TEST_REPORT_DIR%\html-combined" "-reporttypes:Html"
    
    if %errorlevel% neq 0 (
        echo Warning: Failed to generate combined HTML report
    ) else (
        echo Combined HTML test report generated at: %cd%\%TEST_REPORT_DIR%\html-combined\index.htm
    )
)

echo ===============================================================
echo Test execution completed
echo Execution time: %date% %time%
echo ===============================================================

pause
