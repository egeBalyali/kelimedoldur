using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

/// <summary>Draws the entire empty board in one UI mesh, including in the editor.</summary>
[ExecuteAlways]
[RequireComponent(typeof(CanvasRenderer))]
public class SentenceBoardGrid : MaskableGraphic
{
    [SerializeField] private Sprite tileSprite;
    [Range(1, 30)] [SerializeField] private int columns = 10;
    [Range(1, 30)] [SerializeField] private int rows = 7;
    [SerializeField] private Vector2 spacing = new Vector2(5f, 5f);

    public int Columns => Mathf.Clamp(columns, 1, 30);
    public int Rows => Mathf.Clamp(rows, 1, 30);
    public override Texture mainTexture => tileSprite != null ? tileSprite.texture : base.mainTexture;

    private Vector2 CellInset => new Vector2(
        Mathf.Clamp(spacing.x, 0f, rectTransform.rect.width / Columns) * 0.5f,
        Mathf.Clamp(spacing.y, 0f, rectTransform.rect.height / Rows) * 0.5f);

    protected override void OnEnable()
    {
        base.OnEnable();
        raycastTarget = false;
    }

    /// <summary>Use the same cell boundaries for the background and foreground.</summary>
    public void PlaceTile(RectTransform tile, int column, int row)
    {
        tile.SetParent(transform, false);
        tile.localScale = Vector3.one;
        tile.anchorMin = new Vector2((float)column / Columns, 1f - (float)(row + 1) / Rows);
        tile.anchorMax = new Vector2((float)(column + 1) / Columns, 1f - (float)row / Rows);
        tile.offsetMin = CellInset;
        tile.offsetMax = -CellInset;
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        Vector2 inset = CellInset;
        Vector4 uv = tileSprite != null ? DataUtility.GetOuterUV(tileSprite) : new Vector4(0f, 0f, 1f, 1f);

        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                float left = rect.xMin + rect.width * column / Columns + inset.x;
                float right = rect.xMin + rect.width * (column + 1) / Columns - inset.x;
                float top = rect.yMax - rect.height * row / Rows - inset.y;
                float bottom = rect.yMax - rect.height * (row + 1) / Rows + inset.y;
                int start = mesh.currentVertCount;
                mesh.AddVert(new Vector3(left, bottom), color, new Vector2(uv.x, uv.y));
                mesh.AddVert(new Vector3(left, top), color, new Vector2(uv.x, uv.w));
                mesh.AddVert(new Vector3(right, top), color, new Vector2(uv.z, uv.w));
                mesh.AddVert(new Vector3(right, bottom), color, new Vector2(uv.z, uv.y));
                mesh.AddTriangle(start, start + 1, start + 2);
                mesh.AddTriangle(start + 2, start + 3, start);
            }
        }
    }
}
