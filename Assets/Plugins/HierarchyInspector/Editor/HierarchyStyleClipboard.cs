#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace FoundFootage.Editor.Hierarchy
{
    /// <summary>Copy/paste of hierarchy styling.</summary>
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
