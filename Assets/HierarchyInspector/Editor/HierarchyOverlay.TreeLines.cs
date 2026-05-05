#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace SpaceWhale.HierarchyInspector.Editor
{
    internal static partial class HierarchyOverlay
    {
        private static void DrawTreeLines(HierarchyItemInfo info, Rect selectionRect, bool isRepaint, float indentWidth)
        {
            if (!T.TreeLines || !isRepaint || info.depth <= 0) return;

            float lineThickness = 1f;
            float midY = selectionRect.y + selectionRect.height / 2f;
            Color lineColor = T.TreeLineColor;

            float parentLineX = selectionRect.x - 1.5f * indentWidth;

            if (info.isLastChild)
            {
                float h = midY - selectionRect.y + lineThickness / 2f;
                QueueRect(new Rect(parentLineX - lineThickness / 2f, selectionRect.y, lineThickness, h), lineColor);
            }
            else
            {
                QueueRect(new Rect(parentLineX - lineThickness / 2f, selectionRect.y, lineThickness, selectionRect.height), lineColor);
            }

            float hEndX = info.hasChildren
                ? selectionRect.x - indentWidth
                : selectionRect.x - 2f;
            float hLen = hEndX - parentLineX;
            if (hLen > 0)
                QueueRect(new Rect(parentLineX, midY - lineThickness / 2f, hLen, lineThickness), lineColor);

            uint mask = info.ancestorContinuationMask;
            int maxStep = info.depth - 1;
            for (int step = 1; step <= maxStep && step <= 32; step++)
            {
                if ((mask & (1u << (step - 1))) == 0u) continue;
                float ancestorX = selectionRect.x - (step + 1.5f) * indentWidth;
                QueueRect(new Rect(ancestorX - lineThickness / 2f, selectionRect.y, lineThickness, selectionRect.height), lineColor);
            }

            FlushRects();
        }
    }
}
#endif
