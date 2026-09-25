$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Add-Type -Path (Join-Path $root 'Assets/Scripts/Sentence/SentenceBoardLayout.cs')

function Assert-Rows([string]$sentence, [int]$columns, [string]$expected) {
    $words = $null
    $rows = [SentenceBoardLayout]::BuildRows($sentence.ToCharArray(), $columns, [ref]$words)
    $rendered = @($rows | ForEach-Object {
        -join @($_ | ForEach-Object { if ($_ -lt 0) { ' ' } else { $sentence[$_] } })
    }) -join '|'
    if ($rendered -cne $expected) { throw "Expected '$expected', got '$rendered'" }
    $indices = @($rows | ForEach-Object { $_ } | Where-Object { $_ -ge 0 })
    $expectedIndices = @(for ($i = 0; $i -lt $sentence.Length; $i++) { if ($sentence[$i] -ne ' ') { $i } })
    if (($indices -join ',') -ne ($expectedIndices -join ',')) { throw 'Lost or duplicated characters/gaps' }
    foreach ($row in $rows) {
        if ($row.Count -gt $columns) { throw 'Row exceeds board width' }
        if ($row.Count -gt 0 -and ($row[0] -eq -1 -or $row[$row.Count - 1] -eq -1)) { throw 'Edge spacer' }
    }
}

Assert-Rows 'THE JOURNEY AROUND THE WORLD' 10 'THE|JOURNEY|AROUND THE|WORLD'
Assert-Rows 'ABCDE FG' 5 'ABCDE|FG'
Assert-Rows 'AB CD' 5 'AB CD'
Assert-Rows '  AB  CD   ' 6 'AB  CD'
Assert-Rows '  AB   CD   ' 5 'AB|CD'
Assert-Rows 'ABCDEFGHIJK Z' 5 'ABCDE|FGHIJ|K Z'
Assert-Rows '_a _9 Z' 4 '_a|_9 Z'
Assert-Rows '   ' 10 ''
Assert-Rows '' 10 ''

$words = $null
$null = [SentenceBoardLayout]::BuildRows('ABCDEFG HI'.ToCharArray(), 4, [ref]$words)
if ($words[0] -ne $words[6] -or $words[6] -eq $words[8]) { throw 'Split word lost its completion group' }

$scene = [IO.File]::ReadAllText((Join-Path $root 'Assets/Scenes/WheelOfFortune.unity'))
$ids = @{}
foreach ($match in [regex]::Matches($scene, '(?m)^--- !u!\d+ &(\d+)')) {
    $id = $match.Groups[1].Value
    if ($ids.ContainsKey($id)) { throw "Duplicate scene object: $id" }
    $ids[$id] = $true
}
foreach ($match in [regex]::Matches($scene, '\{fileID: (\d+)\}')) {
    $id = $match.Groups[1].Value
    if ($id -ne '0' -and !$ids.ContainsKey($id)) { throw "Missing scene object: $id" }
}
if (!$scene.Contains('tileBoard: {fileID: 248259105}')) { throw 'Board is not connected' }
'Passed: board wrapping, green spaces, boundary cases, gap indices, word groups, and scene references.'
