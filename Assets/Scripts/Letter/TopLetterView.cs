using TMPro;
using UnityEngine;

/// <summary>
/// Displays case-sensitive character sprites through LetterView when enabled.
/// Text mode displays TMPro glyphs over a static background instead.
/// Both modes retain LetterView's tile state, interaction, and factory methods.
/// </summary>
public class TopLetterView : LetterView
{
    [Header("Top Letter Display")]
    [Tooltip("Display character sprites from the inherited Sprite Library. Disable to use the text layers and a static background instead.")]
    [SerializeField] private bool useLetterSprites;

    [Tooltip("TMPro text element(s) used to display the letter glyph (e.g. a main + shadow/outline layer). The Image inherited from LetterView is left untouched and acts as the static background.")]
    [SerializeField] private TMP_Text[] letterTexts;

    [Tooltip("Text shown when the tile is empty (e.g. an empty string, an underscore, etc).")]
    [SerializeField] private string emptyText = "";

    public override void SetLetter(char letter)
    {
        if (useLetterSprites)
        {
            SetAllTexts("");
            base.SetLetter(letter);
            return;
        }

        // Intentionally does NOT call base.SetLetter - we don't want the
        // sprite-swap logic touching the (always-same) background image.
        Letter = letter;
        IsEmpty = false;

        SetAllTexts(letter.ToString());
    }

    public override void SetEmpty()
    {
        if (useLetterSprites)
        {
            SetAllTexts("");
            base.SetEmpty();
            return;
        }

        Letter = '\0';
        IsEmpty = true;

        SetAllTexts(emptyText);
    }

    private void SetAllTexts(string value)
    {
        if (letterTexts == null)
            return;

        for (int i = 0; i < letterTexts.Length; i++)
        {
            if (letterTexts[i] != null)
                letterTexts[i].text = value;
        }
    }
}
