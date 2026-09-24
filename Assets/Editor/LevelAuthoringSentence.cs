using System;
using System.Collections.Generic;
using System.Text;

// Plain data so draft editing and gap conversion can be tested outside Unity.
[Serializable]
public class LevelAuthoringSentence
{
    public string text = "";
    public List<bool> hidden = new List<bool>();

    public static LevelAuthoringSentence FromRaw(string raw)
    {
        var result = new LevelAuthoringSentence();
        var plain = new StringBuilder();
        for (int i = 0; i < raw.Length; i++)
        {
            bool gap = raw[i] == '_';
            if (gap && ++i == raw.Length)
                throw new FormatException("A sentence ends with '_' without an answer letter.");
            plain.Append(raw[i]);
            result.hidden.Add(gap);
        }
        result.text = plain.ToString();
        return result;
    }

    public string ToRaw()
    {
        var raw = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            if (hidden[i]) raw.Append('_');
            raw.Append(text[i]);
        }
        return raw.ToString();
    }

    // Preserve choices in the unchanged prefix and suffix when text is inserted/deleted.
    public void SetText(string value)
    {
        int prefix = 0;
        while (prefix < text.Length && prefix < value.Length && text[prefix] == value[prefix]) prefix++;
        int suffix = 0;
        while (suffix < text.Length - prefix && suffix < value.Length - prefix &&
            text[text.Length - suffix - 1] == value[value.Length - suffix - 1]) suffix++;
        var next = new List<bool>(new bool[value.Length]);
        for (int i = 0; i < prefix; i++) next[i] = hidden[i];
        for (int i = 0; i < suffix; i++) next[value.Length - i - 1] = hidden[text.Length - i - 1];
        text = value;
        hidden = next;
    }

    public int GapCount => hidden.FindAll(value => value).Count;

    // Same word budgeting as SentenceView: trim edge spaces; never split a word.
    public List<List<int>> Wrap(int columns)
    {
        var lines = new List<List<int>> { new List<int>() };
        var line = lines[0];
        int pendingSpaces = 0;
        for (int i = 0; i < text.Length;)
        {
            if (text[i] == ' ')
            {
                if (line.Count > 0) pendingSpaces++;
                i++;
                continue;
            }
            int start = i;
            while (i < text.Length && text[i] != ' ') i++;
            if (line.Count > 0 && line.Count + pendingSpaces + i - start > columns)
            {
                line = new List<int>();
                lines.Add(line);
                pendingSpaces = 0;
            }
            for (int s = start - pendingSpaces; s < start; s++) line.Add(s);
            pendingSpaces = 0;
            for (int j = start; j < i; j++) line.Add(j);
        }
        return lines;
    }
}
