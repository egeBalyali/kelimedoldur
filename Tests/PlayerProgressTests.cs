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
        public static int GetInt(string key, int fallback) => Values.TryGetValue(key, out int value) ? value : fallback;
        public static void SetInt(string key, int value) => Values[key] = value;
        public static void Save() { }
    }
}

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
        return "PASS: defaults, clamping, linear progression, resume, live changes, final completion, reset and looping.";
    }
}
