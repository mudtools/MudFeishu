<#
.SYNOPSIS
    为 Mud.Feishu.DataModels 项目中所有 DTO 类型添加 [HttpJsonSerializable] 特性。
.DESCRIPTION
    - 遍历指定根目录下所有 .cs 文件（排除 obj 目录与已标注文件）。
    - 以文件相对于根目录的“第一级文件夹名”作为 SerializerClassName 取值。
    - 仅对顶层的 class / record / struct 添加特性；跳过 enum / interface /
      delegate / static class，以及嵌套类型。
    - 幂等：文件已包含 [HttpJsonSerializable 则跳过。
#>

param(
    # 数据模型根目录
    [string]$RootPath = "d:\Repos\MudFeishu\FeishuV3\Mud.Feishu.DataModels"
)

$attributeName = 'HttpJsonSerializable'
$attrPattern  = [regex]'(?<![\w.])HttpJsonSerializable\b'

# 匹配类型声明行：捕获 (1) 修饰符  (2) 类型种类  (3) 类型名
$typeRegex = [regex]::new(
    '^(?<indent>\s*)' +
    '(?<mods>(?:(?:public|internal|protected|private|file|sealed|abstract|static|partial|readonly|unsafe)\s+)*)' +
    '(?<kind>class|record|struct|enum|interface|delegate)\s+(?<name>\w+)',
    [System.Text.RegularExpressions.RegexOptions]::IgnoreCase
)

$files = Get-ChildItem -Path $RootPath -Filter *.cs -Recurse |
    Where-Object { $_.FullName -notmatch '\\obj\\' }

$stats = @{ Total = 0; Changed = 0; Skipped = 0; NoType = 0 }

foreach ($file in $files) {
    $stats.Total++

    # 计算第一级文件夹名（根目录的直接子目录）
    $relPath  = $file.FullName.Substring($RootPath.TrimEnd('\').Length + 1)
    $segments = $relPath -split '\\'
    $module   = $segments[0]   # 如 AI / Approval / Common ...

    $lines = Get-Content -Path $file.FullName -Encoding UTF8

    # 已标注则跳过（幂等）
    if (($lines -join "`n") -match $attrPattern) {
        $stats.Skipped++
        continue
    }

    $candidates = @()   # 收集需要标注的行号（1-based）

    foreach ($i in 0..($lines.Count - 1)) {
        $line = $lines[$i]
        $m = $typeRegex.Match($line)
        if (-not $m.Success) { continue }

        $kind = $m.Groups['kind'].Value.ToLower()
        $mods = $m.Groups['mods'].Value

        # 跳过非 DTO 类型
        if ($kind -in @('enum', 'interface', 'delegate')) { continue }
        # 跳过 static class / static record / static struct
        if ($mods -match '\bstatic\b') { continue }

        $candidates += ($i + 1)   # 1-based 行号
    }

    if ($candidates.Count -eq 0) {
        $stats.NoType++
        continue
    }

    # 通过缩进过滤嵌套类型：取所有候选行的最小缩进作为“顶层”基准
    $minIndent = ($candidates | ForEach-Object { $lines[$_ - 1].Length - $lines[$_ - 1].TrimStart().Length } | Measure-Object -Minimum).Minimum

    # 从后往前插入，避免行号偏移
    $newLines = [System.Collections.ArrayList]::new($lines)
    $added = 0
    foreach ($ln in ($candidates | Sort-Object -Descending)) {
        $classLine = $lines[$ln - 1]
        $indentLen = $classLine.Length - $classLine.TrimStart().Length
        # 仅标注顶层（缩进 == 最小缩进）的类型，跳过嵌套类型
        if ($indentLen -gt $minIndent) { continue }

        $indent = ' ' * $indentLen
        $attrLine = "$indent[$attributeName(SerializerClassName = `"$module`")]"
        $newLines.Insert($ln - 1, $attrLine)
        $added++
    }

    if ($added -gt 0) {
        Set-Content -Path $file.FullName -Value $newLines -Encoding UTF8
        $stats.Changed++
        Write-Host "已修改 [$module] $($file.Name)  (+$added 特性)"
    } else {
        $stats.Skipped++
    }
}

Write-Host "`n==== 统计 ===="
Write-Host "扫描文件总数 : $($stats.Total)"
Write-Host "成功修改     : $($stats.Changed)"
Write-Host "已标注跳过   : $($stats.Skipped)"
Write-Host "无DTO类型    : $($stats.NoType)"
