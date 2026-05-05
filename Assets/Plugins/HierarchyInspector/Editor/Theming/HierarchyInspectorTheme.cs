#if UNITY_EDITOR
using System;
using UnityEngine;

namespace FoundFootage.Editor.Hierarchy
{
    /// <summary>ScriptableObject holding all hierarchy overlay configuration.</summary>
    [CreateAssetMenu(fileName = "HierarchyTheme", menuName = "Tools/Hierarchy Inspector/Theme", order = 101)]
    public class HierarchyInspectorTheme : ScriptableObject
    {
        public static event Action OnThemeChanged;

        [Tooltip("Master switch. When false, the overlay unsubscribes all callbacks and renders nothing.")]
        [SerializeField] private bool enabled = true;

        [Tooltip("Alternating row stripe shading.")]
        [SerializeField] private bool alternatingRows = true;
        [Tooltip("Highlight the row under the mouse cursor.")]
        [SerializeField] private bool rowHover = true;
        [Tooltip("Subtle 1px borders between rows.")]
        [SerializeField] private bool rowBorders = false;
        [Tooltip("Dim inactive (disabled) GameObject rows.")]
        [SerializeField] private bool inactiveDimming = true;
        [Tooltip("Color fades left-to-right on rows that have a custom color.")]
        [SerializeField] private bool gradientBackground = false;
        [Tooltip("Thin color bar on the left edge of colored rows.")]
        [SerializeField] private bool leftColorStripe = true;
        [Tooltip("Thin accent line drawn on colored rows.")]
        [SerializeField] private bool accentLine = false;
        [Tooltip("Soft drop-shadow at the left edge of each nesting level.")]
        [SerializeField] private bool depthShadow = true;
        [Tooltip("Glow effect on selected rows.")]
        [SerializeField] private bool selectedRowGlow = true;

        [Tooltip("Row body color used on even rows when Unity's editor is set to the dark skin.")]
        [SerializeField] private Color rowColorDarkPrimary = new Color(0.220f, 0.220f, 0.220f, 1f);
        [Tooltip("Row body color used on odd (alternating) rows when Unity's editor is set to the dark skin.")]
        [SerializeField] private Color rowColorDarkSecondary = new Color(0.255f, 0.255f, 0.255f, 1f);

        [Tooltip("Row body color used on even rows when Unity's editor is set to the light skin.")]
        [SerializeField] private Color rowColorLightPrimary = new Color(0.760f, 0.760f, 0.760f, 1f);
        [Tooltip("Row body color used on odd (alternating) rows when Unity's editor is set to the light skin.")]
        [SerializeField] private Color rowColorLightSecondary = new Color(0.715f, 0.715f, 0.715f, 1f);

        [Tooltip("Multiplier applied to row colors when the GameObject is inactive (0 = fully dimmed, 1 = no dim).")]
        [Range(0f, 1f)]
        [SerializeField] private float inactiveDimAlpha = 0.4f;

        [Tooltip("Row body color used when the hierarchy window has keyboard focus.")]
        [SerializeField] private Color selectionColorFocused = new Color(0.172f, 0.365f, 0.529f, 1f);
        [Tooltip("Row body color used when the hierarchy window does not have keyboard focus.")]
        [SerializeField] private Color selectionColorUnfocused = new Color(0.30f, 0.30f, 0.30f, 1f);

        [Tooltip("Draw connector lines between parent and child rows.")]
        [SerializeField] private bool treeLines = true;
        [Tooltip("Color of the tree connector lines drawn between parent and child rows.")]
        [SerializeField] private Color treeLineColor = new Color(0.35f, 0.35f, 0.35f);

        [Tooltip("Show component icons in the row gutter.")]
        [SerializeField] private bool componentIcons = true;
        [Tooltip("Maximum component icons to render per row.")]
        [SerializeField] private int maxIcons = 5;
        [Tooltip("Pixel size of component icons rendered in the row gutter.")]
        [SerializeField] private float iconSize = 16f;
        [Tooltip("Brighten icons under the mouse cursor.")]
        [SerializeField] private bool iconGlow = true;
        [Tooltip("Click an icon to toggle the component's enabled state.")]
        [SerializeField] private bool componentQuickToggle = true;
        [Tooltip("Tint component icons with the row's custom color.")]
        [SerializeField] private bool iconTinting = false;

        [Tooltip("Auto-tint prefab instance rows.")]
        [SerializeField] private bool prefabTinting = true;
        [Tooltip("Show a dot indicator next to prefab instances that have unsaved overrides.")]
        [SerializeField] private bool prefabOverrideDot = true;
        [Tooltip("Red highlight when a GameObject has a missing script component.")]
        [SerializeField] private bool missingScriptWarning = true;
        [Tooltip("Yellow indicator when a serialized reference field is null.")]
        [SerializeField] private bool missingReferences = true;
        [Tooltip("Auto-detect ---Name--- separator objects and render them as dividers.")]
        [SerializeField] private bool separatorDetection = true;

        [Tooltip("Tint blended into prefab instance rows when prefab tinting is enabled.")]
        [SerializeField] private Color prefabTintColor = new Color(0.3f, 0.5f, 0.8f, 1f);
        [Tooltip("Tint blended into rows that have a missing script component.")]
        [SerializeField] private Color missingScriptTintColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        [Tooltip("Color of the prefab override dot drawn next to the GameObject icon.")]
        [SerializeField] private Color prefabOverrideDotColor = new Color(0.3f, 0.6f, 1.0f, 1f);

        [Tooltip("Allow per-object styling via the gear popup.")]
        [SerializeField] private bool perObjectStyling = true;
        [Tooltip("Auto-style rows that contain only child objects (treat as folders).")]
        [SerializeField] private bool folderStyling = true;
        [Tooltip("Use the first non-Transform component icon for GameObjects without a custom icon.")]
        [SerializeField] private bool useMainComponentIcon = true;
        [Tooltip("Pixel size of the GameObject's primary icon. Affects override-dot, label start, and bookmark badge positions.")]
        [Range(12f, 32f)]
        [SerializeField] private float mainIconSize = 16f;
        [Tooltip("Show the eye/lock active toggle column.")]
        [SerializeField] private bool activeToggle = true;

        [Tooltip("Slight indent animation when the row is hovered.")]
        [SerializeField] private bool hoverSlide = true;
        [Tooltip("Fade-in animation for newly created GameObjects.")]
        [SerializeField] private bool fadeInAnimation = true;
        [Tooltip("Brief flash when a GameObject is renamed.")]
        [SerializeField] private bool renameFlash = true;

        [Tooltip("Tint color applied to the foldout arrow drawn on top of the row body.")]
        [SerializeField] private Color foldoutArrowColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        [Tooltip("Tint color applied to the per-object settings (gear) icon.")]
        [SerializeField] private Color gearIconColor = new Color(1f, 1f, 1f, 1f);

        public bool Enabled => enabled;

        public bool AlternatingRows => alternatingRows;
        public bool RowHover => rowHover;
        public bool RowBorders => rowBorders;
        public bool InactiveDimming => inactiveDimming;
        public bool GradientBackground => gradientBackground;
        public bool LeftColorStripe => leftColorStripe;
        public bool AccentLine => accentLine;
        public bool DepthShadow => depthShadow;
        public bool SelectedRowGlow => selectedRowGlow;

        public Color RowColorDarkPrimary => rowColorDarkPrimary;
        public Color RowColorDarkSecondary => rowColorDarkSecondary;
        public Color RowColorLightPrimary => rowColorLightPrimary;
        public Color RowColorLightSecondary => rowColorLightSecondary;

        public Color RowColorPrimary =>
            UnityEditor.EditorGUIUtility.isProSkin ? rowColorDarkPrimary : rowColorLightPrimary;
        public Color RowColorSecondary =>
            UnityEditor.EditorGUIUtility.isProSkin ? rowColorDarkSecondary : rowColorLightSecondary;

        public float InactiveDimAlpha => inactiveDimAlpha;

        public Color SelectionColorFocused => selectionColorFocused;
        public Color SelectionColorUnfocused => selectionColorUnfocused;

        public bool TreeLines => treeLines;
        public Color TreeLineColor => treeLineColor;

        public bool ComponentIcons => componentIcons;
        public int MaxIcons => maxIcons;
        public float IconSize => iconSize;
        public bool IconGlow => iconGlow;
        public bool ComponentQuickToggle => componentQuickToggle;
        public bool IconTinting => iconTinting;

        public bool PrefabTinting => prefabTinting;
        public bool PrefabOverrideDot => prefabOverrideDot;
        public bool MissingScriptWarning => missingScriptWarning;
        public bool MissingReferences => missingReferences;
        public bool SeparatorDetection => separatorDetection;

        public Color PrefabTintColor => prefabTintColor;
        public Color MissingScriptTintColor => missingScriptTintColor;
        public Color PrefabOverrideDotColor => prefabOverrideDotColor;

        public bool PerObjectStyling => perObjectStyling;
        public bool FolderStyling => folderStyling;
        public bool UseMainComponentIcon => useMainComponentIcon;
        public float MainIconSize => mainIconSize;
        public bool ActiveToggle => activeToggle;

        public bool HoverSlide => hoverSlide;
        public bool FadeInAnimation => fadeInAnimation;
        public bool RenameFlash => renameFlash;

        public Color FoldoutArrowColor => foldoutArrowColor;
        public Color GearIconColor => gearIconColor;

        public void ResetToDefaults() => HierarchyThemeDefaults.Apply(this);

        private void OnValidate() => OnThemeChanged?.Invoke();
    }
}
#endif
