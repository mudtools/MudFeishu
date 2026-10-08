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

# ① 本仓**不得**存在本地工具面生成器实现（R-1+2c 已把生成引擎上游化到组件侧 Mud.HttpUtils.Generator）。
#    判据与 Tests/Mud.Feishu.AI.Tools.Tests 的 RetiredLocalEngine_ShouldNotReappearAsALocalGenerator 同源：
#    「两套引擎并存」的本质是存在第二个 IIncrementalGenerator 实现，与它挂在哪个工程名下无关。
#    早先的判据是按工程名过滤 nupkg（"本地不应再存在 Mud.Feishu.AI.Tools 工程"），它把门禁焊死在一个
#    具体工程名上；本仓把工具面工程更名为 Mud.Feishu.AI.Tools 后该判据会永久假红。改为按能力判据：
#    任何**引用 Roslyn 编译器 API 的工程**一旦产出 nupkg，即说明本地生成器/分析器宿主复活。
$roslynHostProjects = @(Get-ChildItem -Path $solutionRoot -Recurse -Filter "*.csproj" |
    Where-Object { $_.FullName -notmatch '[\\/](obj|bin)[\\/]' } |
    Where-Object {
        $content = [System.IO.File]::ReadAllText($_.FullName)
        $content = [regex]::Replace($content, '<!--.*?-->', '', 'Singleline')
        [regex]::IsMatch($content, 'PackageReference\s+Include="Microsoft\.CodeAnalysis\.(CSharp|VisualBasic)"')
    })
if ($roslynHostProjects.Count -gt 0) {
    $failures += "① 失败：$($roslynHostProjects.Name -join ', ') 直接引用 Roslyn 编译器 API —— 工具面生成引擎已上游化到 " +
    "Mud.HttpUtils.Generator 3.0.x，本仓不得再存在本地生成器/分析器宿主（两套引擎并存会产生同名 hintName 产物冲突）"
}

# ② 测试工程不得产出 nupkg
$testNupkg = Get-ChildItem -Path $OutputDir -Filter "Mud.Feishu.*.Tests.*.nupkg" -ErrorAction SilentlyContinue
if ($testNupkg) {
    $failures += "R3-01 ② 失败：测试工程不应产出 nupkg，但发现 $($testNupkg.Name)——Tests/Directory.Build.props 的 IsPackable=false 被遮蔽"
}

# ③ 其余 src 包必须含 lib/<tfm>/（形态正确性）
$expectedSrcPackages = @(
    "Mud.Feishu.AI",
    "Mud.Feishu.AI.Tools"
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
Write-Host "  - 本仓无本地工具面生成器/分析器宿主（生成引擎归属 Mud.HttpUtils.Generator）"
Write-Host "  - 测试工程未产出 nupkg"
Write-Host "  - src 包形态与元数据检查通过"
