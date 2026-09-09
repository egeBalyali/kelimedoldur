using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureGenerator
{
    [MenuItem("Tools/VFX/Generate Soft Dot Texture")]
    public static void GenerateSoftDot()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - (dist / radius));
                alpha = Mathf.Pow(alpha, 2f); // Smooth falloff curve
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        byte[] bytes = tex.EncodeToPNG();
        string path = Application.dataPath + "/Tex_SoftDot.png";
        File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();
        Debug.Log("Generated Soft Dot PNG at: Assets/Tex_SoftDot.png");
    }
}