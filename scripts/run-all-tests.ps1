$projects = @(
    'Tests/Mud.Feishu.Abstractions.Tests/Mud.Feishu.Abstractions.Tests.csproj',
    'Tests/Mud.Feishu.AI.Tests/Mud.Feishu.AI.Tests.csproj',
    'Tests/Mud.Feishu.AI.Tools.Tests/Mud.Feishu.AI.Tools.Tests.csproj',
    'Tests/Mud.Feishu.Agent.Demo.Tests/Mud.Feishu.Agent.Demo.Tests.csproj'
)
foreach ($p in $projects) {
    $full = Join-Path 'e:\Temp\MudFeishu' $p
    $out = dotnet test $full --nologo 2>&1
    $summary = $out | Select-String -Pattern '已通过!|失败!'
    $name = ($p -split '/')[1]
    if ($summary) { Write-Output ($name + ' => ' + $summary[-1].Line.Trim()) } else { Write-Output ($name + ' => NO SUMMARY (build error?)') }
    $err = $out | Select-String -Pattern 'error CS'
    if ($err) { $err | Select-Object -First 3 | ForEach-Object { Write-Output ('   ' + $_.Line) } }
}
