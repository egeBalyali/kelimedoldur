using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One sentence/stage within a level.
/// Filled characters are known letters or spaces; '_' marks a slot
/// the player must fill at runtime, immediately followed by its expected character (e.g., "TH_e C_a T_s").
/// </summary>
[Serializable]
public class SentenceData
{
    [Tooltip("Raw format: Use '_X' for a gap expecting letter 'X' (e.g., 'TH_e C_a T_s').")]
    [SerializeField] private string rawSentence;

    // Parsed cache (not serialized)
    [NonSerialized] private char[] displayCharacters;
    [NonSerialized] private Dictionary<int, char> gapSolutions;
    [NonSerialized] private bool isParsed;

    // --- Public Properties & Parsers ---

    /// <summary>Displayable sentence characters where '_' represents a blank slot.</summary>
    public char[] DisplayCharacters
    {
        get
        {
            EnsureParsed();
            return displayCharacters;
        }
    }

    public int Length => DisplayCharacters?.Length ?? 0;

    public bool IsEmptySlot(int index) => DisplayCharacters != null && index < DisplayCharacters.Length && DisplayCharacters[index] == '_';

    /// <summary>Checks if a letter provided for a given raw character index matches the answer.</summary>
    public bool IsCorrect(int rawIndex, char letter)
    {
        EnsureParsed();
        if (gapSolutions != null && gapSolutions.TryGetValue(rawIndex, out char expected))
        {
            return char.ToUpperInvariant(expected) == char.ToUpperInvariant(letter);
        }
        return false;
    }

    /// <summary>Gets the expected char for a given gap slot index.</summary>
    public char GetExpectedLetter(int rawIndex)
    {
        EnsureParsed();
        if (gapSolutions != null && gapSolutions.TryGetValue(rawIndex, out char expected))
        {
            return expected;
        }
        return '\0';
    }

    /// <summary>Indices of all player-fillable slots, in order.</summary>
    public int[] GetEmptySlotIndices()
    {
        EnsureParsed();
        var result = new List<int>();
        for (int i = 0; i < displayCharacters.Length; i++)
        {
            if (displayCharacters[i] == '_')
                result.Add(i);
        }
        return result.ToArray();
    }

    /// <summary>Reconstructs display string, replacing '_' with a placeholder.</summary>
    public string ToDisplayString(char emptyPlaceholder = '_')
    {
        EnsureParsed();
        var buffer = new char[displayCharacters.Length];
        for (int i = 0; i < displayCharacters.Length; i++)
            buffer[i] = displayCharacters[i] == '_' ? emptyPlaceholder : displayCharacters[i];
        return new string(buffer);
    }

    /// <summary>Initializes or updates raw text and parses inline gap answers.</summary>
    public void SetRawSentence(string raw)
    {
        rawSentence = raw;
        ParseRawSentence();
    }

    private void EnsureParsed()
    {
        if (!isParsed)
        {
            ParseRawSentence();
        }
    }

    private void ParseRawSentence()
    {
        if (string.IsNullOrEmpty(rawSentence))
        {
            displayCharacters = Array.Empty<char>();
            gapSolutions = new Dictionary<int, char>();
            isParsed = true;
            return;
        }

        var displayList = new List<char>();
        gapSolutions = new Dictionary<int, char>();

        for (int i = 0; i < rawSentence.Length; i++)
        {
            char c = rawSentence[i];

            if (c == '_')
            {
                int gapDisplayIndex = displayList.Count;
                displayList.Add('_'); // Add gap tile to display

                // Read the immediately following character as the secret solution key
                if (i + 1 < rawSentence.Length)
                {
                    char answerChar = rawSentence[i + 1];
                    gapSolutions[gapDisplayIndex] = answerChar;
                    i++; // SKIP solution char so it never appears in displayCharacters
                }
                else
                {
                    Debug.LogError($"[SentenceData] Malformed sentence: '_' at end without answer key in: '{rawSentence}'");
                }
            }
            else
            {
                displayList.Add(c); // Regular fixed letter or space
            }
        }

        displayCharacters = displayList.ToArray();
        isParsed = true;
    }
}

/// <summary>
/// Holds one level's worth of data: an ordered sequence of sentences (stages)
/// and the pool of letters available to the player for the whole level.
/// </summary>
[CreateAssetMenu(fileName = "LevelData", menuName = "WordGame/Level Data")]
public class LevelData : ScriptableObject
{
    [Tooltip("Stages of this level, played in order.")]
    [SerializeField] private List<SentenceData> sentences = new List<SentenceData>();

    [Tooltip("All letters available to the player for this level.")]
    public char[] letters;

    public int SentenceCount => sentences?.Count ?? 0;

    public SentenceData GetSentence(int index)
    {
        if (sentences == null || index < 0 || index >= sentences.Count)
            return null;
        return sentences[index];
    }

    public IReadOnlyList<SentenceData> Sentences => sentences;

    public void AddSentence(SentenceData sentence)
    {
        if (sentence == null)
            throw new ArgumentNullException(nameof(sentence));

        sentences.Add(sentence);
    }

    /// <summary>
    /// Builds a SentenceData from inline raw string format (e.g., "TH_e C_a T_s") and adds it.
    /// </summary>
    public SentenceData AddSentence(string raw)
    {
        if (raw == null)
            throw new ArgumentNullException(nameof(raw));

        var sentence = new SentenceData();
        sentence.SetRawSentence(raw);
        sentences.Add(sentence);
        return sentence;
    }

    public void InsertSentence(int index, SentenceData sentence)
    {
        if (sentence == null)
            throw new ArgumentNullException(nameof(sentence));

        sentences.Insert(Mathf.Clamp(index, 0, sentences.Count), sentence);
    }

    public bool RemoveSentenceAt(int index)
    {
        if (sentences == null || index < 0 || index >= sentences.Count)
            return false;

        sentences.RemoveAt(index);
        return true;
    }

    public void ClearSentences()
    {
        sentences.Clear();
    }
}