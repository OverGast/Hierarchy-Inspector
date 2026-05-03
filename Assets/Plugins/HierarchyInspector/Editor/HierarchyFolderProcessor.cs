#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace FoundFootage.Editor.Hierarchy
{
    /// <summary>Strips HierarchyInspectorData folders from build scenes and re-parents their children.</summary>
    public class HierarchyFolderProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => -100;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null) return; // editor play mode

            var folders = new List<HierarchyInspectorData>();
            var rootObjects = scene.GetRootGameObjects();
            foreach (var root in rootObjects)
                FindFoldersRecursive(root.transform, folders);

            // Deepest first so nested folders unwind correctly.
            folders.Sort((a, b) => GetDepth(b.transform).CompareTo(GetDepth(a.transform)));

            foreach (var folder in folders)
            {
                if (!folder.IsFolder) continue;

                var folderTransform = folder.transform;
                var parent = folderTransform.parent;
                int siblingIndex = folderTransform.GetSiblingIndex();

                while (folderTransform.childCount > 0)
                {
                    var child = folderTransform.GetChild(folderTransform.childCount - 1);
                    child.SetParent(parent, worldPositionStays: true);
                    child.SetSiblingIndex(siblingIndex);
                }

                Object.DestroyImmediate(folder.gameObject);
            }
        }

        private static void FindFoldersRecursive(Transform t, List<HierarchyInspectorData> folders)
        {
            var data = t.GetComponent<HierarchyInspectorData>();
            if (data != null && data.IsFolder)
                folders.Add(data);

            for (int i = 0; i < t.childCount; i++)
                FindFoldersRecursive(t.GetChild(i), folders);
        }

        private static int GetDepth(Transform t)
        {
            int depth = 0;
            while (t.parent != null) { depth++; t = t.parent; }
            return depth;
        }
    }
}
#endif
