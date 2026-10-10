param(
    [string]$Project = 'Mud.Feishu.AI.Tools/Mud.Feishu.AI.Tools.csproj',
    [string]$ApiFile = 'Mud.Feishu.AI.Tools/PublicAPI.Unshipped.txt',
    [string]$Golden = 'Mud.Feishu.AI.Tools/FeishuToolSchemas.golden.txt'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

# ── 1)补全缺失条目（RS0016）──
$log = dotnet build $Project -c Release --nologo -v n --no-incremental 2>&1 | Out-String

# ⚠️ BUG-7：只解析**目标工程**的诊断。构建目标工程会把依赖工程的诊断一并写进同一份日志
#    （实测：Mud.Feishu.AI 的 RS0016 曾被写进 Mud.Feishu.AI.Tools 的 PublicAPI 文件，
#      反之 AI 文件里的陈旧条目会被 AI.Tools 的 RS0017 顺带删除），故按诊断行尾的归属标记
#    `[<绝对路径>\<Project>.csproj::TargetFramework=...]` 过滤。
#    过滤后为空 ⇒ 直接中止：否则"过滤正则写坏"会退化成"什么都不补"的假绿。
$projName = [System.IO.Path]::GetFileName($Project)
$ownPattern = '\[[^\[\]]*' + [regex]::Escape($projName) + '::'
$ownLines = @($log -split "`r?`n" | Where-Object { $_ -match $ownPattern })
if ($ownLines.Count -eq 0) {
    throw "未解析到目标工程 '$projName' 的任何诊断行——归属过滤可能整体失效（假绿风险），自证失败，脚本中止。"
}
$ownLog = $ownLines -join "`n"
Write-Host ("目标工程诊断行: {0}（共 {1} 行日志）" -f $ownLines.Count, ($log -split "`r?`n").Count)

$existing = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($line in [System.IO.File]::ReadAllLines($ApiFile)) {
    if ($line.Trim()) { [void]$existing.Add($line.Trim()) }
}

$missing = New-Object 'System.Collections.Generic.List[string]'
# ⚠️ 不可用 `[^']+` 捕获符号：工具描述里**合法地**含有单引号（如 `适用于'这条不该我批'`），
#    `[^']+` 会在中间提前截断导致整条正则匹配失败 ⇒ 脚本**静默漏补**这些工具的 PublicAPI
#    条目（RS0016 永久残留，且不会有任何提示）。改为非贪婪 + 锚定尾界 `' is not part`。
foreach ($m in [regex]::Matches($ownLog, "RS0016: Symbol '(?<s>.*?)' is not part")) {
    $s = $m.Groups['s'].Value
    if (-not $existing.Contains($s)) { $missing.Add($s) }
}

$uniq = @($missing | Sort-Object -Unique)
Write-Host ("缺失条目(唯一): {0}" -f $uniq.Count)

# ── 1b) 清理陈旧条目（RS0017）──
#    RS0017 = "已声明为公开 API、但在程序集里找不到" ⇒ 上一轮**改名或回滚**留下的残留。
#    本脚本原先只补不删，于是这些残留在文件里**单向累积**（实测已累积 112 条），
#    每次构建都刷屏，最终淹没真正的 API 漂移信号。
$stale = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($m in [regex]::Matches($ownLog, "RS0017: Symbol '(?<s>.*?)' is part of")) {
    [void]$stale.Add($m.Groups['s'].Value.Trim())
}

# 同一次构建里"既报 RS0017 旧签名、又报 RS0016 新签名"的符号是**签名变更**而非陈旧，
# 必须保留新签名 —— 否则会把刚改对的条目又删掉。
$added = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($u in $uniq) { [void]$added.Add($u) }
foreach ($s in @($stale)) { if ($added.Contains($s)) { [void]$stale.Remove($s) } }

Write-Host ("陈旧条目(将删除): {0}" -f $stale.Count)

# ── 2) 同步 Schema 常量载荷（RS0017 的根因是内嵌的 JSON 文本过期）──
#    转义顺序必须是先 "\" 再 "\""：JSON 内嵌引号的 JSON 转义是 \"，C# 字符串需写成 \\"。
$map = @{}
foreach ($l in [System.IO.File]::ReadAllLines($Golden)) {
    $i = $l.IndexOf("`t")
    if ($i -gt 0) { $map[$l.Substring(0, $i).Replace('.', '_')] = $l.Substring($i + 1) }
}

$all = New-Object 'System.Collections.Generic.List[string]'
foreach ($l in [System.IO.File]::ReadAllLines($ApiFile)) {
    if (-not $l.Trim()) { continue }

    # 跳过陈旧条目（改名/回滚残留），否则它们会永久滞留。
    if ($stale.Contains($l.Trim())) { continue }

    $all.Add($l)
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
# BUG-7（可读性项）：三项计数分别列出——原实现只打印"Schema常量更新"，
# 走 RS0016/RS0017 补删路径时该计数恒为 0，容易被误读成"脚本没干活"。
Write-Host ("补入条目: {0}；删除陈旧条目: {1}；Schema载荷替换: {2}；总行数: {3}" -f `
    $uniq.Count, $stale.Count, $schemasUpdated, $sorted.Count)
