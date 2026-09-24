using System;
using UnityEngine;

/// <summary>One-based sequence position; count + 1 means the sequence is finished.</summary>
public static class PlayerLevelProgress
{
    public const string LevelCountKey = "LevelCount";
    public static event Action Changed;
    public static int LevelNumber => Math.Max(1, PlayerPrefs.GetInt(LevelCountKey, 1));

    public static void SetLevelNumber(int number)
    {
        PlayerPrefs.SetInt(LevelCountKey, Math.Max(1, number));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static void ResetProgress() => SetLevelNumber(1);
}
