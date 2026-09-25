using System;
using UnityEngine;

public static class GameSettings
{
    public static event Action Changed;
    public static bool HapticsEnabled { get => Read("Haptics"); set => Save("Haptics", value); }
    public static bool SoundEnabled { get => Read("Sound"); set => Save("Sound", value); }
    public static bool MusicEnabled { get => Read("Music"); set => Save("Music", value); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents() => Changed = null;

    private static bool Read(string name) => PlayerPrefs.GetInt("GameSettings." + name, 1) != 0;
    private static void Save(string name, bool value)
    {
        if (Read(name) == value) return;
        PlayerPrefs.SetInt("GameSettings." + name, value ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
