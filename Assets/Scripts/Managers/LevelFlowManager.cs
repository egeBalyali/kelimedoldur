using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives a single level's flow: shows the current sentence (upper half) with its
/// gaps, sets up the letter pool for the level (lower third), and routes presses
/// between them via the "active gap".
/// </summary>
public class LevelFlowManager : MonoBehaviour
{
    [SerializeField] private SentenceView sentenceView;
    [SerializeField] private LetterPoolController letterPool;
    [SerializeField] private LevelData tmpLevelData;

    private LevelData currentLevel;
    private int currentSentenceIndex;
    private int activeGapIndex = -1;

    // Maps a raw gap index to the specific pool LetterView tile that filled it,
    // so the exact tile can be returned to the pool if the gap is cleared.
    private readonly Dictionary<int, LetterView> gapToPoolLetter = new Dictionary<int, LetterView>();

    public event Action LevelCompleted;
    public event Action<int> SentenceCompleted; // passes the sentence index that just finished
    public event Action WrongLetterPressed;     // Optional event for error SFX/animations

    private void OnEnable()
    {
        sentenceView.GapPressed += OnGapPressed;
        letterPool.LetterPressed += OnPoolLetterPressed;

        LoadLevel(tmpLevelData);
    }

    private void OnDisable()
    {
        sentenceView.GapPressed -= OnGapPressed;
        letterPool.LetterPressed -= OnPoolLetterPressed;
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

        letterPool.Setup(level != null ? level.letters : null);
        ShowCurrentSentence();
    }

    private void ShowCurrentSentence()
    {
        gapToPoolLetter.Clear();

        SentenceData sentence = currentLevel.GetSentence(currentSentenceIndex);
        sentenceView.Setup(sentence);

        activeGapIndex = sentenceView.FirstGapIndex;
        sentenceView.SetActiveGap(activeGapIndex);
    }

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
            return; // no gap selected / sentence has no gaps left

        if (sentenceView.IsGapFilled(activeGapIndex))
            return; // safety - active gap should always be empty

        // 1. Fetch current sentence data and check character truth
        SentenceData currentSentence = currentLevel.GetSentence(currentSentenceIndex);

        if (!IsLetterCorrect(currentSentence, activeGapIndex, poolLetter.Letter))
        {
            // Letter does not match expected character for this gap
            WrongLetterPressed?.Invoke();
            Debug.Log($"Incorrect letter '{poolLetter.Letter}' pressed for gap at index {activeGapIndex}");
            return; // Stop placement
        }

        // 2. Letter is correct -> Place in view and mark used
        sentenceView.SetGapLetter(activeGapIndex, poolLetter.Letter);
        gapToPoolLetter[activeGapIndex] = poolLetter;
        letterPool.SetLetterUsed(poolLetter, true);

        AdvanceToNextGap();
    }

    /// <summary>
    /// Checks whether the selected letter matches the expected character in the sentence data.
    /// Supports case-insensitive comparison.
    /// </summary>
    private bool IsLetterCorrect(SentenceData sentence, int gapIndex, char pressedLetter)
    {
        if (sentence == null) return false;

        // Uses original SolutionCharacters from raw sentence parsing
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
                OnSentenceComplete();

            return;
        }

        activeGapIndex = nextGap;
        sentenceView.SetActiveGap(activeGapIndex);
    }

    private void ReturnGapLetterToPool(int rawIndex)
    {
        if (gapToPoolLetter.TryGetValue(rawIndex, out LetterView poolLetter))
        {
            letterPool.SetLetterUsed(poolLetter, false);
            gapToPoolLetter.Remove(rawIndex);
        }

        sentenceView.ClearGap(rawIndex);
    }

    private void OnSentenceComplete()
    {
        SentenceCompleted?.Invoke(currentSentenceIndex);

        currentSentenceIndex++;

        if (currentSentenceIndex < currentLevel.SentenceCount)
        {
            ShowCurrentSentence();
        }
        else
        {
            LevelCompleted?.Invoke();
        }
    }
}