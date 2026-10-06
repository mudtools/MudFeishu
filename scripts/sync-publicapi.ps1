param(
    [string]$Project = 'Mud.Feishu.AI.FeishuTools/Mud.Feishu.AI.FeishuTools.csproj',
    [string]$ApiFile = 'Mud.Feishu.AI.FeishuTools/PublicAPI.Unshipped.txt',
    [string]$Golden = 'Mud.Feishu.AI.FeishuTools/FeishuToolSchemas.golden.txt'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

# ── 1)补全缺失条目（RS0016）──
$log = dotnet build $Project -c Release --nologo -v n --no-incremental 2>&1 | Out-String

$existing = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($line in [System.IO.File]::ReadAllLines($ApiFile)) {
    if ($line.Trim()) { [void]$existing.Add($line.Trim()) }
}

$missing = New-Object 'System.Collections.Generic.List[string]'
foreach ($m in [regex]::Matches($log, "RS0016: Symbol '(?<s>[^']+)' is not part")) {
    $s = $m.Groups['s'].Value
    if (-not $existing.Contains($s)) { $missing.Add($s) }
}

$uniq = @($missing | Sort-Object -Unique)
Write-Host ("缺失条目(唯一): {0}" -f $uniq.Count)

# ── 2) 同步 Schema 常量载荷（RS0017 的根因是内嵌的 JSON 文本过期）──
#    转义顺序必须是先 "\" 再 "\""：JSON 内嵌引号的 JSON 转义是 \"，C# 字符串需写成 \\"。
$map = @{}
foreach ($l in [System.IO.File]::ReadAllLines($Golden)) {
    $i = $l.IndexOf("`t")
    if ($i -gt 0) { $map[$l.Substring(0, $i).Replace('.', '_')] = $l.Substring($i + 1) }
}

$all = New-Object 'System.Collections.Generic.List[string]'
foreach ($l in [System.IO.File]::ReadAllLines($ApiFile)) {
    if ($l.Trim()) { $all.Add($l) }
}
foreach ($u in $uniq) { $all.Add($u) }

$schemasUpdated = 0
for ($i = 0; $i -lt $all.Count; $i++) {
    $m = [regex]::Match($all[$i], 'FeishuToolSchemas\.(?<n>\w+)SchemaJson = ".*" -> string!$')
    if ($m.Success -and $map.ContainsKey($m.Groups['n'].Value)) {
        $esc = $map[$m.Groups['n'].Value].Replace('\', '\\').Replace('"', '\"')
        $new = 'const Mud.Feishu.AI.Tools.Generated.FeishuToolSchemas.' + $m.Groups['n'].Value + 'SchemaJson = "' + $esc + '" -> string!'
        if ($new -ne $all[$i]) { $all[$i] = $new; $schemasUpdated++ }
    }
}

$sorted = @($all | Sort-Object -Unique)
[System.IO.File]::WriteAllLines($ApiFile, $sorted, (New-Object System.Text.UTF8Encoding($false)))
Write-Host ("Schema常量更新: {0}；总行数: {1}" -f $schemasUpdated, $sorted.Count)
