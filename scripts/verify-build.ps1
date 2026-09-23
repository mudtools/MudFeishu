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
      步骤 5  Redis 集成测试（Docker 可用时逐 (工程, TFM) 运行，断言 total>0 / failed=0 / skipped=0；
              无 Docker 时告警并登记覆盖缺口）——T-R2-04
      步骤 6  dotnet format --verify-no-changes（默认仅告警，见 -StrictFormat）

.PARAMETER ClearStaleCache
    检测到 Mud.HttpUtils 依赖缓存内容与本地源不一致时自动清理，而不是仅报错退出。

.PARAMETER StrictFormat
    将步骤 6 的格式差异视为门禁失败。默认仅告警，因为仓库存在历史格式偏差，
    强制通过会产生与本次改动无关的大量 diff。

.PARAMETER CacheCheckOnly
    仅执行步骤 0（依赖缓存新鲜度自检）后退出。供 CI 在 Restore 之前调用
    （CI runner 为全新环境，本地源通常不存在，此时会输出 [SKIP] 并通过）。

.EXAMPLE
    ./scripts/verify-build.ps1
    ./scripts/verify-build.ps1 -ClearStaleCache -StrictFormat
    ./scripts/verify-build.ps1 -CacheCheckOnly
#>
[CmdletBinding()]
param(
    [switch]$ClearStaleCache,
    [switch]$StrictFormat,
    [switch]$CacheCheckOnly
)

$ErrorActionPreference = 'Continue'
$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'Mud.Feishu.slnx'

$failures = New-Object System.Collections.Generic.List[string]
$cacheCleared = $false
# T-R2-04：Redis 集成测试覆盖缺口说明（Docker 不可用或运行时缺失时登记，供汇总与 CI 日志捕获）
$redisIntegrationGap = ''

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

# 版本号以仓库 csproj 中的实际声明为准，而不是"artifacts 里的第一个包"：
# CACHE-1 事故期间 artifacts 曾同时存在多个历史版本，取第一个会校验到错误的包并给出误导性结论。
$declaredVersion = ''
$csprojFiles = Get-ChildItem -Path $repoRoot -Recurse -Filter '*.csproj' -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } |
    Select-Object -ExpandProperty FullName
if ($csprojFiles) {
    $declaration = Select-String -Path $csprojFiles -Pattern 'Mud\.HttpUtils\.Generator"\s+Version="([^"]+)"' |
        Select-Object -First 1
    if ($declaration) {
        $declaredVersion = $declaration.Matches[0].Groups[1].Value
    }
}

if (Test-Path $localSource) {
    $pkg = if ($declaredVersion) {
        Get-ChildItem $localSource -Filter "Mud.HttpUtils.Generator.$declaredVersion.nupkg" | Select-Object -First 1
    }
    else {
        Get-ChildItem $localSource -Filter 'Mud.HttpUtils.Generator.*.nupkg' | Select-Object -First 1
    }

    if (-not $pkg -and $declaredVersion) {
        Write-Host "  [WARN] 本地源 $localSource 中缺少 csproj 声明的 Mud.HttpUtils.Generator $declaredVersion。" -ForegroundColor Yellow
        Write-Host "         声明版本与本地源不一致时还原会回退到 nuget.org 或直接失败，请先发布对应版本的组件包。" -ForegroundColor Yellow
    }

    if ($pkg) {
        $version = ($pkg.Name -replace 'Mud.HttpUtils.Generator\.', '' -replace '\.nupkg$', '')
        $cachedDll = Join-Path $globalPackages "mud.httputils.generator/$version/analyzers/dotnet/cs/Mud.HttpUtils.Generator.dll"

        if (-not (Test-Path $cachedDll)) {
            Write-Host "  [ OK ] 缓存中尚无 $version，将由本次还原首次解包（无陈旧风险）" -ForegroundColor Green
        }

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

if ($CacheCheckOnly) {
    Write-Host ''
    if ($failures.Count -gt 0) {
        Write-Host "缓存自检未通过，共 $($failures.Count) 项：" -ForegroundColor Red
        $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
        exit 1
    }
    Write-Host '缓存自检通过（-CacheCheckOnly）。' -ForegroundColor Green
    exit 0
}

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
Write-Host "[步骤 3] AotStrictMode 冒烟（net8.0，源项目）" -ForegroundColor Cyan
# 两处必须注意，否则本步骤会「假绿」：
#  ① 不能对整个解决方案使用 -f net8.0。Demos 下存在单 TFM 项目（net9.0/net10.0/net8.0 各不相同），
#     MSBuild 会报 NETSDK1005「资产文件没有 net8.0 的目标」，构建实际失败；
#  ② 只断言 AOT00x 计数会掩盖上述失败（构建失败时 AOT00x 天然为 0），必须同时断言错误数=0。
#     历史实现即因此长期输出 [ OK ]，实际从未完成过一次严格的 AOT 冒烟。
# 因此改为逐个构建**源项目**（均含 net8.0 目标），并同时断言错误数与 IL2026/IL3050。
# 注意：只取仓库根下的 Mud.Feishu* **源项目目录**（Tests/Demos 不在 AOT-3 范围内：
#   - Tests 被 Tests/Directory.Build.props 遮蔽，不启用 AOT 分析器；
#   - Demos 被 Demos/Directory.Build.props 遮蔽，且 Mud.Feishu.AotVerification 有意保留反射调用
#     以验证「用户自行使用反射 JsonSerializer 仍可工作」，纳入严格模式只会产生与库无关的噪音）。
$sourceRoots = @(Get-ChildItem -Path $repoRoot -Directory -Filter 'Mud.Feishu*' -ErrorAction SilentlyContinue)
$strictProjects = @($sourceRoots |
    ForEach-Object { Get-ChildItem -Path $_.FullName -Filter '*.csproj' -File -ErrorAction SilentlyContinue } |
    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } |
    Sort-Object FullName)
$strictLog = Join-Path $env:TEMP "mudfeishu-verify-strict-$([guid]::NewGuid().ToString('N')).log"
if ($strictProjects.Count -eq 0) {
    $script:failures.Add('未找到任何源项目，AotStrictMode 冒烟无法执行')
    Write-Host '  [FAIL] 未找到任何源项目' -ForegroundColor Red
}
else {
    # ③ 必须强制重编译（--no-incremental）。CoreCompile 的"最新"判定只看输入/输出时间戳，
    #    **不比较 csc 命令行**；步骤 1 已用同一 TFM 构建过一次，本步骤即便带上
    #    -p:AotStrictMode=true 也会命中「正在跳过目标 CoreCompile」，于是 IL2026/IL3050
    #    一条都不产生，步骤 3 恒为 [ OK ]。实测：成功构建后再跑严格模式得到 0 错误，
    #    而同一命令加 --no-incremental 立刻暴露出 Mud.Feishu.WebSocket 的 4 条违规。
    foreach ($proj in $strictProjects) {
        Write-Host "  -> $($proj.Name)" -ForegroundColor DarkGray
        dotnet build $proj.FullName -c Release -f net8.0 -p:AotStrictMode=true --no-incremental --nologo 2>&1 |
            Tee-Object -FilePath $strictLog -Append | Out-Null
    }
    Assert-Zero -Name 'AotStrictMode 构建错误' -Count ((Select-String -Path $strictLog -Pattern ': error ' -AllMatches).Count) -Hint "详见 $strictLog"
    Assert-Zero -Name 'AotStrictMode AOT00x'    -Count ((Select-String -Path $strictLog -Pattern 'AOT00[1-7]' -AllMatches).Count) -Hint "详见 $strictLog"
    Assert-Zero -Name 'AotStrictMode IL2026'    -Count ((Select-String -Path $strictLog -Pattern 'IL2026' -AllMatches).Count) -Hint "详见 $strictLog"
    Assert-Zero -Name 'AotStrictMode IL3050'    -Count ((Select-String -Path $strictLog -Pattern 'IL3050' -AllMatches).Count) -Hint "详见 $strictLog"
}

# ---------------------------------------------------------------- 步骤 4
Write-Host "[步骤 4] 单元测试" -ForegroundColor Cyan

# TMA2-03 修复：门禁改为 TRX 解析（locale 无关），不再依赖中文摘要正则。
# 同时新增"每个测试工程都必须有结果"的断言（按 .slnx 中 Tests/** 清单逐个断言）。
$trxDir = Join-Path $env:TEMP "mudfeishu-verify-trx-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $trxDir -Force | Out-Null
$testLog = Join-Path $env:TEMP "mudfeishu-verify-test-$([guid]::NewGuid().ToString('N')).log"

# 同时设置中文 locale（兼容旧日志解析）和 TRX logger（locale 无关）
$env:DOTNET_CLI_UI_LANGUAGE = 'zh-CN'

# GATE-4 修复（2026-09-17）：改为**逐测试工程 + 逐 TFM** 运行。此前单条 `dotnet test $solution`
# 把结果目录当作 LogFileName 传入，VSTest 实际只落盘最后一次运行的 TRX（实测 7 个工程 × 2 个
# TFM 共 14 次运行仅得 1 个），造成两个假绿：
#   ① 「每个测试工程都必须有结果」的断言从未真正成立（靠日志文本匹配兜过）；
#   ② 一旦解析到任意 TRX，日志正则兜底会被跳过，其余工程/TFM 的失败用例被静默忽略。
# 同一工程多 TFM 会写同名 TRX 互相覆盖，故每个 (工程, TFM) 组合单独指定结果目录与文件名。
$testProjectPaths = @()
if (Test-Path $solution) {
    $slnxContent = Get-Content -LiteralPath $solution -Raw -Encoding utf8
    # 仅匹配 Project 条目（排除 File 条目，如 Tests/Directory.Build.props）
    foreach ($m in [regex]::Matches($slnxContent, '<Project Path="(Tests/[^"]+)"')) {
        $testProjectPaths += (Join-Path $repoRoot ($m.Groups[1].Value -replace '/', '\'))
    }
}
if ($testProjectPaths.Count -eq 0) {
    $script:failures.Add('未从 .slnx 解析到任何测试工程')
    Write-Host '  [FAIL] 未从 .slnx 解析到任何测试工程' -ForegroundColor Red
}

$testRuns = @()
foreach ($testProject in $testProjectPaths) {
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($testProject)
    # TFM 必须取**求值后**的属性：多个测试工程把 <TargetFrameworks> 放在 Tests/Directory.Build.props，
    # 只用文本正则扫自身 csproj 会得到空集，该工程会被静默跳过（实测 7 个工程只跑了 4 个）。
    $tfmRaw = (dotnet msbuild $testProject -getProperty:TargetFrameworks -p:Configuration=Release -nologo 2>$null |
        Where-Object { $_ -match '^net' } | Select-Object -Last 1)
    if (-not $tfmRaw) {
        $tfmRaw = (dotnet msbuild $testProject -getProperty:TargetFramework -p:Configuration=Release -nologo 2>$null |
            Where-Object { $_ -match '^net' } | Select-Object -Last 1)
    }
    $tfms = if ($tfmRaw) {
        @($tfmRaw.Split(';') | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    }
    else { @($null) }

    foreach ($tfm in $tfms) {
        $label = if ($tfm) { "$projectName.$tfm" } else { $projectName }
        $testRuns += [pscustomobject]@{ Label = $label; Project = $testProject; Tfm = $tfm }
    }
}

# 目标 TFM 的运行时缺失时 testhost 会「启动后中止」——此时 VSTest 仍会落盘一个 total=0 的 TRX，
# 退出码为 1。原实现对退出码 1 只发一条 WARN（注释称"已知 CLI 行为"），于是本机（仅装 .NET 8/9/10）
# 上全部 net6.0 测试实际从未执行，却一路绿灯。这里显式按运行时可用性过滤并单独播报。
$installedRuntimeMajors = @(dotnet --list-runtimes 2>$null |
    ForEach-Object { if ($_ -match '^Microsoft\.NETCore\.App (\d+)\.') { [int]$Matches[1] } } |
    Sort-Object -Unique)

# ASP.NET Core 是**独立**的共享框架：只探测 Microsoft.NETCore.App 会把"依赖 AspNetCore 的测试工程"
# （如 Mud.Feishu.Webhook.Tests 经 ProjectReference 传递依赖）误判为可运行，随后 testhost 启动即中止、
# TRX total=0，最终以误导性的"testhost 启动失败？"计入门禁失败。这里单独探测该框架族，
# 并按被测工程的**实际构建产物**（runtimeconfig.json 的 frameworks 列表）判定是否依赖它。
$installedAspNetCoreMajors = @(dotnet --list-runtimes 2>$null |
    ForEach-Object { if ($_ -match '^Microsoft\.AspNetCore\.App (\d+)\.') { [int]$Matches[1] } } |
    Sort-Object -Unique)

$testExit = 0
$skippedRuns = New-Object System.Collections.Generic.List[string]
$executedLabels = New-Object System.Collections.Generic.List[string]
$runTotals = @{}
foreach ($run in $testRuns) {
    if ($run.Tfm -and $run.Tfm -match '^net(\d+)\.') {
        $major = [int]$Matches[1]
        if ($installedRuntimeMajors -notcontains $major) {
            $skippedRuns.Add("$($run.Label)（未安装 .NET $major 运行时）")
            Write-Host "  -- $($run.Label)：跳过（未安装 .NET $major 运行时）" -ForegroundColor Yellow
            continue
        }

        # 该运行的构建产物是否声明依赖 ASP.NET Core 共享框架（传递依赖由 runtimeconfig 体现）。
        # 产物缺失时按"不依赖"处理（保持原行为，不引入回归）。
        $needsAspNetCore = $false
        $binDir = Join-Path (Split-Path -Parent $run.Project) "bin\Release\$($run.Tfm)"
        $runtimeConfig = Join-Path $binDir (([System.IO.Path]::GetFileNameWithoutExtension($run.Project)) + '.runtimeconfig.json')
        if (Test-Path $runtimeConfig) {
            $needsAspNetCore = (Get-Content -Path $runtimeConfig -Raw) -match 'Microsoft\.AspNetCore\.App'
        }
        if ($needsAspNetCore -and ($installedAspNetCoreMajors -notcontains $major)) {
            $skippedRuns.Add("$($run.Label)（未安装 ASP.NET Core $major 运行时）")
            Write-Host "  -- $($run.Label)：跳过（未安装 ASP.NET Core $major 运行时）" -ForegroundColor Yellow
            continue
        }
    }

    $runDir = Join-Path $trxDir $run.Label
    New-Item -ItemType Directory -Path $runDir -Force | Out-Null

    Write-Host "  -> $($run.Label)" -ForegroundColor DarkGray
    $testArgs = @('test', $run.Project, '-c', 'Release', '--no-build', '--nologo',
        '--results-directory', $runDir, '--logger', "trx;LogFileName=$($run.Label).trx")
    if ($run.Tfm) { $testArgs += @('-f', $run.Tfm) }
    # WSR2-14：xUnit 的 [Trait] **不会**自动排除用例——`dotnet test` 无 --filter 时执行全部。
    # 压力/长稳用例（Category=Stress：突发帧、静默存活等待）必须显式排除，否则全量门禁
    # 会变慢并引入不稳定性。手工执行方式见 documents/WebSocket/故障排查手册.md §6。
    $testArgs += @('--filter', 'Category!=Stress')
    dotnet @testArgs 2>&1 | Tee-Object -FilePath $testLog -Append | Out-Null
    if ($LASTEXITCODE -ne 0) { $testExit = $LASTEXITCODE }
    $executedLabels.Add($run.Label)
}
$testRunLabels = @($executedLabels)

if ($skippedRuns.Count -gt 0) {
    Write-Host "  [WARN] 因缺少运行时未执行的组合（本机不阻断；CI 需补齐对应运行时）: $($skippedRuns -join ', ')" -ForegroundColor Yellow
}

# --- TRX 解析（locale 无关）---
$failed = 0
$passed = 0
$total = 0
$trxAssemblies = @{}
$failedTestNames = New-Object System.Collections.Generic.List[string]

$trxFiles = Get-ChildItem -Path $trxDir -Filter '*.trx' -File -Recurse -ErrorAction SilentlyContinue
if ($trxFiles -and $trxFiles.Count -gt 0) {
    foreach ($trx in $trxFiles) {
        try {
            [xml]$doc = Get-Content -LiteralPath $trx.FullName -Raw -Encoding utf8
            # GATE-5 修复（2026-09-17）：计数器节点路径错误。TRX 的实际结构是
            # <TestRun><ResultSummary><Counters .../></ResultSummary><Results>...，
            # 原实现写成 $doc.TestRun.Results.ResultSummary.Counters（Results 之下并无 ResultSummary），
            # 恒为 $null → 从未累计任何计数 → 每次都打印"未发现 TRX 文件"并回退到
            # TMA2-03 本已废弃的中文正则解析，TRX 机制形同虚设。
            $counters = $doc.TestRun.ResultSummary.Counters
            if ($counters) {
                $failed += [int]$counters.failed
                $passed += [int]$counters.passed
                $total += [int]$counters.total
                $trxAssemblies[$trx.BaseName] = $true
                $runTotals[$trx.BaseName] = [int]$counters.total
            }
            # 收集失败用例名
            $testDefs = $doc.TestRun.TestDefinitions
            $testResults = $doc.TestRun.Results.UnitTestResult
            if ($testResults) {
                foreach ($r in $testResults) {
                    if ($r.outcome -eq 'failed') {
                        $testName = $r.testName
                        if (-not $testName -and $r.TestMethod) {
                            $testName = "$($r.TestMethod.className).$($r.TestMethod.name)"
                        }
                        $failedTestNames.Add($testName)
                    }
                }
            }
        }
        catch {
            Write-Host "  [WARN] 解析 TRX 失败: $($trx.Name) - $_" -ForegroundColor Yellow
        }
    }
}

# --- 中文摘要正则兜底（当 TRX 解析无结果时回退）---
if ($trxAssemblies.Count -eq 0) {
    Write-Host "  [WARN] 未发现 TRX 文件，回退到日志正则解析" -ForegroundColor Yellow
    foreach ($m in (Select-String -Path $testLog -Pattern '失败:\s*(\d+)' -AllMatches).Matches) {
        $failed += [int]$m.Groups[1].Value
    }
    foreach ($m in (Select-String -Path $testLog -Pattern '通过:\s*(\d+)' -AllMatches).Matches) {
        $passed += [int]$m.Groups[1].Value
    }
    $ranAssemblies = (Select-String -Path $testLog -Pattern '(已通过!|失败!)\s*-\s*失败' -AllMatches).Count
    if ($ranAssemblies -eq 0 -and $failed -eq 0) {
        # 英文回退
        foreach ($m in (Select-String -Path $testLog -Pattern 'Failed!\s*--\s*Failed:\s*(\d+)' -AllMatches).Matches) {
            $failed += [int]$m.Groups[1].Value
        }
        $ranAssemblies = (Select-String -Path $testLog -Pattern '(Passed!|Failed!)\s*--\s*Failed' -AllMatches).Count
    }
}

# --- 断言：每个（测试工程, TFM）组合都必须有结果 ---
$missingProjects = @()
foreach ($expected in $testRunLabels) {
    # GATE-4：改为按「该运行组合专属 TRX 是否存在」严格断言。
    # 原实现把"日志里出现过工程名"也算命中，而 dotnet test 正常输出必然包含工程名，
    # 断言因此恒真（即便该工程根本没产出 TRX、甚至整个工程被跳过也不会失败）。
    $projectTrx = @(Get-ChildItem -Path (Join-Path $trxDir $expected) -Filter '*.trx' -File -ErrorAction SilentlyContinue)
    if ($projectTrx.Count -eq 0) {
        $missingProjects += $expected
    }
}

if ($missingProjects.Count -gt 0) {
    $script:failures.Add("以下测试工程未产出结果: $($missingProjects -join ', ')（详见 $testLog）")
    Write-Host "  [FAIL] 未产出结果的测试工程: $($missingProjects -join ', ')" -ForegroundColor Red
}

# --- 断言：每个已执行的（工程, TFM）组合都必须真的跑过用例 ---
# testhost 启动失败（缺运行时、架构不符等）时 VSTest 仍会写出一个 total=0 的 TRX 并以退出码 1 结束。
# 仅断言"TRX 存在"会把这种"什么都没跑"当成通过；这里追加 total>0 的门槛。
$emptyRuns = @($testRunLabels | Where-Object {
        (-not $runTotals.ContainsKey($_)) -or ($runTotals[$_] -le 0)
    })
if ($emptyRuns.Count -gt 0) {
    $script:failures.Add("以下测试运行组合未执行任何用例（testhost 启动失败？）: $($emptyRuns -join ', ')")
    Write-Host "  [FAIL] 未执行任何用例的组合: $($emptyRuns -join ', ')" -ForegroundColor Red
}

if ($failed -ne 0) {
    $script:failures.Add("单元测试失败 $failed 项（详见 $testLog）")
    Write-Host "  [FAIL] 单元测试失败 = $failed" -ForegroundColor Red
    # 输出失败用例名（TRX 可直取）
    if ($failedTestNames.Count -gt 0) {
        Write-Host "  失败用例:" -ForegroundColor Red
        foreach ($name in $failedTestNames) {
            Write-Host "    - $name" -ForegroundColor Red
        }
    }
}
else {
    Write-Host "  [ OK ] 单元测试失败 = 0（通过 $passed / 总计 $total）" -ForegroundColor Green
    if ($testExit -ne 0) {
        # 已执行的组合全部有 TRX 且 total>0（上方空跑断言已把关），退出码非零只可能来自
        # 被跳过的组合之外的 CLR 级原因；如实提示而不再断言"已知 CLI 行为"。
        Write-Host "  [WARN] dotnet test 退出码为 $testExit，但已执行组合均产出有效 TRX 且未执行用例数为 0" -ForegroundColor Yellow
    }
}

# 清理 TRX 临时目录
if (Test-Path $trxDir) {
    Remove-Item $trxDir -Recurse -Force -ErrorAction SilentlyContinue
}

# ---------------------------------------------------------------- 步骤 5
Write-Host "[步骤 5] Redis 集成测试（条件必跑）" -ForegroundColor Cyan

# T-R2-04：真实 Redis 集成测试（Testcontainers.Redis）需要 Docker。
# 关键动机：集成用例在未启用环境时是**显式跳过**（RedisFactAttribute 在发现期置 Skip），
# 步骤 4 的 "total > 0" 断言无法区分"真的跑了"与"全被跳过"，因此这里单独断言
# total>0 / failed=0 / **skipped=0**，把"全跳过"这类假绿挡在门外。
# Docker 不可用时不阻断本机开发，但登记明确的覆盖缺口（供汇总与 CI 日志捕获）。
function Test-DockerAvailable {
    # docker info 在 daemon 未运行时返回非零退出码。先确认可执行文件存在
    # （避免"命令未找到"落在非终止错误流上、$LASTEXITCODE 保留陈旧值导致误判），再以退出码判定。
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { return $false }
    try {
        & docker info *> $null
        return ($LASTEXITCODE -eq 0)
    }
    catch {
        return $false
    }
}

if (-not (Test-DockerAvailable)) {
    $redisIntegrationGap = 'Docker 不可用：Redis 集成测试未执行（覆盖缺口：SeqID 生命周期/重连清理等命令语义无回归证明）'
    Write-Host "  [WARN] $redisIntegrationGap" -ForegroundColor Yellow
}
else {
    $redisRuns = @($testRuns | Where-Object { $_.Label -like '*IntegrationTests*' })
    if ($redisRuns.Count -eq 0) {
        $failures.Add('未找到 Redis 集成测试工程（应位于 .slnx 的 Tests/** 且工程名含 IntegrationTests）')
        Write-Host '  [FAIL] 未找到 Redis 集成测试工程' -ForegroundColor Red
    }
    else {
        $redisTrxDir = Join-Path $env:TEMP "mudfeishu-redis-trx-$([guid]::NewGuid().ToString('N'))"
        New-Item -ItemType Directory -Path $redisTrxDir -Force | Out-Null
        $redisTestLog = Join-Path $env:TEMP "mudfeishu-redis-test-$([guid]::NewGuid().ToString('N')).log"

        $previousRedisEnv = $env:MUDFEISHU_REDIS_TESTS
        $env:MUDFEISHU_REDIS_TESTS = '1'
        try {
            foreach ($redisRun in $redisRuns) {
                # 运行时缺失（本机仅装 8/9/10 时不会命中）→ 不能算"通过"，必须登记为缺口
                if ($redisRun.Tfm -and $redisRun.Tfm -match '^net(\d+)\.') {
                    $major = [int]$Matches[1]
                    if ($installedRuntimeMajors -notcontains $major) {
                        $script:failures.Add("Redis 集成测试 $($redisRun.Label) 无法执行：未安装 .NET $major 运行时")
                        Write-Host "  [FAIL] $($redisRun.Label)：未安装 .NET $major 运行时，无法证明真实执行" -ForegroundColor Red
                        continue
                    }
                }

                $runDir = Join-Path $redisTrxDir $redisRun.Label
                New-Item -ItemType Directory -Path $runDir -Force | Out-Null

                Write-Host "  -> $($redisRun.Label)" -ForegroundColor DarkGray
                $redisArgs = @('test', $redisRun.Project, '-c', 'Release', '--no-build', '--nologo',
                    '--results-directory', $runDir, '--logger', "trx;LogFileName=$($redisRun.Label).trx")
                if ($redisRun.Tfm) { $redisArgs += @('-f', $redisRun.Tfm) }
                dotnet @redisArgs 2>&1 | Tee-Object -FilePath $redisTestLog -Append | Out-Null

                $redisTrx = Get-ChildItem -Path $runDir -Filter '*.trx' -File -ErrorAction SilentlyContinue |
                    Select-Object -First 1
                if (-not $redisTrx) {
                    $script:failures.Add("Redis 集成测试 $($redisRun.Label) 未产出 TRX（详见 $redisTestLog）")
                    Write-Host "  [FAIL] $($redisRun.Label) 未产出 TRX（可能 testhost 启动失败）" -ForegroundColor Red
                    continue
                }

                try {
                    [xml]$redisDoc = Get-Content -LiteralPath $redisTrx.FullName -Raw -Encoding utf8
                    $counters = $redisDoc.TestRun.ResultSummary.Counters

                    $rTotal = if ($counters -and $null -ne $counters.total) { [int]$counters.total } else { 0 }
                    $rFailed = if ($counters -and $null -ne $counters.failed) { [int]$counters.failed } else { 0 }
                    # VSTest 的 <Counters> 未必暴露 skipped 属性（跳过用例多以 notExecuted 统计）；
                    # 缺失时回退 notExecuted，保证"跳过"可被观测而不是恒为 0。
                    $rawSkipped = $null
                    if ($counters) {
                        if ($null -ne $counters.skipped) { $rawSkipped = $counters.skipped }
                        elseif ($null -ne $counters.notExecuted) { $rawSkipped = $counters.notExecuted }
                    }
                    $rSkipped = if ($null -ne $rawSkipped) { [int]$rawSkipped } else { 0 }

                    $problems = New-Object System.Collections.Generic.List[string]
                    if ($rTotal -le 0) { $problems.Add('total=0（未执行任何用例：MUDFEISHU_REDIS_TESTS 未生效或 testhost 启动失败）') }
                    if ($rFailed -ne 0) { $problems.Add("failed=$rFailed") }
                    if ($rSkipped -ne 0) { $problems.Add("skipped=$rSkipped（集成用例被跳过，未能证明真实执行）") }

                    if ($problems.Count -gt 0) {
                        $script:failures.Add("Redis 集成测试 $($redisRun.Label) 未达门禁：$($problems -join '；')（详见 $redisTestLog）")
                        Write-Host "  [FAIL] $($redisRun.Label)：$($problems -join '；')" -ForegroundColor Red
                    }
                    else {
                        Write-Host "  [ OK ] $($redisRun.Label)（总计 $rTotal / 失败 0 / 跳过 0）" -ForegroundColor Green
                    }
                }
                catch {
                    $script:failures.Add("Redis 集成测试 $($redisRun.Label) 的 TRX 解析失败: $_")
                    Write-Host "  [FAIL] $($redisRun.Label) 的 TRX 解析失败: $_" -ForegroundColor Red
                }
            }
        }
        finally {
            # 复原环境变量，避免污染后续步骤与调用环境
            if ($null -eq $previousRedisEnv) { Remove-Item Env:MUDFEISHU_REDIS_TESTS -ErrorAction SilentlyContinue }
            else { $env:MUDFEISHU_REDIS_TESTS = $previousRedisEnv }

            if (Test-Path $redisTrxDir) {
                Remove-Item $redisTrxDir -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }
}

# ---------------------------------------------------------------- 步骤 6
Write-Host "[步骤 6] 代码格式校验" -ForegroundColor Cyan
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
if ($redisIntegrationGap) {
    Write-Host "集成测试覆盖缺口：$redisIntegrationGap" -ForegroundColor Yellow
}

if ($failures.Count -gt 0) {
    Write-Host "门禁未通过，共 $($failures.Count) 项：" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host '门禁全部通过。' -ForegroundColor Green
exit 0
