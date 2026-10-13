<#
.SYNOPSIS
    R-3 / F-11：AI 相关变更的**规则式语义审查**（CI 门禁）。

.DESCRIPTION
    本仓有 44+ 契约守卫，但它们锁的是**状态**（当前源码是否自洽），而 F-1 总结的
    "新增一个域要动 12 处触点"里有一类失败是**状态自洽但变更集不完整**——典型的
    "编译能过、测试能过，只是忘了同批重固化产物/文档"，直到别人拉代码才发现。

    在没有可用 LLM 接入的前提下，按方案 F-11 的"规则式替代"落地为三条**可机械判定**的规则：

      规则 A：新增 `[FeishuTool]` 声明 ⇒ 必须同批更新
              golden 快照 / skills 产物 / 权限对照表 / PublicAPI / 状态基线。
      规则 B：`Guidance/` 资产变更 ⇒ 必须同批重固化 `skills/` 产物。
      规则 C：`Curation/` 变更 ⇒ 必须同批更新 `Guidance/` 资产
              （"有工具无引导"不触发任何构建诊断，模型表现为"能用但用不对"）。

    判据刻意**窄**（只认新增声明行 / 只认目录前缀）：被拦下的都是真缺口，不会拿"改了注释"
    去为难提交者。取不到 diff（浅克隆 / ref 不存在）时**跳过而不阻断**——CI 的失败必须有据可查。

.PARAMETER BaseRef
    对比基准（默认 `HEAD~1`；CI 传 PR base sha）。空值按默认处理。
#>
param(
    [string]$BaseRef = 'HEAD~1'
)

# ⚠️ 刻意用 Continue 而不是 Stop：git 会把进度/换行提示写到 stderr，Windows PowerShell 5.1
#    在 `Stop` 下会把它提升为终止性 NativeCommandError，导致"git 正常返回 0 但脚本崩"。
#    本脚本的全部控制流都走显式 `exit` / `$LASTEXITCODE`，不依赖 Stop。
$ErrorActionPreference = 'Continue'

# ⚠️ 两个编码设定缺一不可（否则中文路径永远匹配不上，规则 A 会**误报**）：
#    ① `-c core.quotepath=false`：git 默认把非 ASCII 路径写成八进制转义（"documents/...\345\267\245..."）；
#    ② Console.OutputEncoding=UTF8：PS 5.1 用控制台代码页解码 git 的 UTF-8 输出（中文路径会被解成乱码）。
try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false) } catch { }
$GitPathArgs = @('-c', 'core.quotepath=false')

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if ([string]::IsNullOrWhiteSpace($BaseRef)) { $BaseRef = 'HEAD~1' }

# ── 取变更清单：`git diff --name-only <base>`（base 到工作区，含未提交改动）──
$changedRaw = & git @GitPathArgs diff --name-only $BaseRef 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "[SKIP] 无法从 '$BaseRef' 取得变更清单（浅克隆 / ref 不存在）——本步骤跳过（不阻断）。"
    exit 0
}

# 未跟踪（新增）文件不在 `git diff` 里——但"新增产物/文档"恰恰是本审查最关心的变更，
# 故显式并入。CI 是干净检出，这一步天然为空；本地开发时它让门禁结论与工作区一致。
$untracked = @(& git @GitPathArgs ls-files --others --exclude-standard 2>$null)

$changed = @($changedRaw + $untracked |
    Where-Object { $_ } |
    ForEach-Object { $_.Replace('\', '/') } |
    Sort-Object -Unique)
if ($changed.Count -eq 0) {
    Write-Host "[ OK ] 相对 '$BaseRef' 无变更。"
    exit 0
}

Write-Host ("变更文件 {0} 个（base: {1}）" -f $changed.Count, $BaseRef)

function Test-ChangedPath([string]$prefix) {
    return @($changed | Where-Object { $_.StartsWith($prefix, [System.StringComparison]::Ordinal) }).Count -gt 0
}

$violations = New-Object 'System.Collections.Generic.List[string]'

# ── 规则 A：新增工具声明 ⇒ 机械触点齐备 ──
$addedDeclarations = @(
    & git @GitPathArgs diff --unified=0 $BaseRef -- 'Mud.Feishu.AI.Tools/Curation' 2>$null |
        Where-Object { $_ -match '^\+\s*\[FeishuTool\(' }
)
if ($addedDeclarations.Count -gt 0) {
    Write-Host ("规则 A：检测到 {0} 行新增 [FeishuTool] 声明。" -f $addedDeclarations.Count)

    $companions = @(
        @{ Path = 'Mud.Feishu.AI.Tools/FeishuToolSchemas.golden.txt'; Why = 'golden 快照重固化（MUDFT014 是 Error 级）' },
        @{ Path = 'skills/'; Why = 'skills 产物重固化（$env:FeishuSkillsUpdate=true）' },
        @{ Path = 'documents/AIAgent/工具权限对照表.md'; Why = '权限对照表增行（数据行数被守卫机械锁定）' },
        @{ Path = 'Mud.Feishu.AI.Tools/PublicAPI.Unshipped.txt'; Why = 'PublicAPI 同步（RS0016）' }
        # 2026-10-13：原要求同批更新「状态基线」文档 —— 该文档已被删除（不存在于 git 历史），
        # 保留它会让每个新增工具的 PR 恒违规。工具面规模的真相源现为
        # Mud.Feishu.AI.Tools/FeishuToolSchemas.golden.txt（MUDFT014 逐字节锁定）。
    )

    foreach ($companion in $companions) {
        if (-not (Test-ChangedPath $companion.Path)) {
            $violations.Add("新增工具但未同批更新 $($companion.Path)：$($companion.Why)")
        }
    }
}

# ── 规则 B：guidance 资产变更 ⇒ skills 产物同批重固化 ──
if (Test-ChangedPath 'Mud.Feishu.AI.Tools/Guidance/') {
    Write-Host '规则 B：检测到 guidance 资产变更。'
    if (-not (Test-ChangedPath 'skills/')) {
        $violations.Add('guidance 资产变更但未同批重固化 skills/ 产物（SkillsExportContractTests 会逐字节报红）')
    }
}

# ── 规则 C：工具声明面变更 ⇒ guidance 资产同批更新 ──
if (Test-ChangedPath 'Mud.Feishu.AI.Tools/Curation/') {
    Write-Host '规则 C：检测到工具声明面（Curation/）变更。'
    if (-not (Test-ChangedPath 'Mud.Feishu.AI.Tools/Guidance/')) {
        $noGuidance = 'Curation/ 变更但未同批更新 Guidance/ 资产——"有工具无引导"不触发任何构建诊断，'
        $noGuidance += '表现为"工具能调但模型不知道先调哪个/失败怎么退"。'
        $violations.Add($noGuidance)
    }
}

if ($violations.Count -gt 0) {
    Write-Host ''
    Write-Host '[FAIL] AI 面变更的机械触点不完整（R-3 / F-11 规则式语义审查）：' -ForegroundColor Red
    foreach ($violation in $violations) {
        Write-Host ("  - {0}" -f $violation) -ForegroundColor Red
    }
    Write-Host ''
    Write-Host 'F-1 的"新增域 12 处触点"清单即本脚本规则 A 的 $companions 表（AGENTS.md「Test Suite Governance」同源）。'
    exit 1
}

Write-Host '[ OK ] AI 面变更的机械触点齐备。' -ForegroundColor Green
exit 0
