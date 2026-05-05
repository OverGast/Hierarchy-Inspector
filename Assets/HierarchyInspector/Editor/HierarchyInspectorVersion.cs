#if UNITY_EDITOR
namespace SpaceWhale.HierarchyInspector.Editor
{
    /// <summary>Single source of truth for version + external resource URLs.</summary>
    internal static class HierarchyInspectorVersion
    {
        public const string Version = "1.0.0";

        // TODO: replace with the live GitBook URL once the space is published.
        public const string DocumentationUrl = "https://hierarchy-inspector.gitbook.io/hierarchy-inspector";
    }
}
#endif
