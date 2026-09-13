using UnityEngine;
using UnityEditor;
using System.IO;

public partial class TextureGenerator
{
    [MenuItem("Tools/VFX/Generate 4-Point Sparkle Texture")]
    public static void GenerateSparkle()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Calculate distance along horizontal and vertical axes
                float dx = Mathf.Abs(x - center.x) / (size / 2f);
                float dy = Mathf.Abs(y - center.y) / (size / 2f);

                // Create sharp 4-point cross falloff + soft radial core
                float cross = Mathf.Clamp01(1f - (dx * 6f)) * Mathf.Clamp01(1f - dy);
                float crossVert = Mathf.Clamp01(1f - (dy * 6f)) * Mathf.Clamp01(1f - dx);
                float core = Mathf.Clamp01(1f - (Vector2.Distance(new Vector2(x, y), center) / (size / 4f)));

                float alpha = Mathf.Clamp01(cross + crossVert + Mathf.Pow(core, 2f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        byte[] bytes = tex.EncodeToPNG();
        string path = Application.dataPath + "/Tex_Sparkle4Point.png";
        File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();
        Debug.Log("Generated Sparkle PNG at: Assets/Tex_Sparkle4Point.png");
    }
}