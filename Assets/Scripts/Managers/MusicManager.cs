using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip musicClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;

    private void Awake()
    {
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = volume;
        if (musicClip != null) musicSource.clip = musicClip;
        ApplySettings();
        if (musicSource.clip != null) musicSource.Play();
    }

    private void OnEnable()
    {
        GameSettings.Changed += ApplySettings;
        ApplySettings();
    }

    private void OnDisable()
    {
        GameSettings.Changed -= ApplySettings;
        if (musicSource != null) musicSource.Pause();
    }

    private void ApplySettings()
    {
        if (musicSource == null) return;
        musicSource.mute = !GameSettings.MusicEnabled;
        if (isActiveAndEnabled) musicSource.UnPause();
    }
}
