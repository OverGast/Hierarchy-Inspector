using UnityEngine;

/// <summary>Per-object hierarchy styling data; stripped from builds. Added/removed via the overlay popup.</summary>
[AddComponentMenu("")]
[DisallowMultipleComponent]
public class HierarchyInspectorData : MonoBehaviour
{
    [SerializeField] private bool isFolder;
    [SerializeField] private bool useCustomColor;
    [SerializeField] private Color backgroundColor = new Color(0.95f, 0.85f, 0.45f, 1f);
    [SerializeField] private bool useCustomIcon;
    [SerializeField] private int iconIndex;
    [SerializeField] private string note = "";
    [SerializeField] private bool isSeparator;
    [SerializeField] private Color separatorColor = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] private bool isBookmarked;
    [SerializeField] private Texture2D customProjectIcon;

    public bool IsFolder => isFolder;
    public bool UseCustomColor => useCustomColor;
    public Color BackgroundColor => backgroundColor;
    public bool UseCustomIcon => useCustomIcon;
    public int IconIndex => iconIndex;
    public string Note => note;
    public bool IsSeparator => isSeparator;
    public Color SeparatorColor => separatorColor;
    public bool IsBookmarked => isBookmarked;
    public Texture2D CustomProjectIcon => customProjectIcon;

#if UNITY_EDITOR
    public void SetIsFolder(bool value) { isFolder = value; }

    public void SetCustomColor(bool enabled, Color color)
    {
        useCustomColor = enabled;
        backgroundColor = color;
    }

    public void SetCustomIcon(bool enabled, int index)
    {
        useCustomIcon = enabled;
        iconIndex = index;
    }

    public void SetNote(string value) { note = value; }
    public void SetIsSeparator(bool value) { isSeparator = value; }
    public void SetSeparatorColor(Color value) { separatorColor = value; }
    public void SetIsBookmarked(bool value) { isBookmarked = value; }
    public void SetCustomProjectIcon(Texture2D value) { customProjectIcon = value; }

    public bool HasAnyCustomization => isFolder || useCustomColor || useCustomIcon ||
        !string.IsNullOrEmpty(note) || isSeparator || isBookmarked || customProjectIcon != null;

    private void Reset()
    {
        hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInBuild;
    }

    private void OnValidate()
    {
        hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInBuild;
    }
#endif
}
