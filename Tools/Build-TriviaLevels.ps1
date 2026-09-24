$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$rows = @'
Music|Easy|HOW MANY STRINGS DOES A STANDARD GUITAR HAVE|SIX
Sports|Easy|WHICH SPORT USES A BAT AND BASES|BASEBALL
Geography|Easy|WHAT IS THE CAPITAL OF FRANCE|PARIS
Science|Easy|WHICH PLANET IS KNOWN AS THE RED PLANET|MARS
History|Easy|WHAT CITY GAVE THE ROMAN EMPIRE ITS NAME|ROME
Movies|Easy|WHAT IS THE COWBOY CALLED IN TOY STORY|WOODY
General Knowledge|Easy|HOW MANY SIDES DOES A TRIANGLE HAVE|THREE
Music|Easy|WHAT COLOR ARE MOST KEYS ON A PIANO|WHITE
Sports|Easy|HOW MANY RINGS ARE ON THE OLYMPIC FLAG|FIVE
Geography|Easy|WHICH COUNTRY IS HOME TO THE EIFFEL TOWER|FRANCE
Science|Easy|WHAT GAS DO HUMANS NEED TO BREATHE|OXYGEN
History|Easy|IN WHICH COUNTRY WERE THE PYRAMIDS AT GIZA BUILT|EGYPT
Movies|Easy|WHICH DISNEY FILM STARS A LION NAMED SIMBA|THE LION KING
General Knowledge|Easy|HOW MANY DAYS ARE IN A LEAP YEAR|366
Music|Medium|WHICH CLEF IS ALSO CALLED THE G CLEF|TREBLE
Sports|Medium|HOW MANY PLAYERS START ON A SOCCER TEAM|ELEVEN
Geography|Medium|WHAT IS THE CAPITAL OF CANADA|OTTAWA
Science|Medium|WHICH ELEMENT HAS THE SYMBOL AU|GOLD
History|Medium|WHO WAS THE FIRST WOMAN TO WIN A NOBEL PRIZE|MARIE CURIE
Movies|Medium|IN THE MATRIX WHICH PILL DOES NEO TAKE|RED
General Knowledge|Medium|HOW MANY SQUARES ARE ON A CHESS BOARD|SIXTY FOUR
Music|Medium|WHICH COMPOSER WROTE THE FOUR SEASONS|VIVALDI
Sports|Medium|IN TENNIS WHAT WORD MEANS ZERO|LOVE
Geography|Medium|WHICH COUNTRY HAS THE CITY OF KYOTO|JAPAN
Science|Medium|IN HUMAN CELLS WHERE IS MOST DNA KEPT|NUCLEUS
History|Medium|WHICH SHIP SANK ON ITS FIRST VOYAGE IN 1912|TITANIC
Movies|Medium|WHICH 1939 FILM FOLLOWS DOROTHY TO OZ|THE WIZARD OF OZ
General Knowledge|Medium|WHAT IS THE ROMAN NUMERAL FOR FIFTY|L
Music|Medium|HOW MANY LINES ARE ON A MUSIC STAFF|FIVE
Sports|Medium|HOW MANY POINTS IS A TRY IN RUGBY UNION|FIVE
Geography|Medium|WHICH COUNTRY HAS THE CITY OF LIMA|PERU
Science|Medium|WHAT GAS MAKES UP MOST OF OUR AIR|NITROGEN
History|Medium|WHO FOUNDED THE MONGOL EMPIRE|GENGHIS KHAN
Movies|Medium|IN STAR WARS WHO TRAINS LUKE ON DAGOBAH|YODA
General Knowledge|Medium|WHICH METAL IS LIQUID AT 20 C|MERCURY
Music|Hard|WHO COMPOSED THE OPERA THE MAGIC FLUTE|MOZART
Sports|Hard|WHICH COUNTRY WON THE FIRST FIFA WORLD CUP|URUGUAY
Geography|Hard|WHAT IS THE CAPITAL OF BHUTAN|THIMPHU
Science|Hard|WHAT IS THE SI UNIT OF ELECTRIC CHARGE|COULOMB
History|Hard|WHERE DID NAPOLEON LOSE HIS LAST BATTLE|WATERLOO
Movies|Hard|WHO DIRECTED SPIRITED AWAY|HAYAO MIYAZAKI
General Knowledge|Hard|WHAT IS THE NAME OF A NINE SIDED SHAPE|NONAGON
Music|Hard|WHICH JAZZ ICON WAS KNOWN AS BIRD|CHARLIE PARKER
Sports|Medium|IN GOLF WHAT IS TWO UNDER PAR CALLED|EAGLE
Geography|Hard|WHICH DESERT COVERS MUCH OF BOTSWANA|KALAHARI
Science|Hard|WHICH ELEMENT HAS THE ATOMIC NUMBER 74|TUNGSTEN
History|Hard|WHICH EMPIRE HAD ITS CAPITAL AT CUSCO|INCA
Movies|Hard|WHICH FILM WON THE FIRST BEST PICTURE OSCAR|WINGS
General Knowledge|Hard|WHO WROTE THE NOVEL BRAVE NEW WORLD|ALDOUS HUXLEY
General Knowledge|Medium|WHAT IS THE SMALLEST PRIME NUMBER|TWO
'@

function Write-Utf8($path, $text) {
    [IO.File]::WriteAllText((Join-Path $repoRoot $path), $text, [Text.UTF8Encoding]::new($false))
}
function Mask-Question($question, $quota) {
    $words = $question.Split(' ')
    $eligible = @(0..($words.Length - 1) | Where-Object { $words[$_].Length -ge 4 })
    # Spread blanks through the question instead of obscuring its opening phrase.
    $selected = @{}
    for ($i = 0; $i -lt [Math]::Min($quota, $eligible.Count); $i++) {
        $slot = [int][Math]::Floor($i * $eligible.Count / [Math]::Min($quota, $eligible.Count))
        $selected[$eligible[$slot]] = $true
    }
    for ($i = 0; $i -lt $words.Length; $i++) {
        if ($selected.ContainsKey($i)) {
            $index = [int][Math]::Floor($words[$i].Length / 2)
            $words[$i] = $words[$i].Insert($index, '_')
        }
    }
    return $words -join ' '
}
function Assert-Fits($text, $name) {
    $lines = 1; $width = 0
    foreach ($word in $text.Split(' ')) {
        if ($word.Length -gt 8) { throw "$name has a word wider than 8: $word" }
        if ($width -gt 0 -and $width + 1 + $word.Length -gt 8) { $lines++; $width = 0 }
        if ($width -gt 0) { $width++ }
        $width += $word.Length
    }
    if ($lines -gt 8) { throw "$name needs $lines lines" }
}

$categories = @('General Knowledge', 'History', 'Science', 'Sports', 'Music', 'Movies', 'Geography')
$difficulties = @('Easy', 'Medium', 'Hard')
$levels = @(); $references = @(); $review = @('# Trivia pack: 50 levels', '',
    'Each level has one question page and one fully hidden answer page. Difficulty labels are editorial estimates.', '',
    '| Level | Category | Difficulty | Question | Answer |', '|---|---|---|---|---|')
$template = Get-Content (Join-Path $repoRoot 'Assets/GeneratedLevels/Level_01.asset') -Raw
$header = $template.Substring(0, $template.IndexOf('  category:'))
$number = 0
foreach ($row in ($rows.Trim() -split '\r?\n')) {
    $number++
    $category, $difficulty, $question, $answer = $row.Split('|')
    $name = 'Trivia_{0:000}' -f $number
    Assert-Fits $question "$name question"
    Assert-Fits $answer "$name answer"
    $questionRaw = Mask-Question $question (@{Easy=2; Medium=4; Hard=6}[$difficulty])
    $answerRaw = [regex]::Replace($answer, '[A-Z0-9]', '_$0')
    $levels += [ordered]@{name=$name; category=$category; difficulty=$difficulty; question=$question; answer=$answer; sentences=@($questionRaw,$answerRaw)}
    $path = "Assets/GeneratedLevels/$name.asset"
    if (Test-Path (Join-Path $repoRoot "$path.meta")) {
        $guid = [regex]::Match((Get-Content (Join-Path $repoRoot "$path.meta") -Raw), 'guid: (\w+)').Groups[1].Value
    } else {
        $guid = [Guid]::NewGuid().ToString('N')
        Write-Utf8 "$path.meta" "fileFormatVersion: 2`nguid: $guid`nNativeFormatImporter:`n  externalObjects: {}`n  mainObjectFileID: 11400000`n"
    }
    $letters = -join ([regex]::Matches($questionRaw + $answerRaw, '_(.)') | ForEach-Object { $_.Groups[1].Value })
    $hex = -join ([Text.Encoding]::Unicode.GetBytes($letters) | ForEach-Object { $_.ToString('x2') })
    $asset = $header.Replace('m_Name: Level_01', "m_Name: $name")
    $asset += "  category: $($categories.IndexOf($category))`n  difficulty: $($difficulties.IndexOf($difficulty))`n  sentences:`n  - rawSentence: '$questionRaw'`n  - rawSentence: '$answerRaw'`n  letters: $hex`n"
    Write-Utf8 $path $asset
    $references += "  - {fileID: 11400000, guid: $guid, type: 2}"
    $review += "| $number | $category | $difficulty | $question | $answer |"
}
if ($number -ne 50) { throw 'Expected 50 levels' }
Write-Utf8 'Assets/trivia_levels_50.json' (([ordered]@{levels=$levels} | ConvertTo-Json -Depth 8) + "`n")
foreach ($example in @('Assets/example_levels.json', 'Assets/example_levels2.json')) {
    Write-Utf8 $example (([ordered]@{levels=@($levels[0..2])} | ConvertTo-Json -Depth 8) + "`n")
}
$sequence = Get-Content (Join-Path $repoRoot 'Assets/GeneratedLevels/LevelSequenceData.asset') -Raw
Write-Utf8 'Assets/GeneratedLevels/LevelSequenceData.asset' ($sequence.Substring(0,$sequence.IndexOf('  levels:')) + "  levels:`n" + ($references -join "`n") + "`n")
$review += @('', '## Selected fact checks', '',
    '- Level 30: [World Rugby scoring rules](https://passport.world.rugby/laws-of-the-game/laws-by-number/8-scoring).',
    '- Level 36: [Dutch National Opera: Mozart and The Magic Flute](https://www.operaballet.nl/de-nationale-opera/2023-2024/die-zauberflote).',
    '- Level 37: [FIFA: Uruguay won the first World Cup](https://inside.fifa.com/news/world-first-for-uruguay-2053846).',
    '- Level 38: [Thimphu City annual report](https://thimphucity.bt/wp-content/uploads/2025/03/AR-2023-for-WEB.pdf).',
    '- Level 41: [Academy Awards: Hayao Miyazaki and Spirited Away](https://www.oscars.org/collection-highlights/hayao-miyazaki?page=2).',
    '- Level 43: [Smithsonian: Charlie Parker, Bird](https://postalmuseum.si.edu/exhibition/the-black-experience-music-jazz-jazz-band-leaders-and-performers/charlie-parker).',
    '- Level 45: [Botswana Tourism: Kalahari Desert](https://botswanatourism.co.bw/explore/kalahari-desert).',
    '- Level 46: [Royal Society of Chemistry: tungsten, atomic number 74](https://periodic-table.rsc.org/element/74/).',
    '- Level 48: [Academy Awards: Wings, 1929](https://www.oscars.org/oscars/ceremonies/1929).')
Write-Utf8 'TRIVIA_LEVELS.md' (($review -join "`n") + "`n")
Write-Output "Generated $number levels and updated the sequence."
$levels | Group-Object { $_.category } | Select-Object Name,Count
$levels | Group-Object { $_.difficulty } | Select-Object Name,Count
