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

            // Fallback: prefer themes that live under Assets/ over those in Packages/,
            // so users on UPM installs do not silently end up with a read-only theme
            // selected when their stored EditorPrefs reference is missing.
            var guids = AssetDatabase.FindAssets("t:HierarchyInspectorTheme");
            if (guids != null && guids.Length > 0)
            {
                HierarchyInspectorTheme firstAny = null;
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (string.IsNullOrEmpty(path)) continue;
                    var asset = AssetDatabase.LoadAssetAtPath<HierarchyInspectorTheme>(path);
                    if (asset == null) continue;
                    if (path.StartsWith("Assets/", System.StringComparison.Ordinal))
                        return asset;
                    if (firstAny == null) firstAny = asset;
                }
                if (firstAny != null) return firstAny;
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
