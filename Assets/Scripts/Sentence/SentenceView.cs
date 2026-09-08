using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders the current sentence (upper half of screen) using word-wrapping rules across lines.
/// Adapts automatically to SentenceData's parsed DisplayCharacters.
/// </summary>
public class SentenceView : MonoBehaviour
{
    [SerializeField] private LetterView letterPrefab;
    [SerializeField] private Transform sentenceContainer; // Should have a Vertical Layout Group
    [SerializeField] private GameObject linePrefab;       // Prefab with Horizontal Layout Group
    [SerializeField] private float spaceWidth = 20f;
    [SerializeField] private int maxCharsPerLine = 11;
    [SerializeField] private int maxLines = 4;

    private readonly Dictionary<int, LetterView> gapViews = new Dictionary<int, LetterView>();
    private readonly List<GameObject> spawned = new List<GameObject>();
    private readonly List<int> gapIndicesInOrder = new List<int>();

    private SentenceData currentSentence;
    private int activeGapIndex = -1;

    public event Action<int> GapPressed;

    public bool IsComplete
    {
        get
        {
            foreach (var kvp in gapViews)
                if (kvp.Value.IsEmpty)
                    return false;
            return true;
        }
    }

    public IReadOnlyList<int> GapIndices => gapIndicesInOrder;
    public int FirstGapIndex => gapIndicesInOrder.Count > 0 ? gapIndicesInOrder[0] : -1;

    /// <summary>Builds the word-wrapped layout for a sentence using DisplayCharacters.</summary>
    public void Setup(SentenceData sentence)
    {
        Clear();
        currentSentence = sentence;

        if (sentence == null || sentence.DisplayCharacters == null || sentence.DisplayCharacters.Length == 0)
            return;

        char[] displayChars = sentence.DisplayCharacters;

        // 1. Group display character indices by token (Words vs Spaces)
        List<List<int>> tokens = TokenizeSentence(displayChars);

        int currentLineIndex = 0;
        int currentLineCharCount = 0;
        Transform currentLineTransform = CreateNewLineContainer();

        foreach (List<int> token in tokens)
        {
            bool isSpace = token.Count == 1 && displayChars[token[0]] == ' ';
            int tokenLength = token.Count;

            // Skip leading space on a brand new line
            if (isSpace && currentLineCharCount == 0)
                continue;

            // 2. Line Wrap Check: If token exceeds line budget, move to next line container
            if (!isSpace && (currentLineCharCount + tokenLength > maxCharsPerLine))
            {
                currentLineIndex++;
                if (currentLineIndex >= maxLines)
                {
                    Debug.LogWarning("[SentenceView] Sentence exceeds maximum line limit!");
                    break;
                }

                currentLineTransform = CreateNewLineContainer();
                currentLineCharCount = 0;
            }

            // 3. Instantiate view tiles into active line container
            foreach (int rawIndex in token)
            {
                char c = displayChars[rawIndex];

                if (c == '_')
                {
                    LetterView gap = LetterView.CreateEmpty(letterPrefab, currentLineTransform);
                    gap.SetInteractable(true);
                    int capturedIndex = rawIndex;
                    gap.Clicked += _ => GapPressed?.Invoke(capturedIndex);

                    gapViews[rawIndex] = gap;
                    gapIndicesInOrder.Add(rawIndex);
                    spawned.Add(gap.gameObject);
                }
                else if (c == ' ')
                {
                    spawned.Add(CreateSpacer(currentLineTransform));
                }
                else
                {
                    LetterView fixedLetter = LetterView.Create(letterPrefab, currentLineTransform, c);
                    fixedLetter.SetInteractable(false);
                    spawned.Add(fixedLetter.gameObject);
                }
            }

            currentLineCharCount += tokenLength;
        }
    }

    /// <summary> Breaks display characters array into token groups of indices representing words/spaces. </summary>
    private List<List<int>> TokenizeSentence(char[] characters)
    {
        List<List<int>> tokens = new List<List<int>>();
        List<int> currentToken = new List<int>();

        for (int i = 0; i < characters.Length; i++)
        {
            char c = characters[i];

            if (c == ' ')
            {
                if (currentToken.Count > 0)
                {
                    tokens.Add(new List<int>(currentToken));
                    currentToken.Clear();
                }
                tokens.Add(new List<int> { i }); // Space token
            }
            else
            {
                currentToken.Add(i);
            }
        }

        if (currentToken.Count > 0)
            tokens.Add(currentToken);

        return tokens;
    }

    private Transform CreateNewLineContainer()
    {
        GameObject lineObj = Instantiate(linePrefab, sentenceContainer);
        spawned.Add(lineObj);
        return lineObj.transform;
    }

    private GameObject CreateSpacer(Transform parent)
    {
        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(parent, false);
        var rect = spacer.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(spaceWidth, rect.sizeDelta.y);

        LayoutElement layoutElement = spacer.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = spaceWidth;
        layoutElement.minWidth = spaceWidth;

        return spacer;
    }

    private void Clear()
    {
        foreach (GameObject go in spawned)
            if (go != null)
                Destroy(go);

        spawned.Clear();
        gapViews.Clear();
        gapIndicesInOrder.Clear();
        activeGapIndex = -1;
        currentSentence = null;
    }

    public void SetGapLetter(int rawIndex, char letter)
    {
        if (gapViews.TryGetValue(rawIndex, out LetterView view))
            view.SetLetter(letter);
    }

    public void ClearGap(int rawIndex)
    {
        if (gapViews.TryGetValue(rawIndex, out LetterView view))
            view.SetEmpty();
    }

    public bool IsGapFilled(int rawIndex)
    {
        return gapViews.TryGetValue(rawIndex, out LetterView view) && !view.IsEmpty;
    }

    public char GetGapLetter(int rawIndex)
    {
        return gapViews.TryGetValue(rawIndex, out LetterView view) ? view.Letter : '\0';
    }

    public void SetActiveGap(int rawIndex)
    {
        if (gapViews.TryGetValue(activeGapIndex, out LetterView previous))
            previous.SetActive(false);

        activeGapIndex = rawIndex;

        if (gapViews.TryGetValue(activeGapIndex, out LetterView next))
            next.SetActive(true);
    }

    /// <summary>Finds the next empty gap at or after fromIndex (wrapping to the start if none found forward).</summary>
    public int FindNextEmptyGap(int fromIndex)
    {
        int startPos = gapIndicesInOrder.IndexOf(fromIndex);
        if (startPos < 0)
            startPos = -1;

        for (int step = 0; step < gapIndicesInOrder.Count; step++)
        {
            int pos = (startPos + 1 + step) % gapIndicesInOrder.Count;
            int idx = gapIndicesInOrder[pos];
            if (gapViews[idx].IsEmpty)
                return idx;
        }

        return -1; // no empty gaps left
    }
}