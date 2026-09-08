using System;
using UnityEngine;

/// <summary>
/// Owns the sequence of levels for the game. Loads the first level on start,
/// and tells the LevelFlowManager to load the next level whenever the current
/// one is completed. LevelFlowManager remains responsible for driving a single
/// level's internal flow (sentences, gaps, letter pool) - this class only
/// decides *which* LevelData to hand it and *when*.
/// </summary>
public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelFlowManager flowManager;
    [SerializeField] private LevelSequenceData levelSequence;

    [Tooltip("If true, wraps back to the first level after the last one is completed.")]
    [SerializeField] private bool loopLevels = false;

    private int currentLevelIndex = -1;

    public event Action<int> LevelLoaded;   // passes the index of the level just loaded
    public event Action AllLevelsCompleted; // fired when the last level finishes and looping is off

    public int CurrentLevelIndex => currentLevelIndex;
    public int LevelCount => levelSequence != null ? levelSequence.LevelCount : 0;

    private void OnEnable()
    {
        flowManager.LevelCompleted += OnLevelCompleted;

        LoadFirstLevel();
    }

    private void OnDisable()
    {
        flowManager.LevelCompleted -= OnLevelCompleted;
    }

    public void LoadFirstLevel()
    {
        LoadLevelAt(0);
    }

    public void LoadLevelAt(int index)
    {
        if (!IsValidIndex(index))
        {
            Debug.LogWarning($"LevelManager: no level at index {index} (level count = {LevelCount})");
            return;
        }

        LevelData level = levelSequence.GetLevel(index);
        if (level == null)
        {
            Debug.LogWarning($"LevelManager: level at index {index} is null in the sequence.");
            return;
        }

        currentLevelIndex = index;
        flowManager.LoadLevel(level);
        LevelLoaded?.Invoke(currentLevelIndex);
    }

    private void OnLevelCompleted()
    {
        int nextIndex = currentLevelIndex + 1;

        if (!IsValidIndex(nextIndex))
        {
            if (loopLevels && LevelCount > 0)
            {
                LoadLevelAt(0);
            }
            else
            {
                AllLevelsCompleted?.Invoke();
            }

            return;
        }

        LoadLevelAt(nextIndex);
    }

    private bool IsValidIndex(int index)
    {
        return levelSequence != null && index >= 0 && index < LevelCount;
    }
}