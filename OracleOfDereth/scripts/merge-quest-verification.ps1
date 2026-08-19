param(
    [Parameter(Mandatory = $true)]
    [string[]]$InputPaths,

    [string]$Server = 'Conquest',
    [string]$CatalogPath = '',
    [switch]$ReplaceVerification
)

# Merge new evidence into the existing server column:
# powershell -ExecutionPolicy Bypass -File .\scripts\merge-quest-verification.ps1 `
#   -InputPaths pillows.txt,stann.txt -Server Conquest
#
# Add -ReplaceVerification to rebuild that column solely from the supplied files.

$ErrorActionPreference = 'Stop'

$expandedInputPaths = [System.Collections.Generic.List[string]]::new()
foreach ($inputPath in $InputPaths) {
    if (Test-Path -LiteralPath $inputPath) {
        $expandedInputPaths.Add($inputPath)
    }
    else {
        foreach ($part in $inputPath.Split(',')) {
            if (![string]::IsNullOrWhiteSpace($part)) { $expandedInputPaths.Add($part.Trim()) }
        }
    }
}

if ([string]::IsNullOrWhiteSpace($CatalogPath)) {
    $CatalogPath = Join-Path $PSScriptRoot '..\Resources\quests.csv'
}
$CatalogPath = [System.IO.Path]::GetFullPath($CatalogPath)

function Read-AccountQuestFlags([string]$path) {
    $path = [System.IO.Path]::GetFullPath($path)
    if (!(Test-Path -LiteralPath $path)) { throw "Input file not found: $path" }

    $flags = [System.Collections.Generic.List[string]]::new()
    $inside = $false
    $expected = 0
    $parsed = 0

    foreach ($raw in [System.IO.File]::ReadLines($path)) {
        $line = $raw -replace '^\s*\d{1,2}:\d{2}:\d{2}\s+', ''

        if ($line -match '^-{4}\s*Account Quests\s*\((\d+)\)') {
            if ($inside) { throw "Nested account quest header in $path" }
            $inside = $true
            $expected = [int]$Matches[1]
            $parsed = 0
            continue
        }

        if ($line -match '^-{4}\s*End of Account Quests') {
            if (!$inside) { continue }
            if ($parsed -ne $expected) {
                throw "Parsed $parsed of $expected account quests from $path"
            }
            $inside = $false
            continue
        }

        if (!$inside) { continue }

        $flag = $null
        if ($line -match '^\d+\.\s+(\S+)(?:\s+\([^)]*\))?\s*$') {
            $flag = $Matches[1]
        }
        elseif ($line -match '^(\S+)\s*$' -and $line -notmatch '^-+$') {
            $flag = $Matches[1]
        }

        if ($null -ne $flag) {
            $flags.Add($flag)
            $parsed++
        }
    }

    if ($inside) { throw "Account quest block has no footer in $path" }
    return $flags
}

function Escape-Csv([object]$value) {
    $text = if ($null -eq $value) { '' } else { [string]$value }
    if ($text.IndexOfAny([char[]]",`"`r`n") -ge 0) {
        return '"' + $text.Replace('"', '""') + '"'
    }
    return $text
}

if (!(Test-Path -LiteralPath $CatalogPath)) { throw "Catalog not found: $CatalogPath" }

$verifiedFlags = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
foreach ($path in $expandedInputPaths) {
    foreach ($flag in Read-AccountQuestFlags $path) { [void]$verifiedFlags.Add($flag) }
}

$rows = @(Import-Csv -LiteralPath $CatalogPath)
if ($rows.Count -eq 0) { throw "Catalog is empty: $CatalogPath" }

$headers = [System.Collections.Generic.List[string]]::new()
foreach ($name in $rows[0].PSObject.Properties.Name) { $headers.Add($name) }

$verifiedColumn = "Verified $Server"
if (!$headers.Contains($verifiedColumn)) {
    $insertAt = $headers.IndexOf('Server') + 1
    while ($insertAt -lt $headers.Count -and $headers[$insertAt].StartsWith('Verified ')) { $insertAt++ }
    $headers.Insert($insertAt, $verifiedColumn)
    foreach ($row in $rows) { $row | Add-Member -NotePropertyName $verifiedColumn -NotePropertyValue '' }
}

if ($ReplaceVerification) {
    foreach ($row in $rows) { $row.$verifiedColumn = '' }
}

$catalogFlags = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
foreach ($row in $rows) {
    [void]$catalogFlags.Add($row.QuestFlag)
    if ($verifiedFlags.Contains($row.QuestFlag)) { $row.$verifiedColumn = 'Verified' }
}

$newFlags = @($verifiedFlags | Where-Object { !$catalogFlags.Contains($_) } | Sort-Object)
foreach ($flag in $newFlags) {
    $row = [pscustomobject]@{}
    foreach ($header in $headers) { $row | Add-Member -NotePropertyName $header -NotePropertyValue '' }
    $row.QuestFlag = $flag
    $row.Server = $Server
    $row.$verifiedColumn = 'Verified'
    if ($headers.Contains('Repeatable')) { $row.Repeatable = 'FALSE' }
    $rows += $row
}

$output = [System.Collections.Generic.List[string]]::new()
$output.Add((($headers | ForEach-Object { Escape-Csv $_ }) -join ','))
foreach ($row in $rows) {
    $output.Add((($headers | ForEach-Object { Escape-Csv $row.$_ }) -join ','))
}
[System.IO.File]::WriteAllLines(
    $CatalogPath,
    $output,
    [System.Text.UTF8Encoding]::new($false))

[pscustomobject]@{
    Catalog = $CatalogPath
    Server = $Server
    SourceFlags = $verifiedFlags.Count
    NewFlags = $newFlags.Count
    TotalRows = $rows.Count
    Mode = if ($ReplaceVerification) { 'Replace' } else { 'Merge' }
}

if ($newFlags.Count -gt 0) {
    'Added flags:'
    $newFlags
}
