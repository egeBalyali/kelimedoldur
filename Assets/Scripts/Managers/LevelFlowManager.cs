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

    [Header("Navigation")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button submitButton;

    private LevelData currentLevel;
    private int currentSentenceIndex;
    private int activeGapIndex = -1;

    // Per-sentence: gap index -> the pool LetterView tile that filled it.
    // Kept per sentence (instead of clearing on every ShowCurrentSentence)
    // so the player can navigate back and forth without losing progress.
    private readonly Dictionary<int, Dictionary<int, LetterView>> sentenceGapFills =
        new Dictionary<int, Dictionary<int, LetterView>>();

    public event Action LevelCompleted;
    public event Action<int> SentenceCompleted;  // fired when a sentence's gaps are all filled
    public event Action WrongLetterPressed;       // wrong letter on the answer sentence
    public event Action AnswerIncorrect;          // Submit pressed but answer isn't right/complete

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

        LoadLevel(tmpLevelData);
    }

    private void OnDisable()
    {
        sentenceView.GapPressed -= OnGapPressed;
        letterPool.LetterPressed -= OnPoolLetterPressed;

        if (previousButton != null) previousButton.onClick.RemoveListener(GoToPreviousSentence);
        if (nextButton != null) nextButton.onClick.RemoveListener(GoToNextSentence);
        if (submitButton != null) submitButton.onClick.RemoveListener(SubmitAnswer);
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
        sentenceGapFills.Clear();

        letterPool.Setup(level != null ? level.letters : null);
        ShowCurrentSentence();
    }

    // ---------------- Navigation buttons ----------------

    public void GoToNextSentence()
    {
        if (!CanGoNext) return;


        currentSentenceIndex++;
        ShowCurrentSentence();
    }

    public void GoToPreviousSentence()
    {
        if (!CanGoPrevious) return;

        currentSentenceIndex--;
        ShowCurrentSentence();
    }

    // ---------------- Submit button (last page only) ----------------

    public void SubmitAnswer()
    {
        if (!IsLastSentence) return;

        if (sentenceView.IsComplete && IsCurrentSentenceFullyCorrect())
        {
            Debug.Log($"[LevelFlowManager] Answer correct — sentence {currentSentenceIndex} fully and exactly matches. Advancing to next level.");
            LevelCompleted?.Invoke();
        }
        else
        {
            AnswerIncorrect?.Invoke();
        }
    }

    // Only the final (answer) sentence is ever checked for correctness.
    // Earlier sentences are unchecked scratch/practice space for the player.
    private bool IsCurrentSentenceFullyCorrect()
    {
        SentenceData sentence = currentLevel.GetSentence(currentSentenceIndex);
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
        SentenceData sentence = currentLevel.GetSentence(currentSentenceIndex);
        sentenceView.Setup(sentence);

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
        if (sentenceView.IsGapFilled(rawIndex))
        {
            ReturnGapLetterToPool(rawIndex);
        }

        activeGapIndex = rawIndex;
        sentenceView.SetActiveGap(activeGapIndex);
    }

    private void OnPoolLetterPressed(LetterView poolLetter)
    {
        if (activeGapIndex < 0)
            return;

        if (sentenceView.IsGapFilled(activeGapIndex))
            return;

        SentenceData currentSentence = currentLevel.GetSentence(currentSentenceIndex);

        // Only the last sentence (the answer) is validated letter-by-letter as you type.
        // Earlier sentences accept whatever letter is pressed - no correctness check at all.
        if (IsLastSentence && !IsLetterCorrect(currentSentence, activeGapIndex, poolLetter.Letter))
        {
            WrongLetterPressed?.Invoke();
            Debug.Log($"Incorrect letter '{poolLetter.Letter}' pressed for gap at index {activeGapIndex}");
            return;
        }

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