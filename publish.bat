@echo off
REM ===============================================================
REM Mud.Feishu local publish script (SDK 3.x)
REM Author: Mud Studio
REM Date: 2026-09-18
REM
REM Usage: publish.bat [version] [/preview] [/skipcheck] [/nopause]
REM
REM   [version]   Package version, e.g. 3.0.0-rc3.
REM               Default: the Version property evaluated from
REM               Directory.Build.props.
REM   /preview    Append "-preview.<yyyyMMdd-HHmmss>" to the version.
REM   /skipcheck  Skip the scripts\verify-build.ps1 quality gate
REM               (build + pack only, much faster).
REM   /nopause    Do not pause at the end (for scripting / CI).
REM
REM Notes:
REM   * v3 ships 9 packages. Mud.Feishu.DataModels and
REM     Mud.Feishu.OpenTelemetry are new since v2 - without them the
REM     package set is unusable because Mud.Feishu depends on
REM     Mud.Feishu.DataModels.
REM   * Directory.Build.props declares <Version> explicitly, so
REM     "dotnet pack --version-suffix" is silently ignored by
REM     MSBuild/NuGet. The version is passed as -p:Version=<value> to
REM     both the build and the pack so that the assembly version and
REM     the package version cannot drift apart.
REM   * The whole solution is built before packing, so a compile error
REM     in any source project (or demo/test project) aborts the run.
REM ===============================================================
setlocal enabledelayedexpansion

cd /d "%~dp0"

set "OUTPUT_DIR=artifacts"
set "PROJECTS=Mud.Feishu.Abstractions Mud.Feishu.DataModels Mud.Feishu.EventCallback Mud.Feishu.OpenTelemetry Mud.Feishu.Redis Mud.Feishu.Webhook Mud.Feishu.WebSocket Mud.Feishu.Authentication Mud.Feishu"
set "VERSION_ARG="
set "VERSION_SOURCE="
set "ADD_PREVIEW=0"
set "SKIP_CHECK=0"
set "NO_PAUSE=0"
set "PS_EXE="
set "EXIT_CODE=0"

REM --------------------------------------------------------------- arguments
:parse_args
if "%~1"=="" goto :args_done
set "ARG=%~1"
if /i "!ARG!"=="/preview" (
    set "ADD_PREVIEW=1"
    shift
    goto :parse_args
)
if /i "!ARG!"=="/skipcheck" (
    set "SKIP_CHECK=1"
    shift
    goto :parse_args
)
if /i "!ARG!"=="/nopause" (
    set "NO_PAUSE=1"
    shift
    goto :parse_args
)
if /i "!ARG!"=="/?" goto :usage
if /i "!ARG!"=="/help" goto :usage
if /i "!ARG!"=="-h" goto :usage
echo !ARG! | findstr /b /c:"/" >nul
if not errorlevel 1 (
    echo Error: unknown option !ARG!
    goto :usage
)
if defined VERSION_ARG (
    echo Error: version already set to !VERSION_ARG! ^(received !ARG! again^)
    goto :usage
)
set "VERSION_ARG=!ARG!"
set "VERSION_SOURCE=command line"
shift
goto :parse_args

:args_done
echo ===============================================================
echo  Mud.Feishu publish
echo  Working directory : %cd%
echo  Started at        : %date% %time%
echo ===============================================================
echo.

REM ------------------------------------------------------ 1. environment
echo [1/6] Checking environment...
if not exist "Mud.Feishu.slnx" (
    echo   Error: not running from the repository root ^(Mud.Feishu.slnx not found^).
    set "EXIT_CODE=1"
    goto :finish
)
where dotnet >nul 2>nul
if errorlevel 1 (
    echo   Error: 'dotnet' was not found. Install the .NET SDK first.
    set "EXIT_CODE=1"
    goto :finish
)
where pwsh >nul 2>nul
if not errorlevel 1 set "PS_EXE=pwsh"
if not defined PS_EXE (
    where powershell >nul 2>nul
    if not errorlevel 1 set "PS_EXE=powershell"
)
if not defined PS_EXE (
    echo   Error: PowerShell ^(pwsh / powershell^) was not found.
    set "EXIT_CODE=1"
    goto :finish
)
echo   dotnet     : OK
echo   PowerShell : !PS_EXE!

REM ---------------------------------------------------------- 2. version
echo.
if not defined VERSION_ARG (
    for /f "usebackq delims=" %%V in (`dotnet msbuild "Mud.Feishu\Mud.Feishu.csproj" -getProperty:Version -nologo`) do set "VERSION_ARG=%%V"
    set "VERSION_SOURCE=Directory.Build.props"
)
if not defined VERSION_ARG (
    echo [2/6] Error: cannot evaluate the Version property from Directory.Build.props.
    set "EXIT_CODE=1"
    goto :finish
)
set "PACKAGE_VERSION=!VERSION_ARG!"
if "!ADD_PREVIEW!"=="1" (
    "!PS_EXE!" -NoProfile -Command "Get-Date -Format 'yyyyMMdd-HHmmss'" > "%TEMP%\mudfeishu-timestamp.txt" 2>nul
    set "TIMESTAMP="
    set /p TIMESTAMP=<"%TEMP%\mudfeishu-timestamp.txt"
    del /q "%TEMP%\mudfeishu-timestamp.txt" >nul 2>nul
    if not defined TIMESTAMP (
        echo [2/6] Error: cannot build the preview timestamp.
        set "EXIT_CODE=1"
        goto :finish
    )
    set "PACKAGE_VERSION=!VERSION_ARG!-preview.!TIMESTAMP!"
)
echo [2/6] Package version : !PACKAGE_VERSION! ^(from !VERSION_SOURCE!^)

REM ---------------------------------------------------------- 3. restore
echo.
echo [3/6] Restoring dependencies...
dotnet restore "Mud.Feishu.slnx" --nologo
if errorlevel 1 (
    echo   Error: restore failed.
    set "EXIT_CODE=1"
    goto :finish
)

REM ------------------------------------------------------ 4. quality gate
echo.
if "!SKIP_CHECK!"=="1" (
    echo [4/6] Quality gate SKIPPED ^(/skipcheck^).
) else (
    echo [4/6] Quality gate: scripts\verify-build.ps1
    echo       ^(build + AOT strict smoke + unit tests + format check^)
    "!PS_EXE!" -NoProfile -ExecutionPolicy Bypass -File ".\scripts\verify-build.ps1"
    if errorlevel 1 (
        echo   Error: quality gate failed - publish aborted.
        echo          Use /skipcheck only if you know what you are doing.
        set "EXIT_CODE=1"
        goto :finish
    )
)

REM -------------------------------------------------------- 5. build+pack
echo.
echo [5/6] Building solution with version !PACKAGE_VERSION!...
dotnet build "Mud.Feishu.slnx" -c Release --nologo -p:Version=!PACKAGE_VERSION!
if errorlevel 1 (
    echo   Error: build failed.
    set "EXIT_CODE=1"
    goto :finish
)

echo.
echo       Packing packages ^(9 expected^)...
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
for %%P in (%PROJECTS%) do (
    if exist "%OUTPUT_DIR%\%%P.!PACKAGE_VERSION!.nupkg" del /q "%OUTPUT_DIR%\%%P.!PACKAGE_VERSION!.nupkg"
)
set "PACK_FAILED="
for %%P in (%PROJECTS%) do (
    echo       - %%P
    dotnet pack "%%P\%%P.csproj" -c Release --nologo -o "%OUTPUT_DIR%" -p:Version=!PACKAGE_VERSION!
    if errorlevel 1 set "PACK_FAILED=!PACK_FAILED! %%P"
)
if defined PACK_FAILED (
    echo   Error: packing failed for:!PACK_FAILED!
    set "EXIT_CODE=1"
    goto :finish
)

REM ------------------------------------------------------------ 6. verify
echo.
echo [6/6] Verifying packages...
set "PACKAGE_COUNT=0"
set "MISSING_PACKAGES="
for %%P in (%PROJECTS%) do (
    if exist "%OUTPUT_DIR%\%%P.!PACKAGE_VERSION!.nupkg" (
        set /a PACKAGE_COUNT+=1
    ) else (
        set "MISSING_PACKAGES=!MISSING_PACKAGES! %%P"
    )
)
if defined MISSING_PACKAGES (
    echo   Error: missing packages for:!MISSING_PACKAGES!
    set "EXIT_CODE=1"
    goto :finish
)
echo   Produced !PACKAGE_COUNT! package^(s^):
dir "%OUTPUT_DIR%\*!PACKAGE_VERSION!.nupkg" /b

set "STALE_LIST="
for %%F in ("%OUTPUT_DIR%\*.nupkg") do (
    echo %%~nxF | findstr /c:"!PACKAGE_VERSION!." >nul
    if errorlevel 1 set "STALE_LIST=!STALE_LIST! %%~nxF"
)
if defined STALE_LIST (
    echo.
    echo   Warning: packages of other versions are still in "%OUTPUT_DIR%":
    echo            !STALE_LIST!
    echo            Do not push the whole directory blindly.
)

REM ------------------------------------------------------------- finish
:finish
echo.
if !EXIT_CODE! neq 0 (
    echo ===============================================================
    echo  PUBLISH FAILED ^(exit code !EXIT_CODE!^)
    echo ===============================================================
) else (
    echo ===============================================================
    echo  PUBLISH SUCCEEDED
    echo  Version : !PACKAGE_VERSION!
    echo  Output  : %cd%\%OUTPUT_DIR%
    echo  Finished: %date% %time%
    echo ===============================================================
    echo.
    echo  Next step - push to nuget.org:
    echo    dotnet nuget push "%OUTPUT_DIR%\*.nupkg" --api-key ^<API_KEY^> --source https://api.nuget.org/v3/index.json --skip-duplicate
)
if "!NO_PAUSE!"=="0" pause
endlocal & exit /b %EXIT_CODE%

:usage
echo.
echo Usage: publish.bat [version] [/preview] [/skipcheck] [/nopause]
echo.
echo   [version]   Package version, e.g. 3.0.0-rc3.
echo               Default: the Version evaluated from Directory.Build.props.
echo   /preview    Append "-preview.<yyyyMMdd-HHmmss>" to the version.
echo   /skipcheck  Skip the scripts\verify-build.ps1 quality gate.
echo   /nopause    Do not pause at the end.
echo.
set "EXIT_CODE=1"
goto :finish
