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
///       "category": "General Knowledge",
///       "sentences": [ "TH_e C_a T_s", "A B_i G_g D_o G_g" ]
///     }
///   ]
/// }
///
/// Rules:
/// - Each string in "sentences" becomes one SentenceData stage, in order.
///   Use '_X' where '_' is the gap and 'X' is the expected answer character.
///   Exactly two nonempty sentences are required: the whole question, then the answer.
/// - LevelData.letters is NOT read from JSON anymore. It is derived automatically from
///   every sentence's gap answer letters, in order, across all sentences in the level.
///   Duplicates are kept intentionally: each gap needs its own letter tile, so a repeated
///   letter (e.g. the A's in "ANKARA") produces multiple tile entries.
/// - A legacy "letters" field is still tolerated in the JSON for backwards compatibility,
///   but it is ignored (and a warning is logged if present) since it can go stale.
/// - Optional "letterOrder" preserves an authored order and must contain exactly the required tiles.
/// </summary>
public static class LevelDataJsonImporter
{
    private const string DefaultOutputFolder = "Assets/GeneratedLevels";

    [Serializable]
    private class LevelJson
    {
        public string name;
        public string category;
        public string difficulty;
        public string letterOrder;

        [Obsolete("No longer used. LevelData.letters is now derived automatically from the sentences' gap answers.")]
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

        // Validate the entire batch before modifying any existing assets.
        foreach (LevelJson level in parsed.levels)
        {
            if (level == null || !LevelCategories.TryParse(level.category, out _))
            {
                Debug.LogError($"LevelDataJsonImporter: invalid category on '{level?.name}'. " +
                    "Use History, Science, Sports, Music, Movies, Geography, or General Knowledge.");
                return;
            }
            if (level.sentences == null || level.sentences.Length != 2 ||
                string.IsNullOrWhiteSpace(level.sentences[0]) || string.IsNullOrWhiteSpace(level.sentences[1]))
            {
                Debug.LogError($"LevelDataJsonImporter: '{level.name}' requires exactly two pages: question and answer.");
                return;
            }
            if (!string.IsNullOrWhiteSpace(level.difficulty) &&
                (!Enum.TryParse(level.difficulty, true, out LevelDifficulty difficulty) ||
                 !Enum.IsDefined(typeof(LevelDifficulty), difficulty)))
            {
                Debug.LogError($"LevelDataJsonImporter: invalid difficulty on '{level.name}'. Use Easy, Medium, or Hard.");
                return;
            }
        }

        foreach (LevelJson level in parsed.levels)
        {
            if (level.letterOrder == null) continue;
            var required = new System.Text.StringBuilder();
            foreach (string raw in level.sentences)
            {
                var sentence = new SentenceData();
                sentence.SetRawSentence(raw);
                required.Append(sentence.GetGapLetters());
            }
            if (!LevelLetterOrder.IsValid(level.letterOrder, required.ToString()))
            {
                Debug.LogError($"LevelDataJsonImporter: '{level.name}' letterOrder must contain exactly its gap letters, including duplicates.");
                return;
            }
        }
        EnsureFolderExists(outputFolder);

        var createdLevels = new LevelData[parsed.levels.Length];

        for (int i = 0; i < parsed.levels.Length; i++)
        {
            LevelJson levelJson = parsed.levels[i];
            string levelName = string.IsNullOrEmpty(levelJson.name) ? $"Level_{i:00}" : levelJson.name;

#pragma warning disable CS0618 // legacy "letters" field is intentionally checked for a migration warning
            if (!string.IsNullOrEmpty(levelJson.letters))
            {
                Debug.LogWarning($"LevelDataJsonImporter: level '{levelName}' has a \"letters\" field in JSON, " +
                                  "but it is ignored. LevelData.letters is now derived automatically from the " +
                                  "sentences' gap answers instead.");
            }
#pragma warning restore CS0618

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
        LevelCategories.TryParse(levelJson.category, out LevelCategory category);
        levelData.SetCategory(category);
        Enum.TryParse(levelJson.difficulty, true, out LevelDifficulty difficulty);
        levelData.SetDifficulty(difficulty);
        levelData.ClearSentences();
        levelData.ClearLetters(); // letters are re-derived below from the sentences' gaps

        if (levelJson.sentences != null)
        {
            foreach (string sentenceRaw in levelJson.sentences)
            {
                if (string.IsNullOrEmpty(sentenceRaw))
                    continue;

                // Adds raw string containing '_X' syntax. LevelData automatically appends
                // each gap's answer letter (duplicates included) onto `letters` as this is
                // added, so the pool always matches what the sentences actually need.
                levelData.AddSentence(sentenceRaw);
            }
        }
        if (levelJson.letterOrder != null) levelData.letters = levelJson.letterOrder.ToCharArray();
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
