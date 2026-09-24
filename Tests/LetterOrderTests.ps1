$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Add-Type -Path (Join-Path $repoRoot 'Assets/Scripts/Data/LevelLetterOrder.cs')
function Check($value, $message) { if (-not $value) { throw $message } }
Check ([LevelLetterOrder]::IsValid('BANANA','AAABNN')) 'Duplicates must be preserved'
Check (-not [LevelLetterOrder]::IsValid('BANAN','AAABNN')) 'Reject missing duplicate'
Check (-not [LevelLetterOrder]::IsValid('BANANN','AAABNN')) 'Reject wrong multiplicities'
Check (-not [LevelLetterOrder]::IsValid('banana','BANANA')) 'Preserve letter case'
Check ([LevelLetterOrder]::Reconcile('BANANA','ANNC') -ceq 'ANNC') 'Remove surplus tiles, retain order, append new tile'
Check ([LevelLetterOrder]::Reconcile($null,'AAB') -ceq 'AAB') 'New levels derive pool'
Check ([LevelLetterOrder]::Move('ABCD',0,3) -ceq 'BCDA') 'Move first to last'
Check ([LevelLetterOrder]::Move('ABCD',3,0) -ceq 'DABC') 'Move last to first'
Check ([LevelLetterOrder]::Move('ABCD',1,2) -ceq 'ACBD') 'Move right'
Check ([LevelLetterOrder]::Move('ABCD',-1,2) -ceq 'ABCD') 'Ignore invalid selection'
$random = [Random]::new(42)
$different = $false
for ($i=0; $i -lt 100; $i++) {
    $shuffled = [LevelLetterOrder]::Shuffle('BANANA', $random)
    Check ([LevelLetterOrder]::IsValid($shuffled,'BANANA')) 'Shuffle preserves all tiles'
    if ($shuffled -cne 'BANANA') { $different = $true }
}
Check $different 'Shuffle changes order'
Check ([LevelLetterOrder]::Shuffle('', $random) -ceq '') 'Empty pool'
Check ([LevelLetterOrder]::Shuffle('A', $random) -ceq 'A') 'Single tile pool'
'PASS: shuffle, manual moves, duplicate counts, input validation, and reconciliation after blank edits.'
