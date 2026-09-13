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

    [Header("Word Trace Effect")]
    [SerializeField] private WordTraceSequencer traceSequencerPrefab;
    [SerializeField] private Transform traceEffectParent; // Where the runtime particle instance is parented. Defaults to sentenceContainer if left empty.
    [SerializeField] private float traceDuration = 0.8f;
    [Tooltip("Pushes the trace target (and therefore the particle effect) this far toward the camera on Z, so its sort order versus the sentence UI graphics is deterministic instead of coin-flipping when both sit at z=0. Same idea as the -1 z offset used for the click particle in LetterView.")]
    [SerializeField] private float traceZOffset = -5f;

    private readonly Dictionary<int, LetterView> gapViews = new Dictionary<int, LetterView>();
    private readonly List<GameObject> spawned = new List<GameObject>();
    private readonly List<int> gapIndicesInOrder = new List<int>();

    // Word-grouping bookkeeping, needed so completed words can be traced.
    private readonly Dictionary<int, List<RectTransform>> wordMemberRects = new Dictionary<int, List<RectTransform>>(); // wordId -> every tile (letter+gap) in that word
    private readonly Dictionary<int, List<int>> wordGapMembers = new Dictionary<int, List<int>>(); // wordId -> gap indices
    private readonly Dictionary<int, int> gapToWordId = new Dictionary<int, int>();
    private int nextWordId;

    // Reusable, layout-independent RectTransform used only to hand a bounding box to WordTraceSequencer.
    private RectTransform traceTargetRect;

    // Runtime instance instantiated from traceSequencerPrefab (destroyed when complete or interrupted).
    private WordTraceSequencer traceSequencerInstance;

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

            if (isSpace)
            {
                spawned.Add(CreateSpacer(currentLineTransform));
            }
            else
            {
                // 3. Instantiate view tiles into active line container
                int wordId = nextWordId++;
                List<RectTransform> memberRects = new List<RectTransform>(token.Count);
                List<int> gapsInWord = new List<int>();

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

                        gapsInWord.Add(rawIndex);
                        gapToWordId[rawIndex] = wordId;
                        memberRects.Add(gap.transform as RectTransform);
                    }
                    else
                    {
                        LetterView fixedLetter = LetterView.Create(letterPrefab, currentLineTransform, c);
                        fixedLetter.SetInteractable(false);
                        spawned.Add(fixedLetter.gameObject);
                        memberRects.Add(fixedLetter.transform as RectTransform);
                    }
                }

                wordMemberRects[wordId] = memberRects;

                // Only words that actually contain gaps need to be tracked for completion.
                if (gapsInWord.Count > 0)
                    wordGapMembers[wordId] = gapsInWord;
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

    public void StopTraceEffect()
    {
        DestroyActiveTraceSequencer();
    }

    private void Clear()
    {
        DestroyActiveTraceSequencer();
        DestroyTraceTargetRect();

        foreach (GameObject go in spawned)
            if (go != null)
                Destroy(go);

        spawned.Clear();
        gapViews.Clear();
        gapIndicesInOrder.Clear();

        wordMemberRects.Clear();
        wordGapMembers.Clear();
        gapToWordId.Clear();
        nextWordId = 0;

        activeGapIndex = -1;
        currentSentence = null;
    }

    public void SetGapLetter(int rawIndex, char letter, bool suppressTrace = false)
    {
        if (gapViews.TryGetValue(rawIndex, out LetterView view))
        {
            view.SetLetter(letter);

            if (!suppressTrace)
            {
                TryTraceIfWordComplete(rawIndex);
            }
        }
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

    /// <summary>
    /// Checks whether the word containing the given gap now has all its gaps filled,
    /// and if so, plays the particle trace around that word's bounding box.
    /// </summary>
    private void TryTraceIfWordComplete(int rawIndex)
    {
        if (!gapToWordId.TryGetValue(rawIndex, out int wordId))
            return;

        if (!wordGapMembers.TryGetValue(wordId, out List<int> gapsInWord))
            return;

        if (currentSentence == null)
            return;

        // Word must be fully filled AND every filled letter must be correct.
        foreach (int gapIdx in gapsInWord)
        {
            if (!gapViews.TryGetValue(gapIdx, out LetterView gv) || gv.IsEmpty)
                return; // not filled yet

            if (!currentSentence.IsCorrect(gapIdx, gv.Letter))
                return; // filled but wrong - bail out before touching the tracer at all
        }

        // Clean up any previously running particle trace instance before starting a new one
        DestroyActiveTraceSequencer();

        WordTraceSequencer traceSequencer = InstantiateTraceSequencer();
        if (traceSequencer == null)
            return;

        if (!wordMemberRects.TryGetValue(wordId, out List<RectTransform> members) || members.Count == 0)
            return;

        RectTransform target = GetOrCreateTraceTargetRect();
        if (target == null)
            return;

        PositionTraceTargetAroundWord(target, members);
        traceSequencer.TraceWordBox(target, traceDuration);
    }

    /// <summary>
    /// Instantiates a fresh WordTraceSequencer instance on demand.
    /// </summary>
    private WordTraceSequencer InstantiateTraceSequencer()
    {
        if (traceSequencerPrefab == null)
            return null;

        Transform parent = traceEffectParent != null ? traceEffectParent : sentenceContainer;

        traceSequencerInstance = Instantiate(traceSequencerPrefab, parent);
        return traceSequencerInstance;
    }

    private void DestroyActiveTraceSequencer()
    {
        if (traceSequencerInstance != null)
        {
            traceSequencerInstance.StopTrace();
            traceSequencerInstance = null;
        }
    }

    private void DestroyTraceTargetRect()
    {
        if (traceTargetRect != null)
        {
            Destroy(traceTargetRect.gameObject);
            traceTargetRect = null;
        }
    }

    /// <summary>
    /// Lazily creates a small helper RectTransform used only to hand a bounding box to
    /// WordTraceSequencer. It's parented to the root Canvas (not to sentenceContainer or any
    /// line), so it is never touched by a Layout Group and cannot affect the sentence layout.
    /// </summary>
    private RectTransform GetOrCreateTraceTargetRect()
    {
        if (traceTargetRect != null)
            return traceTargetRect;

        if (sentenceContainer == null)
            return null;

        Canvas canvas = sentenceContainer.GetComponentInParent<Canvas>();
        if (canvas == null)
            return null;

        GameObject go = new GameObject("WordTraceTarget", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);

        traceTargetRect = go.GetComponent<RectTransform>();
        traceTargetRect.pivot = Vector2.zero;
        traceTargetRect.anchorMin = Vector2.zero;
        traceTargetRect.anchorMax = Vector2.zero;

        return traceTargetRect;
    }

    /// <summary>
    /// Sizes and positions the helper RectTransform so that GetWorldCorners() on it returns
    /// the axis-aligned bounding box enclosing every letter/gap tile belonging to the word.
    /// </summary>
    private void PositionTraceTargetAroundWord(RectTransform target, List<RectTransform> members)
    {
        if (members == null || members.Count == 0) return;

        // Force unity layout groups to update sizes before calculating world corners
        Canvas.ForceUpdateCanvases();

        RectTransform parentRect = target.parent as RectTransform;
        if (parentRect == null) return;

        Canvas canvas = target.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera
            : null;

        bool first = true;
        Vector2 minLocal = Vector2.zero;
        Vector2 maxLocal = Vector2.zero;
        Vector3[] corners = new Vector3[4];

        foreach (RectTransform member in members)
        {
            if (member == null) continue;

            member.GetWorldCorners(corners);
            for (int i = 0; i < 4; i++)
            {
                // Convert world corners directly to local space relative to the target's parent rect
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, cam, out Vector2 localPoint);

                if (first)
                {
                    minLocal = localPoint;
                    maxLocal = localPoint;
                    first = false;
                }
                else
                {
                    minLocal = Vector2.Min(minLocal, localPoint);
                    maxLocal = Vector2.Max(maxLocal, localPoint);
                }
            }
        }

        if (first) return;

        target.sizeDelta = maxLocal - minLocal;

        target.anchorMin = new Vector2(0.5f, 0.5f);
        target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot = Vector2.zero;

        target.anchoredPosition3D = new Vector3(minLocal.x, minLocal.y, traceZOffset);
    }
}