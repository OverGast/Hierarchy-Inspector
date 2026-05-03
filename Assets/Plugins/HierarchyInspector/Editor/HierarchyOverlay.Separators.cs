#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace FoundFootage.Editor.Hierarchy
{
    internal static partial class HierarchyOverlay
    {
        private static readonly Color _defaultSeparatorColor = new Color(0.25f, 0.25f, 0.28f);
        private static GUIStyle _separatorLabelStyle;

        private static void DrawSeparator(
            GameObject go,
            HierarchyItemInfo info,
            HierarchyInspectorData data,
            Rect selectionRect,
            bool isRepaint,
            bool isSelected)
        {
            if (!isRepaint) return;

            Color separatorColor = (data != null && data.IsSeparator)
                ? data.SeparatorColor
                : _defaultSeparatorColor;

            // Inset to leave the gear and active-toggle columns visible.
            float endX = GetEffectiveRowEndX();
            float startX = GearColumnRightX;
            float toggleReserve = T.ActiveToggle ? ToggleWidth + ToggleReservePadding : 0f;
            float barEndX = endX - toggleReserve;
            float barWidth = Mathf.Max(0f, barEndX - startX);
            var fullRect = new Rect(startX, selectionRect.y, barWidth, selectionRect.height);
            QueueRect(fullRect, separatorColor);

            Color accentColor = new Color(
                Mathf.Min(separatorColor.r + 0.08f, 1f),
                Mathf.Min(separatorColor.g + 0.08f, 1f),
                Mathf.Min(separatorColor.b + 0.08f, 1f));
            QueueRect(new Rect(startX, selectionRect.y, barWidth, 1f), accentColor);
            QueueRect(new Rect(startX, selectionRect.yMax - 1f, barWidth, 1f), accentColor);
            FlushRects();

            InitSeparatorLabelStyle();
            GUI.Label(fullRect, info.separatorLabel, _separatorLabelStyle);
        }

        private static void InitSeparatorLabelStyle()
        {
            if (_separatorLabelStyle != null) return;

            _separatorLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = EditorStyles.boldLabel.fontSize + 1,
                normal = { textColor = new Color(1f, 1f, 1f, 0.85f) },
                padding = new RectOffset(0, 0, 0, 0)
            };
        }
    }
}
#endif
