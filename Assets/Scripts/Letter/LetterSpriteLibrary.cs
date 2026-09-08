using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds the letter -> sprite mapping for all English letters (A-Z).
/// Assign one shared instance of this asset to the LetterView prefab so every
/// tile (sentence gaps and pool letters alike) draws from the same set of images.
/// </summary>
[CreateAssetMenu(fileName = "LetterSpriteLibrary", menuName = "WordGame/Letter Sprite Library")]
public class LetterSpriteLibrary : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        [Tooltip("Single uppercase letter, e.g. 'A'.")]
        public char letter;
        public Sprite sprite;
    }

    [Tooltip("One entry per letter. Set up all 26 English letters here.")]
    [SerializeField] private Entry[] entries;

    private Dictionary<char, Sprite> lookup;

    private void BuildLookupIfNeeded()
    {
        if (lookup != null)
            return;

        lookup = new Dictionary<char, Sprite>();
        if (entries == null)
            return;

        foreach (Entry entry in entries)
        {
            char key = char.ToUpperInvariant(entry.letter);
            if (!lookup.ContainsKey(key))
                lookup.Add(key, entry.sprite);
        }
    }

    public bool TryGetSprite(char letter, out Sprite sprite)
    {
        BuildLookupIfNeeded();
        return lookup.TryGetValue(char.ToUpperInvariant(letter), out sprite);
    }
}
