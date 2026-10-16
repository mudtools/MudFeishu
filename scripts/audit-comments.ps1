# Code-comment governance audit  (T-M0-1)
#
# Purpose: keep process / decision history OUT of source comments. Source comments may only
# carry (a) API contracts for consumers and (b) constraints a maintainer cannot infer from the
# code. Everything else -- upstream adaptation history, defect post-mortems, task/phase ids,
# links to internal planning docs -- belongs in the decision archive, not next to the code.
#
# Rules + rationale : documents/<comment standard>.md (normative; tracked, created in T-M0-2)
#                     AGENTS.md section "Documentation" (summary + pointer)
#                     .docs/<comment remediation plan>-v1.md (plan, sections 4/5/8; NOT tracked,
#                     .docs/ is gitignored -- never make a committed file depend on it alone)
# Rendered API doc  : <remarks> ships inside the NuGet package and in IDE tooltips, so a
#                     polluted <remarks> is a public-facing documentation defect, not a
#                     cosmetic one.
#
# ---------------------------------------------------------------------------
# Scope
# ---------------------------------------------------------------------------
#   The 8 shipping projects.  Demos/ and Tests/ are out of scope by policy, bin/ obj/ .git/
#   and build output are excluded mechanically.
#   Mud.Feishu.DataModels is excluded by default: the remediation plan section 3.2 declares the
#   generated DTO surface out of scope, and the pre-remediation survey measured it clean
#   (0 hits).  Use -IncludeDataModels for the M5 re-evaluation only.
#
# ---------------------------------------------------------------------------
# Levels
# ---------------------------------------------------------------------------
#   E  blocking  : R-01 internal numbering, R-02 repair-history wording, R-04 internal doc
#                  paths, R-06 decorative separators.  Only E-level hits are baselined.
#   W  advisory  : R-03 overlong <remarks>, R-05 repeated boilerplate, R-08 strong-word
#                  density, plus the A-class vocabulary markers (upstream/adapted/misjudged/
#                  regression/refactor).  Never blocks; printed as a worklist.
#
# ---------------------------------------------------------------------------
# Baseline (freeze legacy, zero tolerance for new)
# ---------------------------------------------------------------------------
#   scripts/comment-baseline.txt holds every E-level hit that existed when the gate was
#   introduced, keyed by (relative path, level, content hash) -- NOT by line number.  Line
#   numbers drift as soon as the remediation edits the file, a path+line key would invalidate
#   the whole baseline on the first commit and silently turn the gate into noise.
#   The gate fails only on hits absent from the baseline, so:
#     * the tree is green from day one (the legacy backlog does not block releases), and
#     * the backlog can only shrink -- -UpdateBaseline refuses to grow it without -Force.
#   Each milestone must DELETE the entries it fixed; the gate then proves the fix landed.
#
# ---------------------------------------------------------------------------
# Exemption
# ---------------------------------------------------------------------------
#   Put      // comment-allow: <reason>      on the offending line or the line directly above.
#   Same inline-marker idiom as scripts/audit-config-keys.ps1 ('audit-allow:'), and likewise a
#   per-line exemption only -- whole-file exemption is how dead code survives a gate.
#
# ---------------------------------------------------------------------------
# Run
# ---------------------------------------------------------------------------
#   pwsh ./scripts/audit-comments.ps1                       # report; always exit 0
#   pwsh ./scripts/audit-comments.ps1 -Strict               # CI gate; exit 1 on new hits
#   pwsh ./scripts/audit-comments.ps1 -ListAll              # full hit list (tuning / review)
#   pwsh ./scripts/audit-comments.ps1 -UpdateBaseline       # rewrite baseline (shrinks only)
#   pwsh ./scripts/audit-comments.ps1 -UpdateBaseline -Force
#
# ---------------------------------------------------------------------------
# ENCODING -- read before editing this file
# ---------------------------------------------------------------------------
#   Keep this file pure ASCII.  Every Chinese keyword is written as a .NET regex \uXXXX escape
#   (e.g. '\u4FEE\u590D\u524D' = "repaired-before"), because Windows PowerShell 5.1 decodes a
#   UTF-8-without-BOM script using the ANSI code page: literal CJK would be mangled and the
#   pattern would match nothing -- a silent zero-hit gate that still reports success.
#   Matching is case-sensitive on purpose (compiled Regex, RegexOptions::None -- the equivalent
#   of -cmatch).  The numbering tokens are mixed-case by design (BC-27 vs bc-27) and
#   case-insensitive matching drags in unrelated prose ('m-1', 'c-2').  See the note above
#   New-RegexList: never rewrite the matcher as PowerShell -match.
param(
    [switch]$Strict,
    [switch]$UpdateBaseline,
    [switch]$Force,
    [switch]$IncludeDataModels,
    [switch]$ListAll,
    [int]$Top = 20
)

$ErrorActionPreference = 'Continue'
$repoRoot = Split-Path -Parent $PSScriptRoot
$baselinePath = Join-Path $PSScriptRoot 'comment-baseline.txt'
$inlineAllowMarker = 'comment-allow:'
$remarksMaxLines = 6

$sourceProjects = @(
    'Mud.Feishu.Abstractions',
    'Mud.Feishu',
    'Mud.Feishu.Authentication',
    'Mud.Feishu.EventCallback',
    'Mud.Feishu.OpenTelemetry',
    'Mud.Feishu.Redis',
    'Mud.Feishu.Webhook',
    'Mud.Feishu.WebSocket'
)
if ($IncludeDataModels) { $sourceProjects += 'Mud.Feishu.DataModels' }

# Normalised to '/' so the fragments behave identically on Windows and Linux runners.
$excludePathFragments = @('/bin/', '/obj/', '/.git/', '/.docs/', '/artifacts/', '/TestResults/', '/.vs/')
$excludeFileNames = @('GlobalUsings.cs')
$excludeFileSuffixes = @('.g.cs', '.Designer.cs')

# --- R-01: internal review / decision numbering must never appear in a comment --------------
# Sourced from the remediation plan section 8.1.  The unambiguous prefixes (BC-/TMA-/TMF-/
# TMR-/WHF-/WS2-/R-E/ARC-/AOT-/NEW-/Phase-/Batch) are matched bare.  The single-letter forms
# (U-4, C-2, F-4, S-2, A-2, D10, I12, P2-3, M-4, R-3, X14) need a left guard so that
# identifiers such as 'UTF16', 'RTX2' or 'D01' inside a longer token do not match.
$r01Patterns = @(
    'BC-\d+'
    'TMA2?-\d+'
    'TMF2?-\d+'
    'TMR2?-\d+'
    'WHF-\d+'
    'WS2?-\d+'
    'R-E\d+'
    'ARC-\d+'
    'AOT-\d+'
    'NEW-[A-Z]'
    'Phase-[ABP]\b'
    'Batch\s+[123]\b'
    '(?<![A-Za-z0-9])U-\d+'
    '(?<![A-Za-z0-9])C-[1-7]\b'
    '(?<![A-Za-z0-9])F-\d+'
    '(?<![A-Za-z0-9])S-\d+'
    '(?<![A-Za-z0-9])A-\d+'
    '(?<![A-Za-z0-9])D\d{2}\b'
    '(?<![A-Za-z0-9])I\d{2}\b'
    '(?<![A-Za-z0-9])P[23]-\d+'
    '(?<![A-Za-z0-9])M-\d+'
    '(?<![A-Za-z0-9])R-\d+'
    '(?<![A-Za-z0-9])X\d+\b'
)

# --- R-02: repair-history wording (compare-with-the-past narration) --------------------------
#   \u4FEE\u590D\u524D = "was fixed before"     \u6539\u9020\u524D = "before the rework"
#   \u539F\u5B9E\u73B0 = "original impl"        \u539F\u5148     = "formerly"
#   \u4E0D\u518D\u8BEF\u5224 = "no longer misjudged"
#   \u672C\u4ED3     = "this repo"
$r02Patterns = @(
    '\u4FEE\u590D\u524D'
    '\u6539\u9020\u524D'
    '\u539F\u5B9E\u73B0'
    '\u539F\u5148'
    '\u4E0D\u518D\u8BEF\u5224'
    '\u672C\u4ED3'
)

# --- R-04: internal document paths must not be hard-coded in source comments -----------------
#   The 'documents/' pattern is anchored with (?<![/\w]) and requires a letter after the slash:
#   Feishu OpenAPI URLs contain '/documents/{document_id}' (lowercase / brace) and would
#   otherwise be reported as violations.  \u9A8C\u8BC1\u62A5\u544A = "verification report".
$r04Patterns = @(
    '\.docs/'
    '(?<![/\w])documents/[A-Za-z]'
    '\bAGENTS\.md'
    '\bCHANGELOG\b'
    '\u9A8C\u8BC1\u62A5\u544A'
)

# --- R-06: decorative separators / ASCII frames ---------------------------------------------
#   U+2500..U+257F = box drawing; U+2193/U+2191 = down/up arrows.
$r06Patterns = @(
    '//\s*[\u2500-\u257F=\*#_]{3,}'
    '//\s*[\u2193\u2191]{2,}'
)

# --- W tier: A-class vocabulary markers (advisory; legitimate uses exist) -------------------
#   \u4E0A\u6E38 = upstream   \u9002\u914D = adapted   \u8BEF\u5224 = misjudged
#   \u56DE\u5F52 = regression \u6539\u9020 = rework    \u91CD\u6784 = refactor
$wVocabularyPatterns = @(
    '\u4E0A\u6E38'
    '\u9002\u914D'
    '\u8BEF\u5224'
    '\u56DE\u5F52'
    '\u6539\u9020'
    '\u91CD\u6784'
)

# --- W tier: strong words (R-08) -- allowed only for real constraints, never for narration --
#   \u7981\u6B62 = forbidden  \u5FC5\u987B = must      \u4E0D\u5F97 = shall not
#   \u52A1\u5FC5 = must      \u6545\u610F = on purpose \u523B\u610F = deliberately
$wStrongWordPattern = '\u7981\u6B62|\u5FC5\u987B|\u4E0D\u5F97|\u52A1\u5FC5|\u6545\u610F|\u523B\u610F'
$wStrongWordPerFileThreshold = 8
$boilerplateMinChars = 80
$boilerplateMinProse = 60
$boilerplateMinOccurrences = 5
$boilerplateMinFiles = 3

# Chinese rule vocabulary for the failure message.  [regex]::Unescape turns the \uXXXX escapes
# into real characters at runtime, so this file stays pure ASCII (see the ENCODING note) while
# the developer reading the CI log still sees the actual forbidden wording instead of escapes.
$zhRepairBefore = [regex]::Unescape('\u4FEE\u590D\u524D')
$zhReworkBefore = [regex]::Unescape('\u6539\u9020\u524D')
$zhOriginalImpl = [regex]::Unescape('\u539F\u5B9E\u73B0')
$zhFormerly = [regex]::Unescape('\u539F\u5148')
$zhNoLongerMisjudged = [regex]::Unescape('\u4E0D\u518D\u8BEF\u5224')
$zhThisRepo = [regex]::Unescape('\u672C\u4ED3')
$zhVerificationReport = [regex]::Unescape('\u9A8C\u8BC1\u62A5\u544A')

# Patterns are compiled into case-SENSITIVE Regex objects and shared by all files.
#
# Do NOT rewrite the matcher back to PowerShell's -match operator: -match is case-INsensitive
# by default (-cmatch is the case-sensitive form).  Measured consequence of using -match here:
# the line '// m-1 ...' matched the 'M-\d+' rule, and because the reported token came from a
# case-sensitive re-match it printed as an empty '<>' -- i.e. a phantom hit with no evidence,
# exactly the kind of noise that gets a gate switched off.
function New-RegexList([string[]]$patterns) {
    return @($patterns | ForEach-Object { [regex]::new($_, [System.Text.RegularExpressions.RegexOptions]::None) })
}

$rules = @(
    @{ Id = 'R-01'; Patterns = (New-RegexList $r01Patterns) }
    @{ Id = 'R-02'; Patterns = (New-RegexList $r02Patterns) }
    @{ Id = 'R-04'; Patterns = (New-RegexList $r04Patterns) }
    @{ Id = 'R-06'; Patterns = (New-RegexList $r06Patterns) }
)
$wVocabularyRegexes = New-RegexList $wVocabularyPatterns
$wStrongWordRegex = [regex]::new($wStrongWordPattern, [System.Text.RegularExpressions.RegexOptions]::None)

function ConvertTo-AuditPath([string]$path) { return $path.Replace('\', '/') }

function Get-TextHash([string]$text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($text))
        return ([BitConverter]::ToString($bytes) -replace '-', '').Substring(0, 16).ToLowerInvariant()
    }
    finally { $sha.Dispose() }
}

# The copyright banner is the leading comment block (6 lines today, defined by
# .editorconfig's file_header_template).  It is skipped by position rather than by matching its
# text: the banner itself contains the strong word "\u7981\u6B62" and would otherwise hit every
# file in the repo.  Derived dynamically so a future header change cannot resurrect the noise.
function Get-FirstCodeIndex([string[]]$lines) {
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $t = $lines[$i].Trim()
        if ($t -eq '') { continue }
        if ($t.StartsWith('//')) { continue }
        return $i
    }
    return $lines.Count
}

# ---------------------------------------------------------------- file discovery
$files = New-Object System.Collections.Generic.List[System.IO.FileInfo]
foreach ($project in $sourceProjects) {
    $dir = Join-Path $repoRoot $project
    if (-not (Test-Path $dir)) {
        Write-Host "[WARN] project directory not found, skipped: $project" -ForegroundColor Yellow
        continue
    }
    foreach ($f in @(Get-ChildItem -Path $dir -Recurse -Filter '*.cs' -File -ErrorAction SilentlyContinue)) {
        $p = ConvertTo-AuditPath $f.FullName
        $skip = $false
        foreach ($frag in $excludePathFragments) {
            if ($p -like "*$frag*") { $skip = $true; break }
        }
        if (-not $skip) {
            foreach ($suffix in $excludeFileSuffixes) {
                if ($f.Name.EndsWith($suffix, [System.StringComparison]::OrdinalIgnoreCase)) { $skip = $true; break }
            }
        }
        if (-not $skip -and ($excludeFileNames -contains $f.Name)) { $skip = $true }
        if (-not $skip) { $files.Add($f) }
    }
}

if ($files.Count -eq 0) {
    Write-Host '[FAIL] No source files matched -- the scan scope is broken.' -ForegroundColor Red
    exit 2
}

# ---------------------------------------------------------------- scan
$eHits = New-Object System.Collections.Generic.List[object]
$wHits = New-Object System.Collections.Generic.List[object]
$remarksBlocks = New-Object System.Collections.Generic.List[object]
$boilerplateCounts = @{}
$boilerplateFirst = @{}
$boilerplateTexts = @{}
$boilerplateFiles = @{}
$linesScanned = 0

foreach ($file in $files) {
    $relPath = ConvertTo-AuditPath $file.FullName.Substring($repoRoot.Length + 1)

    $lines = @(Get-Content -LiteralPath $file.FullName -Encoding UTF8 -ErrorAction SilentlyContinue)
    if ($lines.Count -eq 0) { continue }
    $linesScanned += $lines.Count
    $firstCodeIndex = Get-FirstCodeIndex $lines

    $strongWords = 0
    $remarksOpen = -1
    $docWindow = New-Object System.Collections.Generic.List[string]
    $docWindowStart = 0

    for ($i = $firstCodeIndex; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $lineNo = $i + 1
        $isDoc = $line -match '^\s*///'
        $isComment = $isDoc -or ($line -match '^\s*//')

        # R-03: <remarks> block length (counted on raw lines, comments only).
        if ($isComment) {
            if ($remarksOpen -lt 0 -and $line -match '<remarks') {
                $remarksOpen = $lineNo
            }
            elseif ($remarksOpen -ge 0 -and $line -match '</remarks>') {
                $len = $lineNo - $remarksOpen + 1
                if ($len -gt $remarksMaxLines) {
                    $remarksBlocks.Add([pscustomobject]@{
                            Path    = $relPath
                            Line    = $remarksOpen
                            Length  = $len
                            Level   = 'W'
                            Rule    = 'R-03'
                            Samples = $lines[($remarksOpen - 1)..([Math]::Min($remarksOpen + 1, $lines.Count - 1))] -join ' / '
                        })
                }
                $remarksOpen = -1
            }
        }

        if (-not $isComment) { continue }

        if ($wStrongWordRegex.IsMatch($line)) { $strongWords++ }

        $allowed = $false
        if ($line.Contains($inlineAllowMarker)) { $allowed = $true }
        elseif ($i -ge 1 -and $lines[$i - 1].Contains($inlineAllowMarker)) { $allowed = $true }
        if ($allowed) { continue }

        $trimmed = $line.Trim()
        if ($trimmed.Length -gt 0) {
            foreach ($rule in $rules) {
                foreach ($rx in $rule.Patterns) {
                    $m = $rx.Match($line)
                    if ($m.Success) {
                        $eHits.Add([pscustomobject]@{
                                Path   = $relPath
                                Line   = $lineNo
                                Level  = 'E'
                                Rule   = $rule.Id
                                Token  = $m.Value
                                Text   = $trimmed
                                Hash   = (Get-TextHash $trimmed)
                            })
                        break
                    }
                }
            }
            foreach ($rx in $wVocabularyRegexes) {
                if ($rx.IsMatch($line)) {
                    $wHits.Add([pscustomobject]@{
                            Path  = $relPath
                            Line  = $lineNo
                            Level = 'W'
                            Rule  = 'R-08/vocab'
                            Text  = $trimmed
                        })
                    break
                }
            }
        }

        # R-05: three-line boilerplate windows repeated across the tree.
        # Thresholds are deliberately coarse-grained.  A first cut (>=3 occurrences, >=40 chars)
        # reported 911 windows -- e.g. x292 on the Authentication interfaces, which is the
        # legitimate "<summary>/<param>/<returns>" skeleton repeated per API, not copy-paste
        # drift.  Requiring length >= 80, >= 5 occurrences AND >= 3 distinct files keeps the
        # real target (the 4-line "HTTP 200 + JSON error body" block copied into 15 interface
        # files) and drops the structural noise that would make the worklist unreadable.
        if ($isDoc) {
            $norm = ($line -replace '^\s*///', '' -replace '\s+', ' ').Trim()
            if ($docWindow.Count -eq 0) { $docWindowStart = $lineNo }
            $docWindow.Add($norm)
            if ($docWindow.Count -ge 3) {
                $key = ($docWindow[0] + '|' + $docWindow[1] + '|' + $docWindow[2])
                # Prose-only measure: strip XML tags before measuring.  Without this the
                # detector drowns in repeated *tag skeletons* (x292 on
                # '<param name="cancellationToken"><see cref="CancellationToken"/>\u53D6\u6D88\u64CD\u4F5C\u4EE4\u724C\u5BF9\u8C61\u3002</param>'
                # across 65 files) which is a legitimate convention, not copy-paste drift.
                # What R-05 targets is repeated *prose*.
                $prose = ($key -replace '<[^>]*>', '')
                if ($key.Length -ge $boilerplateMinChars -and $prose.Length -ge $boilerplateMinProse) {
                    if (-not $boilerplateCounts.ContainsKey($key)) {
                        $boilerplateCounts[$key] = 0
                        $boilerplateFirst[$key] = "${relPath}:${docWindowStart}"
                        $boilerplateTexts[$key] = $docWindow[0]
                        $boilerplateFiles[$key] = @{}
                    }
                    $boilerplateCounts[$key]++
                    $boilerplateFiles[$key][$relPath] = $true
                }
                $docWindow.RemoveAt(0)
                $docWindowStart++
            }
        }
        else {
            $docWindow.Clear()
        }
    }

    if ($strongWords -ge $wStrongWordPerFileThreshold) {
        $wHits.Add([pscustomobject]@{
                Path  = $relPath
                Line  = 0
                Level = 'W'
                Rule  = 'R-08/density'
                Text  = "strong-word density = $strongWords"
            })
    }
}

# ---------------------------------------------------------------- baseline IO
function Read-Baseline([string]$path) {
    $counts = @{}
    if (-not (Test-Path $path)) { return $counts }
    foreach ($line in @(Get-Content -LiteralPath $path -Encoding UTF8 -ErrorAction SilentlyContinue)) {
        $t = $line.Trim()
        if ($t -eq '' -or $t.StartsWith('#')) { continue }
        $parts = $t.Split("`t")
        if ($parts.Count -ge 3) {
            $key = "$($parts[0])|$($parts[1])|$($parts[2])"
            $count = 1
            if ($parts.Count -ge 4) { [void][int]::TryParse($parts[3], [ref]$count) }
            $counts[$key] = $count
        }
    }
    return $counts
}

$baselineCounts = Read-Baseline $baselinePath
$baselineTotal = 0
foreach ($v in $baselineCounts.Values) { $baselineTotal += $v }

$currentCounts = @{}
foreach ($hit in $eHits) {
    $key = "$($hit.Path)|$($hit.Level)|$($hit.Hash)"
    if (-not $currentCounts.ContainsKey($key)) { $currentCounts[$key] = 0 }
    $currentCounts[$key]++
}

$newHits = New-Object System.Collections.Generic.List[object]
$matchedHits = 0
foreach ($hit in $eHits) {
    $key = "$($hit.Path)|$($hit.Level)|$($hit.Hash)"
    $allowedCount = 0
    if ($baselineCounts.ContainsKey($key)) { $allowedCount = $baselineCounts[$key] }
    # Duplicate identical lines are baselined as a multiset count; only the surplus is new.
    if ($allowedCount -gt 0) {
        $baselineCounts[$key] = $allowedCount - 1
        $matchedHits++
    }
    else {
        $newHits.Add($hit)
    }
}
$resolvedCount = 0
foreach ($v in $baselineCounts.Values) { $resolvedCount += $v }

# ---------------------------------------------------------------- report
Write-Host ''
Write-Host 'Code-comment audit (process/decision narrative in source comments)' -ForegroundColor Cyan
Write-Host ("  projects scanned : {0}" -f ($sourceProjects -join ', ')) -ForegroundColor DarkGray
Write-Host ("  files / lines    : {0} / {1}" -f $files.Count, $linesScanned) -ForegroundColor DarkGray

Write-Host ''
Write-Host '  [statistics] E-level hits (blocking rules) by project:' -ForegroundColor Cyan
$byProject = @($eHits | Group-Object { $_.Path.Split('/')[0] } | Sort-Object Count -Descending)
if ($byProject.Count -eq 0) { Write-Host '    (none)' -ForegroundColor Green }
foreach ($g in $byProject) { Write-Host ("    {0,-28} {1,5}" -f $g.Name, $g.Count) }
Write-Host ("    {0,-28} {1,5}" -f '(total)', $eHits.Count) -ForegroundColor DarkGray

Write-Host ''
Write-Host '  [statistics] E-level hits by rule:' -ForegroundColor Cyan
foreach ($g in @($eHits | Group-Object Rule | Sort-Object Count -Descending)) {
    Write-Host ("    {0,-8} {1,5}" -f $g.Name, $g.Count)
}

Write-Host ''
Write-Host '  [statistics] hottest files (top 15, E-level):' -ForegroundColor Cyan
foreach ($g in @($eHits | Group-Object Path | Sort-Object Count -Descending | Select-Object -First 15)) {
    Write-Host ("    {0,4}  {1}" -f $g.Count, $g.Name)
}

if ($remarksBlocks.Count -gt 0) {
    Write-Host ''
    Write-Host ("  [W] R-03 overlong <remarks> blocks (>{0} lines): {1}" -f $remarksMaxLines, $remarksBlocks.Count) -ForegroundColor DarkYellow
    foreach ($b in @($remarksBlocks | Sort-Object Length -Descending | Select-Object -First $Top)) {
        Write-Host ("    {0,4} lines  {1}:{2}" -f $b.Length, $b.Path, $b.Line) -ForegroundColor DarkGray
    }
}

if ($wHits.Count -gt 0) {
    Write-Host ''
    Write-Host ("  [W] A-class vocabulary markers (advisory, {0} lines):" -f $wHits.Count) -ForegroundColor DarkYellow
    foreach ($g in @($wHits | Group-Object Path | Sort-Object Count -Descending | Select-Object -First $Top)) {
        Write-Host ("    {0,4}  {1}" -f $g.Count, $g.Name) -ForegroundColor DarkGray
    }
}

$boiler = @($boilerplateCounts.GetEnumerator() |
        Where-Object {
            $_.Value -ge $boilerplateMinOccurrences -and
            @($boilerplateFiles[$_.Key].Keys).Count -ge $boilerplateMinFiles
        } |
        Sort-Object Value -Descending)
if ($boiler.Count -gt 0) {
    Write-Host ''
    Write-Host ("  [W] R-05 repeated 3-line doc boilerplate (>={0} chars, >={1} prose chars, >={2} occurrences, >={3} files): {4} window(s)" -f $boilerplateMinChars, $boilerplateMinProse, $boilerplateMinOccurrences, $boilerplateMinFiles, $boiler.Count) -ForegroundColor DarkYellow
    foreach ($b in @($boiler | Select-Object -First $Top)) {
        $preview = $boilerplateTexts[$b.Key]
        if ($preview.Length -gt 100) { $preview = $preview.Substring(0, 100) + '...' }
        Write-Host ("    x{0,-4} in {1,-3} file(s)  first at {2}" -f $b.Value, @($boilerplateFiles[$b.Key].Keys).Count, $boilerplateFirst[$b.Key]) -ForegroundColor DarkGray
        Write-Host ("        {0}" -f $preview) -ForegroundColor DarkGray
    }
}

# ---------------------------------------------------------------- baseline update
if ($UpdateBaseline) {
    $newTotal = $eHits.Count
    if ((Test-Path $baselinePath) -and $newTotal -gt $baselineTotal -and -not $Force) {
        Write-Host ''
        Write-Host ("[FAIL] Baseline would GROW: {0} -> {1}. The backlog may only shrink." -f $baselineTotal, $newTotal) -ForegroundColor Red
        Write-Host '       Fix the new hits, or re-run with -Force if the growth is deliberate.' -ForegroundColor Red
        exit 2
    }

    $grouped = $eHits | Group-Object { "$($_.Path)|$($_.Level)|$($_.Hash)" }
    $out = New-Object System.Collections.Generic.List[string]
    $out.Add('# scripts/comment-baseline.txt -- frozen legacy E-level comment hits (T-M0-1)')
    $out.Add('# Format: <repo-relative path><TAB><level><TAB><content hash><TAB><count>')
    $out.Add('# Keyed by content hash, not line number, so re-indentation and line moves do not')
    $out.Add('# invalidate the baseline. This file must only ever SHRINK: every milestone deletes')
    $out.Add('# the entries it fixed, and the gate must pass with an empty file after M4.')
    $out.Add('# Regenerate: pwsh ./scripts/audit-comments.ps1 -UpdateBaseline')
    $out.Add('# ------------------------------------------------')
    $out.Add(("# generated  : {0}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')))
    $out.Add(("# entries    : {0}" -f $grouped.Count))
    $out.Add(("# hits       : {0}" -f $newTotal))
    $out.Add('# ------------------------------------------------')
    foreach ($g in @($grouped | Sort-Object Name)) {
        $first = $g.Group[0]
        $out.Add(("{0}`t{1}`t{2}`t{3}" -f $first.Path, $first.Level, $first.Hash, $g.Count))
    }
    Set-Content -LiteralPath $baselinePath -Value $out -Encoding ASCII
    Write-Host ''
    Write-Host ("[OK] Baseline written: {0} entries / {1} hits -> {2}" -f $grouped.Count, $newTotal, $baselinePath) -ForegroundColor Green
    exit 0
}

# ---------------------------------------------------------------- verdict
Write-Host ''
Write-Host ("  baseline         : {0} hit(s) frozen, {1} already resolved" -f $baselineTotal, $resolvedCount) -ForegroundColor DarkGray

if ($ListAll) {
    Write-Host ''
    Write-Host '  [all E-level hits]' -ForegroundColor Cyan
    foreach ($hit in @($eHits | Sort-Object Path, Line)) {
        Write-Host ("    [{0}] <{1}> {2}:{3}: {4}" -f $hit.Rule, $hit.Token, $hit.Path, $hit.Line, $hit.Text) -ForegroundColor DarkGray
    }
}

if ($newHits.Count -eq 0) {
    Write-Host ''
    Write-Host '[OK] Comment audit: no new process/decision narrative in source comments.' -ForegroundColor Green
    if ($baselineTotal -gt 0) {
        Write-Host ("     ({0} legacy hit(s) still frozen in the baseline -- delete entries as milestones land.)" -f $baselineTotal) -ForegroundColor DarkGray
    }
    exit 0
}

Write-Host ''
Write-Host ("[FAIL] Comment audit: {0} NEW process/decision narrative hit(s) (not in the baseline):" -f $newHits.Count) -ForegroundColor Red
foreach ($hit in @($newHits | Select-Object -First $Top)) {
    # Include the matched token: without it a report reads like an accusation with no evidence,
    # and the reader has to re-grep the line to find which substring tripped the rule.
    Write-Host ("    [{0}] <{1}> {2}:{3}: {4}" -f $hit.Rule, $hit.Token, $hit.Path, $hit.Line, $hit.Text) -ForegroundColor Red
}
if ($newHits.Count -gt $Top) {
    Write-Host ("    ... and {0} more (use -Top to widen)." -f ($newHits.Count - $Top)) -ForegroundColor Red
}
Write-Host ''
Write-Host '    Rules -- documents/<comment standard>.md (full text), AGENTS.md (summary):' -ForegroundColor Yellow
Write-Host '      R-01  internal review numbering  BC-27 / TMF-02 / TMR2-P1-5 / WHF-07 / WS2-07 / TMA-09 /' -ForegroundColor Yellow
Write-Host '            A-2 / C-2 / F-4 / S-2 / U-3 / D10 / I12 / X5 / P2-3 / M-4 / R-3 / R-E1 /' -ForegroundColor Yellow
Write-Host '            ARC-2 / AOT-3 / NEW-M / Phase-A / Batch 3' -ForegroundColor Yellow
Write-Host ("      R-02  repair-history wording     {0} / {1} / {2} / {3} / {4} / {5}" -f `
        $zhRepairBefore, $zhReworkBefore, $zhOriginalImpl, $zhFormerly, $zhNoLongerMisjudged, $zhThisRepo) -ForegroundColor Yellow
Write-Host ("      R-04  internal paths             .docs/ / documents/... / AGENTS.md / CHANGELOG / {0}" -f $zhVerificationReport) -ForegroundColor Yellow
Write-Host ("      R-06  decoration                 // ---- / // ==== / // {0}{0}{0}" -f ([char]0x2193)) -ForegroundColor Yellow
Write-Host '    Keep the constraint, move the history: the narrative belongs in the decision archive' -ForegroundColor Yellow
Write-Host '    (documents/<decision archive>/), not next to the code.' -ForegroundColor Yellow
Write-Host "    If a hit is a genuine constraint that must stay, mark it with '// comment-allow: <reason>'" -ForegroundColor Yellow
Write-Host '    on that line or the line directly above.' -ForegroundColor Yellow

if ($Strict) {
    Write-Host '[FAIL] -Strict: comment audit failed.' -ForegroundColor Red
    exit 1
}

Write-Host '(warn only; use -Strict to fail CI)' -ForegroundColor DarkGray
exit 0
