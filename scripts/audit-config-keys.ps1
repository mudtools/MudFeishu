# R4/R5 config-key audit (ASCII-only to avoid PS encoding issues)
#
# Purpose: stop a *removed* config API / config key from silently reappearing in source.
#
# ---------------------------------------------------------------------------
# R5 redesign (see .docs/配置面可用性修复与收敛方案-R5.md, G-03 / RK13)
# ---------------------------------------------------------------------------
# Three problems were found while trying to wire this script into CI as a gate:
#
#   1) The allowlist exempted WHOLE FILES (e.g. FeishuWebhookOptions.cs). A blank exemption
#      hides every future dead key in that file -- that is exactly how EnableRequestLogging
#      survived R4.  -> Replaced by per-line inline markers:  // audit-allow: <reason>
#         honoured on the matching line or on the line directly above it.
#
#   2) Demos/ was scanned.  Demos intentionally keep pre-migration config shapes and are
#      shadowed by Demos/Directory.Build.props (no governance properties), so scanning them
#      only adds noise.  -> Excluded.
#
#   3) The R4-era patterns are broad heuristics that produce false positives even on a clean
#      tree, e.g. '\.TimeOut\s*=' (case-insensitive) matches `client.Timeout = ...`, and
#      '\.RetryCount\s*=' matches `FailedEventInfo.RetryCount = ...` (an unrelated type).
#      Gating CI on them would either fail on day one or force another blanket allowlist.
#      -> Split into two tiers:
#           $strictPatterns : exact, high-precision "removed key" names  -> gated by -Strict
#           $warnPatterns   : broad R4-era heuristics                    -> always warn only
#         This is what makes -Strict usable as a CI gate today.
#
# Run:  powershell -File ./scripts/audit-config-keys.ps1            # warn
#       powershell -File ./scripts/audit-config-keys.ps1 -Strict    # CI gate
param([switch]$Strict)

$repoRoot = Split-Path -Parent $PSScriptRoot

# --- Tier 1: exact, high-precision patterns. Any hit is a genuine regression. -----------------
# Append the pattern for a key in the SAME phase that removes the key, so the gate never
# fails on a still-supported configuration surface.
#   R5.0: EnableRequestLogging (X2) + the 3 unused dedup Consts (X14)
#   R5.1: 'IOptions<FailedEventRetryOptions>' (X3)
#   R5.2: '\.AutoRegisterEndpoint\s*=', 'EnablePerformanceMonitoring' (X4 / X10)
$strictPatterns = @(
    'EnableRequestLogging',
    'DefaultDeduplicationRetryCount',
    'DefaultDeduplicationInitialRetryDelayMs',
    'DefaultDeduplicationMaxRetryDelayMs'
)

# --- Tier 2: broad R4-era heuristics. Warn only (false positives are expected). ---------------
$warnPatterns = @(
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

# Paths that never contain SDK configuration surfaces, intentionally keep pre-migration shapes,
# or must be able to *name* removed keys in order to assert their absence (Tests / ContractGuards).
$excludePathFragments = @(
    '\bin\',
    '\obj\',
    '\.git\',
    '\.docs\',
    '\.tmp\',
    '\Demos\'          # R5/G-03: demos keep legacy config shapes on purpose
)

# The only legitimate *whole-file* exemptions: assets that document what was removed.
$allowedPathFragments = @(
    'documents\Configuration\',
    'scripts\audit-config-keys.ps1',
    'CHANGELOG'
)

$inlineAllowMarker = 'audit-allow:'

$files = Get-ChildItem -Path $repoRoot -Recurse -Include $include -File -ErrorAction SilentlyContinue |
    Where-Object {
        $p = $_.FullName
        $skip = $false
        foreach ($frag in $excludePathFragments) {
            if ($p -like "*$frag*") { $skip = $true; break }
        }
        -not $skip
    }

# Cache file content per path so the inline-marker lookup stays O(files) rather than O(hits).
$contentCache = @{}
function Get-Lines([string]$path) {
    if (-not $contentCache.ContainsKey($path)) {
        $contentCache[$path] = @(Get-Content -LiteralPath $path -ErrorAction SilentlyContinue)
    }
    return $contentCache[$path]
}

function Test-InlineAllowed([string]$path, [int]$oneBasedLine) {
    $lines = Get-Lines $path
    $idx = $oneBasedLine - 1
    $candidates = @()
    if ($idx -ge 0 -and $idx -lt $lines.Count) { $candidates += $lines[$idx] }
    if ($idx -ge 1) { $candidates += $lines[$idx - 1] }
    return ($candidates -join "`n").Contains($inlineAllowMarker)
}

function Invoke-Audit([string[]]$patterns, [switch]$CaseSensitive) {
    $hits = @()
    foreach ($pattern in $patterns) {
        $found = if ($CaseSensitive) {
            $files | Select-String -Pattern $pattern -CaseSensitive -ErrorAction SilentlyContinue
        }
        else {
            $files | Select-String -Pattern $pattern -ErrorAction SilentlyContinue
        }

        foreach ($item in $found) {
            $path = $item.Path

            $allowed = $false
            foreach ($frag in $allowedPathFragments) {
                if ($path -like "*$frag*") { $allowed = $true; break }
            }
            if ($allowed) { continue }
            if (Test-InlineAllowed $path ([int]$item.LineNumber)) { continue }

            $hits += "[{0}] {1}:{2}: {3}" -f $pattern, $path, $item.LineNumber, $item.Line.Trim()
        }
    }
    return $hits
}

$strictHits = @(Invoke-Audit $strictPatterns -CaseSensitive)
$warnHits = @(Invoke-Audit $warnPatterns)

if ($warnHits.Count -gt 0) {
    Write-Host "[INFO] R4-era heuristic hits ($($warnHits.Count), warn-only -- broad patterns, false positives expected):" -ForegroundColor DarkYellow
    $warnHits | Select-Object -First 20 | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray }
}

if ($strictHits.Count -eq 0) {
    Write-Host "[OK] Config audit: no removed config API residues in source." -ForegroundColor Green
    exit 0
}

Write-Host "[FAIL] Config audit: $($strictHits.Count) removed config API residue(s):" -ForegroundColor Red
$strictHits | Select-Object -First 40 | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }

if ($Strict) {
    Write-Host "[FAIL] -Strict: migrate to nested Options (see AGENTS.md)." -ForegroundColor Red
    Write-Host "       If a hit is legitimate documentation/assertion, mark it with '// audit-allow: <reason>' on that line or the line above." -ForegroundColor Red
    exit 1
}

Write-Host "(warn only; use -Strict to fail CI)"
exit 0
