<#
.SYNOPSIS
    为 documents 下的接口文档补充「接口文档」列。

.DESCRIPTION
    documents/*.md 的函数列表表格形如：
        | 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
    本脚本从 Mud.Feishu/Interfaces 源码中提取「方法名 -> 接口文档地址」映射，
    然后为指定目录下的 md 表格补上「接口文档」列。

    映射提取方式：读取每个接口文件，跟踪 XML 注释中的
    `<para><see href="URL">接口文档</see></para>`，随后遇到方法签名中最后一个
    形如 `Task<...> Name(` / `ValueTask<...> Name(` 的标识符即视为该方法名。

.PARAMETER DocumentsPath
    documents 目录（默认 ../../documents 相对于脚本位置）。

.PARAMETER Folder
    只处理文档目录下的哪些子目录（如 Organization、Spreadsheets）；不传则处理全部。

.PARAMETER WhatIf
    只输出将要修改的文件与新增行数，不写盘。
#>
[CmdletBinding()]
param(
    [string]$DocumentsPath,
    [string]$Folder,
    [string]$ExcludeFolder,
    [switch]$WhatIf
)

# 便于命令行传入逗号分隔的目录名
$Folder = if ($Folder) { $Folder -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ } } else { $null }
$ExcludeFolder = if ($ExcludeFolder) { $ExcludeFolder -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ } } else { $null }

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $DocumentsPath) { $DocumentsPath = Join-Path $repoRoot 'documents' }
$interfacesRoot = Join-Path $repoRoot 'Mud.Feishu/Interfaces'

# ---------- 1. 提取 (接口, 方法名) -> 文档地址 ----------
# 关键：方法名在不同模块间会重名（如 UploadFileAsync 存在于 Approval / Lingo / Message），
# 因此必须以「声明它的接口」为作用域，避免跨模块张冠李戴。
$mapByInterface = @{}   # 接口名 -> @{ 方法名 = 文档地址 }
$methodUrlAll = @{}     # 方法名 -> 全部候选地址集合（用于全局兜底与歧义判定）

# 只认方法级链接（锚文本为「接口文档」），避免误用接口级/参数内的通用链接
$docUrlRe = [regex]'<see href="(https://open\.feishu\.cn/document/[^"]+)">接口文档</see>'
# 方法名 = 行内第一个「标识符 + 左括号」，且该标识符不是返回类型关键字。
$callRe = [regex]'(\w+)\s*\('
$reservedNames = @('Task', 'ValueTask', 'void', 'Func', 'Action')
$interfaceRe = [regex]'^\s*(?:public|internal)?\s*(?:partial\s+)?interface\s+(\w+)'

foreach ($f in Get-ChildItem -Path $interfacesRoot -Recurse -Filter '*.cs') {
    $lines = [System.IO.File]::ReadAllLines($f.FullName)
    $pendingUrl = $null
    $currentInterface = $null
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '^\s*///') {
            $m = $docUrlRe.Match($line)
            if ($m.Success) { $pendingUrl = $m.Groups[1].Value }
            continue
        }
        $im = $interfaceRe.Match($line)
        if ($im.Success) {
            $currentInterface = $im.Groups[1].Value
            # 接口级链接（位于 interface 声明之前）不得被其内部第一个方法「继承」
            $pendingUrl = $null
            continue
        }
        if ($line -match '^\s*\[') { continue }
        if ($pendingUrl) {
            foreach ($mm in $callRe.Matches($line)) {
                $name = $mm.Groups[1].Value
                if ($reservedNames -contains $name) { continue }
                if ($currentInterface) {
                    if (-not $mapByInterface.ContainsKey($currentInterface)) { $mapByInterface[$currentInterface] = @{} }
                    if (-not $mapByInterface[$currentInterface].ContainsKey($name)) {
                        $mapByInterface[$currentInterface][$name] = $pendingUrl
                    }
                }
                if (-not $methodUrlAll.ContainsKey($name)) {
                    $methodUrlAll[$name] = New-Object System.Collections.Generic.HashSet[string]
                }
                [void]$methodUrlAll[$name].Add($pendingUrl)
                $pendingUrl = $null
                break
            }
        }
    }
}
Write-Host "提取到接口：$($mapByInterface.Count)   方法名（含重名）：$($methodUrlAll.Count)"
$ambiguous = @($methodUrlAll.Keys | Where-Object { $methodUrlAll[$_].Count -gt 1 })
Write-Host "跨模块重名方法：$($ambiguous.Count)"

# ---------- 2. 为 md 表格补列 ----------
$headerRe = [regex]'^\|\s*函数名称\s*\|(.+)\|\s*$'
$folders = if ($Folder) { $Folder | ForEach-Object { Join-Path $DocumentsPath $_ } } else { @($DocumentsPath) }

$files = foreach ($p in $folders) {
    if (Test-Path $p) { Get-ChildItem -Path $p -Recurse -Filter '*.md' }
}
$files = $files | Sort-Object FullName -Unique
if ($ExcludeFolder) {
    $excludePaths = $ExcludeFolder | ForEach-Object { Join-Path $DocumentsPath $_ }
    $files = $files | Where-Object {
        $p = $_.FullName
        -not ($excludePaths | Where-Object { $p.StartsWith($_) })
    }
}

$changedFiles = 0; $addedCells = 0; $skipped = 0
$ifaceTokenRe = [regex]'IFeishu[A-Za-z0-9_]+'
foreach ($f in $files) {
    $lines = [System.IO.File]::ReadAllLines($f.FullName)

    # 基础层：全仓无歧义的方法映射（同名方法只有一个地址时可直接采用）
    $methodUrl = @{}
    foreach ($k in $methodUrlAll.Keys) {
        if ($methodUrlAll[$k].Count -eq 1) { $methodUrl[$k] = @($methodUrlAll[$k])[0] }
    }
    # 覆盖层：该 md 文档涉及的接口（文件内出现的 IFeishuXxx 标识符）优先，用于纠正跨模块重名
    # 注意：方法通常声明在派生接口的「基接口」上（IFeishuTenantV1LingoFile -> IFeishuV1LingoFile），
    # 故同时纳入去掉 Tenant/User 后的基接口名。
    $text = ($lines -join "`n")
    $ifaceTokens = New-Object System.Collections.Generic.List[string]
    foreach ($t in ($ifaceTokenRe.Matches($text) | ForEach-Object { $_.Value } | Sort-Object -Unique)) {
        $ifaceTokens.Add($t)
        $base = $t -replace '^IFeishu(Tenant|User)', 'IFeishu'
        if ($base -ne $t) { $ifaceTokens.Add($base) }
    }
    foreach ($t in ($ifaceTokens | Sort-Object -Unique)) {
        if ($mapByInterface.ContainsKey($t)) {
            foreach ($k in $mapByInterface[$t].Keys) {
                $methodUrl[$k] = $mapByInterface[$t][$k]
            }
        }
    }

    $dirty = $false
    $inTable = $false
    $hasColumn = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $hm = $headerRe.Match($line)
        if ($hm.Success) {
            $hasColumn = $line -match '接口文档'
            if (-not $hasColumn) {
                # 表头：追加一列
                $lines[$i] = $line.TrimEnd() + ' 接口文档 |'
                # 紧随其后的分隔行
                if ($i + 1 -lt $lines.Count -and $lines[$i + 1] -match '^\|[\s\-:|]+\|\s*$') {
                    $lines[$i + 1] = $lines[$i + 1].TrimEnd() + ' --- |'
                }
                $dirty = $true
            }
            $inTable = $true
            continue
        }
        if ($inTable) {
            # 仅处理方法行（本仓约定接口方法名以 Async 结尾），避免误改参数表
            if ($line -match '^\|\s*(\w+Async)\s*\|') {
                $name = $Matches[1]
                $url = $methodUrl[$name]
                $cell = if ($url) { " [$name]($url) |" } else { ' — |' }
                if (-not $url) { $skipped++ }
                $base = $line.TrimEnd()
                # 该表已带「接口文档」列时，替换最后一格而非追加
                if ($hasColumn) { $base = $base -replace '\s*(\[[^\]]*\]\([^)]*\)|—)\s*\|\s*$', '' }
                $lines[$i] = $base + $cell
                $addedCells++
                $dirty = $true
            }
            elseif ($line -notmatch '^\|') {
                $inTable = $false
            }
        }
    }
    if ($dirty -and -not $WhatIf) {
        [System.IO.File]::WriteAllLines($f.FullName, $lines)
    }
    if ($dirty) { $changedFiles++ }
}

Write-Host "处理文件：$changedFiles   新增单元格：$addedCells   未匹配到地址：$skipped"
