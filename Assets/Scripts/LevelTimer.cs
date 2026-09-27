using System;
using UnityEngine;
using TMPro;

/// <summary>
/// Per-level countdown timer. Total time budget scales with how many letters
/// the level requires the player to fill (LevelData.TotalGapCount), so longer
/// levels automatically get proportionally more time.
/// </summary>
public class LevelTimer : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    private int displayedSeconds = -1;

    private void RefreshText()
    {
        int seconds = Mathf.CeilToInt(timeRemaining);
        if (timerText == null || seconds == displayedSeconds) return;
        displayedSeconds = seconds;
        timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
    }

    [Header("Time Budget")]
    [Tooltip("Flat seconds granted regardless of level size.")]
    [SerializeField] private float baseSeconds = 20f;

    [Tooltip("Extra seconds granted per missing letter (gap) in the level.")]
    [SerializeField] private float secondsPerGap = 2.5f;

    private float timeRemaining;
    private bool isRunning;

    /// <summary>Seconds left on the clock.</summary>
    public float TimeRemaining => timeRemaining;

    /// <summary>Total seconds the current level was given (fixed once StartLevel runs).</summary>
    public float TotalDuration { get; private set; }

    public bool IsRunning => isRunning;

    /// <summary>Fires every tick while running, with the new remaining-seconds value.</summary>
    public event Action<float> TimeChanged;

    /// <summary>Fires once, the moment the timer reaches zero.</summary>
    public event Action TimeExpired;

    /// <summary>Computes the level's time budget from its gap count and starts the countdown.</summary>
    public void StartLevel(LevelData level)
    {
        int gapCount = level != null ? level.TotalGapCount : 0;

        TotalDuration = baseSeconds + gapCount * secondsPerGap;
        timeRemaining = TotalDuration;
        isRunning = true;

        RefreshText();
        TimeChanged?.Invoke(timeRemaining);
    }

    public void Pause() => isRunning = false;

    public void Resume() => isRunning = true;

    /// <summary>Stops the countdown outright (level finished, quit, etc.) without firing TimeExpired.</summary>
    public void Stop() => isRunning = false;

    private void Update()
    {
        if (!isRunning)
            return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            isRunning = false;
            RefreshText();
            TimeChanged?.Invoke(timeRemaining);
            TimeExpired?.Invoke();
            return;
        }

        RefreshText();
        TimeChanged?.Invoke(timeRemaining);
    }
}
