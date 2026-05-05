#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace SpaceWhale.HierarchyInspector.Editor
{
    internal static partial class HierarchyOverlay
    {
        // Our opaque row background hides Unity's native eye/hand icons in the 32px gutter, so we re-render
        // them on top and forward clicks to SceneVisibilityManager.

        private const float NativeToggleSize = 16f;
        private const float EyeColumnX = 0f;
        private const float HandColumnX = 16f;

        // Lazy: IconContent lookups during static init can fail before the editor finishes booting.
        private static GUIContent _eyeVisibleHover;
        private static GUIContent _eyeHidden;
        private static GUIContent _handPickableHover;
        private static GUIContent _handNotPickable;
        private static bool _nativeToggleIconsResolved;

        private static void EnsureNativeToggleIcons()
        {
            if (_nativeToggleIconsResolved) return;
            _eyeVisibleHover = EditorGUIUtility.IconContent("scenevis_visible_hover");
            _eyeHidden = EditorGUIUtility.IconContent("scenevis_hidden_hover");
            _handPickableHover = EditorGUIUtility.IconContent("scenepicking_pickable_hover");
            _handNotPickable = EditorGUIUtility.IconContent("scenepicking_notpickable_hover");
            _nativeToggleIconsResolved = true;
        }

        // Alt-click toggles descendants, matching Unity's stock behaviour.
        private static void DrawNativeVisibilityToggles(GameObject go, in HierarchyItemInfo info, Rect fullRowRect, Rect selectionRect)
        {
            EnsureNativeToggleIcons();

            bool isHidden = info.sceneVisIsHidden;
            bool isPickingDisabled = info.sceneVisIsPickingDisabled;
            // fullRowRect starts at GutterX; widen for hover detection over the eye/hand gutter.
            var rowHoverRect = new Rect(0f, fullRowRect.y, fullRowRect.xMax, fullRowRect.height);
            bool isRowHovered = rowHoverRect.Contains(Event.current.mousePosition);

            float iconY = selectionRect.y + (selectionRect.height - NativeToggleSize) * 0.5f;
            var eyeRect = new Rect(EyeColumnX, iconY, NativeToggleSize, NativeToggleSize);
            var handRect = new Rect(HandColumnX, iconY, NativeToggleSize, NativeToggleSize);
            bool isMouseOnEye = eyeRect.Contains(Event.current.mousePosition);
            bool isMouseOnHand = handRect.Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.Repaint)
            {
                Color prevColor = GUI.color;

                if (isHidden)
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(eyeRect, _eyeHidden.image);
                }
                else if (isMouseOnEye)
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(eyeRect, _eyeVisibleHover.image);
                }
                else if (isRowHovered)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.6f);
                    GUI.DrawTexture(eyeRect, _eyeVisibleHover.image);
                }

                if (isPickingDisabled)
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(handRect, _handNotPickable.image);
                }
                else if (isMouseOnHand)
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(handRect, _handPickableHover.image);
                }
                else if (isRowHovered)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.6f);
                    GUI.DrawTexture(handRect, _handPickableHover.image);
                }

                GUI.color = prevColor;
                return;
            }

            if (Event.current.type != EventType.MouseDown || Event.current.button != 0) return;

            var mouse = Event.current.mousePosition;
            bool alt = Event.current.alt;

            // Resolve the manager only on actual click (rare).
            if (eyeRect.Contains(mouse))
            {
                SceneVisibilityManager.instance.ToggleVisibility(go, alt);
                Event.current.Use();
                EditorApplication.RepaintHierarchyWindow();
            }
            else if (handRect.Contains(mouse))
            {
                SceneVisibilityManager.instance.TogglePicking(go, alt);
                Event.current.Use();
                EditorApplication.RepaintHierarchyWindow();
            }
        }
    }
}
#endif
