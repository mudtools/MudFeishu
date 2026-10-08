# -----------------------------------------------------------------------
#  作者：Mud Studio  版权所有 (c) Mud Studio 2026
#  R3-01：打包内容门禁——断言源生成器工程不产出 nupkg，测试工程不产出 nupkg。
#  用法：pwsh ./scripts/verify-pack.ps1
#  CI：dotnet pack 后紧邻一步执行。
# -----------------------------------------------------------------------

[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "./nupkg"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 3.0

$solutionRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $solutionRoot

$failures = @()

# ① 工具面源生成器**不得**以本地工程形态重新出现（R-1+2c 已把生成引擎上游化到组件侧
#    Mud.HttpUtils.Generator；本地 Mud.Feishu.AI.Tools 工程与其 driver 测试工程已摘除）。
#    本检查保留为"回归陷阱"：若有人重新引入本地引擎工程且未设 IsPackable=false，
#    它会产出 Mud.Feishu.AI.Tools.*.nupkg —— 那意味着两套引擎并存的窗口被再次打开。
$aiToolsNupkg = Get-ChildItem -Path $OutputDir -Filter "Mud.Feishu.AI.Tools.*.nupkg" -ErrorAction SilentlyContinue
if ($aiToolsNupkg) {
    $failures += "① 失败：发现 $($aiToolsNupkg.Name)——工具面生成引擎已上游化到 Mud.HttpUtils.Generator 3.0.x，" +
    "本地不应再存在 Mud.Feishu.AI.Tools 工程（两套引擎并存会产生同名 hintName 产物冲突）"
}

# ② 测试工程不得产出 nupkg
$testNupkg = Get-ChildItem -Path $OutputDir -Filter "Mud.Feishu.*.Tests.*.nupkg" -ErrorAction SilentlyContinue
if ($testNupkg) {
    $failures += "R3-01 ② 失败：测试工程不应产出 nupkg，但发现 $($testNupkg.Name)——Tests/Directory.Build.props 的 IsPackable=false 被遮蔽"
}

# ③ 其余 src 包必须含 lib/<tfm>/（形态正确性）
$expectedSrcPackages = @(
    "Mud.Feishu.AI",
    "Mud.Feishu.AI.FeishuTools"
)
foreach ($pkgId in $expectedSrcPackages) {
    $nupkg = Get-ChildItem -Path $OutputDir -Filter "$pkgId.*.nupkg" -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $nupkg) {
        # 可能在未打包时跳过——仅在 nupkg 存在时检查内容
        continue
    }
    # 解包检查 lib/ 目录存在
    $tempDir = Join-Path $env:TEMP "verify-pack-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
    try {
        Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
        [System.IO.Compression.ZipFile]::ExtractToDirectory($nupkg.FullName, $tempDir)
        $libDir = Get-ChildItem -Path $tempDir -Recurse -Directory -Filter "lib" -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $libDir) {
            $failures += "R3-01 ③ 失败：$pkgId 包内未找到 lib/ 目录——打包形态异常"
        }
    }
    finally {
        if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue }
    }
}

# ④ 每个产出的 src 包 Description 非空
foreach ($pkgId in $expectedSrcPackages) {
    $nupkg = Get-ChildItem -Path $OutputDir -Filter "$pkgId.*.nupkg" -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $nupkg) { continue }
    $nuspecPath = Join-Path $env:TEMP "verify-pack-nuspec-$([System.Guid]::NewGuid().ToString('N').Substring(0,8)).xml"
    try {
        Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
        $zip = [System.IO.Compression.ZipFile]::OpenRead($nupkg.FullName)
        $nuspecEntry = $zip.Entries | Where-Object { $_.Name -like "*.nuspec" } | Select-Object -First 1
        if ($nuspecEntry) {
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($nuspecEntry, $nuspecPath, $true)
            [xml]$nuspec = Get-Content $nuspecPath -Raw
            $desc = $nuspec.package.metadata.description
            if ([string]::IsNullOrWhiteSpace($desc)) {
                $failures += "R3-01 ④ 失败：$pkgId 包 Description 为空"
            }
        }
        $zip.Dispose()
    }
    finally {
        if (Test-Path $nuspecPath) { Remove-Item $nuspecPath -Force -ErrorAction SilentlyContinue }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "## verify-pack: FAIL ($($failures.Count) 项)" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "## verify-pack: PASS" -ForegroundColor Green
Write-Host "  - 本地工具面生成引擎工程未复活（R-1+2c：生成引擎归属 Mud.HttpUtils.Generator）"
Write-Host "  - 测试工程未产出 nupkg"
Write-Host "  - src 包形态与元数据检查通过"
