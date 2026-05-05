#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace SpaceWhale.HierarchyInspector.Editor
{
    internal static partial class HierarchyOverlay
    {
        private static readonly Color BookmarkGold = new Color(1f, 0.95f, 0.25f, 1f);

        // Picked by luminance against the active selection color so a near-white user theme stays readable.
        private static readonly Color _selectedLabelLight = Color.white;
        private static readonly Color _selectedLabelDark = new Color(0.1f, 0.1f, 0.1f, 1f);

        private static float _foldoutWidth = -1f;
        private static float FoldoutWidth
        {
            get
            {
                if (_foldoutWidth < 0f)
                    _foldoutWidth = Mathf.Max(14f, EditorStyles.foldout.CalcSize(GUIContent.none).x);
                return _foldoutWidth;
            }
        }

        private static void DrawBackgrounds(
            GameObject go,
            HierarchyItemInfo info,
            HierarchyInspectorData data,
            Rect selectionRect,
            Rect fullRowRect,
            bool isRepaint,
            bool isSelected)
        {
            if (!isRepaint) return;

            bool isFolder = data != null && data.IsFolder && T.FolderStyling;
            bool hasCustomColor = data != null && T.PerObjectStyling && data.UseCustomColor;

            // Paint over Unity's native selection bar so the overlay reads as one highlight.
            if (isSelected)
            {
                Color selectionBody = _focusedIsHierarchyThisPass
                    ? T.SelectionColorFocused
                    : T.SelectionColorUnfocused;
                // Forced opaque so a translucent theme color can't leak Unity's selection bar through.
                selectionBody.a = 1f;

                // Leave a 1px gap top + bottom so Unity's drag-reorder insertion bar stays visible.
                bool dragActive = _dragActiveThisPass;
                float bodyY = fullRowRect.y + (dragActive ? 1f : 0f);
                float bodyH = fullRowRect.height - (dragActive ? 2f : 0f);
                var selectedBodyRect = new Rect(0f, bodyY, fullRowRect.xMax, bodyH);
                QueueRect(selectedBodyRect, selectionBody);

                // Bottom glow mirrors the body's horizontal extent so the gutter doesn't notch under it.
                if (T.SelectedRowGlow)
                {
                    QueueRect(new Rect(0f, fullRowRect.yMax - 2f, fullRowRect.xMax, 2f),
                        new Color(0.4f, 0.7f, 0.95f, 0.6f));
                }
                FlushRects();

                // Stripe / hover / gradient / depth-shadow / accent suppressed on selection so the body color reads clean.
                return;
            }

            // y / rowHeight gives the row index because selectionRect during Repaint is at its final on-screen position.
            float rowHeight = selectionRect.height > 0f ? selectionRect.height : 16f;
            int rowIndex = Mathf.RoundToInt(selectionRect.y / rowHeight);
            bool isStripeRow = T.AlternatingRows && (rowIndex & 1) == 1;

            // Row colors come from the theme, paired by Unity skin (dark/light) and selected by parity.
            Color stripeAdjustedBase = isStripeRow ? T.RowColorSecondary : T.RowColorPrimary;
            stripeAdjustedBase.a = 1f;

            Color bgColor = stripeAdjustedBase;

            if (hasCustomColor)
            {
                float blend = isFolder ? 0.55f : 0.35f;
                bgColor = Color.Lerp(bgColor, data.BackgroundColor, blend);
            }

            // Hover hits fullRowRect; selectionRect would only cover the label.
            bool isHovered = T.RowHover
                && fullRowRect.Contains(Event.current.mousePosition);
            if (isHovered)
                bgColor = Color.Lerp(bgColor, new Color(0.25f, 0.25f, 0.28f), 0.5f);

            if (T.PrefabTinting && info.isPrefabInstance)
                bgColor = Color.Lerp(bgColor, T.PrefabTintColor, 0.08f);

            if (T.MissingScriptWarning && info.hasMissingScript)
                bgColor = Color.Lerp(bgColor, T.MissingScriptTintColor, 0.2f);

            // Forced opaque AFTER blends so prefab/missing-script tints survive.
            bgColor.a = 1f;

            // Body widens to x=0 to cover the leftmost 32px gutter; fullRowRect itself is unchanged for hit-testing.
            var bodyRect = new Rect(0f, fullRowRect.y, fullRowRect.xMax, fullRowRect.height);
            QueueRect(bodyRect, bgColor);

            // 32 sub-rects produce a near-imperceptible step.
            if (T.GradientBackground && hasCustomColor)
            {
                Color baseCol = stripeAdjustedBase;
                baseCol.a = 1f;
                const int gradientSteps = 32;
                float gradientStartX = 0f;
                float gradientWidth = fullRowRect.xMax;
                float stepW = gradientWidth / gradientSteps;
                for (int s = 0; s < gradientSteps; s++)
                {
                    float t = (s + 0.5f) / gradientSteps;
                    QueueRect(
                        new Rect(gradientStartX + s * stepW, fullRowRect.y, stepW + 0.5f, fullRowRect.height),
                        Color.Lerp(bgColor, baseCol, t));
                }
            }

            // Clamped right of the gear column to avoid overlapping the eye/lock icons at depth 0.
            if (T.DepthShadow && info.depth > 0)
            {
                float shadowAlpha = Mathf.Min(info.depth * 0.10f, 0.45f);
                float shadowX = Mathf.Max(GearColumnRightX, selectionRect.x - 4f);
                QueueRect(new Rect(shadowX, fullRowRect.y, 4f, fullRowRect.height),
                    new Color(0, 0, 0, shadowAlpha));
            }

            // Pinned to the leftmost edge of the row as a prefix indicator, depth-independent.
            // The native eye/hand icons (drawn by Unity on hover at x in [0, 32]) sit on top.
            if (T.LeftColorStripe && hasCustomColor)
                QueueRect(new Rect(0f, fullRowRect.y, 3f, fullRowRect.height), data.BackgroundColor);

            if (T.AccentLine && hasCustomColor)
                QueueRect(new Rect(0f, fullRowRect.yMax - 1, fullRowRect.xMax, 1), data.BackgroundColor);

            if (T.RowBorders)
                QueueRect(new Rect(0f, fullRowRect.yMax - 1, fullRowRect.xMax, 1),
                    new Color(0, 0, 0, 0.15f));

            FlushRects();
        }

        // Runs on selected rows too: DrawBackgrounds covers Unity's selection bar and its native foldout with it.
        private static void DrawNativeFoldoutArrow(EntityId entityId, in HierarchyItemInfo info, Rect selectionRect)
        {
            if (!info.hasChildren) return;

            Rect foldoutRect = new Rect(
                selectionRect.x - FoldoutWidth, selectionRect.y,
                FoldoutWidth, selectionRect.height);
            bool isExpanded = IsItemExpanded(entityId);

            var prevColor = GUI.color;
            GUI.color = T.FoldoutArrowColor;
            EditorGUI.Foldout(foldoutRect, isExpanded, GUIContent.none);
            GUI.color = prevColor;
        }

        private static void DrawForeground(
            EntityId entityId,
            GameObject go,
            HierarchyItemInfo info,
            HierarchyInspectorData data,
            Rect selectionRect,
            bool isRepaint,
            bool isSelected)
        {
            if (!isRepaint) return;

            bool isFolder = data != null && data.IsFolder && T.FolderStyling;

            float hoverOffset = T.HoverSlide
                ? HierarchyAnimationState.GetHoverSlideOffsetForItem(entityId)
                : 0f;

            // Icon priority: per-object custom > folder > GameObject icon.
            Texture drawIcon = info.itemIcon;
            if (data != null && data.UseCustomIcon && T.PerObjectStyling)
            {
                var paletteIcon = HierarchyIconPalette.GetIcon(data.IconIndex);
                if (paletteIcon != null) drawIcon = paletteIcon;
            }
            else if (isFolder)
            {
                if (_folderIcon == null)
                {
                    var fc = EditorGUIUtility.IconContent("Folder Icon");
                    _folderIcon = fc != null ? fc.image : null;
                }
                if (_folderIcon != null) drawIcon = _folderIcon;
            }

            // dimAlpha is visibility (1 full, 0 invisible); applied to icon + label only.
            bool isInactive = T.InactiveDimming && !go.activeInHierarchy;
            float dimAlpha = isInactive ? T.InactiveDimAlpha : 1f;

            if (drawIcon != null)
            {
                // Lock the icon to the row origin while selected; only the label slides on hover.
                float iconOffset = isSelected ? 0f : hoverOffset;
                float iconSize = T.MainIconSize;
                // Force pure white before DrawTexture so stale upstream tints don't dim our icon.
                var prevIconColor = GUI.color;
                GUI.color = isInactive ? new Color(1f, 1f, 1f, dimAlpha) : Color.white;
                GUI.DrawTexture(new Rect(selectionRect.x + iconOffset, selectionRect.y,
                    iconSize, selectionRect.height), drawIcon, ScaleMode.ScaleToFit);
                GUI.color = prevIconColor;
            }

            const float OverrideDotSize = 6f;
            const float OverrideDotReserve = 8f;
            bool drawOverrideDot = T.PrefabOverrideDot && info.hasPrefabOverrides;
            if (drawOverrideDot)
            {
                float dotX = selectionRect.x + T.MainIconSize + 2f + hoverOffset;
                float dotY = selectionRect.y + (selectionRect.height - OverrideDotSize) / 2f;
                EditorGUI.DrawRect(new Rect(dotX, dotY, OverrideDotSize, OverrideDotSize),
                    T.PrefabOverrideDotColor);
            }

            InitLabelStyles();
            int iconCount = EffectiveIconCount(info);
            float iconStripLeftX = iconCount > 0
                ? GetIconStripRightX(selectionRect, info.isPrefabInstance) - iconCount * (T.IconSize + IconGap)
                : selectionRect.xMax;
            float labelStartX = selectionRect.x + T.MainIconSize + IconLabelGap
                + (drawOverrideDot ? OverrideDotReserve : 0f) + hoverOffset;
            float labelEndX = iconStripLeftX - 4f;
            float labelW = Mathf.Max(0f, labelEndX - labelStartX);
            var labelRect = new Rect(labelStartX, selectionRect.y, labelW, selectionRect.height);

            // _hierarchyLabelDimStyle has its own dim text color, so no extra GUI.color tint is needed.
            GUIStyle style = isFolder ? _folderLabelStyle
                : go.activeInHierarchy ? _hierarchyLabelStyle
                : _hierarchyLabelDimStyle;

            // BT.601 luminance picks light or dark text against the user-themed selection color.
            if (isSelected)
            {
                Color bg = _focusedIsHierarchyThisPass
                    ? T.SelectionColorFocused
                    : T.SelectionColorUnfocused;
                float lum = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;
                var prevLabelColor = GUI.color;
                GUI.color = lum > 0.5f ? _selectedLabelDark : _selectedLabelLight;
                GUI.Label(labelRect, info.cachedName, style);
                GUI.color = prevLabelColor;
            }
            else
            {
                GUI.Label(labelRect, info.cachedName, style);
            }
        }

        // Sits in the bottom-right of the icon rect so it doesn't overlap the label and survives selection redraws.
        internal static void DrawBookmarkStarBadge(
            EntityId entityId,
            GameObject go,
            HierarchyInspectorData data,
            Rect selectionRect)
        {
            if (data == null || !data.IsBookmarked) return;
            if (Event.current.type != EventType.Repaint) return;

            InitBookmarkStarStyle();

            bool isInactive = T.InactiveDimming && !go.activeInHierarchy;
            float dimAlpha = isInactive ? T.InactiveDimAlpha : 1f;
            float hoverOffset = T.HoverSlide
                ? HierarchyAnimationState.GetHoverSlideOffsetForItem(entityId)
                : 0f;

            const float BadgeSize = 10f;
            float iconX = selectionRect.x + hoverOffset;
            float iconY = selectionRect.y;
            float badgeX = iconX + T.MainIconSize - BadgeSize + 1f;
            float badgeY = iconY + selectionRect.height - BadgeSize - 1f;
            var badgeRect = new Rect(badgeX, badgeY, BadgeSize, BadgeSize);

            var prevColor = GUI.color;
            GUI.color = isInactive
                ? new Color(BookmarkGold.r, BookmarkGold.g, BookmarkGold.b, dimAlpha)
                : BookmarkGold;
            GUI.Label(badgeRect, "★", _bookmarkStarStyle);
            GUI.color = prevColor;
        }

        private static GUIStyle _bookmarkStarStyle;
        private static void InitBookmarkStarStyle()
        {
            if (_bookmarkStarStyle != null) return;
            _bookmarkStarStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0)
            };
        }

        // Lives inside hierarchyWindowItemByEntityIdOnGUI on the scene header row so click events route through Unity's IMGUI flow.
        internal static void DrawSceneBookmarkButton(
            UnityEngine.SceneManagement.Scene scene,
            Rect selectionRect,
            EventType eventType)
        {
            // Right-side slot: left side of a scene row is taken by the foldout/icon/name with no gap.
            const float ButtonSize = 16f;
            const float RightInset = 4f;
            float yCenter = selectionRect.y + (selectionRect.height - ButtonSize) * 0.5f;
            var rect = new Rect(selectionRect.xMax - ButtonSize - RightInset, yCenter,
                ButtonSize, ButtonSize);

            // Custom paint + transparent GUI.Button keeps the gold tint instead of Unity's native button chrome.
            InitSceneBookmarkButtonStyle();

            if (eventType == EventType.Repaint)
            {
                bool isHovered = rect.Contains(Event.current.mousePosition);
                if (isHovered)
                    EditorGUI.DrawRect(rect, new Color(BookmarkGold.r, BookmarkGold.g, BookmarkGold.b, 0.18f));

                var prevColor = GUI.color;
                GUI.color = BookmarkGold;
                GUI.Label(rect, "★", _sceneBookmarkButtonStyle);
                GUI.color = prevColor;
            }

            // GUI.Button consumes the click so it doesn't fall through to the scene-row foldout.
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                HierarchyBookmarkManager.ShowBookmarkMenuForScene(scene);

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
        }

        private static GUIStyle _sceneBookmarkButtonStyle;
        private static void InitSceneBookmarkButtonStyle()
        {
            if (_sceneBookmarkButtonStyle != null) return;
            _sceneBookmarkButtonStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0)
            };
        }
    }
}
#endif
