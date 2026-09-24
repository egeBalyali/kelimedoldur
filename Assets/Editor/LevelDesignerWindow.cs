using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class LevelDesignerWindow : EditorWindow
{
    [SerializeField] private LevelData source;
    [SerializeField] private string levelName = "New Level";
    [SerializeField] private LevelCategory category;
    [SerializeField] private LevelDifficulty difficulty;
    [SerializeField] private List<LevelAuthoringSentence> stages = new List<LevelAuthoringSentence>();
    [SerializeField] private int selected;
    [SerializeField] private LevelSequenceData sequence;
    [SerializeField] private bool addToSequence = true;
    [SerializeField] private int columns = 8;
    [SerializeField] private int maxLines = 8;
    [SerializeField] private bool reveal;
    [SerializeField] private LetterSpriteLibrary spriteLibrary;
    [SerializeField] private string savedState;
    [SerializeField] private string poolOrder = "";
    [SerializeField] private int selectedPoolTile = -1;
    private int moveToPosition = 1;
    private Vector2 editScroll, previewScroll;

    [Serializable] private class JsonLevel
    {
        public string name;
        public string category;
        public string difficulty;
        public string letterOrder;
        public string[] sentences;
    }
    [Serializable] private class JsonFile { public JsonLevel[] levels; }

    [MenuItem("WordGame/Level Designer")]
    public static void Open() => GetWindow<LevelDesignerWindow>("Level Designer");

    [OnOpenAsset]
    private static bool OnOpenAsset(int id, int line)
    {
        var level = EditorUtility.InstanceIDToObject(id) as LevelData;
        if (level == null) return false;
        var window = GetWindow<LevelDesignerWindow>("Level Designer");
        if (window.CanReplaceDraft()) window.Load(level);
        return true;
    }

    private void OnEnable()
    {
        minSize = new Vector2(900, 650);
        saveChangesMessage = "Save this level draft before closing?";
        Undo.undoRedoPerformed += OnUndoRedo;
        EnsureTwoPages();
        if (sequence == null)
            sequence = AssetDatabase.LoadAssetAtPath<LevelSequenceData>("Assets/GeneratedLevels/LevelSequenceData.asset");
        if (savedState == null) savedState = Snapshot();
        RefreshDirty();
    }

    private void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;
    private void OnUndoRedo()
    {
        // Undo records the whole window, so restore the saved baseline independently.
        if (source != null)
        {
            var serialized = new SerializedObject(source);
            var list = serialized.FindProperty("sentences");
            var raw = new string[list.arraySize];
            for (int i = 0; i < raw.Length; i++)
                raw[i] = list.GetArrayElementAtIndex(i).FindPropertyRelative("rawSentence").stringValue;
            savedState = JsonUtility.ToJson(new JsonFile { levels = new[] { new JsonLevel
            {
                name = source.name, category = LevelCategories.DisplayName(source.Category),
                difficulty = source.Difficulty.ToString(), sentences = raw,
                letterOrder = LevelLetterOrder.Reconcile(new string(source.letters ?? Array.Empty<char>()),
                    string.Concat(source.Sentences.SelectMany(s => s.GetGapLetters())))
            } } });
        }
        RefreshDirty();
        Repaint();
    }
    private string Snapshot() => JsonUtility.ToJson(new JsonFile { levels = new[] { ToJsonLevel() } });
    private JsonLevel ToJsonLevel() => new JsonLevel
    {
        name = levelName, category = LevelCategories.DisplayName(category),
        difficulty = difficulty.ToString(),
        letterOrder = LevelLetterOrder.Reconcile(poolOrder, RequiredLetters()),
        sentences = stages.Select(stage => stage.ToRaw()).ToArray()
    };
    private void RefreshDirty()
    {
        hasUnsavedChanges = Snapshot() != savedState;
    }
    private void Record(string action) => Undo.RecordObject(this, action);

    public override void SaveChanges()
    {
        if (Save(false)) base.SaveChanges();
    }

    private bool CanReplaceDraft()
    {
        if (!hasUnsavedChanges) return true;
        int choice = EditorUtility.DisplayDialogComplex("Unsaved level", "Save your current draft first?",
            "Save", "Cancel", "Discard");
        return choice == 2 || (choice == 0 && Save(false));
    }

    private void Load(LevelData level)
    {
        var data = new SerializedObject(level);
        var sentences = data.FindProperty("sentences");
        var loaded = new List<LevelAuthoringSentence>();
        try
        {
            for (int i = 0; i < sentences.arraySize; i++)
                loaded.Add(LevelAuthoringSentence.FromRaw(sentences.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("rawSentence").stringValue ?? ""));
        }
        catch (FormatException error)
        {
            EditorUtility.DisplayDialog("Cannot open level", error.Message, "OK");
            return;
        }
        Undo.ClearUndo(this);
        source = level;
        levelName = level.name;
        category = level.Category;
        difficulty = level.Difficulty;
        stages = loaded;
        EnsureTwoPages();
        poolOrder = LevelLetterOrder.Reconcile(new string(level.letters ?? Array.Empty<char>()), RequiredLetters());
        selectedPoolTile = -1;
        selected = 0;
        savedState = Snapshot();
        RefreshDirty();
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(45)) && CanReplaceDraft())
            {
                Undo.ClearUndo(this);
                source = null;
                levelName = "New Level";
                category = LevelCategory.GeneralKnowledge;
                difficulty = LevelDifficulty.Easy;
                stages = new List<LevelAuthoringSentence> { new LevelAuthoringSentence(), new LevelAuthoringSentence() };
                poolOrder = "";
                selectedPoolTile = -1;
                selected = 0;
                savedState = Snapshot();
            }
            var next = (LevelData)EditorGUILayout.ObjectField(source, typeof(LevelData), false, GUILayout.Width(230));
            if (next != null && next != source && CanReplaceDraft()) Load(next);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save level", EditorStyles.toolbarButton)) Save(false);
            if (GUILayout.Button("Save as new", EditorStyles.toolbarButton)) Save(true);
            if (GUILayout.Button("Export JSON", EditorStyles.toolbarButton)) Export();
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.53f))) DrawEditor();
            using (new EditorGUILayout.VerticalScope()) DrawPreview();
        }
        RefreshDirty();
    }

    private void DrawEditor()
    {
        editScroll = EditorGUILayout.BeginScrollView(editScroll);
        GUILayout.Label("BUILD YOUR LEVEL", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        string nameValue;
        using (new EditorGUI.DisabledScope(source != null))
            nameValue = EditorGUILayout.TextField("Level name", levelName);
        var categoryValue = (LevelCategory)EditorGUILayout.EnumPopup("Category", category);
        var difficultyValue = (LevelDifficulty)EditorGUILayout.EnumPopup("Difficulty", difficulty);
        if (EditorGUI.EndChangeCheck()) { Record("Edit level details"); levelName = nameValue; category = categoryValue; difficulty = difficultyValue; }
        GUILayout.Space(10);
        EditorGUILayout.HelpBox("Every level has two pages: one complete question, then its answer. Type normally and click letters to make blanks.", MessageType.Info);
        for (int i = 0; i < stages.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string label = i == 0 ? "Question" : "Answer";
                if (GUILayout.Toggle(selected == i, label + "  ·  " + stages[i].text, "Button")) selected = i;
            }
        }
        selected = Mathf.Clamp(selected, 0, stages.Count - 1);
        var stage = stages[selected];
        GUILayout.Space(12);
        GUILayout.Label(selected == 1 ? "ANSWER TEXT" : "QUESTION TEXT", EditorStyles.boldLabel);
        string text = EditorGUILayout.TextField(stage.text, GUILayout.Height(28));
        if (text != stage.text) { Record("Edit sentence text"); stage.SetText(text); }
        GUILayout.Label("Click tiles to hide/reveal letters. Blue = blank in the puzzle.", EditorStyles.wordWrappedMiniLabel);
        DrawTiles(stage, Mathf.Max(6, (int)((position.width * 0.53f - 35) / 34)), true, false);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Hide all letters")) SetGaps(stage, true);
            if (GUILayout.Button("Reveal all")) SetGaps(stage, false);
        }
        GUILayout.Label($"{stage.GapCount} blanks on this page · {stages.Sum(s => s.GapCount)} tiles in the level pool", EditorStyles.miniLabel);
        GUILayout.Space(12);
        sequence = (LevelSequenceData)EditorGUILayout.ObjectField("Level sequence", sequence, typeof(LevelSequenceData), false);
        addToSequence = EditorGUILayout.Toggle("Include on save", addToSequence);
        GUILayout.Label("Use the pool controls in the preview to shuffle or reorder tiles. Saving keeps that order.", EditorStyles.wordWrappedMiniLabel);
        foreach (string issue in Issues()) EditorGUILayout.HelpBox(issue, MessageType.Warning);
        EditorGUILayout.EndScrollView();
    }

    private void EnsureTwoPages()
    {
        if (stages.Count > 2)
        {
            // Opening a legacy asset combines its question pages without losing chosen blanks.
            string question = string.Join(" ", stages.Take(stages.Count - 1).Select(s => s.ToRaw().Trim()));
            stages = new List<LevelAuthoringSentence>
                { LevelAuthoringSentence.FromRaw(question), stages.Last() };
        }
        while (stages.Count < 2) stages.Add(new LevelAuthoringSentence());
        selected = Mathf.Clamp(selected, 0, 1);
    }

    private void SetGaps(LevelAuthoringSentence stage, bool hide)
    {
        Record("Choose blank letters");
        for (int i = 0; i < stage.text.Length; i++) stage.hidden[i] = hide && char.IsLetterOrDigit(stage.text[i]);
    }

    private void DrawPreview()
    {
        poolOrder = LevelLetterOrder.Reconcile(poolOrder, RequiredLetters());
        previewScroll = EditorGUILayout.BeginScrollView(previewScroll);
        GUILayout.Label("PLAYER PREVIEW", EditorStyles.boldLabel);
        reveal = EditorGUILayout.Toggle("Show solutions", reveal);
        columns = EditorGUILayout.IntSlider("Characters per line", columns, 6, 24);
        maxLines = EditorGUILayout.IntSlider("Maximum lines", maxLines, 1, 10);
        spriteLibrary = (LetterSpriteLibrary)EditorGUILayout.ObjectField("Letter artwork", spriteLibrary, typeof(LetterSpriteLibrary), false);
        if (GUILayout.Button("Read layout from open scene"))
        {
            var view = FindFirstObjectByType<SentenceView>();
            if (view != null)
            {
                var settings = new SerializedObject(view);
                columns = Mathf.Max(1, settings.FindProperty("maxCharsPerLine").intValue);
                maxLines = Mathf.Max(1, settings.FindProperty("maxLines").intValue);
            }
            else ShowNotification(new GUIContent("Open a game scene with a SentenceView first."));
        }
        EditorGUILayout.HelpBox("Layout preview only. Uses the game's word-wrap rules; artwork, spacing and screen proportions may differ. These controls do not change the scene.", MessageType.None);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label(LevelCategories.DisplayName(category), EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label($"Page {selected + 1} / {stages.Count}" + (selected == stages.Count - 1 ? " · ANSWER" : ""), EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(18);
            DrawTiles(stages[selected], columns, false, true);
            GUILayout.Space(24);
            GUILayout.Label("LETTER POOL · ENTIRE LEVEL", EditorStyles.centeredGreyMiniLabel);
            DrawPoolControls();
            string pool = poolOrder;
            int rows = pool.Length < 5 ? 1 : pool.Length <= 10 ? 2 : 3;
            int offset = 0;
            for (int row = 0; row < rows; row++)
            {
                int count = pool.Length / rows + (row < pool.Length % rows ? 1 : 0);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    float size = Mathf.Clamp((position.width * .47f - 60) / Mathf.Max(1, count) - 4, 14, 38);
                    for (int i = 0; i < count; i++)
                    {
                        int index = offset + i;
                        Color old = GUI.backgroundColor;
                        if (selectedPoolTile == index) GUI.backgroundColor = new Color(.3f, .7f, 1f);
                        Sprite artwork = null;
                        bool hasArt = spriteLibrary != null && spriteLibrary.TryGetSprite(pool[index], out artwork);
                        if (GUILayout.Button(new GUIContent(hasArt ? "" : pool[index].ToString(), $"Tile {index + 1}: click to select"), GUILayout.Width(size), GUILayout.Height(36)))
                        {
                            selectedPoolTile = index;
                            moveToPosition = index + 1;
                        }
                        GUI.backgroundColor = old;
                        if (hasArt && Event.current.type == EventType.Repaint)
                        {
                            Rect rect = GUILayoutUtility.GetLastRect();
                            Rect uv = artwork.rect;
                            uv.x /= artwork.texture.width; uv.width /= artwork.texture.width;
                            uv.y /= artwork.texture.height; uv.height /= artwork.texture.height;
                            GUI.DrawTextureWithTexCoords(new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 6), artwork.texture, uv);
                        }
                    }
                    GUILayout.FlexibleSpace();
                }
                offset += count;
            }
            GUILayout.Space(10);
            GUILayout.Label("Click a tile, then move it. Order reads left to right, top to bottom.", EditorStyles.wordWrappedMiniLabel);
        }
        EditorGUILayout.EndScrollView();
    }

    private string RequiredLetters() => string.Concat(stages.SelectMany(s => s.text.Where((c, i) => s.hidden[i])));

    private void DrawPoolControls()
    {
        using (new EditorGUI.DisabledScope(poolOrder.Length < 2))
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Shuffle"))
            {
                Record("Shuffle letter pool");
                poolOrder = LevelLetterOrder.Shuffle(poolOrder, new System.Random());
                selectedPoolTile = -1;
            }
            if (GUILayout.Button("Question / answer order"))
            {
                Record("Restore letter pool order"); poolOrder = RequiredLetters(); selectedPoolTile = -1;
            }
        }
        selectedPoolTile = Mathf.Clamp(selectedPoolTile, -1, poolOrder.Length - 1);
        using (new EditorGUI.DisabledScope(selectedPoolTile < 0))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(selectedPoolTile <= 0))
                    if (GUILayout.Button("Move left")) MovePoolTile(selectedPoolTile - 1);
                using (new EditorGUI.DisabledScope(selectedPoolTile >= poolOrder.Length - 1))
                    if (GUILayout.Button("Move right")) MovePoolTile(selectedPoolTile + 1);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Position", GUILayout.Width(55));
                moveToPosition = EditorGUILayout.IntField(moveToPosition, GUILayout.Width(55));
                if (GUILayout.Button("Move to")) MovePoolTile(Mathf.Clamp(moveToPosition - 1, 0, poolOrder.Length - 1));
            }
        }
    }

    private void MovePoolTile(int destination)
    {
        if (selectedPoolTile < 0 || destination < 0 || destination >= poolOrder.Length) return;
        Record("Move letter tile");
        poolOrder = LevelLetterOrder.Move(poolOrder, selectedPoolTile, destination);
        selectedPoolTile = destination;
        moveToPosition = destination + 1;
    }

    private void DrawTiles(LevelAuthoringSentence stage, int width, bool clickable, bool limitLines)
    {
        var lines = stage.Wrap(width);
        float available = position.width * (clickable ? 0.53f : 0.47f) - 50;
        int widest = lines.Max(line => line.Count);
        float size = Mathf.Clamp(available / Mathf.Max(width, widest) - 4, 10, 34);
        for (int row = 0; row < lines.Count; row++)
        {
            if (limitLines && row >= maxLines)
            {
                EditorGUILayout.HelpBox($"{lines.Count - maxLines} more line(s) would be cut off in the game.", MessageType.Error);
                break;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                foreach (int index in lines[row])
                {
                    char letter = stage.text[index];
                    if (letter == ' ') { GUILayout.Space(size * 0.55f); continue; }
                    Rect rect = GUILayoutUtility.GetRect(size, size + 6, GUILayout.Width(size));
                    Color old = GUI.backgroundColor;
                    GUI.backgroundColor = stage.hidden[index] ? new Color(0.3f, 0.7f, 1f) : Color.white;
                    bool blank = stage.hidden[index] && !clickable && !reveal;
                    string label = blank ? "_" : letter.ToString();
                    bool art = !clickable && !blank && spriteLibrary != null && spriteLibrary.TryGetSprite(letter, out _);
                    if (GUI.Button(rect, new GUIContent(art ? "" : label, stage.hidden[index] ? "Blank: " + letter : "Visible: " + letter)) &&
                        clickable && char.IsLetterOrDigit(letter))
                    {
                        Record("Toggle blank letter"); stage.hidden[index] = !stage.hidden[index];
                    }
                    GUI.backgroundColor = old;
                    if (art && Event.current.type == EventType.Repaint && spriteLibrary.TryGetSprite(letter, out Sprite sprite))
                    {
                        Rect uv = sprite.rect;
                        uv.x /= sprite.texture.width; uv.width /= sprite.texture.width;
                        uv.y /= sprite.texture.height; uv.height /= sprite.texture.height;
                        GUI.DrawTextureWithTexCoords(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), sprite.texture, uv);
                    }
                }
                GUILayout.FlexibleSpace();
            }
        }
    }

    private List<string> Issues()
    {
        var issues = new List<string>();
        if (string.IsNullOrWhiteSpace(levelName)) issues.Add("Enter a level name.");
        for (int i = 0; i < stages.Count; i++)
        {
            var s = stages[i];
            if (string.IsNullOrWhiteSpace(s.text)) issues.Add($"Page {i + 1} is empty.");
            if (s.text.Any(c => c == '_' || char.IsControl(c))) issues.Add($"Page {i + 1}: use ordinary text without underscores, tabs or line breaks.");
            if (s.Wrap(columns).Count > maxLines) issues.Add($"Page {i + 1} exceeds the preview's line limit. Shorten its text to fit one page.");
            if (s.text.Split(' ').Any(word => word.Length > columns)) issues.Add($"Page {i + 1} has a word wider than the preview's line budget.");
        }
        if (stages[stages.Count - 1].GapCount == 0) issues.Add("Choose at least one blank on the final answer page.");
        return issues;
    }

    private bool ValidContent()
    {
        if (stages.Count != 2 || string.IsNullOrWhiteSpace(levelName) || stages.Any(s => string.IsNullOrWhiteSpace(s.text) ||
                s.text.Any(c => c == '_' || char.IsControl(c))) || stages.Last().GapCount == 0)
        {
            EditorUtility.DisplayDialog("Level needs attention", "Give the level a name, fill every page, remove underscores/control characters, and choose at least one answer blank.", "OK");
            return false;
        }
        return true;
    }

    private bool Save(bool asNew)
    {
        if (!ValidContent()) return false;
        LevelData target = asNew ? null : source;
        if (target == null)
        {
            string path = EditorUtility.SaveFilePanelInProject("Save level", levelName, "asset", "Choose a location for the level.", "Assets/GeneratedLevels");
            if (string.IsNullOrEmpty(path)) return false;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                EditorUtility.DisplayDialog("Asset already exists", "Choose a new filename, or open that level and use Save level.", "OK");
                return false;
            }
            target = CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(target, path);
            Undo.RegisterCreatedObjectUndo(target, "Create level");
        }
        Undo.RecordObject(target, "Save level design");
        target.SetCategory(category);
        target.SetDifficulty(difficulty);
        target.ClearSentences(); target.ClearLetters();
        foreach (var stage in stages) target.AddSentence(stage.ToRaw());
        poolOrder = LevelLetterOrder.Reconcile(poolOrder, RequiredLetters());
        target.letters = poolOrder.ToCharArray();
        // Asset names and exported JSON names must agree for re-import by name.
        target.name = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(target));
        levelName = target.name;
        EditorUtility.SetDirty(target);
        if (addToSequence && sequence != null && !(sequence.levels ?? Array.Empty<LevelData>()).Contains(target))
        {
            Undo.RecordObject(sequence, "Add level to sequence");
            var levels = new List<LevelData>(sequence.levels ?? Array.Empty<LevelData>()) { target };
            sequence.levels = levels.ToArray();
            EditorUtility.SetDirty(sequence);
        }
        AssetDatabase.SaveAssets();
        source = target;
        savedState = Snapshot();
        RefreshDirty();
        EditorGUIUtility.PingObject(target);
        return true;
    }

    private void Export()
    {
        if (!ValidContent()) return;
        string path = EditorUtility.SaveFilePanel("Export level JSON", Application.dataPath, levelName, "json");
        if (string.IsNullOrEmpty(path)) return;
        File.WriteAllText(path, JsonUtility.ToJson(new JsonFile { levels = new[] { ToJsonLevel() } }, true));
        AssetDatabase.Refresh();
        ShowNotification(new GUIContent("Exported one level. Importing it rebuilds the sequence."));
    }
}

[CustomEditor(typeof(LevelData))]
public class LevelDesignerInspector : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open Visual Level Designer", GUILayout.Height(30))) AssetDatabase.OpenAsset(target);
        DrawDefaultInspector();
    }
}
