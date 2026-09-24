using System;
using UnityEngine;

/// <summary>Plays the saved position in one ordered sequence. Categories describe levels.</summary>
[DefaultExecutionOrder(100)]
public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelFlowManager flowManager;
    [SerializeField] private LevelSequenceData levelSequence;
    [SerializeField] private bool loopLevels;
    private int currentLevelIndex = -1;
    private bool started;
    private bool completed;
    private bool savingProgress;

    public event Action<int> LevelLoaded;
    public event Action AllLevelsCompleted;
    public int CurrentLevelIndex => currentLevelIndex;
    public int LevelCount => levelSequence != null ? levelSequence.LevelCount : 0;

    private void OnEnable()
    {
        if (flowManager != null) flowManager.LevelCompleted += OnLevelCompleted;
        PlayerLevelProgress.Changed += OnProgressChanged;
        if (started) ResumeSavedLevel();
    }

    private void Start() { started = true; ResumeSavedLevel(); }

    private void OnDisable()
    {
        if (flowManager != null) flowManager.LevelCompleted -= OnLevelCompleted;
        PlayerLevelProgress.Changed -= OnProgressChanged;
    }

    private void OnProgressChanged()
    {
        if (started && !savingProgress) ResumeSavedLevel();
    }

    public void ResumeSavedLevel()
    {
        int index = PlayerLevelProgress.LevelNumber - 1;
        if (loopLevels && LevelCount > 0) index %= LevelCount;
        if (index >= LevelCount)
        {
            completed = true;
            currentLevelIndex = -1;
            if (flowManager != null) flowManager.LoadLevel(null);
            AllLevelsCompleted?.Invoke();
            return;
        }
        LoadLevelAt(index);
    }

    public void LoadFirstLevel() => LoadLevelAt(0);

    public void RestartCurrentLevel()
    {
        if (!completed && currentLevelIndex >= 0) LoadLevelAt(currentLevelIndex);
    }

    public void ResetUnfinishedWords()
    {
        if (!completed && currentLevelIndex >= 0 && flowManager != null) flowManager.ResetUnfinishedWords();
    }

    public void LoadLevelAt(int index)
    {
        if (index < 0 || index >= LevelCount || flowManager == null) return;
        LevelData level = levelSequence.GetLevel(index);
        if (level == null)
        {
            completed = true;
            currentLevelIndex = -1;
            flowManager.LoadLevel(null);
            Debug.LogWarning($"LevelManager: missing level at sequence position {index + 1}.");
            return;
        }
        currentLevelIndex = index;
        completed = false;
        SaveLevelNumber(index + 1);
        flowManager.LoadLevel(level);
        LevelLoaded?.Invoke(index);
    }

    private void SaveLevelNumber(int number)
    {
        if (PlayerLevelProgress.LevelNumber == number) return;
        savingProgress = true;
        try { PlayerLevelProgress.SetLevelNumber(number); }
        finally { savingProgress = false; }
    }

    private void OnLevelCompleted()
    {
        if (completed || currentLevelIndex < 0) return;
        completed = true;
        int next = currentLevelIndex + 1;
        SaveLevelNumber(loopLevels && next >= LevelCount ? 1 : next + 1);
        ResumeSavedLevel();
    }
}
