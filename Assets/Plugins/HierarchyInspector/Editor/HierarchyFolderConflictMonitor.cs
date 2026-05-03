#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FoundFootage.Editor.Hierarchy
{
    /// <summary>Logs an error when a virtualized folder gains user-added components that build-time stripping would silently destroy.</summary>
    [InitializeOnLoad]
    public static class HierarchyFolderConflictMonitor
    {
        // Only logs on count INCREASE; cleared per-instance when the GameObject stops being a folder.
        private static readonly Dictionary<EntityId, int> _previousExtraCount = new Dictionary<EntityId, int>();

        // Editor main thread only, single static buffer is safe.
        private static readonly List<Component> _scratchBuffer = new List<Component>(8);

        static HierarchyFolderConflictMonitor()
        {
            ObjectChangeEvents.changesPublished += OnChangesPublished;
        }

        private static void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            int length = stream.length;
            for (int i = 0; i < length; i++)
            {
                switch (stream.GetEventType(i))
                {
                    case ObjectChangeKind.ChangeGameObjectStructure:
                    {
                        stream.GetChangeGameObjectStructureEvent(i, out var evt);
                        EvaluateInstance(evt.entityId);
                        break;
                    }
                    case ObjectChangeKind.ChangeGameObjectOrComponentProperties:
                    {
                        stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var evt);
                        EvaluateInstance(evt.entityId);
                        break;
                    }
                    case ObjectChangeKind.DestroyGameObjectHierarchy:
                    {
                        stream.GetDestroyGameObjectHierarchyEvent(i, out var evt);
                        _previousExtraCount.Remove(evt.entityId);
                        break;
                    }
                }
            }
        }

        private static void EvaluateInstance(EntityId entityId)
        {
            var obj = EditorUtility.EntityIdToObject(entityId);
            GameObject go = obj as GameObject;
            if (go == null && obj is Component c)
                go = c.gameObject;
            if (go == null) return;

            EntityId id = go.GetEntityId();
            var data = go.GetComponent<HierarchyInspectorData>();
            if (data == null || !data.IsFolder)
            {
                _previousExtraCount.Remove(id);
                return;
            }

            HierarchyFolderValidation.TryGetStrippedComponents(go, _scratchBuffer);
            int currentCount = _scratchBuffer.Count;
            int previousCount = _previousExtraCount.TryGetValue(id, out var p) ? p : 0;

            if (currentCount > previousCount)
            {
                string list = HierarchyFolderValidation.FormatComponentList(_scratchBuffer);
                Debug.LogError(
                    $"[Hierarchy Decorator] Virtualized folder '{go.name}' has components " +
                    $"that will be silently stripped at build time: {list}. Move them to " +
                    "a child GameObject or remove the Virtualized Folder marker.",
                    go);
            }

            _previousExtraCount[id] = currentCount;
        }
    }
}
#endif
