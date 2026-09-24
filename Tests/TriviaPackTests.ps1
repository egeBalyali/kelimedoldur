$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Add-Type -Path (Join-Path $repoRoot 'Assets/Editor/LevelAuthoringSentence.cs')
$levels = (Get-Content (Join-Path $repoRoot 'Assets/trivia_levels_50.json') -Raw | ConvertFrom-Json).levels
if ($levels.Count -ne 50) { throw 'Expected exactly 50 trivia levels' }
if (@($levels.question | Sort-Object -Unique).Count -ne 50) { throw 'Repeated question' }
$categoryNames = @('General Knowledge','History','Science','Sports','Music','Movies','Geography')
$difficultyNames = @('Easy','Medium','Hard')
$sequence = Get-Content (Join-Path $repoRoot 'Assets/GeneratedLevels/LevelSequenceData.asset') -Raw
$refs = [regex]::Matches($sequence, 'fileID: 11400000, guid: (\w+)')
if ($refs.Count -ne 50) { throw 'Sequence does not contain exactly 50 levels' }
for ($i = 0; $i -lt $levels.Count; $i++) {
    $level = $levels[$i]
    if ($level.sentences.Count -ne 2) { throw "$($level.name): not two pages" }
    $gapLetters = ''
    for ($page = 0; $page -lt 2; $page++) {
        $sentence = [LevelAuthoringSentence]::FromRaw($level.sentences[$page])
        $expected = if ($page -eq 0) { $level.question } else { $level.answer }
        if ($sentence.text -cne $expected) { throw "$($level.name): wrong page text" }
        if ($sentence.Wrap(8).Count -gt 8 -or @($sentence.text.Split(' ') | Where-Object { $_.Length -gt 8 }).Count) {
            throw "$($level.name): page does not fit scene layout"
        }
        if ($sentence.GapCount -eq 0) { throw "$($level.name): no blanks on page $page" }
        if ($page -eq 1 -and $sentence.GapCount -ne $level.answer.Replace(' ','').Length) { throw 'Answer is not fully hidden' }
        $gapLetters += -join ([regex]::Matches($level.sentences[$page], '_(.)') | ForEach-Object { $_.Groups[1].Value })
    }
    $path = Join-Path $repoRoot "Assets/GeneratedLevels/$($level.name).asset"
    $asset = Get-Content $path -Raw
    $sentences = @([regex]::Matches($asset, "(?m)^  - rawSentence: '(.*)'\r?$") | ForEach-Object { $_.Groups[1].Value })
    if ($sentences.Count -ne 2 -or ($sentences -join "`n") -cne ($level.sentences -join "`n")) { throw 'JSON/asset mismatch' }
    $hex = [regex]::Match($asset, 'letters: ([0-9a-f]*)').Groups[1].Value
    $bytes = [byte[]]@(for ($b=0; $b -lt $hex.Length; $b+=2) { [Convert]::ToByte($hex.Substring($b,2),16) })
    $actualLetters = [Text.Encoding]::Unicode.GetString($bytes)
    if ((-join ($actualLetters.ToCharArray() | Sort-Object)) -cne (-join ($gapLetters.ToCharArray() | Sort-Object))) {
        throw "$($level.name): incorrect letter pool"
    }
    if ($null -ne $level.letterOrder -and $actualLetters -cne $level.letterOrder) { throw 'Authored JSON order mismatch' }
    $category = $categoryNames.IndexOf($level.category)
    $difficulty = $difficultyNames.IndexOf($level.difficulty)
    if ($category -lt 0 -or $difficulty -lt 0 -or -not $asset.Contains("category: $category") -or -not $asset.Contains("difficulty: $difficulty")) { throw 'Metadata mismatch' }
    $guid = [regex]::Match((Get-Content "$path.meta" -Raw),'guid: (\w+)').Groups[1].Value
    if ($refs[$i].Groups[1].Value -ne $guid) { throw 'Sequence order mismatch' }
    foreach ($library in @('LetterSpriteLibrary 1.asset','HappyBottomLetterSpriteLibrary 1.asset')) {
        $libraryText = Get-Content (Join-Path $repoRoot "Assets/Data/$library") -Raw
        $supported = @([regex]::Matches($libraryText, 'letter: (\d+)') | ForEach-Object { [char][int]$_.Groups[1].Value })
        foreach ($letter in $gapLetters.ToCharArray()) {
            if ($supported -cnotcontains $letter -and $supported -cnotcontains [char]::ToLowerInvariant($letter)) { throw "Unsupported pool tile: $letter" }
        }
    }
}
if (@($levels.category | Sort-Object -Unique).Count -ne 7) { throw 'Missing categories' }
if (@($levels.difficulty | Sort-Object -Unique).Count -ne 3) { throw 'Missing difficulties' }
'PASS: 50 unique questions, 100 fitting pages, 7 categories, 3 difficulties, complete letter pools, artwork coverage, and exact sequence order.'
