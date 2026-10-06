# -----------------------------------------------------------------------
#  作者：Mud Studio  版权所有 (c) Mud Studio 2026
#  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
#  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
#  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
# -----------------------------------------------------------------------

<#
.SYNOPSIS
    AI 工具描述符诊断（MUDFTxxx）门禁的**单一真相源**（R5 / B-11）。

.DESCRIPTION
    为什么需要这个文件：

    R5/B-11 的根因不是"5 条 Warning 不进门禁"，而是**CI 上一条 MUDFT 都没有断言**——
    18 个零容忍 Error 只写在 scripts/verify-build.ps1 的步骤 2 里（PowerShell 局部事实），
    .github/workflows/dotnet-publish.yml 的 "Assert diagnostics whitelist" 只有 6 条无关正则。
    即"门禁的门禁本身失效"（R5 新增根因 R-G）。

    本文件把ID 清单与判定口径抽出来，供**双方** dot-source：
      · scripts/verify-build.ps1（本地全量门禁，步骤 2）
      · .github/workflows/dotnet-publish.yml（CI 断言）
    并由 Tests/Mud.Feishu.AI.FeishuTools.Tests/ContractGuards/DiagnosticsGateParityContractGuards.cs
    机械锁定"本文件 ↔ Diagnostics.ZeroToleranceIds ↔ CI workflow"三方一致，防再次漂移。

.PARAMETER DenyWarnings
    是否把 Warning 级诊断纳入绝对 0 断言（MUDFT005/006/018）与基线增量断言（MUDFT009/021）。
    CI 传-（开关），本地默认不传（避免存量告警阻塞日常开发）。

.NOTES
    口径纪律：只匹配「诊断形态」（warning/error + ID + 冒号），不能只匹配 ID 字符串。
    生成器工程自身的 RS2008 警告正文里就带这些 ID（"为包含规则“MUDFT015”的分析器项目
    启用分析器发布跟踪"），只匹配 ID 会让"生成器工程被重新编译"这一无害动作变成假红。
#>

Set-StrictMode -Off

# ── 零容忍 Error（Severity=Error，出现即构建失败）─────────────────────────
#   与 Mud.Feishu.AI.Tools/Diagnostics.cs 的 ZeroToleranceIds 一一对应（由守卫测试锁定）：
#     001 缺工具名 / 002 命名不符范式 / 003 工具名冲突 / 004 返回类型不可映射
#     008 上传参数不可映射 / 010 查询参数展开失败 / 011 条件必填组引用不存在的参数 / 014 golden 漂移
#     015 Schema内部不一致 / 016 身份与接口令牌类型不符 / 017 读写分类与 SDK 事实脱钩
#     019 SDK 源无法解析 / 020 参数类型无解包映射
#     022 工具未绑定执行器 / 023 绑定不成立 / 024 执行器方法签名不符 / 025 执行器构造参数无法解析
#     026/027 派生契约名冲突
$MudftZeroToleranceIds = @(
    '001', '002', '003', '004', '008', '010', '011', '014', '015', '016', '017',
    '019', '020', '022', '023', '024', '025', '026', '027'
)

# ── 恒为 0 的 Warning/Info（描述质量退化 / 能力目录未更新）──────────────────
#   MUDFT005 描述缺失或过短 / MUDFT006 描述含未验证的绝对化措辞
#   MUDFT018 能力目录 Info（域/工具面变动后未同批更新目录常量）
$MudftAlwaysZeroIds = @('005', '006', '018')

# ── 基线计数的 Warning（天然 >0，只拦"增量"）──────────────────────────────
#   MUDFT009 output_schema 截断（深度 4 上限导致）/ MUDFT021 参数可空性存疑
$MudftBaselineIds = @('009', '021')

# ── 基线文件（当前计数；增量即红）────────────────────────────────────────
$MudftBaselinePath = Join-Path $PSScriptRoot 'mudft-warning-baseline.txt'

function Get-MudftBaseline {
    <#
    .SYNOPSIS
        读取 MUDFT 基线计数（形如 "009=3" 的行）。
    .OUTPUTS
        @{ '009' = 3; '021' = 0 }；文件不存在时返回空哈希表（调用方按"无基线"处理）。
    #>
    $baseline = @{}
    if (-not (Test-Path $MudftBaselinePath)) {
        return $baseline
    }

    foreach ($line in Get-Content -Path $MudftBaselinePath) {
        $trimmed = $line.Trim()
        if ($trimmed -is [string] -and $trimmed -match '^#') { continue }
        if ($trimmed -is [string] -and $trimmed -match '^(\d{3})\s*=\s*(\d+)$') {
            $baseline[$Matches[1]] = [int]$Matches[2]
        }
    }

    return $baseline
}

function Get-MudftZeroTolerancePattern {
    <#
    .SYNOPSIS
        生成零容忍集的「诊断形态」正则（供 Select-String -Pattern 使用）。
    .EXAMPLE
        Select-String -Path $log -Pattern (Get-MudftZeroTolerancePattern) -AllMatches
    #>
    return '(?:warning|error) MUDFT(' + ($MudftZeroToleranceIds -join '|') + '):'
}

function Get-MudftPattern {
    <#
    .SYNOPSIS
        生成指定 ID 集合的「诊断形态」正则。
    .PARAMETER Ids
        ID 列表（如 @('005','006')）。
    #>
    param([string[]]$Ids)

    if (-not $Ids -or $Ids.Count -eq 0) {
        return '(?!)' #永不匹配的空正则
    }

    return '(?:warning|error) MUDFT(' + ($Ids -join '|') + '):'
}

function Measure-Mudft {
    <#
    .SYNOPSIS
        统计构建日志中某ID 集合的真实诊断出现次数。
    .PARAMETER LogPath
        构建日志路径。
    .PARAMETER Ids
        ID 列表。
    .NOTES
        Count 按 **匹配出现次数** 计（与既有Assert-Zero 的口径一致：`-AllMatches`），
        而非"命中行数"——同一行既含 warning 又含 error 时按 2 次计，宁严勿松。
    #>
    param(
        [Parameter(Mandatory = $true)][string]$LogPath,
        [string[]]$Ids
    )

    if (-not (Test-Path $LogPath)) { return 0 }
    return (Select-String -Path $LogPath -Pattern (Get-MudftPattern -Ids $Ids) -AllMatches).Count
}