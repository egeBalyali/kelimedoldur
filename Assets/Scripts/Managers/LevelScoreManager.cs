using System;
using UnityEngine;

/// <summary>
/// Tracks the player's score for the current level. The starting score is a
/// level-specific maximum (scaled by gap count and difficulty), then drains
/// automatically over time and on penalties, floored at zero.
/// </summary>
public class LevelScoreManager : MonoBehaviour
{
    [Header("Starting Score Scaling")]
    [Tooltip("Flat points granted regardless of level size.")]
    [SerializeField] private int baseScore = 500;

    [Tooltip("Extra starting points per missing letter (gap) in the level.")]
    [SerializeField] private int pointsPerGap = 40;

    [Tooltip("Multiplier applied to the whole starting score, indexed by LevelDifficulty " +
             "(0 = Easy, 1 = Medium, 2 = Hard). Harder levels are worth more.")]
    [SerializeField] private float[] difficultyMultipliers = { 1f, 1.4f, 1.8f };

    [Header("Decay & Penalties")]
    [Tooltip("Points lost per second while the level is active.")]
    [SerializeField] private float decayPerSecond = 8f;

    [Tooltip("Points added per gap when a whole word in the FIRST sentence becomes fully correct.")]
    [SerializeField] private float pointsPerCorrectFirstSentenceGap = 25f;

    [Tooltip("Points lost on each incorrect submission.")]
    [SerializeField] private float wrongSubmitPenalty = 40f;

    private float currentScore;
    private float maxScore;
    private bool isActive;

    public int CurrentScore => Mathf.RoundToInt(currentScore);
    public int MaxScore => Mathf.RoundToInt(maxScore);

    /// <summary>Fires whenever the score changes, with the new rounded value.</summary>
    public event Action<int> ScoreChanged;
    public event Action<int> ScoreAwarded;
    public event Action FeedbackCleared;

    /// <summary>Computes the level's starting score from its gap count and difficulty, then begins draining it over time.</summary>
    public void StartLevel(LevelData level)
    {
        FeedbackCleared?.Invoke();
        int gapCount = level != null ? level.TotalGapCount : 0;
        LevelDifficulty difficulty = level != null ? level.Difficulty : LevelDifficulty.Easy;

        maxScore = (baseScore + gapCount * pointsPerGap) * GetDifficultyMultiplier(difficulty);
        currentScore = maxScore;
        isActive = true;

        ScoreChanged?.Invoke(CurrentScore);
    }

    /// <summary>Freezes the score (level complete, paused, quit, etc.).</summary>
    public void StopLevel()
    {
        isActive = false;
        FeedbackCleared?.Invoke();
    }

    /// <summary>
    /// Call when a word in the first sentence becomes fully and correctly filled, passing how
    /// many gaps that word had. Awards points scaled to the word's missing-letter count - this
    /// is a reward, so it is added on top of the current score rather than capped at MaxScore.
    /// </summary>
    public void AddFirstSentenceWordBonus(int gapCount)
    {
        if (!isActive || gapCount <= 0)
            return;

        float bonus = pointsPerCorrectFirstSentenceGap * gapCount;
        if (bonus <= 0f) return;
        currentScore += bonus;
        ScoreChanged?.Invoke(CurrentScore);
        ScoreAwarded?.Invoke(Mathf.RoundToInt(bonus));
    }

    /// <summary>Call whenever the player submits an incorrect letter.</summary>
    public void NotifyWrongSubmit()
    {
        if (!isActive)
            return;

        currentScore /= 2f;
        ScoreChanged?.Invoke(CurrentScore);
    }

    private float GetDifficultyMultiplier(LevelDifficulty difficulty)
    {
        if (difficultyMultipliers == null || difficultyMultipliers.Length == 0)
            return 1f;

        int index = Mathf.Clamp((int)difficulty, 0, difficultyMultipliers.Length - 1);
        return difficultyMultipliers[index];
    }

    private void ApplyPenalty(float amount)
    {
        currentScore = Mathf.Max(0f, currentScore - amount);
        ScoreChanged?.Invoke(CurrentScore);
    }

    private void Update()
    {
        if (!isActive)
            return;

        ApplyPenalty(decayPerSecond * Time.deltaTime);
    }
}
