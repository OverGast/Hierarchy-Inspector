#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

namespace FoundFootage.Editor.Hierarchy
{
    /// <summary>Tracks bookmarked GameObjects and exposes per-scene navigation menus.</summary>
    public static class HierarchyBookmarkManager
    {
        private static readonly HashSet<EntityId> _bookmarkedIDs = new HashSet<EntityId>(16);
        private static readonly List<EntityId> _bookmarkOrder = new List<EntityId>(16);
        private static bool _dirty = true;

        public static int Count => _bookmarkOrder.Count;
        public static bool IsBookmarked(EntityId entityId) => _bookmarkedIDs.Contains(entityId);
        public static void MarkDirty() => _dirty = true;

        public static void RebuildFromScene()
        {
            if (!_dirty) return;
            _dirty = false;

            _bookmarkedIDs.Clear();
            _bookmarkOrder.Clear();

            // Include inactive: bookmarks on disabled GameObjects must still surface in the menu.
            var allData = Object.FindObjectsByType<HierarchyInspectorData>(FindObjectsInactive.Include);
            for (int i = 0; i < allData.Length; i++)
            {
                if (allData[i] != null && allData[i].IsBookmarked)
                {
                    EntityId id = allData[i].gameObject.GetEntityId();
                    if (_bookmarkedIDs.Add(id))
                        _bookmarkOrder.Add(id);
                }
            }
        }

        public static void ShowBookmarkMenuForScene(Scene scene)
        {
            RebuildFromScene();

            var menu = new GenericMenu();
            int matchCount = 0;

            for (int i = 0; i < _bookmarkOrder.Count; i++)
            {
                EntityId id = _bookmarkOrder[i];
                var go = EditorUtility.EntityIdToObject(id) as GameObject;
                if (go == null) continue;
                if (go.scene != scene) continue;

                EntityId captured = id;
                menu.AddItem(new GUIContent(go.name), false, () =>
                {
                    var resolved = EditorUtility.EntityIdToObject(captured) as GameObject;
                    if (resolved == null) return;
                    Selection.activeGameObject = resolved;
                    EditorGUIUtility.PingObject(resolved);
                });
                matchCount++;
            }

            if (matchCount == 0)
                menu.AddDisabledItem(new GUIContent($"(no bookmarks in {scene.name})"));

            menu.ShowAsContext();
        }
    }
}
#endif
