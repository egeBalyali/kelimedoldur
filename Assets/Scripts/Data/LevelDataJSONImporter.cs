#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only tool that imports levels from a JSON design file and generates
/// LevelData assets (plus an optional LevelSequenceData referencing them all).
///
/// JSON schema (see example_levels.json):
/// {
///   "levels": [
///     {
///       "name": "Level_01",
///       "letters": "ABCDEFGH",
///       "sentences": [ "TH_e C_a T_s", "A B_i G_g D_o G_g" ]
///     }
///   ]
/// }
///
/// Rules:
/// - "letters" is a flat string; each character becomes one entry in LevelData.letters.
/// - Each string in "sentences" becomes one SentenceData stage, in order.
///   Use '_X' where '_' is the gap and 'X' is the expected answer character.
/// </summary>
public static class LevelDataJsonImporter
{
    private const string DefaultOutputFolder = "Assets/GeneratedLevels";

    [Serializable]
    private class LevelJson
    {
        public string name;
        public string letters;
        public string[] sentences;
    }

    [Serializable]
    private class LevelListJson
    {
        public LevelJson[] levels;
    }

    [MenuItem("WordGame/Import Levels From JSON...")]
    public static void ImportFromJsonMenuItem()
    {
        string path = EditorUtility.OpenFilePanel("Select Level Design JSON", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path))
            return;

        string json = File.ReadAllText(path);
        ImportFromJson(json, DefaultOutputFolder, buildSequenceAsset: true);
    }

    /// <summary>
    /// Parses the given JSON text and creates/updates LevelData assets in outputFolder.
    /// If buildSequenceAsset is true, also creates/updates a LevelSequenceData asset
    /// referencing every imported level, in file order.
    /// </summary>
    public static void ImportFromJson(string json, string outputFolder, bool buildSequenceAsset)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("LevelDataJsonImporter: empty JSON input.");
            return;
        }

        LevelListJson parsed;
        try
        {
            parsed = JsonUtility.FromJson<LevelListJson>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"LevelDataJsonImporter: failed to parse JSON. {e.Message}");
            return;
        }

        if (parsed?.levels == null || parsed.levels.Length == 0)
        {
            Debug.LogWarning("LevelDataJsonImporter: no levels found in JSON.");
            return;
        }

        EnsureFolderExists(outputFolder);

        var createdLevels = new LevelData[parsed.levels.Length];

        for (int i = 0; i < parsed.levels.Length; i++)
        {
            LevelJson levelJson = parsed.levels[i];
            string levelName = string.IsNullOrEmpty(levelJson.name) ? $"Level_{i:00}" : levelJson.name;

            LevelData levelData = CreateOrLoadLevelAsset(outputFolder, levelName);
            PopulateLevelData(levelData, levelJson);

            EditorUtility.SetDirty(levelData);
            createdLevels[i] = levelData;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (buildSequenceAsset)
        {
            BuildOrUpdateSequenceAsset(outputFolder, createdLevels);
        }

        Debug.Log($"LevelDataJsonImporter: imported {createdLevels.Length} level(s) into '{outputFolder}'.");
    }

    private static LevelData CreateOrLoadLevelAsset(string folder, string levelName)
    {
        string assetPath = $"{folder}/{levelName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
        if (existing != null)
            return existing;

        var newAsset = ScriptableObject.CreateInstance<LevelData>();
        AssetDatabase.CreateAsset(newAsset, assetPath);
        return newAsset;
    }

    private static void PopulateLevelData(LevelData levelData, LevelJson levelJson)
    {
        levelData.ClearSentences();

        levelData.letters = string.IsNullOrEmpty(levelJson.letters)
            ? Array.Empty<char>()
            : levelJson.letters.ToCharArray();

        if (levelJson.sentences != null)
        {
            foreach (string sentenceRaw in levelJson.sentences)
            {
                if (string.IsNullOrEmpty(sentenceRaw))
                    continue;

                // Adds raw string containing '_X' syntax directly to LevelData
                levelData.AddSentence(sentenceRaw);
            }
        }
    }

    private static void BuildOrUpdateSequenceAsset(string folder, LevelData[] levels)
    {
        string assetPath = $"{folder}/LevelSequenceData.asset";
        var sequence = AssetDatabase.LoadAssetAtPath<LevelSequenceData>(assetPath);

        if (sequence == null)
        {
            sequence = ScriptableObject.CreateInstance<LevelSequenceData>();
            AssetDatabase.CreateAsset(sequence, assetPath);
        }

        sequence.levels = levels;
        EditorUtility.SetDirty(sequence);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureFolderExists(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string[] parts = folder.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif