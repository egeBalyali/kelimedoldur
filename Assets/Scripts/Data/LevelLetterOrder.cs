using System;
using System.Collections.Generic;
using System.Text;

/// <summary>Reorders required tiles without losing duplicate letters or adding unusable tiles.</summary>
public static class LevelLetterOrder
{
    public static string Reconcile(string current, string required)
    {
        var remaining = new Dictionary<char, int>();
        foreach (char c in required) { remaining.TryGetValue(c, out int count); remaining[c] = count + 1; }
        var result = new StringBuilder();
        foreach (char c in current ?? "")
            if (remaining.TryGetValue(c, out int count) && count > 0) { result.Append(c); remaining[c]--; }
        foreach (char c in required)
            if (remaining[c] > 0) { result.Append(c); remaining[c]--; }
        return result.ToString();
    }

    public static bool IsValid(string order, string required) =>
        order != null && order.Length == required.Length && Reconcile(order, required) == order;

    public static string Shuffle(string order, Random random)
    {
        char[] tiles = order.ToCharArray();
        for (int i = tiles.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            char c = tiles[i]; tiles[i] = tiles[j]; tiles[j] = c;
        }
        return new string(tiles);
    }

    public static string Move(string order, int from, int to)
    {
        if (from < 0 || from >= order.Length || to < 0 || to >= order.Length || from == to) return order;
        char tile = order[from];
        return order.Remove(from, 1).Insert(to, tile.ToString());
    }
}
