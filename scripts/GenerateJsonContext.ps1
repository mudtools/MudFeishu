<#
.SYNOPSIS
    Runs Mud.HttpUtils.JsonContextScaffolder to generate JsonSerializerContext source files
    for the Mud.Feishu.DataModels project.
.DESCRIPTION
    The scaffolder is distributed as a NuGet dotnet tool (command: mud-jsonctx). This script
    does NOT depend on any local source/build path, so it works on any machine / any clone
    location:
      1. Resolves the target project relative to the script's own directory (no hardcoded paths).
      2. Detects the tool (global `mud-jsonctx` or local `dotnet mud-jsonctx`), installing it
         globally when missing (unless -NoInstall).
      3. Runs the tool to scan [HttpJsonSerializable] and emit *_JsonContext.g.cs grouped by
         SerializerClassName.
    Generated files should be committed (re-run only when [HttpJsonSerializable] annotations
    are added/changed).
    See the tool README (NuGet package / repo Tools/Mud.HttpUtils.JsonContextScaffolder) for details.

    Alternative: enable the built-in MSBuild target in the .csproj
    (<MudEnableJsonContextScaffolder>true</MudEnableJsonContextScaffolder>) to run the scaffolder
    automatically during the consumer build. This script is for the "generate and commit .g.cs"
    workflow.
#>

param(
    # NuGet package id (used only for auto-install)
    [string]$ToolPackageId = "Mud.HttpUtils.JsonContextScaffolder",
    # Version to install; empty = latest stable
    [string]$ToolVersion = "",
    # Target data-model project (relative to script dir or absolute)
    [string]$TargetProject = "Mud.Feishu.DataModels/Mud.Feishu.DataModels.csproj",
    # Output directory for generated context files (relative to script dir or absolute)
    [string]$OutputDir = "Mud.Feishu.DataModels/Generated",
    # Auto-complete polymorphic derived types within the same assembly
    [switch]$AutoDerivedTypes = $true,
    # Preview only, do not write files
    [switch]$DryRun = $false,
    # Auto-install the tool as a global dotnet tool when missing (default on)
    [switch]$InstallTool = $true,
    # Do not auto-install; error if the tool is missing
    [switch]$NoInstall = $false
)

# Repo root = script directory (bound to the script location, works on any clone path)
$RepoRoot = $PSScriptRoot

# Fix working directory to the repo root so relative paths and any local dotnet tool
# manifest are resolved consistently.
Set-Location $RepoRoot

# Resolve a path relative to the repo root into an absolute path.
function Resolve-RepoPath([string]$p) {
    if ([System.IO.Path]::IsPathRooted($p)) { return $p }
    return Join-Path $RepoRoot $p
}

$TargetProject = Resolve-RepoPath $TargetProject
$OutputDir     = Resolve-RepoPath $OutputDir

if (-not (Test-Path $TargetProject)) {
    Write-Error "Target project not found: $TargetProject"
    exit 1
}

# ---------------------------------------------------------------------------
# Locate / install the scaffolder tool (dotnet tool, no local source path).
# Supports both install shapes:
#   - global tool: `mud-jsonctx` on PATH
#   - local tool : `dotnet mud-jsonctx` within a dotnet-tools manifest directory
# ---------------------------------------------------------------------------

# Returns the invocation array for the tool, or $null if not found.
function Find-ToolInvocation {
    # 1) global: command on PATH
    try {
        $null = Get-Command "mud-jsonctx" -ErrorAction Stop
        return @("mud-jsonctx")
    } catch { }

    # 2) local: dotnet can resolve the manifest tool (run from manifest dir or subdir)
    try {
        $out = & dotnet mud-jsonctx --help 2>&1
        if ($LASTEXITCODE -eq 0) {
            return @("dotnet", "mud-jsonctx")
        }
    } catch { }

    return $null
}

$toolInvocation = Find-ToolInvocation

if ($null -eq $toolInvocation) {
    if ($NoInstall -or -not $InstallTool) {
        Write-Error ("Tool 'mud-jsonctx' not found (neither global nor local). Install it first:" +
                     "  global: dotnet tool install --global $ToolPackageId" +
                     "  local : dotnet new tool-manifest; dotnet tool install $ToolPackageId" +
                     "  or pass -InstallTool to let this script install it automatically.")
        exit 1
    }

    Write-Host "Tool '$ToolPackageId' not detected, installing as global dotnet tool..."
    $installArgs = @("tool", "install", "--global", $ToolPackageId)
    if ($ToolVersion) { $installArgs += "--version"; $installArgs += $ToolVersion }

    & dotnet @installArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Tool installation failed, exit code: $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    $dotnetToolsPath = Join-Path $env:USERPROFILE ".dotnet\tools"
    if ($env:PATH -notlike "*$dotnetToolsPath*") {
        $env:PATH = "$dotnetToolsPath;$env:PATH"
    }

    $toolInvocation = Find-ToolInvocation
    if ($null -eq $toolInvocation) {
        Write-Error "Tool still not found after install. Ensure dotnet tools path is on PATH: $dotnetToolsPath"
        exit 1
    }
}

$toolMode = if ($toolInvocation.Count -eq 1) { "global (mud-jsonctx)" } else { "local (dotnet mud-jsonctx)" }
Write-Host "Detected tool invocation: $toolMode"

# ---------------------------------------------------------------------------
# Build and run the tool arguments
# ---------------------------------------------------------------------------
$toolArgs = @(
    "--project", (Resolve-Path $TargetProject)
    "-o", $OutputDir
)
if ($AutoDerivedTypes) { $toolArgs += "--auto-derived-types" }
if ($DryRun)           { $toolArgs += "--dry-run" }

Write-Host "==== Mud.HttpUtils JsonContext Scaffolder ===="
Write-Host "Command   : $($toolInvocation -join ' ') (dotnet tool: $ToolPackageId)"
Write-Host "Project   : $TargetProject"
Write-Host "Output    : $OutputDir"
Write-Host "AutoDerived: $AutoDerivedTypes"
Write-Host "Dry run   : $DryRun"
Write-Host ""

& $toolInvocation @toolArgs
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
    Write-Error "Scaffolder failed, exit code: $exitCode"
    exit $exitCode
}

Write-Host ""
Write-Host "==== Done ===="
if (-not $DryRun) {
    Write-Host "Generated context files are in: $OutputDir"
    Write-Host "Add them to version control, e.g.: git add $OutputDir"
}
