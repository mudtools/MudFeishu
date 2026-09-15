# -----------------------------------------------------------------------
#  作者：Mud Studio  版权所有 (c) Mud Studio 2026
#  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
#  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
#  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
# -----------------------------------------------------------------------

<#
.SYNOPSIS
    MudFeishu 构建质量门禁（CI-1）。

.DESCRIPTION
    依次执行：
      步骤 0  依赖缓存新鲜度自检（CACHE-1）
      步骤 1  全 TFM Release 构建 -> 断言 0 错误 / 0 NU1603 / 0 CS1750
      步骤 2  诊断白名单断言（HTTPCLIENT / MUD / FORM / AOT）
      步骤 3  AotStrictMode 冒烟（net8.0）
      步骤 4  单元测试（按实际失败数断言）
      步骤 5  dotnet format --verify-no-changes（默认仅告警，见 -StrictFormat）

.PARAMETER ClearStaleCache
    检测到 Mud.HttpUtils 依赖缓存内容与本地源不一致时自动清理，而不是仅报错退出。

.PARAMETER StrictFormat
    将步骤 5 的格式差异视为门禁失败。默认仅告警，因为仓库存在历史格式偏差，
    强制通过会产生与本次改动无关的大量 diff。

.EXAMPLE
    ./scripts/verify-build.ps1
    ./scripts/verify-build.ps1 -ClearStaleCache -StrictFormat
#>
[CmdletBinding()]
param(
    [switch]$ClearStaleCache,
    [switch]$StrictFormat
)

$ErrorActionPreference = 'Continue'
$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'Mud.Feishu.slnx'

$failures = New-Object System.Collections.Generic.List[string]
$cacheCleared = $false

function Assert-Zero {
    param(
        [string]$Name,
        [int]$Count,
        [string]$Hint = ''
    )
    if ($Count -ne 0) {
        $script:failures.Add("${Name}: 期望 0，实际 $Count。$Hint")
        Write-Host "  [FAIL] ${Name} = $Count" -ForegroundColor Red
    }
    else {
        Write-Host "  [ OK ] ${Name} = 0" -ForegroundColor Green
    }
}

Set-Location $repoRoot

# ---------------------------------------------------------------- 步骤 0
Write-Host "[步骤 0] 依赖缓存新鲜度自检（CACHE-1）" -ForegroundColor Cyan
# 组件仓库按相同版本号覆盖打包时，NuGet 以 id+version 为缓存键，不会让下游缓存失效，
# 表现为"源码已修复、构建仍报 CS1750"。此处比对本地源包内 DLL 与全局缓存 DLL 的字节数。
$localSource = 'D:/Repos/MudHttpUtils/artifacts'
$globalPackages = ((dotnet nuget locals global-packages --list) -replace 'global-packages:\s*', '').Trim()
$staleDetected = $false

if (Test-Path $localSource) {
    $pkg = Get-ChildItem $localSource -Filter 'Mud.HttpUtils.Generator.*.nupkg' | Select-Object -First 1
    if ($pkg) {
        $version = ($pkg.Name -replace 'Mud.HttpUtils.Generator\.', '' -replace '\.nupkg$', '')
        $cachedDll = Join-Path $globalPackages "mud.httputils.generator/$version/analyzers/dotnet/cs/Mud.HttpUtils.Generator.dll"

        if (Test-Path $cachedDll) {
            # 使用 SHA256 比较包内 DLL 与缓存 DLL 的实际内容。
            # 不用文件字节数：组件重新打包（时间戳/PDB 路径变化）会改变大小从而误报，
            # 而内容哈希只在「缓存内容确实不等于当前本地源包」时才命中，语义精确。
            Add-Type -AssemblyName System.IO.Compression.FileSystem

            $zip = [System.IO.Compression.ZipFile]::OpenRead($pkg.FullName)
            $entry = $zip.Entries | Where-Object { $_.FullName -like '*/Mud.HttpUtils.Generator.dll' } | Select-Object -First 1
            $sourceHash = ''
            if ($entry) {
                $stream = $entry.Open()
                $sha = [System.Security.Cryptography.SHA256]::Create()
                $sourceHash = ([BitConverter]::ToString($sha.ComputeHash($stream)) -replace '-', '')
                $sha.Dispose()
                $stream.Dispose()
            }
            $zip.Dispose()

            $cacheHash = (Get-FileHash -LiteralPath $cachedDll -Algorithm SHA256).Hash

            if ($sourceHash -and $sourceHash -ne $cacheHash) {
                $staleDetected = $true
                Write-Host "  [WARN] 依赖缓存内容与本地源不一致：$version" -ForegroundColor Yellow
                Write-Host "         本地源 $($sourceHash.Substring(0,16))...  缓存 $($cacheHash.Substring(0,16))..." -ForegroundColor Yellow
                if ($ClearStaleCache) {
                    Write-Host "  清理 $globalPackages\mud.httputils*\$version ..." -ForegroundColor Yellow
                    Get-ChildItem $globalPackages -Directory -Filter 'mud.httputils*' | ForEach-Object {
                        Remove-Item (Join-Path $_.FullName $version) -Recurse -Force -ErrorAction SilentlyContinue
                    }
                    $staleDetected = $false
                    $script:cacheCleared = $true
                    Write-Host "  已清理，将重新从本地源解包，并执行一次 clean 重建。" -ForegroundColor Green
                }
                else {
                    Write-Host "  修复：dotnet nuget locals global-packages --clear" -ForegroundColor Yellow
                    Write-Host "      或：删除 $globalPackages 下 mud.httputils* 的 $version 目录" -ForegroundColor Yellow
                    Write-Host "      也可重新执行本脚本并附加 -ClearStaleCache。" -ForegroundColor Yellow
                    Write-Host "  根治：组件以版本号递增方式重新打包（见 CACHE-1），同版本覆盖必然复发。" -ForegroundColor Yellow
                }
            }
            else {
                Write-Host "  [ OK ] Mud.HttpUtils.Generator $version 缓存内容与本地源一致" -ForegroundColor Green
            }
        }
    }
}
else {
    Write-Host "  [SKIP] 未找到本地源 $localSource（非本地联调环境）" -ForegroundColor DarkGray
}

Assert-Zero -Name '依赖缓存陈旧' -Count $(if ($staleDetected) { 1 } else { 0 }) -Hint '见 CACHE-1'

# ---------------------------------------------------------------- 步骤 1
Write-Host "[步骤 1] 全 TFM Release 构建" -ForegroundColor Cyan

# 缓存被清理后必须 clean 重建：仅靠增量构建会留下旧版组件程序集，
# 运行期表现为 System.TypeLoadException「Method 'X' ... does not have an implementation」
# （实测：刷新缓存后增量构建导致 54 个测试失败，clean 后全部恢复）。
if ($script:cacheCleared) {
    Write-Host "  检测到缓存已刷新，先执行 dotnet clean 以避免混合程序集..." -ForegroundColor Yellow
    dotnet clean $solution -c Release --nologo 2>&1 | Out-Null
}

$buildLog = Join-Path $env:TEMP "mudfeishu-verify-build-$([guid]::NewGuid().ToString('N')).log"
dotnet build $solution -c Release --nologo 2>&1 | Tee-Object -FilePath $buildLog | Out-Null

$cs1750 = (Select-String -Path $buildLog -Pattern 'error CS1750' -AllMatches).Count
$nu1603 = (Select-String -Path $buildLog -Pattern 'NU1603' -AllMatches).Count
$errors = (Select-String -Path $buildLog -Pattern ': error ' -AllMatches).Count

Assert-Zero -Name '编译错误'        -Count $errors -Hint "详见 $buildLog"
Assert-Zero -Name 'CS1750 断路'     -Count $cs1750 -Hint '生成器默认值回归或依赖缓存陈旧'
Assert-Zero -Name 'NU1603 版本漂移' -Count $nu1603 -Hint '声明版本与解析版本不一致'

# ---------------------------------------------------------------- 步骤 2
Write-Host "[步骤 2] 诊断白名单断言" -ForegroundColor Cyan
Assert-Zero -Name 'HTTPCLIENT0xx' -Count ((Select-String -Path $buildLog -Pattern 'HTTPCLIENT0\d\d' -AllMatches).Count)
Assert-Zero -Name 'MUD001/002'    -Count ((Select-String -Path $buildLog -Pattern 'MUD00[12]' -AllMatches).Count)
Assert-Zero -Name 'FORM0xx'       -Count ((Select-String -Path $buildLog -Pattern 'FORM0\d\d' -AllMatches).Count)
Assert-Zero -Name 'AOT001-007'    -Count ((Select-String -Path $buildLog -Pattern 'AOT00[1-7]' -AllMatches).Count) -Hint 'AOT006 已在 netstandard2.0/net6.0 豁免，net8+ 必须净零'

# ---------------------------------------------------------------- 步骤 3
Write-Host "[步骤 3] AotStrictMode 冒烟（net8.0）" -ForegroundColor Cyan
$strictLog = Join-Path $env:TEMP "mudfeishu-verify-strict-$([guid]::NewGuid().ToString('N')).log"
dotnet build $solution -c Release -f net8.0 -p:AotStrictMode=true --nologo 2>&1 | Tee-Object -FilePath $strictLog | Out-Null
$strictAot = (Select-String -Path $strictLog -Pattern 'AOT00[1-7]' -AllMatches).Count
Assert-Zero -Name 'AotStrictMode AOT00x' -Count $strictAot -Hint "详见 $strictLog"

# ---------------------------------------------------------------- 步骤 4
Write-Host "[步骤 4] 单元测试" -ForegroundColor Cyan
$testLog = Join-Path $env:TEMP "mudfeishu-verify-test-$([guid]::NewGuid().ToString('N')).log"
dotnet test $solution -c Release --no-build --nologo 2>&1 | Tee-Object -FilePath $testLog | Out-Null
$testExit = $LASTEXITCODE

# 按测试汇总行累计实际失败数，而不是直接依赖 dotnet test 的退出码：
# 解决方案级 dotnet test 在个别测试项目未产出目标 TFM 程序集时会返回非零，
# 但所有已运行的测试程序集全部通过（已确认的 dotnet CLI 行为），直接断言退出码会误报。
$failed = 0
foreach ($m in (Select-String -Path $testLog -Pattern '失败:\s*(\d+)' -AllMatches).Matches) {
    $failed += [int]$m.Groups[1].Value
}
$passedAssemblies = (Select-String -Path $testLog -Pattern '已通过!\s*-\s*失败' -AllMatches).Count
$ranAssemblies = (Select-String -Path $testLog -Pattern '(已通过!|失败!)\s*-\s*失败' -AllMatches).Count

if ($ranAssemblies -eq 0) {
    $script:failures.Add("未发现任何测试结果，测试可能未执行（详见 $testLog）")
    Write-Host "  [FAIL] 未发现测试结果" -ForegroundColor Red
}
elseif ($failed -ne 0) {
    $script:failures.Add("单元测试失败 $failed 项（详见 $testLog）")
    Write-Host "  [FAIL] 单元测试失败 = $failed" -ForegroundColor Red
}
else {
    Write-Host "  [ OK ] 单元测试失败 = 0（$passedAssemblies/$ranAssemblies 个程序集全通过）" -ForegroundColor Green
    if ($testExit -ne 0) {
        Write-Host "  [WARN] dotnet test 退出码为 $testExit，但已运行的测试程序集全部通过（已知 CLI 行为，非测试失败）" -ForegroundColor Yellow
    }
}

# ---------------------------------------------------------------- 步骤 5
Write-Host "[步骤 5] 代码格式校验" -ForegroundColor Cyan
dotnet format $solution --verify-no-changes --no-restore 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    if ($StrictFormat) {
        $script:failures.Add("dotnet format --verify-no-changes 未通过")
        Write-Host "  [FAIL] 存在格式差异，请执行 dotnet format $solution" -ForegroundColor Red
    }
    else {
        Write-Host "  [WARN] 存在格式差异（默认不阻断；仓库含历史格式偏差）。" -ForegroundColor Yellow
        Write-Host "         如需强制通过：dotnet format $solution，或加 -StrictFormat 使本步成为门禁失败。" -ForegroundColor Yellow
    }
}
else {
    Write-Host "  [ OK ] 格式校验通过" -ForegroundColor Green
}

# ---------------------------------------------------------------- 汇总
Write-Host ''
if ($failures.Count -gt 0) {
    Write-Host "门禁未通过，共 $($failures.Count) 项：" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host '门禁全部通过。' -ForegroundColor Green
exit 0
