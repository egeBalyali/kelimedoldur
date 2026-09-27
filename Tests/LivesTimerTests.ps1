$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$stubs = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'ResetHapticsTests.cs')).Split('public static class ResetHapticsTests')[0]
$stubs = $stubs.Replace('public void SetActive(bool value) { }', 'public bool activeSelf; public void SetActive(bool value) { activeSelf = value; }')
$stubs = $stubs.Replace('public static class Mathf {', 'public static class Time { public static float deltaTime; } public static class Mathf { public static int CeilToInt(float v) => (int)Math.Ceiling(v); public static int RoundToInt(float v) => (int)Math.Round(v); public static float Max(float a, float b) => Math.Max(a,b);')
$stubs = $stubs.Replace('Log(string value', 'Log(object value').Replace('messages.Add(value)', 'messages.Add(value.ToString())')
$checks = @"
namespace TMPro { public class TMP_Text { public string text; } }
public static class LivesTimerChecks
{
    static void Check(bool value, string label) { if (!value) throw new System.Exception(label); }
    static void Set(object o, string n, object v) => o.GetType().GetField(n, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(o,v);
    static void Call(object o, string n) => o.GetType().GetMethod(n, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(o,null);
    public static string Run()
    {
        var flow = new LevelFlowManager(); var timer = new LevelTimer(); var text = new TMPro.TMP_Text();
        var hearts = new [] { new UnityEngine.GameObject(), new UnityEngine.GameObject(), new UnityEngine.GameObject() };
        Set(flow,"sentenceView",new SentenceView()); Set(flow,"letterPool",new LetterPoolController());
        Set(flow,"levelTimer",timer); Set(flow,"hearts",hearts); Set(timer,"timerText",text);
        var level = new LevelData(); level.AddSentence("C_a_t");
        int fails = 0; flow.AnswerIncorrect += () => fails++;
        flow.LoadLevel(level);
        Check(text.text == "00:25" && timer.IsRunning, "Initial timer display");
        flow.SubmitAnswer(); Check(hearts[0].activeSelf && hearts[1].activeSelf && !hearts[2].activeSelf && fails == 0, "First wrong answer");
        flow.SubmitAnswer(); Check(hearts[0].activeSelf && !hearts[1].activeSelf && fails == 0, "Second wrong answer");
        flow.SubmitAnswer(); Check(!hearts[0].activeSelf && fails == 1 && !timer.IsRunning, "Third wrong answer fails");
        flow.SubmitAnswer(); Check(fails == 1, "No repeated failure after game over");
        flow.RetryFailedLevel(); Check(hearts[0].activeSelf && hearts[1].activeSelf && hearts[2].activeSelf && timer.IsRunning, "Retry restores lives and timer");
        UnityEngine.Time.deltaTime = 0.1f; Call(timer,"Update"); Check(text.text == "00:25", "Round countdown upward");
        UnityEngine.Time.deltaTime = 1f; Call(timer,"Update"); Check(text.text == "00:24", "Countdown updates");
        int expired = 0; timer.TimeExpired += () => { expired++; Call(flow,"HandleTimeExpired"); };
        UnityEngine.Time.deltaTime = 100f; Call(timer,"Update"); Call(timer,"Update");
        Check(text.text == "00:00" && expired == 1 && !timer.IsRunning, "Expiry displays zero once");
        flow.SubmitAnswer(); Check(fails == 1, "Expiry blocks submissions");
        flow.LoadLevel(level); Check(timer.IsRunning && hearts[2].activeSelf, "New level resets");
        flow.LoadLevel(null); Check(!timer.IsRunning, "No countdown without a level");
        var score = new LevelScoreManager();
        int awards = 0, awardPoints = 0, clears = 0;
        score.ScoreAwarded += points => { awards++; awardPoints += points; };
        score.FeedbackCleared += () => clears++;
        score.StartLevel(level);
        Check(awards == 0 && clears == 1, "Level start clears feedback without celebrating starting score");
        score.AddFirstSentenceWordBonus(2);
        Check(awards == 1 && awardPoints == 50, "Word bonus emits the earned points");
        score.NotifyWrongSubmit(); Call(score,"Update"); score.AddFirstSentenceWordBonus(0);
        Check(awards == 1, "Penalties, decay, and empty bonuses do not celebrate");
        score.StopLevel(); score.AddFirstSentenceWordBonus(2);
        Check(awards == 1 && clears == 2, "Level end clears feedback and suppresses further awards");
        return "PASS: countdown text, three lives, failure lock, retry, expiry and level reset.";
    }
}
"@
$source = $stubs + $checks
foreach ($file in @('Assets/Scripts/Data/LevelData.cs','Assets/Scripts/LevelTimer.cs','Assets/Scripts/Managers/LevelScoreManager.cs','Assets/Scripts/Managers/LevelFlowManager.cs')) {
    $body = [IO.File]::ReadAllText((Join-Path $root $file)) -replace '(?m)^using [^;]+;\r?\n', ''
    $source += "`n" + $body
}
$source = "using TMPro;`nusing UnityEngine;`nusing UnityEngine.UI;`n" + $source
Add-Type -TypeDefinition $source -CompilerOptions '/nowarn:0649,0067,0414'
[LivesTimerChecks]::Run()
