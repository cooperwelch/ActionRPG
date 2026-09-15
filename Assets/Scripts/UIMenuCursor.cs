using UnityEngine;
using UnityEngine.UI;

public static class UIMenuCursor
{
    public static void PositionLeftOf(Image cursor, RectTransform optionRect, float offsetFromLeft, Vector2 size)
    {
        if (cursor == null || optionRect == null)
        {
            return;
        }

        RectTransform cursorRect = cursor.rectTransform;
        RectTransform parent = cursorRect.parent as RectTransform;
        if (parent == null)
        {
            return;
        }

        cursorRect.anchorMin = new Vector2(0f, 1f);
        cursorRect.anchorMax = new Vector2(0f, 1f);
        cursorRect.pivot = new Vector2(0.5f, 0.5f);
        cursorRect.sizeDelta = size;
        cursorRect.SetAsLastSibling();

        Vector3 worldTarget = optionRect.TransformPoint(new Vector3(
            optionRect.rect.xMin - offsetFromLeft,
            optionRect.rect.center.y,
            0f
        ));
        Vector3 localPoint = parent.InverseTransformPoint(worldTarget);
        cursorRect.localPosition = new Vector3(
            SnapToPixel(localPoint.x, size.x),
            SnapToPixel(localPoint.y, size.y),
            cursorRect.localPosition.z
        );
    }

    private static float SnapToPixel(float value, float dimension)
    {
        float offset = (Mathf.RoundToInt(dimension) % 2 == 0) ? 0f : 0.5f;
        return Mathf.Round(value - offset) + offset;
    }
}
