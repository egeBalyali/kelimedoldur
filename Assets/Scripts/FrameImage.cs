using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FrameImage : MonoBehaviour
{
    [Header("Frames")]
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite firstFrame;
    [SerializeField] private Sprite secondFrame;

    [Header("Timing")]
    [Tooltip("Total repeating cycle, including the flicker at the end.")]
    [Min(0.1f)] [SerializeField] private float cycleDuration = 5f;
    [Tooltip("Time each frame stays visible during the steady loop.")]
    [Min(0.01f)] [SerializeField] private float loopInterval = 0.5f;
    [Tooltip("Flicker at the end of each cycle, capped at one second.")]
    [Range(0f, 1f)] [SerializeField] private float flickerDuration = 1f;
    [Tooltip("Random time between frame swaps during the flicker.")]
    [SerializeField] private Vector2 flickerInterval = new Vector2(0.03f, 0.1f);
    [SerializeField] private bool useUnscaledTime = true;

    private float elapsed;
    private float nextFlickerTime;
    private bool isFlickering;
    private bool showingSecondFrame;

    private void Reset()
    {
        targetImage = GetComponent<Image>();
    }

    private void OnValidate()
    {
        cycleDuration = Mathf.Max(0.1f, cycleDuration);
        loopInterval = Mathf.Max(0.01f, loopInterval);
        flickerDuration = Mathf.Clamp(flickerDuration, 0f, Mathf.Min(1f, cycleDuration));
        flickerInterval.x = Mathf.Max(0.01f, flickerInterval.x);
        flickerInterval.y = Mathf.Max(flickerInterval.x, flickerInterval.y);
    }

    private void OnEnable()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        elapsed = 0f;
        isFlickering = false;
        ShowFrame(false);
    }

    private void OnDisable()
    {
        ShowFrame(false);
    }

    private void Update()
    {
        if (targetImage == null || firstFrame == null || secondFrame == null)
            return;

        float duration = Mathf.Max(0.1f, cycleDuration);
        float flickerStart = duration - Mathf.Clamp(flickerDuration, 0f, Mathf.Min(1f, duration));
        elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        if (elapsed >= duration)
        {
            elapsed %= duration;
            isFlickering = false;
            ShowFrame(false);
        }

        if (elapsed < flickerStart)
        {
            isFlickering = false;
            ShowFrame(Mathf.FloorToInt(elapsed / Mathf.Max(0.01f, loopInterval)) % 2 != 0);
            return;
        }

        if (!isFlickering || elapsed >= nextFlickerTime)
        {
            isFlickering = true;
            ShowFrame(!showingSecondFrame);
            float minimum = Mathf.Max(0.01f, flickerInterval.x);
            nextFlickerTime = elapsed + Random.Range(minimum, Mathf.Max(minimum, flickerInterval.y));
        }
    }

    private void ShowFrame(bool second)
    {
        showingSecondFrame = second;
        Sprite frame = second ? secondFrame : firstFrame;
        if (targetImage != null && frame != null && targetImage.sprite != frame)
            targetImage.sprite = frame;
    }
}
