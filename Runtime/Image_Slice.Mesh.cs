using UnityEngine;
using UnityEngine.UI;
using Sprites = UnityEngine.Sprites;

/// <summary>
/// Image_Slice — 9-slice 경계를 유지하면서 선형 fill 영역과 겹치는 셀만 메시로 생성한다.
/// Image의 fillAmount/fillCenter/pixelsPerUnit/overrideSprite 계약을 그대로 사용한다.
/// </summary>
public partial class Image_Slice
{
    // 3x3 slice 셀과 전체 fill 구간을 교차시켜, 경계 셀도 UV 왜곡 없이 잘라서 그린다.
    private void GenerateSlicedFilledSprite(VertexHelper vh, Sprite spr, FillDir dir)
    {
        vh.Clear();
        if (fillAmount < 0.001f) return;

        Rect rect = GetPixelAdjustedRect();
        Vector4 outer = Sprites.DataUtility.GetOuterUV(spr);
        Vector4 inner = Sprites.DataUtility.GetInnerUV(spr);
        Vector4 padding = Sprites.DataUtility.GetPadding(spr) / multipliedPixelsPerUnit;
        Vector4 borders = ResolveBorders(spr.border / multipliedPixelsPerUnit, rect);

        Vector4 xPositions = new Vector4(
            rect.x + padding.x,
            rect.x + borders.x,
            rect.xMax - borders.z,
            rect.xMax - padding.z);
        Vector4 yPositions = new Vector4(
            rect.y + padding.y,
            rect.y + borders.y,
            rect.yMax - borders.w,
            rect.yMax - padding.w);
        Vector4 xUvs = new Vector4(outer.x, inner.x, inner.z, outer.z);
        Vector4 yUvs = new Vector4(outer.y, inner.y, inner.w, outer.w);

        bool horizontal = dir is FillDir.Left or FillDir.Right;
        bool forward = dir is FillDir.Right or FillDir.Up;
        Vector4 axisPositions = horizontal ? xPositions : yPositions;
        float axisStart = axisPositions[0];
        float axisEnd = axisPositions[3];
        float filledLength = Mathf.Max(0f, axisEnd - axisStart) * Mathf.Clamp01(fillAmount);
        float fillMin = forward ? axisStart : axisEnd - filledLength;
        float fillMax = forward ? axisStart + filledLength : axisEnd;

        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (!fillCenter && x == 1 && y == 1) continue;

                float cellMin = horizontal ? xPositions[x] : yPositions[y];
                float cellMax = horizontal ? xPositions[x + 1] : yPositions[y + 1];
                float clippedMin = Mathf.Max(cellMin, fillMin);
                float clippedMax = Mathf.Min(cellMax, fillMax);
                if (clippedMax <= clippedMin || cellMax <= cellMin) continue;

                float clipStart = Mathf.InverseLerp(cellMin, cellMax, clippedMin);
                float clipEnd = Mathf.InverseLerp(cellMin, cellMax, clippedMax);
                float left = xPositions[x];
                float right = xPositions[x + 1];
                float bottom = yPositions[y];
                float top = yPositions[y + 1];
                float uvLeft = xUvs[x];
                float uvRight = xUvs[x + 1];
                float uvBottom = yUvs[y];
                float uvTop = yUvs[y + 1];

                if (horizontal)
                {
                    left = clippedMin;
                    right = clippedMax;
                    uvLeft = Mathf.Lerp(xUvs[x], xUvs[x + 1], clipStart);
                    uvRight = Mathf.Lerp(xUvs[x], xUvs[x + 1], clipEnd);
                }
                else
                {
                    bottom = clippedMin;
                    top = clippedMax;
                    uvBottom = Mathf.Lerp(yUvs[y], yUvs[y + 1], clipStart);
                    uvTop = Mathf.Lerp(yUvs[y], yUvs[y + 1], clipEnd);
                }

                if (right <= left || top <= bottom) continue;
                AddQuad(vh, left, bottom, right, top, uvLeft, uvBottom, uvRight, uvTop);
            }
        }
    }

    // pixel-adjusted rect와 원본 RectTransform 비율을 맞춘 뒤, 좋은 형태를 유지하도록 양쪽 border 합을 rect 길이 안으로 제한한다.
    private Vector4 ResolveBorders(Vector4 borders, Rect adjustedRect)
    {
        Vector2 sourceSize = rectTransform.rect.size;
        ResolveBorderAxis(ref borders.x, ref borders.z, sourceSize.x, adjustedRect.width);
        ResolveBorderAxis(ref borders.y, ref borders.w, sourceSize.y, adjustedRect.height);
        return borders;
    }

    private static void ResolveBorderAxis(ref float near, ref float far, float sourceLength, float adjustedLength)
    {
        if (sourceLength != 0f)
        {
            float pixelAdjustment = adjustedLength / sourceLength;
            near *= pixelAdjustment;
            far *= pixelAdjustment;
        }

        float combined = near + far;
        if (combined <= adjustedLength || combined <= 0f) return;

        float fit = adjustedLength / combined;
        near *= fit;
        far *= fit;
    }

    private void AddQuad(
        VertexHelper vh,
        float left,
        float bottom,
        float right,
        float top,
        float uvLeft,
        float uvBottom,
        float uvRight,
        float uvTop)
    {
        int first = vh.currentVertCount;
        vh.AddVert(new Vector3(left, bottom, 0f), color, new Vector2(uvLeft, uvBottom));
        vh.AddVert(new Vector3(left, top, 0f), color, new Vector2(uvLeft, uvTop));
        vh.AddVert(new Vector3(right, top, 0f), color, new Vector2(uvRight, uvTop));
        vh.AddVert(new Vector3(right, bottom, 0f), color, new Vector2(uvRight, uvBottom));
        vh.AddTriangle(first, first + 1, first + 2);
        vh.AddTriangle(first + 2, first + 3, first);
    }
}
