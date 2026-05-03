#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace FoundFootage.Editor.Hierarchy
{
    /// <summary>Shared icon palette for HierarchyOverlay, HierarchyItemPopup, and other consumers.</summary>
    public static class HierarchyIconPalette
    {
        // First 10 entries are stable across versions; serialized iconIndex values in scenes depend on it.
        // New entries are appended at the end only.
        public static readonly string[] IconNames =
        {
            // Legacy palette (indices 0-9, do not reorder)
            "Folder Icon",                  // 0  Folder
            "d_Favorite Icon",              // 1  Star
            "d_FilterByLabel",              // 2  Tag
            "d_FilterByType",               // 3  Type
            "d_ViewToolOrbit",              // 4  Eye
            "d_SceneViewCamera",            // 5  Camera
            "d_console.warnicon.sml",       // 6  Warning
            "d_console.erroricon.sml",      // 7  Error
            "d_Profiler.Audio",             // 8  Audio
            "d_AreaLight Icon",             // 9  Light

            "d_Prefab Icon",                // 10 Prefab
            "d_GameObject Icon",            // 11 Object
            "d_ScriptableObject Icon",      // 12 Data
            "d_cs Script Icon",             // 13 Script
            "d_Mesh Icon",                  // 14 Mesh
            "d_Material Icon",              // 15 Material
            "d_Animation Icon",             // 16 Animation
            "d_Animator Icon",              // 17 Animator
            "d_ParticleSystem Icon",        // 18 Particles
            "d_Terrain Icon",               // 19 Terrain
            "d_Canvas Icon",                // 20 UI Canvas
            "d_EventSystem Icon",           // 21 Event
            "d_NavMeshAgent Icon",          // 22 NavMesh
            "d_WindZone Icon",              // 23 Wind
            "d_RenderTexture Icon"          // 24 Render
        };

        public static readonly string[] IconTooltips =
        {
            "Folder", "Star", "Tag", "Type", "Eye", "Camera", "Warning", "Error", "Audio", "Light",
            "Prefab", "Object", "Data", "Script", "Mesh", "Material", "Animation", "Animator",
            "Particles", "Terrain", "UI Canvas", "Event", "NavMesh", "Wind", "Render"
        };

        public static int Count => IconNames.Length;

        private static Texture[] _cachedIcons;

        public static Texture GetIcon(int index)
        {
            if (index < 0 || index >= IconNames.Length)
                return null;

            EnsureCached();
            return _cachedIcons[index];
        }

        public static Texture[] GetAllIcons()
        {
            EnsureCached();
            return _cachedIcons;
        }

        private static void EnsureCached()
        {
            if (_cachedIcons != null) return;

            _cachedIcons = new Texture[IconNames.Length];
            for (int i = 0; i < IconNames.Length; i++)
            {
                var content = EditorGUIUtility.IconContent(IconNames[i]);
                _cachedIcons[i] = content != null ? content.image : null;
            }
        }
    }
}
#endif
