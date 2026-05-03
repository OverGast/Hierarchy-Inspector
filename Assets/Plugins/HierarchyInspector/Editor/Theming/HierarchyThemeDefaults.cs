#if UNITY_EDITOR
using UnityEngine;

namespace FoundFootage.Editor.Hierarchy
{
    /// <summary>Built-in default values for <see cref="HierarchyInspectorTheme"/>, sourced from a fresh template via JSON roundtrip.</summary>
    internal static class HierarchyThemeDefaults
    {
        internal static void Apply(HierarchyInspectorTheme target)
        {
            if (target == null) return;
            var template = ScriptableObject.CreateInstance<HierarchyInspectorTheme>();
            try
            {
                string json = JsonUtility.ToJson(template);
                JsonUtility.FromJsonOverwrite(json, target);
            }
            finally
            {
                Object.DestroyImmediate(template);
            }
        }
    }
}
#endif
