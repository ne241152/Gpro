using UnityEngine;
using UnityEngine.UI;

// 矢印を文字や画像にせず、単独の三角形として描画する。
[AddComponentMenu("UI/Selection Triangle")]
[RequireComponent(typeof(CanvasRenderer))]
public class SelectionTriangleGraphic : MaskableGraphic
{
    [SerializeField] private bool pointsRight;

    public bool PointsRight
    {
        get => pointsRight;
        set
        {
            if (pointsRight == value) return;
            pointsRight = value;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vertices)
    {
        vertices.Clear();
        Rect rect = GetPixelAdjustedRect();
        float baseX = pointsRight ? rect.xMin : rect.xMax;
        float tipX = pointsRight ? rect.xMax : rect.xMin;
        vertices.AddVert(new Vector3(baseX, rect.yMin, 0f), color, Vector2.zero);
        vertices.AddVert(new Vector3(baseX, rect.yMax, 0f), color, Vector2.zero);
        vertices.AddVert(new Vector3(tipX, rect.center.y, 0f), color, Vector2.zero);
        if (pointsRight) vertices.AddTriangle(0, 1, 2);
        else vertices.AddTriangle(0, 2, 1);
    }
}
