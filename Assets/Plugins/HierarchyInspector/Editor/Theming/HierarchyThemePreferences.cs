#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FoundFootage.Editor.Hierarchy
{
    /// <summary>Per-user Preferences pane for picking the active <see cref="HierarchyInspectorTheme"/>.</summary>
    internal static class HierarchyThemePreferences
    {
        private const string PreferencesPath = "Preferences/Hierarchy Decorator";
        private const string DefaultThemeFolder = "Assets/Editor/Themes/Hierarchy";

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new SettingsProvider(PreferencesPath, SettingsScope.User)
            {
                label = "Hierarchy Decorator",
                guiHandler = _ => DrawGUI(),
                keywords = new[] { "hierarchy", "decorator", "theme", "editor", "tree", "stripes" }
            };
        }

        private static void DrawGUI()
        {
            EditorGUILayout.LabelField("Active Theme", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            var current = HierarchyThemeProvider.Active;
            var newSelection = (HierarchyInspectorTheme)EditorGUILayout.ObjectField(
                "Theme Asset", current, typeof(HierarchyInspectorTheme), false);

            if (newSelection != current)
                HierarchyThemeProvider.SetActive(newSelection);

            EditorGUILayout.Space(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create New Theme", GUILayout.Width(160)))
                    CreateNewTheme();

                if (current != null && AssetDatabase.Contains(current))
                {
                    if (GUILayout.Button("Reveal Asset", GUILayout.Width(120)))
                        EditorGUIUtility.PingObject(current);

                    if (GUILayout.Button("Reset to Defaults", GUILayout.Width(140)))
                    {
                        Undo.RecordObject(current, "Reset Hierarchy Decorator Theme");
                        current.ResetToDefaults();
                        EditorUtility.SetDirty(current);
                        AssetDatabase.SaveAssetIfDirty(current);
                    }
                }
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.HelpBox(
                "The active theme selection is stored per-user in EditorPrefs; the theme assets " +
                "themselves are project files. Edit the asset directly to tweak colors and sizes; " +
                "all open hierarchy windows live-update.",
                MessageType.Info);

            if (current != null && AssetDatabase.Contains(current))
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Theme Inspector", EditorStyles.boldLabel);
                var editor = UnityEditor.Editor.CreateEditor(current);
                if (editor != null)
                    editor.OnInspectorGUI();
            }
        }

        private static void CreateNewTheme()
        {
            if (!AssetDatabase.IsValidFolder(DefaultThemeFolder))
            {
                Directory.CreateDirectory(DefaultThemeFolder);
                AssetDatabase.Refresh();
            }

            var asset = ScriptableObject.CreateInstance<HierarchyInspectorTheme>();
            HierarchyThemeDefaults.Apply(asset);

            string path = AssetDatabase.GenerateUniqueAssetPath($"{DefaultThemeFolder}/HierarchyTheme.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            HierarchyThemeProvider.SetActive(asset);
            EditorGUIUtility.PingObject(asset);
        }
    }
}
#endif
