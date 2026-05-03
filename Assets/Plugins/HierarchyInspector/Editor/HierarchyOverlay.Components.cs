#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FoundFootage.Editor.Hierarchy
{
    internal static partial class HierarchyOverlay
    {
        private static void DrawActiveToggle(GameObject go, Rect selectionRect)
        {
            if (!T.ActiveToggle) return;

            var toggleRect = new Rect(
                selectionRect.xMax - ToggleWidth + 1f, selectionRect.y,
                ToggleWidth, selectionRect.height);

            bool isActive = go.activeSelf;
            bool newActive = GUI.Toggle(toggleRect, isActive, GUIContent.none);

            if (newActive != isActive)
            {
                Undo.RecordObject(go, "Toggle Active");
                go.SetActive(newActive);
            }
        }

        private const float IconHoverExpand = 2f;

        // GUI.color clamps at 1.0, so a brightness multiplier can't lift bright icons; paint a halo instead.
        private static readonly Color _glowHaloColor = new Color(1.0f, 0.82f, 0.25f, 0.35f);

        private static void DrawComponentIcons(HierarchyItemInfo info, HierarchyInspectorData data,
            Rect selectionRect, bool isRepaint)
        {
            if (!T.ComponentIcons || EffectiveIconCount(info) <= 0) return;

            float iconSize = T.IconSize;
            float iconStartX = GetIconStripRightX(selectionRect);
            int count = EffectiveIconCount(info);
            float iconY = selectionRect.y + (selectionRect.height - iconSize) / 2f;

            bool applyTint = T.IconTinting && data != null && data.UseCustomColor;
            var tintColor = applyTint ? Color.Lerp(Color.white, data.BackgroundColor, 0.3f) : Color.white;

            bool checkGlow = T.IconGlow && isRepaint;
            bool checkToggle = T.ComponentQuickToggle;
            var mousePos = Event.current.mousePosition;
            var prevColor = GUI.color;
            int bufferBase = info.iconStart;

            for (int i = 0; i < count; i++)
            {
                float x = iconStartX - (i + 1) * (iconSize + IconGap);
                var iconRect = new Rect(x, iconY, iconSize, iconSize);

                // Consume MouseDown before the repaint short-circuit so clicks aren't dropped.
                if (checkToggle
                    && Event.current.type == EventType.MouseDown
                    && Event.current.button == 0
                    && iconRect.Contains(mousePos))
                {
                    var component = _flatComponentBuffer[bufferBase + i];
                    if (component is Behaviour behaviour)
                    {
                        Undo.RecordObject(component, "Toggle Component");
                        behaviour.enabled = !behaviour.enabled;
                        Event.current.Use();
                    }
                }

                if (!isRepaint) continue;

                var drawColor = tintColor;

                if (checkGlow)
                {
                    var hoverRect = new Rect(
                        iconRect.x - IconHoverExpand, iconRect.y - IconHoverExpand,
                        iconRect.width + IconHoverExpand * 2f, iconRect.height + IconHoverExpand * 2f);
                    if (hoverRect.Contains(mousePos))
                    {
                        var haloRect = new Rect(iconRect.x - 2f, iconRect.y - 2f,
                            iconRect.width + 4f, iconRect.height + 4f);
                        Color savedHaloColor = GUI.color;
                        GUI.color = _glowHaloColor;
                        GUI.DrawTexture(haloRect, EditorGUIUtility.whiteTexture);
                        GUI.color = savedHaloColor;
                    }
                }

                var refComponent = _flatComponentBuffer[bufferBase + i];
                if (refComponent is Behaviour b && !b.enabled)
                    drawColor.a *= 0.4f;

                GUI.color = drawColor;
                GUI.DrawTexture(iconRect, _flatIconBuffer[bufferBase + i]);
            }

            GUI.color = prevColor;
        }

        private static void DrawSettingsGear(GameObject go, HierarchyInspectorData data, Rect selectionRect, bool isRepaint)
        {
            if (!T.PerObjectStyling) return;

            if (_settingsIcon == null)
            {
                var content = EditorGUIUtility.IconContent("_Popup");
                _settingsIcon = content != null ? content.image : null;
            }

            var fullRowRect = new Rect(0, selectionRect.y, selectionRect.xMax, selectionRect.height);
            bool isRowHovered = fullRowRect.Contains(Event.current.mousePosition);
            bool hasCustomization = data != null && data.HasAnyCustomization;

            var gearRect = new Rect(GutterX, selectionRect.y + (selectionRect.height - SettingsIconSize) / 2f,
                SettingsIconSize, SettingsIconSize);
            bool isGearHovered = gearRect.Contains(Event.current.mousePosition);

            if (isRepaint && (isRowHovered || hasCustomization) && _settingsIcon != null)
            {
                // Three stages: full on gear hover, dimmed on row hover, faint when only the customization marker shows.
                float alpha = isGearHovered ? 1f : (isRowHovered ? 0.7f : 0.3f);
                var prevColor = GUI.color;
                var tint = T.GearIconColor;
                GUI.color = new Color(tint.r, tint.g, tint.b, tint.a * alpha);
                GUI.DrawTexture(gearRect, _settingsIcon);
                GUI.color = prevColor;
            }

            if (Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && gearRect.Contains(Event.current.mousePosition))
            {
                var selected = Selection.gameObjects;
                var targets = selected.Length > 1 ? selected : new[] { go };
                var popup = new HierarchyItemPopup(targets);
                PopupWindow.Show(gearRect, popup);
                Event.current.Use();
            }
        }

        private static Texture _strippedFolderWarningIcon;

        // Held separately from the validator's scratch so the two callers can't alias on the same row.
        private static List<Component> _strippedFolderDrawBuffer;

        // Cache: tooltip + GUIContent reused while the component-type list is unchanged.
        private static readonly Dictionary<EntityId, (int hash, GUIContent content)> _strippedTooltipCache
            = new Dictionary<EntityId, (int, GUIContent)>();

        private static void DrawStrippedComponentsWarning(GameObject go, HierarchyInspectorData data,
            Rect selectionRect, bool isRepaint)
        {
            if (!isRepaint) return;
            if (data == null || !data.IsFolder) return;
            if (!HierarchyFolderValidation.HasStrippedComponents(go)) return;

            if (_strippedFolderWarningIcon == null)
            {
                var content = EditorGUIUtility.IconContent("console.warnicon.sml");
                _strippedFolderWarningIcon = content != null ? content.image : null;
            }
            if (_strippedFolderWarningIcon == null) return;

            // Eye/hand x=[0..32], gear x=[32..46], so warning sits at x=48..62.
            const float Padding = 2f;
            float iconX = GutterX + SettingsIconSize + Padding;
            var iconRect = new Rect(iconX,
                selectionRect.y + (selectionRect.height - SettingsIconSize) / 2f,
                SettingsIconSize, SettingsIconSize);

            if (_strippedFolderDrawBuffer == null)
                _strippedFolderDrawBuffer = new List<Component>(8);
            HierarchyFolderValidation.TryGetStrippedComponents(go, _strippedFolderDrawBuffer);

            int hash = ComputeStrippedHash(_strippedFolderDrawBuffer);
            EntityId entityId = go.GetEntityId();
            if (!_strippedTooltipCache.TryGetValue(entityId, out var entry) || entry.hash != hash)
            {
                string tooltip = "Virtualized folder will be stripped at build time. "
                    + "These components will be lost:\n  "
                    + HierarchyFolderValidation.FormatComponentList(_strippedFolderDrawBuffer);
                entry = (hash, new GUIContent(_strippedFolderWarningIcon, tooltip));
                _strippedTooltipCache[entityId] = entry;
            }

            GUI.Label(iconRect, entry.content);
        }

        private static int ComputeStrippedHash(List<Component> components)
        {
            int hash = 17;
            for (int i = 0; i < components.Count; i++)
            {
                var c = components[i];
                hash = hash * 31 + (c == null ? 0 : c.GetType().GetHashCode());
            }
            return hash;
        }
    }
}
#endif
