#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SpaceWhale.HierarchyInspector.Editor
{
    /// <summary>Resolves the active <see cref="HierarchyInspectorTheme"/> for overlay draw partials.</summary>
    public static class HierarchyThemeProvider
    {
        private const string ActiveGuidKey = "HierarchyTheme.ActiveGuid";

        private static HierarchyInspectorTheme _cached;
        private static HierarchyInspectorTheme _inMemoryDefault;

        public static HierarchyInspectorTheme Active
        {
            get
            {
                if (_cached != null) return _cached;
                _cached = Resolve();
                return _cached;
            }
        }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.projectChanged += InvalidateCache;
            HierarchyInspectorTheme.OnThemeChanged += OnThemeMutated;
        }

        public static void SetActive(HierarchyInspectorTheme theme)
        {
            if (theme == null)
            {
                EditorPrefs.DeleteKey(ActiveGuidKey);
            }
            else
            {
                string path = AssetDatabase.GetAssetPath(theme);
                string guid = AssetDatabase.AssetPathToGUID(path);
                EditorPrefs.SetString(ActiveGuidKey, guid);
            }
            InvalidateCache();
        }

        public static void InvalidateCache()
        {
            _cached = null;
            EditorApplication.RepaintHierarchyWindow();
        }

        private static void OnThemeMutated()
        {
            EditorApplication.RepaintHierarchyWindow();
        }

        private static HierarchyInspectorTheme Resolve()
        {
            string storedGuid = EditorPrefs.GetString(ActiveGuidKey, null);
            if (!string.IsNullOrEmpty(storedGuid))
            {
                string path = AssetDatabase.GUIDToAssetPath(storedGuid);
                if (!string.IsNullOrEmpty(path))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<HierarchyInspectorTheme>(path);
                    if (asset != null) return asset;
                }
            }

            var guids = AssetDatabase.FindAssets("t:HierarchyInspectorTheme");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var asset = AssetDatabase.LoadAssetAtPath<HierarchyInspectorTheme>(path);
                if (asset != null) return asset;
            }

            if (_inMemoryDefault == null)
            {
                _inMemoryDefault = ScriptableObject.CreateInstance<HierarchyInspectorTheme>();
                _inMemoryDefault.hideFlags = HideFlags.DontSave;
                HierarchyThemeDefaults.Apply(_inMemoryDefault);
            }
            return _inMemoryDefault;
        }
    }
}
#endif
