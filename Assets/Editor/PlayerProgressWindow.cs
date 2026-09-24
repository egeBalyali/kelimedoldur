using UnityEditor;
using UnityEngine;

public class PlayerProgressWindow : EditorWindow
{
    private int levelNumber;
    private LevelSequenceData sequence;

    [MenuItem("WordGame/Player Progress")]
    public static void Open() => GetWindow<PlayerProgressWindow>("Player Progress");

    private void OnEnable()
    {
        sequence = AssetDatabase.LoadAssetAtPath<LevelSequenceData>("Assets/GeneratedLevels/LevelSequenceData.asset");
        ReadProgress();
        PlayerLevelProgress.Changed += ReadProgress;
    }

    private void OnDisable() => PlayerLevelProgress.Changed -= ReadProgress;
    private void ReadProgress() { levelNumber = PlayerLevelProgress.LevelNumber; Repaint(); }

    private void OnGUI()
    {
        GUILayout.Label("SAVED LEVEL", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Level 1 is the first entry in the sequence. Setting a number updates the main menu and loads that level during Play Mode.", MessageType.Info);
        sequence = (LevelSequenceData)EditorGUILayout.ObjectField("Sequence (preview)", sequence, typeof(LevelSequenceData), false);
        levelNumber = EditorGUILayout.IntField("Level number", levelNumber);
        LevelData level = sequence != null ? sequence.GetLevel(Mathf.Max(1, levelNumber) - 1) : null;
        if (level != null) EditorGUILayout.LabelField(level.name, LevelCategories.DisplayName(level.Category));
        else EditorGUILayout.HelpBox("No level at this position. Past the final entry means all levels are complete.", MessageType.Warning);
        if (GUILayout.Button("Set saved level")) PlayerLevelProgress.SetLevelNumber(levelNumber);
        if (GUILayout.Button("Reset to Level 1")) PlayerLevelProgress.ResetProgress();
        GUILayout.Label("PlayerPrefs key: LevelCount. Other preferences are preserved.", EditorStyles.wordWrappedMiniLabel);
    }
}
