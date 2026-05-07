#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceWhale.HierarchyInspector.Editor
{
    /// <summary>
    /// Auto-opens once after the asset is first imported. Surfaces the documentation,
    /// the demo scene, and the theme settings so first-time users have a clear path
    /// instead of having to find the Tools menu themselves. On first open we also
    /// duplicate the bundled themes from the package into Assets/HierarchyInspector/Themes/
    /// when running from a UPM install, so users on either install path end up with
    /// editable theme assets in their project.
    /// </summary>
    internal sealed class HierarchyInspectorWelcome : EditorWindow
    {
        private const string ShownPrefKey = "SpaceWhale.HierarchyInspector.Welcome.Shown";
        // GUID lookup keeps "Open Demo Scene" working whether the package was installed
        // as a .unitypackage (Assets/...) or via UPM (Packages/...).
        private const string DemoSceneGuid = "2980097aef4c4ef44b039166a2f40abc";
        private const string PreferencesPath = "Preferences/Hierarchy Inspector";
        private const string ThemesAssetFolder = "Assets/HierarchyInspector/Themes";
        private const string DefaultThemeFileName = "HierarchyTheme-Default.asset";

        private static GUIStyle s_titleStyle;
        private static GUIStyle s_versionStyle;
        private static GUIStyle s_bodyStyle;

        // ─── auto-open on first import ───────────────────────────────────────

        [InitializeOnLoadMethod]
        private static void TryAutoOpenOnFirstLoad()
        {
            if (EditorPrefs.GetBool(ShownPrefKey, false)) return;
            // Defer until the editor is fully booted; opening a window during the
            // initial domain reload is rejected by Unity.
            EditorApplication.delayCall += AutoOpen;
        }

        private static void AutoOpen()
        {
            EditorApplication.delayCall -= AutoOpen;
            if (EditorPrefs.GetBool(ShownPrefKey, false)) return;
            EditorPrefs.SetBool(ShownPrefKey, true);

            // Bring bundled themes into the user's Assets/ folder before showing the
            // window, so the "Open Theme Settings" button leads to editable assets.
            BootstrapBundledThemes();

            ShowWindow();
        }

        // ─── manual open ────────────────────────────────────────────────────

        [MenuItem("Tools/Hierarchy Inspector/Welcome")]
        public static void ShowWindow()
        {
            var window = GetWindow<HierarchyInspectorWelcome>(true, "Welcome to Hierarchy Inspector");
            // Fixed-size window so the layout doesn't break on resize.
            var size = new Vector2(440, 280);
            window.minSize = size;
            window.maxSize = size;
            window.ShowUtility();
        }

        // ─── drawing ────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EnsureStyles();

            GUILayout.Space(18);
            GUILayout.Label("Hierarchy Inspector", s_titleStyle);
            GUILayout.Label("v" + HierarchyInspectorVersion.Version, s_versionStyle);

            GUILayout.Space(14);
            GUILayout.Label(
                "Thanks for installing! Hierarchy Inspector adds row visuals, per-object styling, " +
                "virtualization folders, bookmarks, and a style clipboard to Unity's Hierarchy window.",
                s_bodyStyle);

            GUILayout.Space(18);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(20);
                using (new EditorGUILayout.VerticalScope())
                {
                    if (GUILayout.Button("Open Documentation", GUILayout.Height(28)))
                        Application.OpenURL(HierarchyInspectorVersion.DocumentationUrl);

                    GUILayout.Space(6);

                    if (GUILayout.Button("Open Demo Scene", GUILayout.Height(28)))
                        OpenDemoScene();

                    GUILayout.Space(6);

                    if (GUILayout.Button("Open Theme Settings", GUILayout.Height(28)))
                        SettingsService.OpenUserPreferences(PreferencesPath);
                }
                GUILayout.Space(20);
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label(
                "Reopen this window any time via Tools → Hierarchy Inspector → Welcome.",
                EditorStyles.centeredGreyMiniLabel);

            GUILayout.Space(8);
        }

        private static void EnsureStyles()
        {
            if (s_titleStyle != null) return;

            s_titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
            };

            s_versionStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
            };
            s_versionStyle.normal.textColor = new Color(0.55f, 0.55f, 0.55f);

            s_bodyStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(20, 20, 0, 0),
            };
        }

        private static void OpenDemoScene()
        {
            string path = AssetDatabase.GUIDToAssetPath(DemoSceneGuid);
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                Debug.LogWarning(
                    "[Hierarchy Inspector] Demo scene not found. The asset's folder may have been moved.");
                return;
            }

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        // ─── theme bootstrap (UPM install) ───────────────────────────────────

        // Copies the bundled theme .asset files from the package's read-only Themes/
        // folder into Assets/HierarchyInspector/Themes/ and switches the active theme
        // to the writable Default copy. No-op for .unitypackage installs (the search
        // finds nothing under Packages/) and idempotent across runs (existing themes
        // are never overwritten).
        private static void BootstrapBundledThemes()
        {
            var bundledGuids = AssetDatabase.FindAssets(
                "t:HierarchyInspectorTheme", new[] { "Packages" });

            if (bundledGuids == null || bundledGuids.Length == 0)
                return; // .unitypackage install ; themes already in Assets/

            EnsureFolder(ThemesAssetFolder);

            string defaultDstPath = null;
            foreach (var guid in bundledGuids)
            {
                string srcPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(srcPath)) continue;

                string fileName = Path.GetFileName(srcPath);
                string dstPath = $"{ThemesAssetFolder}/{fileName}";

                bool alreadyExists = AssetDatabase.LoadAssetAtPath<HierarchyInspectorTheme>(dstPath) != null;

                if (!alreadyExists)
                {
                    if (!AssetDatabase.CopyAsset(srcPath, dstPath))
                    {
                        Debug.LogWarning(
                            "[Hierarchy Inspector] Failed to copy bundled theme " + srcPath +
                            " into " + dstPath + ". Recreate it via " +
                            "Edit > Preferences > Hierarchy Inspector > Create New Theme.");
                        continue;
                    }
                }

                if (fileName == DefaultThemeFileName)
                    defaultDstPath = dstPath;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!string.IsNullOrEmpty(defaultDstPath))
            {
                var defaultCopy = AssetDatabase.LoadAssetAtPath<HierarchyInspectorTheme>(defaultDstPath);
                if (defaultCopy != null)
                    HierarchyThemeProvider.SetActive(defaultCopy);
            }
        }

        // Recursive folder creation via AssetDatabase so the new folders are picked
        // up immediately without a full Refresh().
        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string leaf = Path.GetFileName(assetPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;
            EnsureFolder(parent);
            if (!AssetDatabase.IsValidFolder(assetPath))
                AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
