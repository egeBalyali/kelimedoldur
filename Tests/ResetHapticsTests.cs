// Isolated harness for the real SentenceData, LevelFlowManager and HapticFeedbackManager.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine
{
    public class MonoBehaviour
    {
        public bool isActiveAndEnabled = true;
        public T GetComponent<T>() where T : class => null;
    }
    public class ScriptableObject { }
    public class SerializeField : Attribute { }
    public class DisallowMultipleComponent : Attribute { }
    public class ContextMenu : Attribute { public ContextMenu(string value) { } }
    public class Tooltip : Attribute { public Tooltip(string value) { } }
    public class Header : Attribute { public Header(string value) { } }
    public class CreateAssetMenu : Attribute { public string fileName; public string menuName; }
    public class GameObject { public void SetActive(bool value) { } }
    public static class Application { public static bool isPlaying = true; }
    public static class Mathf { public static int Clamp(int v, int min, int max) => Math.Min(max, Math.Max(min, v)); }
    public static class Debug
    {
        public static List<string> messages = new List<string>();
        public static void Log(string value, object context = null) => messages.Add(value);
        public static void LogError(string value) => throw new Exception(value);
    }
}
namespace UnityEngine.UI
{
    public class Button
    {
        public UnityEngine.GameObject gameObject = new UnityEngine.GameObject();
        public Click onClick = new Click();
        public class Click { public void AddListener(Action a) { } public void RemoveListener(Action a) { } }
    }
}
public class LetterView { public char Letter; }
public class LetterPoolController
{
    public event Action<LetterView> LetterPressed;
    public Dictionary<LetterView, bool> used = new Dictionary<LetterView, bool>();
    public int setups;
    public void Setup(char[] letters) { setups++; }
    public void SetLetterUsed(LetterView view, bool value) => used[view] = value;
}
public class SentenceView
{
    public event Action<int> GapPressed;
    public event Action CorrectWordTraceStarted;
    public Dictionary<int, char> visible = new Dictionary<int, char>();
    private SentenceData sentence;
    public int feedbackCalls;
    public int FirstGapIndex => sentence == null || sentence.GetEmptySlotIndices().Length == 0 ? -1 : sentence.GetEmptySlotIndices()[0];
    public bool IsComplete
    {
        get
        {
            foreach (int gap in sentence.GetEmptySlotIndices()) if (!visible.ContainsKey(gap)) return false;
            return true;
        }
    }
    public void Setup(SentenceData value, bool last = false) { sentence = value; visible.Clear(); }
    public void SetGapLetter(int index, char value, bool suppressTrace = false)
    {
        visible[index] = value;
        if (!suppressTrace) { feedbackCalls++; CorrectWordTraceStarted?.Invoke(); }
    }
    public void ClearGap(int index) => visible.Remove(index);
    public bool IsGapFilled(int index) => visible.ContainsKey(index);
    public void SetActiveGap(int index) { }
    public int FindNextEmptyGap(int from)
    {
        foreach (int gap in sentence.GetEmptySlotIndices()) if (!visible.ContainsKey(gap)) return gap;
        return -1;
    }
    public void CompleteWord() => CorrectWordTraceStarted?.Invoke();
}
public static class ResetHapticsTests
{
    private static void Check(bool value, string label) { if (!value) throw new Exception(label); }
    private static FieldInfo Field(object obj, string name) => obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    private static void Set(object obj, string name, object value) => Field(obj, name).SetValue(obj, value);
    private static void Call(object obj, string name) => obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, null);
    public static string Run()
    {
        var flow = new LevelFlowManager();
        var view = new SentenceView();
        var pool = new LetterPoolController();
        Set(flow, "sentenceView", view); Set(flow, "letterPool", pool);
        var level = new LevelData();
        level.AddSentence("C_a_t D_o_g");
        level.AddSentence("S_u_n M_o_o_n");
        flow.LoadLevel(level);
        var pages = (Dictionary<int, Dictionary<int, LetterView>>)Field(flow, "sentenceGapFills").GetValue(flow);
        pages[0] = new Dictionary<int, LetterView> { {1, new LetterView {Letter='A'}}, {2, new LetterView {Letter='t'}},
            {5, new LetterView {Letter='o'}}, {6, new LetterView {Letter='x'}} };
        pages[1] = new Dictionary<int, LetterView> { {1, new LetterView {Letter='u'}}, {5, new LetterView {Letter='o'}},
            {6, new LetterView {Letter='o'}}, {7, new LetterView {Letter='n'}} };
        var wrong = pages[0][6]; var partialCorrect = pages[0][5]; var partial = pages[1][1]; var retained = pages[1][6];
        foreach (var page in pages.Values) foreach (var tile in page.Values) pool.SetLetterUsed(tile, true);
        flow.GoToNextSentence();
        var haptics = new HapticFeedbackManager();
        Set(haptics, "sentenceView", view); Set(haptics, "flowManager", flow); Set(haptics, "logInEditor", true);
        Call(haptics, "OnEnable");
        flow.ResetUnfinishedWords();
        Check(flow.IsFirstSentence && pages[0].Count == 2 && pages[1].Count == 3, "Reset keeps only whole correct words across all pages");
        Check(!pool.used[wrong] && !pool.used[partialCorrect] && !pool.used[partial] && pool.used[retained], "Return only unpreserved tiles");
        Check(pool.setups == 1 && view.visible.Count == 2, "Retained tile instances survive without rebuilding pool");
        Check(view.feedbackCalls == 0 && UnityEngine.Debug.messages.Count == 0, "Reset/navigation do not replay feedback");
        flow.ResetUnfinishedWords();
        Check(pages[0].Count == 2 && pages[1].Count == 3, "Repeated reset is stable");
        view.CompleteWord();
        Check(UnityEngine.Debug.messages[0] == "[Haptics] Happy", "Happy routes from correct word event");
        flow.GoToNextSentence(); flow.SubmitAnswer();
        Check(UnityEngine.Debug.messages[1] == "[Haptics] Sad", "Sad routes from incomplete/false submit");
        haptics.SetHapticsEnabled(false); view.CompleteWord(); flow.SubmitAnswer();
        Check(UnityEngine.Debug.messages.Count == 2, "Disable toggle suppresses feedback");
        haptics.SetHapticsEnabled(true); Call(haptics, "OnDisable"); view.CompleteWord(); flow.SubmitAnswer();
        Check(UnityEngine.Debug.messages.Count == 2, "Disable removes subscriptions");
        return "PASS: correct-word preservation, wrong/partial tile return, all pages, repeated reset, and happy/sad haptic routing.";
    }
}
