using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingsMenuController : MonoBehaviour
{
    [SerializeField] private Button openButton;
    [SerializeField] private Canvas settingsCanvas;
    [SerializeField] private Image panel;
    [SerializeField] private Image closeImage;
    [SerializeField] private Image hapticImage;
    [SerializeField] private Image soundImage;
    [SerializeField] private Image musicImage;
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;
    private Button closeButton, hapticButton, soundButton, musicButton, outsideButton;

    private void Awake()
    {
        closeButton = Bind(closeImage, Close);
        hapticButton = Bind(hapticImage, ToggleHaptics);
        soundButton = Bind(soundImage, ToggleSound);
        musicButton = Bind(musicImage, ToggleMusic);
        if (onSprite == null && hapticImage != null) onSprite = hapticImage.sprite;
        if (panel != null) panel.raycastTarget = true;
        if (settingsCanvas != null)
        {
            var outside = new GameObject("OutsideClickToClose", typeof(RectTransform), typeof(Image), typeof(Button));
            outside.layer = settingsCanvas.gameObject.layer;
            var rect = (RectTransform)outside.transform;
            rect.SetParent(settingsCanvas.transform, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = outside.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.4f);
            outsideButton = Bind(image, Close);
            outsideButton.transition = Selectable.Transition.None;
        }
        if (openButton != null) openButton.onClick.AddListener(Open);
        Close();
    }

    private void OnEnable()
    {
        GameSettings.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameSettings.Changed -= Refresh;
        Close();
    }

    private void OnDestroy()
    {
        if (openButton != null) openButton.onClick.RemoveListener(Open);
        Unbind(closeButton, Close);
        Unbind(hapticButton, ToggleHaptics);
        Unbind(soundButton, ToggleSound);
        Unbind(musicButton, ToggleMusic);
        if (outsideButton != null) Destroy(outsideButton.gameObject);
    }

    public void Open()
    {
        Refresh();
        if (settingsCanvas != null) settingsCanvas.gameObject.SetActive(true);
    }

    public void Close()
    {
        if (settingsCanvas != null) settingsCanvas.gameObject.SetActive(false);
    }

    private void ToggleHaptics() => GameSettings.HapticsEnabled = !GameSettings.HapticsEnabled;
    private void ToggleSound() => GameSettings.SoundEnabled = !GameSettings.SoundEnabled;
    private void ToggleMusic() => GameSettings.MusicEnabled = !GameSettings.MusicEnabled;
    private void Refresh()
    {
        SetState(hapticImage, GameSettings.HapticsEnabled);
        SetState(soundImage, GameSettings.SoundEnabled);
        SetState(musicImage, GameSettings.MusicEnabled);
    }

    private void SetState(Image image, bool enabled)
    {
        if (image == null) return;
        image.sprite = !enabled && offSprite != null ? offSprite : onSprite;
        // Visible fallback until the OFF artwork is assigned.
        image.color = !enabled && offSprite == null ? Color.gray : Color.white;
    }

    private static Button Bind(Image image, UnityAction action)
    {
        if (image == null) return null;
        image.raycastTarget = true;
        Button button = image.GetComponent<Button>();
        if (button == null) button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(action);
        return button;
    }

    private static void Unbind(Button button, UnityAction action)
    {
        if (button != null) button.onClick.RemoveListener(action);
    }
}
