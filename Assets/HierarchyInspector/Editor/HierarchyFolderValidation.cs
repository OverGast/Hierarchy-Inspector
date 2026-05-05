#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SpaceWhale.HierarchyInspector.Editor
{
    /// <summary>Detects components that build-time folder stripping will silently destroy.</summary>
    public static class HierarchyFolderValidation
    {
        // Reused across calls; per-call allocation would land in a hot path.
        [System.ThreadStatic]
        private static List<Component> _scratchBuffer;

        // Held separately so we never alias the public scratch buffer.
        [System.ThreadStatic]
        private static List<Component> _getComponentsBuffer;

        public static bool TryGetStrippedComponents(GameObject folder, List<Component> buffer)
        {
            if (buffer == null) return false;
            buffer.Clear();
            if (folder == null) return false;

            if (_getComponentsBuffer == null)
                _getComponentsBuffer = new List<Component>(8);
            _getComponentsBuffer.Clear();
            folder.GetComponents(_getComponentsBuffer);

            for (int i = 0; i < _getComponentsBuffer.Count; i++)
            {
                var c = _getComponentsBuffer[i];
                if (IsExpectedComponent(c)) continue;
                buffer.Add(c); // null entries (missing scripts) preserved on purpose
            }

            return buffer.Count > 0;
        }

        /// <summary>Allocation-free overload safe to call per-row from hierarchy draw.</summary>
        public static bool HasStrippedComponents(GameObject folder)
        {
            if (_scratchBuffer == null)
                _scratchBuffer = new List<Component>(8);
            return TryGetStrippedComponents(folder, _scratchBuffer);
        }

        public static string FormatComponentList(IReadOnlyList<Component> components)
        {
            if (components == null || components.Count == 0) return string.Empty;

            var sb = new StringBuilder(64);
            for (int i = 0; i < components.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                var c = components[i];
                sb.Append(c == null ? "<Missing Script>" : c.GetType().Name);
            }
            return sb.ToString();
        }

        private static bool IsExpectedComponent(Component c)
        {
            // Null entries (missing scripts) flagged on purpose: they vanish too and the user should know.
            if (c == null) return false;
            if (c is Transform) return true;
            if (c is HierarchyInspectorData) return true;
            return false;
        }
    }
}
#endif
