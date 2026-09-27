// Isolated logic harness: substitutes storage and scene objects, runs the real progress/manager code.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine
{
    public class MonoBehaviour { }
    public class SerializeField : Attribute { }
    public class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int order) { } }
    public static class Debug { public static void LogWarning(string text) { } }
    public static class PlayerPrefs
    {
        private static readonly Dictionary<string, int> Values = new Dictionary<string, int>();
        private static readonly Dictionary<string, string> Strings = new Dictionary<string, string>();
        public static string GetString(string key, string fallback) => Strings.TryGetValue(key, out string value) ? value : fallback;
        public static void SetString(string key, string value) => Strings[key] = value;
        public static int GetInt(string key, int fallback) => Values.TryGetValue(key, out int value) ? value : fallback;
        public static void SetInt(string key, int value) => Values[key] = value;
        public static void Save() { }
    }
}

namespace TMPro { public class TMP_Text { public string text; } }

public class LevelData { public string category; }
public class LevelSequenceData
{
    public LevelData[] levels;
    public int LevelCount => levels.Length;
    public LevelData GetLevel(int index) => levels[index];
}
public class LevelFlowManager
{
    public event Action LevelCompleted;
    public event Action AnswerIncorrect;
    public event Action TimeExpired;
    public void Fail() => AnswerIncorrect?.Invoke();
    public void Expire() => TimeExpired?.Invoke();
    public void RetryFailedLevel() { resetCount++; }
    public bool stopped;
    public void StopLevel() { stopped = true; }
    public LevelData current;
    public int loadCount;
    public int resetCount;
    public void ResetUnfinishedWords() { resetCount++; }
    public void LoadLevel(LevelData value) { current = value; loadCount++; }
    public void Complete() => LevelCompleted?.Invoke();
}

public static class PlayerProgressTests
{
    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
    }
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Call(object target, string method) =>
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

    public static string Run()
    {
        Check(PlayerLevelProgress.LevelNumber == 1, "Default level");
        PlayerLevelProgress.SetLevelNumber(-5);
        Check(PlayerLevelProgress.LevelNumber == 1, "Clamp negative input");
        UnityEngine.PlayerPrefs.SetInt("UnrelatedSetting", 42);
        var sequence = new LevelSequenceData { levels = new[]
        {
            new LevelData { category = "Music" }, new LevelData { category = "Sports" },
            new LevelData { category = "History" }
        } };
        var flow = new LevelFlowManager();
        var manager = new LevelManager();
        Set(manager, "flowManager", flow);
        Set(manager, "levelSequence", sequence);
        Call(manager, "OnEnable"); Call(manager, "Start");
        Check(flow.current == sequence.levels[0], "Load first level");
        flow.Complete();
        Check(PlayerLevelProgress.LevelNumber == 2 && flow.current == sequence.levels[1], "Advance across categories");
        int previousLoads = flow.loadCount;
        manager.RestartCurrentLevel();
        Check(flow.loadCount == previousLoads + 1 && flow.current == sequence.levels[1] &&
            PlayerLevelProgress.LevelNumber == 2, "Retry reloads the same level without changing saved progress");
        manager.ResetUnfinishedWords();
        Check(flow.resetCount == 1 && flow.loadCount == previousLoads + 1 && PlayerLevelProgress.LevelNumber == 2,
            "Soft reset delegates to flow without reloading the level or changing progress");
        Call(manager, "OnDisable"); Call(manager, "OnEnable");
        Check(manager.CurrentLevelIndex == 1, "Resume saved position");
        PlayerLevelProgress.SetLevelNumber(3);
        Check(flow.current == sequence.levels[2], "Test tool reloads active level");
        flow.Complete();
        Check(PlayerLevelProgress.LevelNumber == 4 && flow.current == null, "Final completion sentinel");
        flow.Complete();
        Check(PlayerLevelProgress.LevelNumber == 4, "No duplicate final completion");
        Call(manager, "OnDisable"); Call(manager, "OnEnable");
        Check(flow.current == null, "Finished sequence stays finished on restart");
        PlayerLevelProgress.ResetProgress();
        Check(flow.current == sequence.levels[0], "Reset returns to first level");
        Check(UnityEngine.PlayerPrefs.GetInt("UnrelatedSetting", 0) == 42, "Reset preserves unrelated prefs");
        Set(manager, "loopLevels", true);
        PlayerLevelProgress.SetLevelNumber(3); flow.Complete();
        Check(PlayerLevelProgress.LevelNumber == 1 && flow.current == sequence.levels[0], "Loop wraps to first level");
        Call(manager, "OnDisable");
        Check(manager.Lives == 9, "Wins do not cost lives");
        Call(manager, "OnEnable");
        flow.Fail(); flow.Expire();
        Check(manager.Lives == 8, "One life per failed attempt even with duplicate failure events");
        Check(manager.RetryFailedLevel(), "Retry available with remaining lives");
        flow.Expire();
        Check(manager.Lives == 7, "Timeout costs one life");
        for (int i = 0; i < 7; i++) { Check(manager.RetryFailedLevel(), "Retry before next failure"); flow.Fail(); }
        Check(manager.Lives == 0 && !manager.CanPlay && !manager.RetryFailedLevel(), "Zero lives blocks play and retry");
        manager.LoadFirstLevel(); Check(flow.current == null, "Zero lives prevents loading a level");
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var tick = typeof(LevelManager).GetMethod("RefreshLivesAt", BindingFlags.NonPublic | BindingFlags.Instance);
        var count = new TMPro.TMP_Text(); var countdown = new TMPro.TMP_Text();
        Set(manager, "lifeCountText", count); Set(manager, "timeUntilNextLifeText", countdown);
        Set(manager, "nextLifeUtc", now + 600);
        tick.Invoke(manager, new object[] { now });
        Check(count.text == "0" && countdown.text == "10:00", "Initial refill countdown");
        tick.Invoke(manager, new object[] { now + 1 });
        Check(countdown.text == "09:59", "Countdown formats minutes and seconds with leading zeroes");
        tick.Invoke(manager, new object[] { now + 599 });
        Check(count.text == "0" && countdown.text == "00:01", "No early refill and seconds count down");
        tick.Invoke(manager, new object[] { now + 600 });
        Check(count.text == "1" && countdown.text == "10:00", "One life at ten minutes");
        tick.Invoke(manager, new object[] { now + 1800 });
        Check(count.text == "3", "Multiple elapsed intervals regenerate multiple lives");
        tick.Invoke(manager, new object[] { now + 10000 });
        Check(count.text == "9" && countdown.text == "Full", "Regeneration capped at nine");
        Check(UnityEngine.PlayerPrefs.GetString("NextLifeUtc", "missing") == "0", "No banked refill while full");
        Call(manager, "OnDisable");
        UnityEngine.PlayerPrefs.SetInt("PlayerLives", 4);
        UnityEngine.PlayerPrefs.SetString("NextLifeUtc", (now - 650).ToString());
        var restored = new LevelManager();
        Check(restored.Lives == 6, "Reload catches up offline regeneration");
        UnityEngine.PlayerPrefs.SetInt("PlayerLives", 9);
        UnityEngine.PlayerPrefs.SetString("NextLifeUtc", "0");
        var exitFlow = new LevelFlowManager();
        var exitManager = new LevelManager();
        Set(exitManager, "flowManager", exitFlow);
        Set(exitManager, "levelSequence", sequence);
        Set(exitManager, "waitForWinScreen", true);
        Call(exitManager, "OnEnable");
        exitManager.LoadFirstLevel();
        int savedLevel = PlayerLevelProgress.LevelNumber;
        exitManager.AbandonCurrentLevel();
        Check(exitManager.Lives == 8 && exitFlow.stopped, "Home costs one life and stops gameplay");
        Check(PlayerLevelProgress.LevelNumber == savedLevel, "Home preserves level progress");
        exitManager.AbandonCurrentLevel(); exitFlow.Expire();
        Check(exitManager.Lives == 8, "Repeated Home or late timeout cannot charge twice");
        exitManager.LoadFirstLevel(); exitFlow.Fail(); exitManager.AbandonCurrentLevel();
        Check(exitManager.Lives == 7, "Home after failure costs no extra life");
        exitManager.LoadFirstLevel(); exitFlow.Complete(); exitManager.AbandonCurrentLevel();
        Check(exitManager.Lives == 7, "Home after winning costs no life");
        Call(exitManager, "OnDisable");
        return "PASS: defaults, clamping, linear progression, resume, live changes, final completion, reset and looping.";
    }
}
