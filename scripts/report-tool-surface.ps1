# -----------------------------------------------------------------------
#  作者：Mud Studio  版权所有 (c) Mud Studio 2026
#  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
#  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
#  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
# -----------------------------------------------------------------------

<#
.SYNOPSIS
    MudFeishu.AI 工具面数字刷新（「状态基线」刷新命令，D-2）。

.DESCRIPTION
    `.docs/AI/状态基线.md` 位于 gitignored 目录，不享受构建期拦截，靠「每轮交付同批刷新」
    纪律维持。本脚本是该文件头部登记的刷新命令：解析唯一真相源并打印所需全部数字
    （可直接粘贴进 §一 表格）：

      1. golden 快照（工具条目 / 只读·写类拆分 / 风险 / 身份 / 域分布）
      2. Curation 声明文件（六组目录计数）
      3. Guidance（L1 / L2）、Affordance（份数）、skills/feishu-*（域产物数）

    自检 fail-closed：读写拆分、写类风险拆分与总数不符时报错退出（解析坏 = 不输出假数字）。

.PARAMETER GoldenPath
    golden 快照路径（相对仓库根，默认工具面标准位置）。

.EXAMPLE
    pwsh ./scripts/report-tool-surface.ps1
#>

param(
    [string]$GoldenPath = "Mud.Feishu.AI.Tools/FeishuToolSchemas.golden.txt"
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $RepoRoot

function Get-RepoPath([string]$relative) {
    return (Join-Path $RepoRoot $relative)
}

$goldenFull = Get-RepoPath $GoldenPath
if (-not (Test-Path $goldenFull)) {
    Write-Error "golden 快照不存在：$goldenFull（工具面数字无法刷新——解析面失效即停，不得凭记忆改数字）"
    exit 1
}

# ---------------------------------------------------------------------------
# 1. golden 快照解析
# ---------------------------------------------------------------------------
$lines = [System.IO.File]::ReadAllLines($goldenFull, [System.Text.Encoding]::UTF8) |
    Where-Object { $_.Trim() -ne '' }

$domainCounts = @{}
$riskCounts = @{}
$identityCounts = @{}
$readonly = 0
$write = 0

foreach ($line in $lines) {
    $tab = $line.IndexOf("`t")
    if ($tab -lt 0) {
        Write-Error "golden 行缺少制表符分隔（行首：$($line.Substring(0, [Math]::Min(40, $line.Length)))）"
        exit 1
    }

    $name = $line.Substring(0, $tab)
    $json = $line.Substring($tab + 1) | ConvertFrom-Json
    $xf = $json.'x-feishu'

    $domain = $name.Split('.')[0]
    $domainCounts[$domain] = $domainCounts[$domain] + 1
    $riskCounts[$xf.risk] = $riskCounts[$xf.risk] + 1
    $identityCounts[$xf.identity] = $identityCounts[$xf.identity] + 1

    if ($xf.is_write -eq $true) { $write++ } else { $readonly++ }
}

$total = $lines.Count
$writeClassRisk = $riskCounts['write'] + $riskCounts['high-risk-write']

# fail-closed 自检：拆分口径必须闭合，否则输出的是坏数字
if ($readonly + $write -ne $total) {
    Write-Error "读写拆分不闭合：$readonly + $write != $total（golden 解析坏，停）"
    exit 1
}

if ($writeClassRisk -ne $write) {
    Write-Error "写类风险拆分不闭合：write($($riskCounts['write'])) + high-risk-write($($riskCounts['high-risk-write'])) != is_write($write)（golden 解析坏，停）"
    exit 1
}

# ---------------------------------------------------------------------------
# 2. 资产目录计数
# ---------------------------------------------------------------------------
$curationGroups = Get-ChildItem (Get-RepoPath "Mud.Feishu.AI.Tools/Curation") -Directory | Sort-Object Name
$curationPerGroup = @()
foreach ($group in $curationGroups) {
    $count = @(Get-ChildItem $group.FullName -Recurse -Filter *.cs -File).Count
    $curationPerGroup += ("$($group.Name) $count")
}
$curationTotal = @(Get-ChildItem (Get-RepoPath "Mud.Feishu.AI.Tools/Curation") -Recurse -Filter *.cs -File).Count

$guidanceRoot = (Get-RepoPath "Mud.Feishu.AI.Tools/Guidance")
$guidanceL1 = @(Get-ChildItem $guidanceRoot -Filter *.md -File).Count
$guidanceL2 = @(Get-ChildItem $guidanceRoot -Recurse -Filter *.md -File |
    Where-Object { $_.DirectoryName -ne (Resolve-Path $guidanceRoot).Path }).Count

$affordanceCount = @(Get-ChildItem (Get-RepoPath "Mud.Feishu.AI.Tools/Affordance") -Filter *.md -File).Count

$skillsCount = @(Get-ChildItem (Get-RepoPath "skills") -Directory |
    Where-Object { $_.Name -like 'feishu-*' -and (Test-Path (Join-Path $_.FullName 'SKILL.md')) }).Count

$domainCount = $domainCounts.Keys.Count
$domainList = ($domainCounts.Keys | Sort-Object | ForEach-Object { "$_ $($domainCounts[$_])" }) -join ' / '

# ---------------------------------------------------------------------------
# 3. 输出（可直接粘贴进 .docs/AI/状态基线.md §一）
# ---------------------------------------------------------------------------
Write-Host "==== MudFeishu.AI 工具面数字刷新（来源：$GoldenPath）===="
Write-Host ""
Write-Host "| 工具条目 | **$total 个工具**（$readonly 只读 + $write 写类） |"
Write-Host "| 风险分布 | read $($riskCounts['read']) / write $($riskCounts['write']) / high-risk-write $($riskCounts['high-risk-write']) |"
Write-Host "| 身份分布 | $(($identityCounts.Keys | Sort-Object | ForEach-Object { "$_ $($identityCounts[$_])" }) -join ' / ') |"
Write-Host "| Curation 声明文件 | $curationTotal（$($curationPerGroup -join ' / ')） |"
Write-Host "| 业务域 | **$domainCount 域** |"
Write-Host "| 域分布 | $domainList |"
Write-Host "| Guidance | L1 $guidanceL1 篇 / L2 $guidanceL2 篇 |"
Write-Host "| Affordance | $affordanceCount 份 |"
Write-Host "| Skills 产物 | $skillsCount 个 skills/feishu-* |"
Write-Host ""
Write-Host "自检：读写拆分闭合（$readonly+$write=$total）✓ / 写类风险闭合（$($riskCounts['write'])+$($riskCounts['high-risk-write'])=$write）✓"
Write-Host "刷新完成：把以上行同批更新进 .docs/AI/状态基线.md（该文件无构建期拦截，靠本命令 + 交付纪律维持）。"
