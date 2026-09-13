using TMPro;
using UnityEngine;

/// <summary>
/// Variant of LetterView used for tiles where the background image is always
/// the same (whatever is assigned to the Image field on the base LetterView -
/// it's never swapped per letter) and the letter glyph itself is rendered with
/// a TMPro text element instead of a per-letter sprite from LetterSpriteLibrary.
///
/// All existing calling code that works with LetterView - Create, CreateEmpty,
/// SetLetter, SetEmpty, SetActive, SetInteractable, SetVisible, the Clicked
/// event, Letter/IsEmpty - keeps working exactly as before. Nothing needed to
/// be renamed; just point the prefab reference at a TopLetterView prefab
/// (with letterText assigned) instead of a plain LetterView prefab.
/// </summary>
public class TopLetterView : LetterView
{
    [Header("Top Letter Display")]
    [Tooltip("TMPro text used to display the letter glyph. The Image inherited from LetterView is left untouched and acts as the static background.")]
    [SerializeField] private TMP_Text letterText;

    [Tooltip("Text shown when the tile is empty (e.g. an empty string, an underscore, etc).")]
    [SerializeField] private string emptyText = "";

    public override void SetLetter(char letter)
    {
        // Intentionally does NOT call base.SetLetter - we don't want the
        // sprite-swap logic touching the (always-same) background image.
        Letter = letter;
        IsEmpty = false;

        if (letterText != null)
            letterText.text = letter.ToString();
    }

    public override void SetEmpty()
    {
        Letter = '\0';
        IsEmpty = true;

        if (letterText != null)
            letterText.text = emptyText;
    }
}