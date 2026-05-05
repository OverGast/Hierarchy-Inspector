#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceWhale.HierarchyInspector.Editor
{
    /// <summary>
    /// Auto-opens once after the asset is first imported. Surfaces the documentation,
    /// the demo scene, and the theme settings so first-time users have a clear path
    /// instead of having to find the Tools menu themselves.
    /// </summary>
    internal sealed class HierarchyInspectorWelcome : EditorWindow
    {
        private const string ShownPrefKey = "SpaceWhale.HierarchyInspector.Welcome.Shown";
        private const string DemoScenePath = "Assets/HierarchyInspector/Demo/DemoScene.unity";
        private const string PreferencesPath = "Preferences/Hierarchy Inspector";

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
            if (System.IO.File.Exists(DemoScenePath))
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
            }
            else
            {
                Debug.LogWarning(
                    "[Hierarchy Inspector] Demo scene not found at " + DemoScenePath +
                    ". The asset's folder may have been moved.");
            }
        }
    }
}
#endif
