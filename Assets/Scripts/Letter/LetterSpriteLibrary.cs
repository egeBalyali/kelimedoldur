using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds character -> sprite mappings for uppercase, lowercase, and digits.
/// Assign one shared instance of this asset to the LetterView prefab so every
/// tile (sentence gaps and pool letters alike) draws from the same set of images.
/// </summary>
[CreateAssetMenu(fileName = "LetterSpriteLibrary", menuName = "WordGame/Letter Sprite Library")]
public class LetterSpriteLibrary : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        [Tooltip("Single character, e.g. 'A', 'a', or '0'. Uppercase and lowercase can have different sprites.")]
        public char letter;
        public Sprite sprite;
    }

    [Tooltip("One entry per character. Assign separate sprites for A-Z, a-z, and 0-9 (and any other characters needed).")]
    [SerializeField] private Entry[] entries;

    private Dictionary<char, Sprite> lookup;

    private void OnValidate()
    {
        lookup = null;
    }

    private void OnEnable()
    {
        lookup = null;
    }

    private void BuildLookupIfNeeded()
    {
        if (lookup != null)
            return;

        lookup = new Dictionary<char, Sprite>();
        if (entries == null)
            return;

        foreach (Entry entry in entries)
        {
            char key = entry.letter;
            if (entry.sprite != null && !lookup.ContainsKey(key))
                lookup.Add(key, entry.sprite);
        }
    }

    public bool TryGetSprite(char letter, out Sprite sprite)
    {
        BuildLookupIfNeeded();
        if (lookup.TryGetValue(letter, out sprite))
            return true;

        // Keep existing single-case libraries usable until both sets are assigned.
        char alternate = char.IsUpper(letter)
            ? char.ToLowerInvariant(letter)
            : char.ToUpperInvariant(letter);
        return alternate != letter && lookup.TryGetValue(alternate, out sprite);
    }
}
