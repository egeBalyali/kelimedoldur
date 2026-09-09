using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A single letter tile, shown as an Image using a dedicated sprite per letter
/// (via LetterSpriteLibrary) rather than rendered text. Purely a view + click
/// source - it does not know whether it represents a sentence gap or a pool letter.
///
/// Create instances via the static factory methods rather than Instantiate + manual setup.
/// The prefab must have spriteLibrary assigned (shared across all instances).
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class LetterView : MonoBehaviour
{
    [SerializeField] private Image letterImage;
    [SerializeField] private Button button;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject activeIndicator; // e.g. an outline/glow, enabled while this is the active gap

    [Header("Effects")]
    [Tooltip("Particle system prefab instantiated on click.")]
    [SerializeField] private GameObject clickParticlePrefab;

    [Tooltip("Shared letter -> sprite mapping. Assign once on the prefab.")]
    [SerializeField] private LetterSpriteLibrary spriteLibrary;

    [Tooltip("Optional sprite shown for an empty gap slot (e.g. a faded/dashed placeholder). Leave empty to just hide the image instead.")]
    [SerializeField] private Sprite emptySprite;

    public char Letter { get; private set; }
    public bool IsEmpty { get; private set; } = true;

    /// <summary>Raised whenever this tile is clicked, passing itself along.</summary>
    public event Action<LetterView> Clicked;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        button.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        SpawnClickParticle();
        Clicked?.Invoke(this);
    }

    private void SpawnClickParticle()
    {
        if (clickParticlePrefab == null)
            return;

        GameObject particleInstance = Instantiate(clickParticlePrefab, transform.position, Quaternion.identity, transform.parent.parent.parent);

        if (particleInstance.TryGetComponent<ParticleSystem>(out var ps))
        {
            float duration = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(particleInstance, duration);
        }
        else
        {
            Destroy(particleInstance, 2f);
        }
    }

    /// <summary>Instantiates a tile already showing a letter (e.g. a fixed sentence letter or a pool letter).</summary>
    public static LetterView Create(LetterView prefab, Transform parent, char letter)
    {
        LetterView instance = Instantiate(prefab, parent);
        instance.SetLetter(letter);
        return instance;
    }

    /// <summary>Instantiates an empty tile (e.g. a sentence gap waiting to be filled).</summary>
    public static LetterView CreateEmpty(LetterView prefab, Transform parent)
    {
        LetterView instance = Instantiate(prefab, parent);
        instance.SetEmpty();
        return instance;
    }

    public void SetLetter(char letter)
    {
        Letter = letter;
        IsEmpty = false;

        if (letterImage == null)
            return;

        if (spriteLibrary != null && spriteLibrary.TryGetSprite(letter, out Sprite sprite))
        {
            letterImage.sprite = sprite;
            letterImage.enabled = true;
        }
        else
        {
            Debug.LogWarning($"LetterView: no sprite found for letter '{letter}'.");
            letterImage.enabled = false;
        }
    }

    public void SetEmpty()
    {
        Letter = '\0';
        IsEmpty = true;

        if (letterImage == null)
            return;

        if (emptySprite != null)
        {
            letterImage.sprite = emptySprite;
            letterImage.enabled = true;
        }
        else
        {
            letterImage.sprite = null;
            letterImage.enabled = false;
        }
    }

    public void SetActive(bool isActive)
    {
        if (activeIndicator != null)
            activeIndicator.SetActive(isActive);
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }
    }

    /// <summary>
    /// Controls visual opacity via CanvasGroup. Pass false to hide the tile 
    /// while preserving its physical layout size and position.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = visible ? 1f : 0f;
    }
}