# R4 config-key audit (ASCII-only to avoid PS encoding issues)
param([switch]$Strict)

$repoRoot = Split-Path -Parent $PSScriptRoot

# Literal patterns (regex). Keep narrow: assignment/access on SDK config APIs.
$legacyPatterns = @(
    'CircuitBreakerEnabled\s*=',
    'AllowProcessingOnFallback',
    'EnableBackgroundProcessing',
    '\.TimeOut\s*=',
    '\.RetryCount\s*=',
    '\.RetryDelayMs\s*=',
    '\.AutoReconnect\s*=',
    'options\.ServerAddress\s*=',
    'FeishuAppConfig\.EnableLogging'
)

$include = @('*.cs', '*.md', '*.json', '*.jsonc')

$files = Get-ChildItem -Path $repoRoot -Recurse -Include $include -File -ErrorAction SilentlyContinue |
    Where-Object {
        $p = $_.FullName
        ($p -notmatch '\\bin\\') -and
        ($p -notmatch '\\obj\\') -and
        ($p -notmatch '\\.git\\') -and
        ($p -notmatch '\\.docs\\') -and
        ($p -notmatch '\\.tmp\\')
    }

$allowedPathFragments = @(
    'documents\Configuration\',
    'scripts\audit-config-keys.ps1',
    'Mud.Feishu.Abstractions\Configuration\DeduplicationOptions.cs', # XML docs mention deleted field
    'Mud.Feishu.Webhook\Configuration\FeishuWebhookOptions.cs',
    'Mud.Feishu.Abstractions\Consts.cs',
    'CHANGELOG'
)

$hits = @()
foreach ($pattern in $legacyPatterns) {
    $found = $files | Select-String -Pattern $pattern -ErrorAction SilentlyContinue
    foreach ($item in $found) {
        $path = $item.Path
        $allowed = $false
        foreach ($frag in $allowedPathFragments) {
            if ($path -like "*$frag*") { $allowed = $true; break }
        }
        if ($allowed) { continue }
        $hits += "[{0}] {1}" -f $pattern, $path
    }
}

if (-not $hits) {
    Write-Host "[OK] Config audit: no deleted flat config API residues in source." -ForegroundColor Green
    exit 0
}

Write-Host "[WARN] Config audit: $($hits.Count) suspicious legacy config API hits:" -ForegroundColor Yellow
$hits | Select-Object -First 40 | ForEach-Object { Write-Host "  $_" }

if ($Strict) {
    Write-Host "[FAIL] -Strict: migrate to nested Options (see AGENTS.md)." -ForegroundColor Red
    exit 1
}

Write-Host "(warn only; use -Strict to fail CI)"
exit 0
