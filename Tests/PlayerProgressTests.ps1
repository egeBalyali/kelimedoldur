$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Add-Type -CompilerOptions '/nowarn:0649' -Path @(
    (Join-Path $repoRoot 'Tests/PlayerProgressTests.cs'),
    (Join-Path $repoRoot 'Assets/Scripts/Data/PlayerLevelProgress.cs'),
    (Join-Path $repoRoot 'Assets/Scripts/Managers/LevelManager.cs')
)
[PlayerProgressTests]::Run()
