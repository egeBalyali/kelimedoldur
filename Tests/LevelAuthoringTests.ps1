$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Add-Type -Path (Join-Path $repoRoot 'Assets/Editor/LevelAuthoringSentence.cs')

function Assert-Equal($actual, $expected, $label) {
    if ($actual -cne $expected) { throw "$label`: expected '$expected', got '$actual'" }
}

# Round-trip every example, including repeated blanks, Turkish letters and spaces.
foreach ($file in @('example_levels.json', 'example_levels2.json', 'trivia_levels_50.json')) {
    $data = Get-Content (Join-Path $repoRoot "Assets/$file") -Raw | ConvertFrom-Json
    foreach ($level in $data.levels) {
        foreach ($raw in $level.sentences) {
            $sentence = [LevelAuthoringSentence]::FromRaw($raw)
            Assert-Equal $sentence.ToRaw() $raw "Round-trip $($level.name)"
        }
    }
}

$sentence = [LevelAuthoringSentence]::FromRaw('C_aT _d_o_g')
Assert-Equal $sentence.text 'CaT dog' 'Plain text'
Assert-Equal $sentence.GapCount 4 'Repeated gap count'
$sentence.SetText('BIG CaT dog')
Assert-Equal $sentence.ToRaw() 'BIG C_aT _d_o_g' 'Insert prefix preserves later gaps'
$sentence.SetText('BIG CaT happy dog')
Assert-Equal $sentence.ToRaw() 'BIG C_aT happy _d_o_g' 'Insert middle preserves gaps'
$sentence.SetText('CaT dog')
Assert-Equal $sentence.ToRaw() 'CaT _d_o_g' 'Replaced text is visible; unchanged suffix survives'
$sentence.SetText('')
Assert-Equal $sentence.GapCount 0 'Clear text removes gaps'

$sentence = [LevelAuthoringSentence]::FromRaw('  AB  CD EF  ')
$lines = $sentence.Wrap(6)
Assert-Equal $lines.Count 2 'Word wrapping'
Assert-Equal (-join ($lines[0] | ForEach-Object { $sentence.text[$_] })) 'AB  CD' 'Interior spaces retained'
Assert-Equal (-join ($lines[1] | ForEach-Object { $sentence.text[$_] })) 'EF' 'Edge spaces omitted'
$sentence = [LevelAuthoringSentence]::FromRaw('_b_a_s_k_e_t_b_a_l_l')
Assert-Equal $sentence.Wrap(8).Count 1 'Long words are not split, matching runtime'
Assert-Equal $sentence.GapCount 10 'Each repeated letter needs a tile'
$rejected = $false
try { [LevelAuthoringSentence]::FromRaw('BAD_') | Out-Null } catch { $rejected = $true }
Assert-Equal $rejected $true 'Malformed inline answer rejected'
Write-Output 'PASS: JSON round-trips, gap editing, repeated letters, wrapping, and malformed input.'
