# Comment budget audit: present the frozen ledger (phased-cleanup targets) + re-freeze entrypoint.
#
# ASCII-only on purpose (same reason as audit-config-keys.ps1): a UTF-8-without-BOM script with
# CJK literals is a parse error in Windows PowerShell 5.1.
#
# The judgement rules have exactly ONE implementation:
#   Tests/Mud.Feishu.AI.Tests/ContractGuards/CommentConventionContractGuards.cs
#     - review anchors must resolve in documents/ or CHANGELOG.md;
#     - process narration ("previous/removed/...") must cite an EXISTING document;
#     - comment lines and longest comment block must not exceed the frozen baseline.
# This script deliberately does NOT re-implement them: it only presents the ledger and drives the
# guard's re-freeze entrypoint, so the two can never drift apart.
#
#   pwsh ./scripts/comment-audit.ps1                          # cleanup targets, largest ledger first
#   pwsh ./scripts/comment-audit.ps1 -Project Mud.Feishu.AI    # one project only
#   pwsh ./scripts/comment-audit.ps1 -LongBlocks               # only longest-block-over-20 files
#   pwsh ./scripts/comment-audit.ps1 -UpdateBaseline           # re-freeze (review the diff!)
param(
    [string]$Project,
    [int]$Top = 20,
    [switch]$LongBlocks,
    [switch]$UpdateBaseline
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$baselinePath = Join-Path $repoRoot 'documents/AIAgent/comment-budget-baseline.json'
$testProject = Join-Path $repoRoot 'Tests/Mud.Feishu.AI.Tests/Mud.Feishu.AI.Tests.csproj'
$guardFilter = 'FullyQualifiedName~CommentConventionContractGuards'

if ($UpdateBaseline) {
    $env:FeishuCommentBaselineUpdate = 'true'
    try {
        dotnet test $testProject -c Debug --filter $guardFilter --nologo
        if ($LASTEXITCODE -ne 0) {
            throw "guard failed (exit $LASTEXITCODE): baseline NOT written; fix the violations first"
        }
    }
    finally {
        Remove-Item Env:FeishuCommentBaselineUpdate -ErrorAction SilentlyContinue
    }
    Write-Host 'Baseline re-frozen: documents/AIAgent/comment-budget-baseline.json (review the diff!)' -ForegroundColor Green
}

if (-not (Test-Path $baselinePath)) {
    throw "baseline missing: $baselinePath (run with -UpdateBaseline first)"
}

$baseline = Get-Content $baselinePath -Raw -Encoding UTF8 | ConvertFrom-Json

$rows = $baseline.files.PSObject.Properties | ForEach-Object {
    [pscustomobject]@{
        File     = $_.Name
        Lines    = [int]$_.Value.lines
        Comments = [int]$_.Value.comments
        Ratio    = if ($_.Value.lines -gt 0) { [math]::Round($_.Value.comments / $_.Value.lines, 3) } else { 0 }
        MaxBlock = [int]$_.Value.maxBlock
    }
}

if ($Project) {
    $rows = $rows | Where-Object { $_.File -like "$Project/*" }
    if (-not $rows) { throw "no ledger entry matches -Project '$Project'" }
}

if ($LongBlocks) {
    $rows = $rows | Where-Object { $_.MaxBlock -gt 20 }
}

Write-Host ''
Write-Host 'Comment ledger (frozen values; tighten-only)' -ForegroundColor Cyan
Write-Host ("  scope      : {0}" -f ($baseline.scope -join ', '))
Write-Host ("  files      : {0}    comment lines: {1}" -f $rows.Count, ($rows | Measure-Object Comments -Sum).Sum)
Write-Host ("  dead refs  : {0}    unlinked narration: {1}" -f $baseline.anchors.Count, $baseline.narration.Count)
Write-Host ''
Write-Host ("Cleanup targets (top $Top by comment lines; '*' = longest block > 20)") -ForegroundColor Cyan
$rows |
    Sort-Object Comments -Descending |
    Select-Object -First $Top |
    ForEach-Object {
        $mark = if ($_.MaxBlock -gt 20) { '*' } else { ' ' }
        "  {0} {1,4} comments / {2,5} lines (ratio {3,-5}) maxBlock {4,3}" -f $mark, $_.Comments, $_.Lines, $_.Ratio, $_.MaxBlock
        "      {0}" -f $_.File
    }

Write-Host ''
Write-Host 'Unresolvable review anchors (token x occurrences)' -ForegroundColor Cyan
$baseline.anchors | Group-Object { ($_ -split '\|')[1] } | Sort-Object Count -Descending | ForEach-Object {
    "  {0,-12} x{1}" -f $_.Name, $_.Count
}

Write-Host ''
Write-Host 'Unlinked narration (keyword x occurrences)' -ForegroundColor Cyan
$baseline.narration | Group-Object { ($_ -split '\|')[1] } | Sort-Object Count -Descending | ForEach-Object {
    "  {0,-6} x{1}" -f $_.Name, $_.Count
}

Write-Host ''
Write-Host 'Cleanup rule: drop round ids and process narration, move decisions to documents/AIAgent/decisions/,'
Write-Host 'keep only the "cannot be read off the code" traps (platform quirks, ordering) inline; then re-freeze.'
