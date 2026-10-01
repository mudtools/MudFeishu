<#
.SYNOPSIS
    飞书开放平台文档地址存在性探测（原子操作，供批量校验复用）。

.DESCRIPTION
    飞书文档站是 SPA，HTTP 状态码恒为 200，无法据此判断页面是否存在。
    实测校准：页面不存在时，渲染后的可见文本固定约 184 字符（错误外壳）；
    页面存在时可见文本 >= 3800 字符。因此以「可见文本长度 > 1000」判定存在。

.NOTES
    单页渲染约 5~15 秒（首次启动更慢），请勿在循环里串行调用大量地址。
#>
function Test-FeishuDocUrl {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Url,
        [string]$EdgePath,
        [int]$VirtualTimeBudgetMs = 12000
    )

    if (-not $EdgePath) {
        $candidates = @(
            "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe",
            "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
        )
        $EdgePath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
        if (-not $EdgePath) { throw "未找到 Microsoft Edge。" }
    }

    $dom = & $EdgePath --headless=new --disable-gpu --no-sandbox --dump-dom `
        --virtual-time-budget=$VirtualTimeBudgetMs $Url 2>$null | Out-String

    $text = [regex]::Replace($dom, '(?s)<script.*?</script>', '')
    $text = [regex]::Replace($text, '(?s)<style.*?</style>', '')
    $text = [regex]::Replace($text, '<[^>]+>', ' ')
    $text = [regex]::Replace($text, '\s+', ' ').Trim()

    # 双保险：错误外壳文本极短，且带有「文档不存在」字样
    $looksLikeErrorShell = ($text.Length -lt 1000) -and ($text -match '文档不存在')

    [PSCustomObject]@{
        Url        = $Url
        Exists     = -not $looksLikeErrorShell -and ($text.Length -gt 1000)
        TextLength = $text.Length
    }
}
