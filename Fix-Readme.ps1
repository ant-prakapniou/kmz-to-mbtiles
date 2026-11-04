<#
.SYNOPSIS
    Fixes the Aerial2Mbtiles README formatting that was lost when copying
    through a chat window: restores lists, numbering, code blocks, and wraps
    technical identifiers in backticks.

.PARAMETER Path
    Path to README.md. A backup is written next to it as README.md.bak.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Path
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
    throw "File not found: $Path"
}

$full = (Resolve-Path -LiteralPath $Path).Path
$dir  = Split-Path -Parent $full

Copy-Item -LiteralPath $full -Destination ($full + '.bak') -Force

$text = Get-Content -LiteralPath $full -Raw -Encoding UTF8

# ---------------------------------------------------------------
# 1. Header: ##Requirements -> ## Requirements
# ---------------------------------------------------------------
$text = [regex]::Replace($text, '(?m)^##(?=\S)', '## ')

# ---------------------------------------------------------------
# 2. Backticks: wrap technical identifiers in `...`
#    (skip inside fenced/indented code blocks is handled by the
#    order of operations below: we apply this BEFORE restoring
#    code blocks, and the identifiers are all plain inline words).
# ---------------------------------------------------------------
$identifiers = @(
    '<name>', '<href>', '<GroundOverlay>',
    '\{master\}\{ext\}', '\{master\}',
    'gx:LatLonQuad', 'LatLonBox', 'doc\.kml', 'files/',
    '\.jpg', '\.jpeg', '\.tif', '\.tiff', '\.png',
    'Aerial2Mbtiles\.bat', 'Aerial2Mbtiles\.exe',
    'compare_mbtiles\.py', 'compare_mbtiles\.bat',
    'e_sqlite3\.dll',
    'gdal_translate', 'gdalwarp', 'gdaladdo', 'gdalinfo',
    'PATH', 'EPSG:4326', 'EPSG:3857', 'WGS84', 'SHA-256',
    'Aerial2Mbtiles-<version>-win-x64\.zip', 'LICENSE'
)

foreach ($id in $identifiers) {
    # Do not re-wrap if already wrapped in backticks.
    $text = [regex]::Replace(
        $text,
        "(?<!`)$id(?!`)",
        { param($m) '`' + $m.Value + '`' }
    )
}

# ---------------------------------------------------------------
# 3. "What it does": restore numbering 1..5 and 3-space continuations.
# ---------------------------------------------------------------
$whatItDoesOrder = @(
    'Resolves the input',
    'Shows the four current names',
    'Asks for a normalized master name',
    'Normalizes the pair',
    'In mode 2, additionally creates'
)

for ($i = 0; $i -lt $whatItDoesOrder.Count; $i++) {
    $num = $i + 1
    $pattern = '(?m)^' + [regex]::Escape($whatItDoesOrder[$i])
    $text = [regex]::Replace($text, $pattern, "$num. $($whatItDoesOrder[$i])")
}

# ---------------------------------------------------------------
# 4. Requirements / Usage / Modes / Supported inputs / Supported binding styles
#    Restore bullet markers on specific lines.
# ---------------------------------------------------------------
$bullets = @(
    '^Windows x64$',
    '^GDAL command-line tools on',
    '^Python 3 \(only for the verification script\)$',
    '^one `\.kmz` file, or$',
    '^a raster image and a `\.kml` file \(in any order\)\.$',
    '^Mode 1 — normalize only\.',
    '^Mode 2 — normalize \+ MBTiles\.',
    '^KMZ produced by Google Earth:',
    '^Pair: a raster image and a `\.kml` file',
    '^`gx:LatLonQuad` — four corner coordinates\.$',
    '^`LatLonBox` with optional'
)

foreach ($b in $bullets) {
    $text = [regex]::Replace($text, "(?m)$b", { param($m) '- ' + $m.Value })
}

# ---------------------------------------------------------------
# 5. Code blocks: indent by 4 spaces the lines of specific sections
#    (Usage commands, Output tree, Verification command,
#    Published files list, Project structure tree).
# ---------------------------------------------------------------
function Indent-Block {
    param([string]$InputText, [string]$StartPattern, [string]$EndPattern)
    $pattern = '(?s)(' + $StartPattern + ')(.*?)(' + $EndPattern + ')'
    return [regex]::Replace($InputText, $pattern, {
        param($m)
        $head = $m.Groups[1].Value
        $body = $m.Groups[2].Value
        $tail = $m.Groups[3].Value
        $lines = $body -split "`r?`n"
        $indented = foreach ($line in $lines) {
            if ([string]::IsNullOrWhiteSpace($line)) { '' }
            else { '    ' + $line.TrimStart() }
        }
        $head + ($indented -join "`n") + $tail
    })
}

# Usage commands: two Aerial2Mbtiles.exe lines after "Or run the executable directly:"
$text = Indent-Block -InputText $text `
    -StartPattern '(?m)^(Or run the executable directly:\r?\n)' `
    -EndPattern '(?m)(\r?\nAfter the input is accepted)'

# Output tree: between "inside a `{master}` directory:" and "## Supported inputs"
$text = Indent-Block -InputText $text `
    -StartPattern '(?m)(inside a `\{master\}` directory:\r?\n)' `
    -EndPattern '(?m)(\r?\n## Supported inputs)'

# Verification command: between "or run from a console:" and "The script requires Python 3"
$text = Indent-Block -InputText $text `
    -StartPattern '(?m)(or run from a console:\r?\n)' `
    -EndPattern '(?m)(\r?\nThe script requires Python 3)'

# Published files list: between "containing five files:" and "## Project structure"
$text = Indent-Block -InputText $text `
    -StartPattern '(?m)(containing five files:\r?\n)' `
    -EndPattern '(?m)(\r?\n## Project structure)'

# Project structure tree: between "## Project structure" and "## License"
$text = Indent-Block -InputText $text `
    -StartPattern '(?m)(## Project structure\r?\n)' `
    -EndPattern '(?m)(\r?\n## License)'

# ---------------------------------------------------------------
# 6. Normalize blank lines between bullets: keep single blank line
#    between sections, collapse multiple blank lines.
# ---------------------------------------------------------------
$text = [regex]::Replace($text, '(\r?\n){3,}', "`r`n`r`n")

# ---------------------------------------------------------------
# 7. Write result (UTF-8 without BOM, CRLF).
# ---------------------------------------------------------------
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$normalized = $text -replace "`r?`n", "`r`n"
[System.IO.File]::WriteAllText($full, $normalized, $utf8NoBom)

Write-Host "Done. Backup: $full.bak"
Write-Host "Updated: $full"