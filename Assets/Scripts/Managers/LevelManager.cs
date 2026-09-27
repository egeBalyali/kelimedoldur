using System;
using UnityEngine;
using TMPro;

/// <summary>Plays the saved position in one ordered sequence. Categories describe levels.</summary>
[DefaultExecutionOrder(100)]
public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelFlowManager flowManager;
    [SerializeField] private LevelSequenceData levelSequence;
    [SerializeField] private bool loopLevels;
    [SerializeField] private bool waitForWinScreen;
    [SerializeField] private TMP_Text lifeCountText;
    [SerializeField] private TMP_Text timeUntilNextLifeText;
    public const int MaxLives = 9;
    private const long LifeIntervalSeconds = 600;
    private const string LivesKey = "PlayerLives";
    private const string NextLifeKey = "NextLifeUtc";
    private int lives;
    private long nextLifeUtc;
    private bool livesLoaded;
    private bool failed;
    public event Action LivesChanged;
    public int Lives { get { RefreshLives(); return lives; } }
    public bool CanPlay => Lives > 0;

    public void RefreshLives() => RefreshLivesAt(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

    private void RefreshLivesAt(long now)
    {
        bool changed = false;
        if (!livesLoaded)
        {
            lives = Math.Max(0, Math.Min(MaxLives, PlayerPrefs.GetInt(LivesKey, MaxLives)));
            long.TryParse(PlayerPrefs.GetString(NextLifeKey, "0"), out nextLifeUtc);
            livesLoaded = true;
            changed = true;
        }
        if (lives < MaxLives)
        {
            if (nextLifeUtc <= 0 || nextLifeUtc > now + LifeIntervalSeconds)
            {
                nextLifeUtc = now + LifeIntervalSeconds;
                changed = true;
            }
            if (now >= nextLifeUtc)
            {
                long gained = Math.Min(MaxLives - lives, 1 + (now - nextLifeUtc) / LifeIntervalSeconds);
                lives += (int)gained;
                nextLifeUtc += gained * LifeIntervalSeconds;
                changed = true;
            }
        }
        if (lives == MaxLives && nextLifeUtc != 0) { nextLifeUtc = 0; changed = true; }
        if (changed) SaveLives();
        if (lifeCountText != null) lifeCountText.text = lives.ToString();
        if (timeUntilNextLifeText != null)
            timeUntilNextLifeText.text = lives == MaxLives ? "Full" : $"{(nextLifeUtc - now) / 60:00}:{(nextLifeUtc - now) % 60:00}";
        if (changed) LivesChanged?.Invoke();
    }

    private void SaveLives()
    {
        PlayerPrefs.SetInt(LivesKey, lives);
        PlayerPrefs.SetString(NextLifeKey, nextLifeUtc.ToString());
        PlayerPrefs.Save();
    }

    private void Update() => RefreshLives();

    private void OnLevelFailed()
    {
        if (failed || completed || currentLevelIndex < 0) return;
        failed = true;
        RefreshLives();
        if (lives <= 0) return;
        if (lives == MaxLives) nextLifeUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + LifeIntervalSeconds;
        lives--;
        SaveLives();
        RefreshLives();
        LivesChanged?.Invoke();
    }

    public bool RetryFailedLevel()
    {
        if (!CanPlay || !failed || completed || currentLevelIndex < 0 || flowManager == null) return false;
        failed = false;
        flowManager.RetryFailedLevel();
        return true;
    }

    public void AbandonCurrentLevel()
    {
        if (failed || completed || currentLevelIndex < 0) return;
        // Charge once without opening the lose screen while navigating home.
        flowManager?.StopLevel();
        OnLevelFailed();
    }
    private int currentLevelIndex = -1;
    private bool started;
    private bool completed;
    private bool savingProgress;

    public event Action<int> LevelLoaded;
    public event Action AllLevelsCompleted;
    public event Action LevelWon;
    public int CurrentLevelIndex => currentLevelIndex;
    public int LevelCount => levelSequence != null ? levelSequence.LevelCount : 0;

    private void OnEnable()
    {
        RefreshLives();
        if (flowManager != null)
        {
            flowManager.AnswerIncorrect += OnLevelFailed;
            flowManager.TimeExpired += OnLevelFailed;
        }
        if (flowManager != null) flowManager.LevelCompleted += OnLevelCompleted;
        PlayerLevelProgress.Changed += OnProgressChanged;
        if (started) ResumeSavedLevel();
    }

    private void Start() { started = true; ResumeSavedLevel(); }

    private void OnDisable()
    {
        if (flowManager != null)
        {
            flowManager.AnswerIncorrect -= OnLevelFailed;
            flowManager.TimeExpired -= OnLevelFailed;
        }
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
        if (!CanPlay) { flowManager.LoadLevel(null); return; }
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
        failed = false;
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
        if (completed || failed || currentLevelIndex < 0) return;
        completed = true;
        int next = currentLevelIndex + 1;
        SaveLevelNumber(loopLevels && next >= LevelCount ? 1 : next + 1);
        LevelWon?.Invoke();
        if (!waitForWinScreen) ResumeSavedLevel();
    }
}
