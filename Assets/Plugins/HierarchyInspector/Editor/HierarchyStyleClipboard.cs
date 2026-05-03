#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace FoundFootage.Editor.Hierarchy
{
    /// <summary>Copy/paste of hierarchy styling plus alt-drag color painting.</summary>
    public static class HierarchyStyleClipboard
    {
        private struct StyleData
        {
            public bool hasColor;
            public Color color;
            public bool hasIcon;
            public int iconIndex;
            public bool isFolder;
            public bool isValid;
        }

        private static StyleData _clipboard;

        /// <summary>Copy the style from the selected GameObject.</summary>
        public static void CopyStyle()
        {
            var go = Selection.activeGameObject;
            if (go == null) return;

            var data = go.GetComponent<HierarchyInspectorData>();
            if (data == null)
            {
                _clipboard = new StyleData { isValid = true };
                return;
            }

            _clipboard = new StyleData
            {
                hasColor = data.UseCustomColor,
                color = data.BackgroundColor,
                hasIcon = data.UseCustomIcon,
                iconIndex = data.IconIndex,
                isFolder = data.IsFolder,
                isValid = true
            };
        }

        /// <summary>Paste the copied style onto selected GameObjects.</summary>
        public static void PasteStyle()
        {
            if (!_clipboard.isValid) return;

            var objects = Selection.gameObjects;
            if (objects == null || objects.Length == 0) return;

            Undo.SetCurrentGroupName("Paste Hierarchy Style");
            // Group must be captured after naming so AddComponent/RecordObject calls collapse into one undo step.
            int undoGroup = Undo.GetCurrentGroup();

            for (int i = 0; i < objects.Length; i++)
            {
                var go = objects[i];
                var data = go.GetComponent<HierarchyInspectorData>();
                if (data == null)
                {
                    if (!_clipboard.hasColor && !_clipboard.hasIcon && !_clipboard.isFolder)
                        continue;
                    data = Undo.AddComponent<HierarchyInspectorData>(go);
                    data.hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInBuild;
                }

                Undo.RecordObject(data, "Paste Hierarchy Style");
                data.SetCustomColor(_clipboard.hasColor, _clipboard.color);
                data.SetCustomIcon(_clipboard.hasIcon, _clipboard.iconIndex);
                data.SetIsFolder(_clipboard.isFolder);

                // hierarchyChanged doesn't fire for component-property edits; force a cache refresh.
                HierarchyOverlay.InvalidateItem(go);
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorApplication.RepaintHierarchyWindow();
        }

        /// <summary>Whether there's a valid style in the clipboard.</summary>
        public static bool HasClipboard => _clipboard.isValid;

        private static bool _isDragging;
        private static Color _dragColor;
        private static int _dragUndoGroup;

        // cachedData is the per-row HierarchyInspectorData from the dispatcher; avoids a per-event GetComponent marshal.
        public static void ProcessDragToColor(GameObject go, Rect selectionRect, HierarchyInspectorData cachedData)
        {
            if (!HierarchyThemeProvider.Active.DragToColor) return;

            var evt = Event.current;

            if (evt.type == EventType.MouseDown && evt.button == 0 && evt.alt && selectionRect.Contains(evt.mousePosition))
            {
                if (cachedData != null && cachedData.UseCustomColor)
                {
                    _isDragging = true;
                    _dragColor = cachedData.BackgroundColor;
                    _dragUndoGroup = Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("Drag Color");
                    evt.Use();
                }
            }
            else if (evt.type == EventType.MouseDrag && _isDragging && selectionRect.Contains(evt.mousePosition))
            {
                var data = cachedData;

                if (data != null && data.UseCustomColor &&
                    Mathf.Abs(data.BackgroundColor.r - _dragColor.r) < 0.01f &&
                    Mathf.Abs(data.BackgroundColor.g - _dragColor.g) < 0.01f &&
                    Mathf.Abs(data.BackgroundColor.b - _dragColor.b) < 0.01f)
                    return;

                if (data == null)
                {
                    data = Undo.AddComponent<HierarchyInspectorData>(go);
                    data.hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInBuild;
                }
                Undo.RecordObject(data, "Drag Color");
                data.SetCustomColor(true, _dragColor);
                HierarchyOverlay.InvalidateItem(go);
            }
            else if (evt.type == EventType.MouseUp && _isDragging)
            {
                _isDragging = false;
                Undo.CollapseUndoOperations(_dragUndoGroup);
            }
        }

        // Ctrl/Cmd+Shift+C copy, Ctrl/Cmd+Shift+V paste.
        public static void ProcessKeyboardShortcuts()
        {
            var evt = Event.current;
            if (evt.type != EventType.KeyDown) return;

            bool modifier = (evt.control || evt.command) && evt.shift;

            if (modifier && evt.keyCode == KeyCode.C)
            {
                CopyStyle();
                evt.Use();
            }
            else if (modifier && evt.keyCode == KeyCode.V)
            {
                PasteStyle();
                evt.Use();
            }
        }
    }
}
#endif
