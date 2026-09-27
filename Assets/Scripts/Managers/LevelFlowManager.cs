using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a level's flow: shows the current sentence (upper half) with its gaps,
/// sets up the letter pool for the level (lower third), and routes presses
/// between them via the "active gap". Supports free navigation between
/// sentences, with the last sentence acting as the "answer" that is validated
/// on Submit. Earlier sentences are unchecked scratch space for the player.
/// </summary>
public class LevelFlowManager : MonoBehaviour
{
    [SerializeField] private SentenceView sentenceView;
    [SerializeField] private LetterPoolController letterPool;
    [SerializeField] private LevelData tmpLevelData;

    [Header("Timer & Scoring")]
    [SerializeField] private LevelTimer levelTimer;
    [SerializeField] private LevelScoreManager scoreManager;

    [Header("Lives")]
    [SerializeField] private GameObject[] hearts = new GameObject[3];
    private const int MaxWrongSubmits = 3;
    private bool levelEnded;

    private void RefreshHearts()
    {
        for (int i = 0; i < hearts.Length; i++)
            if (hearts[i] != null) hearts[i].SetActive(i < MaxWrongSubmits - wrongSubmitCount);
    }

    [Header("Navigation")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button submitButton;

    private LevelData currentLevel;
    private int currentSentenceIndex;
    private int activeGapIndex = -1;
    private int wrongSubmitCount = 0;

    // Per-sentence: gap index -> the pool LetterView tile that filled it.
    // Kept per sentence (instead of clearing on every ShowCurrentSentence)
    // so the player can navigate back and forth without losing progress.
    private readonly Dictionary<int, Dictionary<int, LetterView>> sentenceGapFills =
        new Dictionary<int, Dictionary<int, LetterView>>();

    // Gap indices in the first sentence that have already earned their word-completion
    // bonus, so a word isn't paid out twice (e.g. after navigating away and back).
    private readonly HashSet<int> creditedFirstSentenceGaps = new HashSet<int>();

    public event Action LevelCompleted;
    public event Action<int> SentenceCompleted;  // fired when a sentence's gaps are all filled
    public event Action AnswerIncorrect;          // Submit pressed but answer isn't right/complete
    public event Action TimeExpired;              // level's timer ran out

    public bool IsFirstSentence => currentSentenceIndex <= 0;
    public bool IsLastSentence => currentLevel != null && currentSentenceIndex >= currentLevel.SentenceCount - 1;
    public bool CanGoNext => currentLevel != null && currentSentenceIndex < currentLevel.SentenceCount - 1;
    public bool CanGoPrevious => currentSentenceIndex > 0;

    private void OnEnable()
    {
        sentenceView.GapPressed += OnGapPressed;
        letterPool.LetterPressed += OnPoolLetterPressed;

        if (previousButton != null) previousButton.onClick.AddListener(GoToPreviousSentence);
        if (nextButton != null) nextButton.onClick.AddListener(GoToNextSentence);
        if (submitButton != null) submitButton.onClick.AddListener(SubmitAnswer);

        if (levelTimer != null) levelTimer.TimeExpired += HandleTimeExpired;

        LoadLevel(tmpLevelData);
    }

    private void OnDisable()
    {
        sentenceView.GapPressed -= OnGapPressed;
        letterPool.LetterPressed -= OnPoolLetterPressed;

        if (previousButton != null) previousButton.onClick.RemoveListener(GoToPreviousSentence);
        if (nextButton != null) nextButton.onClick.RemoveListener(GoToNextSentence);
        if (submitButton != null) submitButton.onClick.RemoveListener(SubmitAnswer);

        if (levelTimer != null) levelTimer.TimeExpired -= HandleTimeExpired;
    }

    private void HandleTimeExpired()
    {
        if (levelEnded) return;
        levelEnded = true;
        scoreManager?.StopLevel();
        TimeExpired?.Invoke();
    }

    [ContextMenu("load level")]
    public void LoadLevelFromMenu()
    {
        LoadLevel(tmpLevelData);
    }

    public void LoadLevel(LevelData level)
    {
        currentLevel = level;
        currentSentenceIndex = 0;
        wrongSubmitCount = 0;
        levelEnded = level == null;
        RefreshHearts();
        sentenceGapFills.Clear();
        creditedFirstSentenceGaps.Clear();

        letterPool.Setup(level != null ? level.letters : null);
        ShowCurrentSentence();

        if (levelTimer != null) levelTimer.StartLevel(level);
        if (scoreManager != null) scoreManager.StartLevel(level);
        if (levelEnded)
        {
            levelTimer?.Stop();
            scoreManager?.StopLevel();
        }
    }

    public void RetryFailedLevel()
    {
        if (currentLevel == null) return;
        levelEnded = false;
        wrongSubmitCount = 0;
        RefreshHearts();
        ResetUnfinishedWords();
        levelTimer?.StartLevel(currentLevel);
        scoreManager?.StartLevel(currentLevel);
    }

    public void StopLevel()
    {
        levelEnded = true;
        levelTimer?.Stop();
        scoreManager?.StopLevel();
    }

    // ---------------- Navigation buttons ----------------

    public void ResetUnfinishedWords()
    {
        if (currentLevel == null || levelEnded) return;
        foreach (var page in sentenceGapFills)
        {
            SentenceData sentence = currentLevel.GetSentence(page.Key);
            if (sentence == null) continue;
            var letters = new Dictionary<int, char>();
            foreach (var fill in page.Value)
                if (fill.Value != null) letters[fill.Key] = fill.Value.Letter;
            HashSet<int> preserved = sentence.GetCorrectWordGapIndices(letters);
            var toRemove = new List<int>();
            foreach (var fill in page.Value)
            {
                if (preserved.Contains(fill.Key)) continue;
                if (fill.Value != null) letterPool.SetLetterUsed(fill.Value, false);
                toRemove.Add(fill.Key);
            }
            foreach (int gap in toRemove) page.Value.Remove(gap);
        }
        currentSentenceIndex = 0;
        // Rebuilds the visible page, stopping the trace; restored words suppress all completion feedback.
        ShowCurrentSentence();
    }

    public void GoToNextSentence()
    {
        if (levelEnded || !CanGoNext) return;


        currentSentenceIndex++;
        ShowCurrentSentence();
    }

    public void GoToPreviousSentence()
    {
        if (levelEnded || !CanGoPrevious) return;

        currentSentenceIndex--;
        ShowCurrentSentence();
    }

    // ---------------- Submit button (last page only) ----------------

    public void SubmitAnswer()
    {
        if (levelEnded || !IsLastSentence) return;

        if (sentenceView.IsComplete && IsCurrentSentenceFullyCorrect())
        {
            Debug.Log($"[LevelFlowManager] Answer correct — sentence {currentSentenceIndex} fully and exactly matches. Advancing to next level.");
            levelTimer?.Stop();
            scoreManager?.StopLevel();
            levelEnded = true;
            LevelCompleted?.Invoke();
        }
        else
        {
            wrongSubmitCount++;
            RefreshHearts();

            if (wrongSubmitCount < MaxWrongSubmits)
            {
                // First two wrong submits: halve the current score
                scoreManager?.NotifyWrongSubmit();
            }
            else
            {
                // 3rd wrong submit: trigger fail state & show fail screen
                levelTimer?.Stop();
                scoreManager?.StopLevel();
                levelEnded = true;
                AnswerIncorrect?.Invoke();
            }
        }
    }

    // Only the final (answer) sentence is ever checked for correctness.
    // Earlier sentences are unchecked scratch/practice space for the player.
    private bool IsCurrentSentenceFullyCorrect()
    {
        SentenceData sentence = currentLevel?.GetSentence(currentSentenceIndex);
        if (sentence == null) return false;

        if (!sentenceGapFills.TryGetValue(currentSentenceIndex, out var fills))
            return false;

        foreach (var kvp in fills)
        {
            int gapIndex = kvp.Key;
            LetterView poolLetter = kvp.Value;

            if (!IsLetterCorrect(sentence, gapIndex, poolLetter.Letter))
                return false;
        }

        return true;
    }

    // ---------------- Sentence display ----------------

    private void ShowCurrentSentence()
    {
        SentenceData sentence = currentLevel?.GetSentence(currentSentenceIndex);
        sentenceView.Setup(sentence, IsLastSentence);

        RestoreGapFillsForCurrentSentence();

        activeGapIndex = FindFirstEmptyGapOrDefault();
        sentenceView.SetActiveGap(activeGapIndex);

        UpdateNavButtons();
    }

    private void UpdateNavButtons()
    {
        if (previousButton != null) previousButton.gameObject.SetActive(CanGoPrevious);
        if (nextButton != null) nextButton.gameObject.SetActive(CanGoNext);
        if (submitButton != null) submitButton.gameObject.SetActive(IsLastSentence);
    }

    private void RestoreGapFillsForCurrentSentence()
    {
        if (!sentenceGapFills.TryGetValue(currentSentenceIndex, out var fills))
            return;

        foreach (var kvp in fills)
        {
            // Suppress trace effects when re-populating gaps during sentence navigation
            sentenceView.SetGapLetter(kvp.Key, kvp.Value.Letter, suppressTrace: true);
        }
    }

    private int FindFirstEmptyGapOrDefault()
    {
        int firstGap = sentenceView.FirstGapIndex;
        if (firstGap < 0) return -1;

        if (!sentenceView.IsGapFilled(firstGap))
            return firstGap;

        return sentenceView.FindNextEmptyGap(firstGap); // -1 if none left
    }

    // ---------------- Gap / letter interactions ----------------

    private void OnGapPressed(int rawIndex)
    {
        if (levelEnded) return;
        if (sentenceView.IsGapFilled(rawIndex))
        {
            ReturnGapLetterToPool(rawIndex);
        }

        activeGapIndex = rawIndex;
        sentenceView.SetActiveGap(activeGapIndex);
    }

    private void OnPoolLetterPressed(LetterView poolLetter)
    {
        if (levelEnded || activeGapIndex < 0)
            return;

        if (sentenceView.IsGapFilled(activeGapIndex))
            return;

        // No sentence is validated letter-by-letter as you type anymore - any letter can be
        // placed in any gap. The answer sentence is only checked as a whole on Submit.
        PlaceLetter(activeGapIndex, poolLetter);
        AdvanceToNextGap();
    }

    private void PlaceLetter(int gapIndex, LetterView poolLetter)
    {
        // Pass suppressTrace: IsLastSentence so it won't trace on the final sentence
        sentenceView.SetGapLetter(gapIndex, poolLetter.Letter, suppressTrace: IsLastSentence);

        if (!sentenceGapFills.TryGetValue(currentSentenceIndex, out var fills))
        {
            fills = new Dictionary<int, LetterView>();
            sentenceGapFills[currentSentenceIndex] = fills;
        }
        fills[gapIndex] = poolLetter;

        letterPool.SetLetterUsed(poolLetter, true);

        if (currentSentenceIndex == 0)
            CheckFirstSentenceWordBonus();
    }

    /// <summary>
    /// Awards points whenever filling this letter completed a whole word in the first sentence
    /// correctly. Reuses SentenceData's word-correctness check (same one ResetUnfinishedWords
    /// uses) and pays out per gap the first time each gap is seen as part of a correct word, so
    /// re-showing the page later never pays the same word twice.
    /// </summary>
    private void CheckFirstSentenceWordBonus()
    {
        SentenceData sentence = currentLevel?.GetSentence(0);
        if (sentence == null || !sentenceGapFills.TryGetValue(0, out var fills))
            return;

        var filledLetters = new Dictionary<int, char>();
        foreach (var kvp in fills)
            if (kvp.Value != null) filledLetters[kvp.Key] = kvp.Value.Letter;

        HashSet<int> correctGaps = sentence.GetCorrectWordGapIndices(filledLetters);

        int newlyCorrectCount = 0;
        foreach (int gapIndex in correctGaps)
            if (creditedFirstSentenceGaps.Add(gapIndex))
                newlyCorrectCount++;

        if (newlyCorrectCount > 0)
            scoreManager?.AddFirstSentenceWordBonus(newlyCorrectCount);
    }

    private bool IsLetterCorrect(SentenceData sentence, int gapIndex, char pressedLetter)
    {
        if (sentence == null) return false;

        char expectedChar = sentence.GetExpectedLetter(gapIndex);
        return char.ToUpperInvariant(expectedChar) == char.ToUpperInvariant(pressedLetter);
    }

    private void AdvanceToNextGap()
    {
        int filledIndex = activeGapIndex;
        int nextGap = sentenceView.FindNextEmptyGap(filledIndex);

        if (nextGap < 0)
        {
            activeGapIndex = -1;
            sentenceView.SetActiveGap(-1);

            if (sentenceView.IsComplete)
                SentenceCompleted?.Invoke(currentSentenceIndex);

            return;
        }

        activeGapIndex = nextGap;
        sentenceView.SetActiveGap(activeGapIndex);
    }

    private void ReturnGapLetterToPool(int rawIndex)
    {
        if (sentenceGapFills.TryGetValue(currentSentenceIndex, out var fills) &&
            fills.TryGetValue(rawIndex, out LetterView poolLetter))
        {
            letterPool.SetLetterUsed(poolLetter, false);
            fills.Remove(rawIndex);
        }

        sentenceView.ClearGap(rawIndex);
    }
}
