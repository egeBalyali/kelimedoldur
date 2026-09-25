using UnityEngine;

/// <summary>Scene-local, non-spatial feedback for correctly completed question words.</summary>
[DisallowMultipleComponent]
public class SoundManager : MonoBehaviour
{
    [SerializeField] private SentenceView sentenceView;
    [SerializeField] private AudioClip correctWordClip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private bool muted;

    private AudioSource effectsSource;

    private void Awake()
    {
        // Own a dedicated source so these settings never change another sound source.
        effectsSource = gameObject.AddComponent<AudioSource>();
        effectsSource.playOnAwake = false;
        effectsSource.loop = false;
        effectsSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        GameSettings.Changed += ApplySettings;
        ApplySettings();
        if (sentenceView == null) sentenceView = GetComponent<SentenceView>();
        if (sentenceView != null) sentenceView.CorrectWordTraceStarted += PlayCorrectWord;
    }

    private void OnDisable()
    {
        GameSettings.Changed -= ApplySettings;
        if (sentenceView != null) sentenceView.CorrectWordTraceStarted -= PlayCorrectWord;
        if (effectsSource != null) effectsSource.Stop();
    }

    private void OnDestroy()
    {
        if (effectsSource != null) Destroy(effectsSource);
    }

    public void SetMuted(bool value)
    {
        GameSettings.SoundEnabled = !value;
    }

    private void ApplySettings()
    {
        if (effectsSource != null) effectsSource.mute = muted || !GameSettings.SoundEnabled;
    }

    public void SetVolume(float value) => volume = Mathf.Clamp01(value);

    public void PlayCorrectWord()
    {
        if (!isActiveAndEnabled || muted || !GameSettings.SoundEnabled || correctWordClip == null || effectsSource == null) return;
        effectsSource.PlayOneShot(correctWordClip, volume);
    }
}
