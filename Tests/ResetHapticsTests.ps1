$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Add-Type -CompilerOptions '/nowarn:0649,0067', '/define:UNITY_EDITOR' -Path @(
    (Join-Path $repoRoot 'Tests/ResetHapticsTests.cs'),
    (Join-Path $repoRoot 'Assets/Scripts/Data/LevelData.cs'),
    (Join-Path $repoRoot 'Assets/Scripts/Managers/LevelFlowManager.cs'),
    (Join-Path $repoRoot 'Assets/Scripts/Managers/HapticFeedbackManager.cs')
)
[ResetHapticsTests]::Run()
