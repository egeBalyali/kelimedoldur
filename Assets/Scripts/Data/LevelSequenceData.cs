using UnityEngine;

/// <summary>
/// Ordered collection of LevelData assets, representing the full game/level sequence.
/// </summary>
[CreateAssetMenu(fileName = "LevelSequenceData", menuName = "WordGame/Level Sequence Data")]
public class LevelSequenceData : ScriptableObject
{
    [Tooltip("Levels in play order.")]
    public LevelData[] levels;

    public int LevelCount => levels?.Length ?? 0;

    public LevelData GetLevel(int index)
    {
        if (levels == null || index < 0 || index >= levels.Length)
            return null;
        return levels[index];
    }
}