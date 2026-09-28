@echo off
REM ===============================================================
REM Mud.Feishu pack-only script (SDK 3.x)
REM
REM Usage: pack.bat [version] [/nopause]
REM   [version]  Package version (e.g. 3.0.0). Default: <Version> from
REM              Directory.Build.props.
REM   /nopause   Do not pause at the end (for scripting / CI).
REM
REM Packs the 9 SDK packages into .\artifacts. No quality gate,
REM no tests, no push - build validation is your own responsibility.
REM ===============================================================
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

set "OUTPUT_DIR=artifacts"
set "PROJECTS=Mud.Feishu.Abstractions Mud.Feishu.DataModels Mud.Feishu.EventCallback Mud.Feishu.OpenTelemetry Mud.Feishu.Redis Mud.Feishu.Webhook Mud.Feishu.WebSocket Mud.Feishu.Authentication Mud.Feishu"
set "VERSION=%~1"
set "NO_PAUSE=%~2"
set "EC=0"

where dotnet >nul 2>nul || (
    echo Error: 'dotnet' was not found. Install the .NET SDK first.
    set "EC=1"
    goto :done
)

if not defined VERSION (
    for /f "usebackq delims=" %%V in (`dotnet msbuild "Mud.Feishu\Mud.Feishu.csproj" -getProperty:Version -nologo`) do set "VERSION=%%V"
)
if not defined VERSION (
    echo Error: cannot resolve the package version. Pass it explicitly, e.g. pack.bat 3.0.0
    set "EC=1"
    goto :done
)
echo Package version : %VERSION%

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
del /q "%OUTPUT_DIR%\*%VERSION%.nupkg" 2>nul

set "FAILED="
for %%P in (%PROJECTS%) do (
    echo Packing %%P ...
    dotnet pack "%%P\%%P.csproj" -c Release --nologo -o "%OUTPUT_DIR%" -p:Version=%VERSION% || set "FAILED=!FAILED! %%P"
)
if defined FAILED (
    echo Error: packing failed for:!FAILED!
    set "EC=1"
    goto :done
)

set "COUNT=0"
for %%P in (%PROJECTS%) do (
    if exist "%OUTPUT_DIR%\%%P.%VERSION%.nupkg" set /a COUNT+=1
)
echo.
echo Produced %COUNT%/9 packages in %cd%\%OUTPUT_DIR%:
dir /b "%OUTPUT_DIR%\*%VERSION%.nupkg"

:done
echo.
if "%EC%"=="0" (
    echo PACK SUCCEEDED  ^(version %VERSION%^)
) else (
    echo PACK FAILED
)
if not "%NO_PAUSE%"=="/nopause" pause
endlocal & exit /b %EC%
