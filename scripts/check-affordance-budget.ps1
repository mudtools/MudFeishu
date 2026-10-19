# scripts/check-affordance-budget.ps1 -- affordance 资产的字符预算自检（与 MUDFT031 同口径）
# 用法：powershell -File ./scripts/check-affordance-budget.ps1 [-Paths <md 路径，逗号或空格分隔，可用通配符>]
# 不带 -Paths 时扫描整个 Mud.Feishu.AI.Tools/Affordance 目录。
# 字符数按 LF 归一化统计（与守卫 ReadAssets 与上游诊断同一口径），退出码 1 = 存在越界。

[CmdletBinding()]
param(
    [string]$Paths
)

$MaxTotal = 1024
$MaxSection = 320
$Headings = @('WHEN', 'Avoid when', 'Prerequisites', 'Tips')

# -File 调用会把后续参数原样传成单个字符串，故显式按逗号/空格拆分并展开通配符。
if ([string]::IsNullOrWhiteSpace($Paths)) {
    $Paths = 'Mud.Feishu.AI.Tools/Affordance/*.md'
}
$files = @($Paths -split '[,\s]+' | Where-Object { $_ } | ForEach-Object { Resolve-Path -Path $_ -ErrorAction SilentlyContinue } | ForEach-Object { $_.Path })
if ($files.Count -eq 0) {
    Write-Host "FAIL: 没有匹配到任何文件（$Paths）"
    exit 1
}

$failed = 0
foreach ($file in $files) {
    $text = [System.IO.File]::ReadAllText($file).Replace("`r`n", "`n")
    $problems = @()

    if ($text.Length -gt $MaxTotal) {
        $problems += "total $($text.Length) > $MaxTotal"
    }

    $sections = @($text -split "(?m)^(?=## )") | Where-Object { $_ -like '## *' }
    $seen = @($sections | ForEach-Object { (($_ -split "`n")[0] -replace '^## ', '').Trim() })
    foreach ($heading in $Headings) {
        if ($seen -notcontains $heading) {
            $problems += "缺少小节 $heading"
        }
    }

    foreach ($section in $sections) {
        $name = (($section -split "`n")[0] -replace '^## ', '').Trim()
        $body = (($section -split "`n") | Select-Object -Skip 1) -join "`n"
        $body = $body.Trim()
        if ($body.Length -eq 0) {
            $problems += "$name 小节为空"
        } elseif ($body.Length -gt $MaxSection) {
            $problems += "$name $($body.Length) > $MaxSection"
        }
    }

    if ($problems.Count -gt 0) {
        $failed++
        Write-Host ("OVER  {0}: {1}" -f $file, ($problems -join '; '))
    }
}

if ($failed -gt 0) {
    Write-Host "$failed/$($files.Count) 份资产越界"
    exit 1
}

Write-Host "OK: $($files.Count) 份资产全部合规"
