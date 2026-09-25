using System;
using System.Collections.Generic;

/// <summary>Plans board rows without creating scene objects. -1 represents a green space cell.</summary>
public static class SentenceBoardLayout
{
    public static List<List<int>> BuildRows(char[] characters, int columns, out Dictionary<int, int> wordForIndex)
    {
        if (characters == null)
            throw new ArgumentNullException(nameof(characters));
        if (columns < 1)
            throw new ArgumentOutOfRangeException(nameof(columns));

        var lines = new List<List<int>> { new List<int>() };
        wordForIndex = new Dictionary<int, int>();
        List<int> line = lines[0];
        int pendingSpaces = 0;
        int wordId = 0;
        for (int index = 0; index < characters.Length;)
        {
            if (characters[index] == ' ')
            {
                if (line.Count > 0)
                    pendingSpaces++;
                index++;
                continue;
            }

            int end = index;
            while (end < characters.Length && characters[end] != ' ')
                end++;
            if (line.Count > 0 && line.Count + pendingSpaces + end - index > columns)
            {
                line = new List<int>();
                lines.Add(line);
                pendingSpaces = 0;
            }
            for (int space = 0; space < pendingSpaces; space++)
                line.Add(-1);
            pendingSpaces = 0;

            for (; index < end; index++)
            {
                // Only words wider than the board are split between rows.
                if (line.Count == columns)
                {
                    line = new List<int>();
                    lines.Add(line);
                }
                line.Add(index);
                wordForIndex[index] = wordId;
            }
            wordId++;
        }
        return lines;
    }
}
