<#
.SYNOPSIS
    运行 Mud.HttpUtils.JsonContextScaffolder，为 Mud.Feishu.DataModels 项目生成 JsonSerializerContext 源文件。
.DESCRIPTION
    脚手架工具以 NuGet 包形式分发（dotnet tool，命令名 mud-jsonctx）。本脚本不依赖任何本地源码/构建路径，
    可在任意机器、任意克隆目录下运行：
      1. 自动定位仓库内的目标项目（基于脚本所在目录，而非硬编码绝对路径）；
      2. 检测 mud-jsonctx 是否可用，缺失时按需安装（全局 dotnet tool）；
      3. 运行工具扫描 [HttpJsonSerializable]，按 SerializerClassName 分组生成 *_JsonContext.g.cs。
    生成的文件应提交版本控制（仅在 [HttpJsonSerializable] 标注新增/变更时重跑）。
    详见工具文档（NuGet 包 README / 仓库 Tools/Mud.HttpUtils.JsonContextScaffolder/README.md）。

    可选：通过 .csproj 中 <MudEnableJsonContextScaffolder>true</MudEnableJsonContextScaffolder> 启用内置 MSBuild 目标，
    使脚手架随消费方构建自动运行（无需手动跑本脚本）。本脚本适用于“手动生成并签入 .g.cs”的工作流。
#>

param(
    # NuGet 包 Id（仅用于自动安装）
    [string]$ToolPackageId = "Mud.HttpUtils.JsonContextScaffolder",
    # 自动安装时使用的包版本；留空表示安装最新稳定版
    [string]$ToolVersion = "",
    # 目标数据模型项目（相对脚本目录或绝对路径均可）
    [string]$TargetProject = "Mud.Feishu.DataModels/Mud.Feishu.DataModels.csproj",
    # 生成输出目录（相对脚本目录或绝对路径均可）
    [string]$OutputDir = "Mud.Feishu.DataModels/Generated",
    # 自动补全同程序集内的多态派生类
    [switch]$AutoDerivedTypes = $true,
    # 仅预览不写入
    [switch]$DryRun = $false,
    # 当 mud-jsonctx 未安装时，自动安装为全局 dotnet tool（默认开启）
    [switch]$InstallTool = $true,
    # 跳过自动安装（使用已存在的工具，缺失则报错）
    [switch]$NoInstall = $false
)

# 仓库根目录 = 脚本所在目录（与脚本位置绑定，跨机器/任意克隆路径均有效）
$RepoRoot = $PSScriptRoot

# 将相对路径解析为基于仓库根目录的绝对路径
function Resolve-RepoPath([string]$p) {
    if ([System.IO.Path]::IsPathRooted($p)) { return $p }
    return Join-Path $RepoRoot $p
}

$TargetProject = Resolve-RepoPath $TargetProject
$OutputDir     = Resolve-RepoPath $OutputDir

if (-not (Test-Path $TargetProject)) {
    Write-Error "找不到目标项目：$TargetProject"
    exit 1
}

# ---------------------------------------------------------------------------
# 定位 / 安装脚手架工具（dotnet tool 方式，不依赖本地源码路径）
# ---------------------------------------------------------------------------
$toolCmd = "mud-jsonctx"

function Test-ToolAvailable {
    try {
        $null = Get-Command $toolCmd -ErrorAction Stop
        return $true
    } catch {
        return $false
    }
}

if (-not (Test-ToolAvailable)) {
    if ($NoInstall -or -not $InstallTool) {
        Write-Error "未找到命令 '$toolCmd'。请先安装工具：" +
                    "`n  dotnet tool install --global $ToolPackageId" +
                    "`n或传入 -InstallTool 让脚本自动安装（默认即会自动安装）。"
        exit 1
    }

    Write-Host "未找到 '$toolCmd'，开始安装全局 dotnet tool：$ToolPackageId …"
    $installArgs = @("tool", "install", "--global", $ToolPackageId)
    if ($ToolVersion) { $installArgs += "--version"; $installArgs += $ToolVersion }

    & dotnet @installArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "工具安装失败，退出码：$LASTEXITCODE"
        exit $LASTEXITCODE
    }

    # 安装后确保工具所在目录在 PATH 中（dotnet tools 通常已在用户 PATH）
    $dotnetToolsPath = Join-Path $env:USERPROFILE ".dotnet\tools"
    if ($env:PATH -notlike "*$dotnetToolsPath*") {
        $env:PATH = "$dotnetToolsPath;$env:PATH"
    }

    if (-not (Test-ToolAvailable)) {
        Write-Error "安装后仍未找到命令 '$toolCmd'。请确认 dotnet tools 路径已加入 PATH：$dotnetToolsPath"
        exit 1
    }
}

# ---------------------------------------------------------------------------
# 组装并运行工具参数
# ---------------------------------------------------------------------------
$toolArgs = @(
    "--project", (Resolve-Path $TargetProject)
    "-o", $OutputDir
)
if ($AutoDerivedTypes) { $toolArgs += "--auto-derived-types" }
if ($DryRun)           { $toolArgs += "--dry-run" }

Write-Host "==== Mud.HttpUtils JsonContext Scaffolder ===="
Write-Host "命令      : $toolCmd (dotnet tool: $ToolPackageId)"
Write-Host "目标项目  : $TargetProject"
Write-Host "输出目录  : $OutputDir"
Write-Host "自动派生  : $AutoDerivedTypes"
Write-Host "Dry run   : $DryRun"
Write-Host ""

& $toolCmd @toolArgs
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
    Write-Error "脚手架执行失败，退出码：$exitCode"
    exit $exitCode
}

Write-Host ""
Write-Host "==== 完成 ===="
if (-not $DryRun) {
    Write-Host "生成的 Context 文件位于：$OutputDir"
    Write-Host "请将其加入版本控制：git add $OutputDir"
}
