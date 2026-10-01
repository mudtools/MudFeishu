<#
.SYNOPSIS
    批量校验飞书开放平台文档地址是否真实存在，并给出修复候选。

.DESCRIPTION
    飞书文档站是 SPA，HTTP 状态码恒为 200，无法据此判断页面是否存在。
    本脚本用本机 Edge 无头渲染页面，以渲染后可见文本长度判定：
      - 页面不存在：可见文本固定约 184 字符（错误外壳）
      - 页面存在  ：可见文本 >= 3800 字符
    判据细节与校准过程见 Test-FeishuDocUrl.ps1。

    -TryVariants 会对失败的地址依次尝试其它 URL 前缀形态（legacy / server-docs / 无前缀），
    输出首个校验通过的候选，用于修复。

.PARAMETER Url
    直接传入待校验地址。

.PARAMETER UrlFile
    从文本文件读取待校验地址（每行一个，忽略非 http 开头的行）。

.PARAMETER FromInterface
    从指定目录（或单个文件）递归提取 open.feishu.cn/document/... 地址后校验。

.PARAMETER OutFile
    结果 TSV 输出路径（Url / Exists / TextLength），便于分批校验后汇总。

.PARAMETER ThrottleLimit
    并发渲染数，默认 6；过高会显著增加内存占用。

.PARAMETER TryVariants
    对失败地址自动探测可用的替代前缀形态。

.EXAMPLE
    pwsh ./scripts/VerifyFeishuDocUrls.ps1 -FromInterface Mud.Feishu/Interfaces/HelpDesk -TryVariants
#>
[CmdletBinding()]
param(
    [string[]]$Url,
    [string]$UrlFile,
    [string]$FromInterface,
    [string]$OutFile,
    [int]$ThrottleLimit = 6,
    [switch]$TryVariants
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Test-FeishuDocUrl.ps1')

$edgeCandidates = @(
    "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe",
    "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
)
$edge = $edgeCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $edge) { throw "未找到 Microsoft Edge，无法进行渲染校验。" }

function Get-Variants {
    param([string]$Target)
    $legacyPrefix = 'https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/'
    $serverPrefix = 'https://open.feishu.cn/document/server-docs/'
    $plainPrefix = 'https://open.feishu.cn/document/'
    if ($Target.StartsWith($legacyPrefix)) {
        $tail = $Target.Substring($legacyPrefix.Length)
        return @(($serverPrefix + $tail), ($plainPrefix + $tail))
    }
    elseif ($Target.StartsWith($serverPrefix)) {
        $tail = $Target.Substring($serverPrefix.Length)
        return @(($legacyPrefix + $tail), ($plainPrefix + $tail))
    }
    else {
        $tail = $Target.Substring($plainPrefix.Length)
        return @(($legacyPrefix + $tail), ($serverPrefix + $tail))
    }
}

$targets = New-Object System.Collections.Generic.List[string]
if ($Url) { $Url | ForEach-Object { $targets.Add($_) } }
if ($UrlFile) {
    Get-Content -Path $UrlFile -Encoding UTF8 |
        Where-Object { $_ -match '^https?://' } |
        ForEach-Object { $targets.Add($_.Trim()) }
}
if ($FromInterface) {
    $items = if (Test-Path $FromInterface -PathType Container) {
        Get-ChildItem -Path $FromInterface -Recurse -Filter *.cs
    }
    else { Get-Item $FromInterface }
    $items | Select-String -Pattern 'open\.feishu\.cn/document/[^"<>)\s]+' -AllMatches |
        ForEach-Object { foreach ($m in $_.Matches) { $targets.Add($m.Value) } }
}
$targets = $targets | Sort-Object -Unique
if ($targets.Count -eq 0) { throw "没有待校验的地址。" }

Write-Host "待校验地址：$($targets.Count) 个，并发 $ThrottleLimit" -ForegroundColor Cyan

$results = $targets | ForEach-Object -Parallel {
    $edgePath = $using:edge
    $dom = & $edgePath --headless=new --disable-gpu --no-sandbox --dump-dom --virtual-time-budget=12000 $_ 2>$null | Out-String
    $text = [regex]::Replace($dom, '(?s)<script.*?</script>', '')
    $text = [regex]::Replace($text, '(?s)<style.*?</style>', '')
    $text = [regex]::Replace($text, '<[^>]+>', ' ')
    $text = [regex]::Replace($text, '\s+', ' ').Trim()
    [PSCustomObject]@{
        Url        = $_
        Exists     = (-not (($text.Length -lt 1000) -and ($text -match '文档不存在'))) -and ($text.Length -gt 1000)
        TextLength = $text.Length
    }
} -ThrottleLimit $ThrottleLimit

if ($OutFile) {
    $results | ForEach-Object { "{0}`t{1}`t{2}" -f $_.Url, $_.Exists, $_.TextLength } |
        Set-Content -Path $OutFile -Encoding UTF8
    Write-Host "结果已写入：$OutFile" -ForegroundColor DarkGray
}

$bad = $results | Where-Object { -not $_.Exists }
Write-Host ""
Write-Host "有效：$(($results | Where-Object Exists).Count)  无效：$($bad.Count)" -ForegroundColor Yellow

if ($bad.Count -gt 0 -and -not $OutFile) {
    Write-Host "`n===== 无效地址 =====" -ForegroundColor Red
    $bad | ForEach-Object { "{0}  (len={1})" -f $_.Url, $_.TextLength }
}

if ($bad.Count -gt 0 -and $TryVariants) {
    Write-Host "`n===== 修复候选 =====" -ForegroundColor Green
    $fix = @()
    foreach ($b in $bad) {
        $fixed = $null
        foreach ($v in (Get-Variants -Target $b.Url)) {
            $r = Test-FeishuDocUrl -Url $v -EdgePath $edge
            if ($r.Exists) { $fixed = $v; break }
        }
        if ($fixed) {
            "{0}`n    -> {1}" -f $b.Url, $fixed
            $fix += "{0}`t{1}" -f $b.Url, $fixed
        }
        else {
            "{0}`n    -> 无可用候选，需人工确认" -f $b.Url
            $fix += "{0}`t" -f $b.Url
        }
    }
    if ($OutFile) {
        $fix | Set-Content -Path ($OutFile -replace '\.tsv$', '') -Encoding UTF8
    }
}
