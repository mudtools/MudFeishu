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

# ---------- 1. 提取 方法名 -> 文档地址 ----------
$methodUrl = @{}
# 只认方法级链接（锚文本为「接口文档」），避免误用接口级/参数内的通用链接
$docUrlRe = [regex]'<see href="(https://open\.feishu\.cn/document/[^"]+)">接口文档</see>'
# 方法名 = 行内第一个「标识符 + 左括号」，且该标识符不是返回类型关键字。
# 这样可正确处理嵌套泛型返回类型，如 Task<FeishuApiResult<X>?> FooAsync(
$callRe = [regex]'(\w+)\s*\('
$reservedNames = @('Task', 'ValueTask', 'void', 'Func', 'Action')

foreach ($f in Get-ChildItem -Path $interfacesRoot -Recurse -Filter '*.cs') {
    $lines = [System.IO.File]::ReadAllLines($f.FullName)
    $pendingUrl = $null
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '^\s*///') {
            $m = $docUrlRe.Match($line)
            if ($m.Success) { $pendingUrl = $m.Groups[1].Value }
            continue
        }
        if ($line -match '^\s*\[') { continue }
        if ($pendingUrl) {
            foreach ($mm in $callRe.Matches($line)) {
                $name = $mm.Groups[1].Value
                if ($reservedNames -contains $name) { continue }
                if (-not $methodUrl.ContainsKey($name)) { $methodUrl[$name] = $pendingUrl }
                $pendingUrl = $null
                break
            }
        }
    }
}
Write-Host "提取到方法映射：$($methodUrl.Count)"

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
foreach ($f in $files) {
    $lines = [System.IO.File]::ReadAllLines($f.FullName)
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
