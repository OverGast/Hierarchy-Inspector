#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SpaceWhale.HierarchyInspector.Editor
{
    /// <summary>Per-object hierarchy customization popup. Supports multi-select edits.</summary>
    public sealed class HierarchyItemPopup : PopupWindowContent
    {
        private readonly GameObject[] _targets;
        private HierarchyInspectorData _primaryData;
        private readonly List<Component> _strippedComponentsBuffer = new List<Component>(8);

        private static readonly Color[] ColorPresets =
        {
            new Color(0.85f, 0.30f, 0.30f), // Red
            new Color(0.90f, 0.55f, 0.20f), // Orange
            new Color(0.90f, 0.80f, 0.25f), // Yellow
            new Color(0.40f, 0.75f, 0.35f), // Green
            new Color(0.30f, 0.75f, 0.80f), // Cyan
            new Color(0.35f, 0.55f, 0.90f), // Blue
            new Color(0.60f, 0.40f, 0.85f), // Purple
            new Color(0.85f, 0.45f, 0.65f), // Pink
        };

        private const float WindowWidth = 260f;
        private const float Margin = 10f;
        private const float ChipWidth = 24f;
        private const float ChipHeight = 16f;
        private const float ChipGap = 4f;
        private const float ClearBtnSize = 16f;
        private const int CornerRadius = 4;

        private const float BaseHeight = 215f;
        private const float NotesSectionHeight = 30f;
        private const float ProjectIconSectionHeight = 30f;

        private static Texture2D _chipFillTex;
        private static Texture2D _chipOutlineTex;
        private static GUIStyle _titleStyle;
        private static GUIStyle _sectionStyle;

        public HierarchyItemPopup(GameObject[] targets)
        {
            _targets = targets;
            if (_targets.Length > 0)
                _primaryData = _targets[0].GetComponent<HierarchyInspectorData>();
            EnsureCached();
        }

        public override Vector2 GetWindowSize()
        {
            float height = BaseHeight + NotesSectionHeight;
            if (HierarchyThemeProvider.Active.PerObjectStyling)
                height += ProjectIconSectionHeight;
            return new Vector2(WindowWidth, height);
        }

        private static void EnsureCached()
        {
            if (_chipFillTex == null)
                _chipFillTex = CreateRoundedRectTexture(ChipWidth, ChipHeight, CornerRadius, true);

            if (_chipOutlineTex == null)
                _chipOutlineTex = CreateRoundedRectTexture(ChipWidth, ChipHeight, CornerRadius, false);

            _titleStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };

            _sectionStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.55f, 0.55f, 0.55f) },
                fontSize = 10
            };
        }

        public override void OnGUI(Rect rect)
        {
            if (_targets == null || _targets.Length == 0) { editorWindow.Close(); return; }
            if (_targets[0] == null) { editorWindow.Close(); return; }
            if (_primaryData == null) _primaryData = _targets[0].GetComponent<HierarchyInspectorData>();

            bool isMultiSelect = _targets.Length > 1;

            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Margin);
            string title = isMultiSelect ? $"{_targets.Length} objects selected" : _targets[0].name;
            EditorGUILayout.LabelField(title, _titleStyle);
            EditorGUILayout.EndHorizontal();

            DrawSeparator();

            DrawSectionLabel("COLOR");
            GUILayout.Space(2);
            DrawColorRow();

            GUILayout.Space(8);

            DrawSectionLabel("ICON");
            GUILayout.Space(2);
            DrawIconRows();

            if (HierarchyThemeProvider.Active.PerObjectStyling)
            {
                GUILayout.Space(4);
                DrawProjectIconPicker();
            }

            GUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Margin);
            DrawFolderButton();
            GUILayout.Space(4);
            DrawBookmarkButton();
            GUILayout.Space(4);
            DrawClearButton();
            GUILayout.Space(Margin);
            EditorGUILayout.EndHorizontal();

            DrawVirtualizedFolderWarning();

            GUILayout.Space(4);

            DrawSectionLabel("NOTES");
            DrawNoteField();

            GUILayout.Space(6);

            // Hover feedback needs explicit repaints because PopupWindow doesn't track mouse motion.
            if (Event.current.type == EventType.MouseMove)
                editorWindow.Repaint();
        }

        private static void DrawSectionLabel(string label)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Margin);
            EditorGUILayout.LabelField(label, _sectionStyle, GUILayout.Height(14));
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawSeparator()
        {
            GUILayout.Space(2);
            var separatorRect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(separatorRect, new Color(0.3f, 0.3f, 0.3f));
            GUILayout.Space(4);
        }

        private void DrawColorRow()
        {
            int selectedIdx = -1;
            if (_primaryData != null && _primaryData.UseCustomColor)
            {
                for (int i = 0; i < ColorPresets.Length; i++)
                {
                    if (ColorsMatch(_primaryData.BackgroundColor, ColorPresets[i])) { selectedIdx = i; break; }
                }
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Margin);

            for (int i = 0; i < ColorPresets.Length; i++)
            {
                if (DrawChip(ColorPresets[i], null, i == selectedIdx))
                {
                    var color = ColorPresets[i];
                    ApplyToAllTargets("Set Hierarchy Color", d => d.SetCustomColor(true, color));
                    RepaintHierarchy();
                }
                GUILayout.Space(ChipGap);
            }

            GUILayout.FlexibleSpace();

            if (_primaryData != null && _primaryData.UseCustomColor && DrawXButton())
            {
                ApplyToAllTargets("Clear Hierarchy Color", d => d.SetCustomColor(false, d.BackgroundColor));
                RepaintHierarchy();
            }

            GUILayout.Space(Margin);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawIconRows()
        {
            int selectedIdx = _primaryData != null && _primaryData.UseCustomIcon ? _primaryData.IconIndex : -1;
            int perRow = 5;
            // Subset of the palette so the popup stays compact; full palette is in the project-icon picker.
            int maxPopupIcons = 15;
            int iconCount = Mathf.Min(HierarchyIconPalette.Count, maxPopupIcons);
            int rowCount = Mathf.CeilToInt((float)iconCount / perRow);

            for (int row = 0; row < rowCount; row++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(Margin);

                for (int col = 0; col < perRow; col++)
                {
                    int idx = row * perRow + col;
                    if (idx >= iconCount) break;

                    var icon = HierarchyIconPalette.GetIcon(idx);
                    if (DrawChip(new Color(0.22f, 0.22f, 0.24f), icon, idx == selectedIdx))
                    {
                        int capturedIdx = idx;
                        ApplyToAllTargets("Set Hierarchy Icon", d => d.SetCustomIcon(true, capturedIdx));
                        RepaintHierarchy();
                    }
                    GUILayout.Space(ChipGap);
                }

                GUILayout.FlexibleSpace();

                if (row == 0 && _primaryData != null && _primaryData.UseCustomIcon && DrawXButton())
                {
                    ApplyToAllTargets("Clear Hierarchy Icon", d => d.SetCustomIcon(false, 0));
                    RepaintHierarchy();
                }

                GUILayout.Space(Margin);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(ChipGap);
            }
        }

        private void DrawProjectIconPicker()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Margin);

            EditorGUILayout.LabelField("Custom Icon", _sectionStyle, GUILayout.Width(70), GUILayout.Height(16));

            var currentTex = _primaryData != null ? _primaryData.CustomProjectIcon : null;
            var newTex = (Texture2D)EditorGUILayout.ObjectField(
                currentTex, typeof(Texture2D), false,
                GUILayout.Width(WindowWidth - Margin * 2 - 74f), GUILayout.Height(16));

            if (newTex != currentTex)
            {
                ApplyToAllTargets("Set Project Icon", d => d.SetCustomProjectIcon(newTex));
                RepaintHierarchy();
            }

            GUILayout.Space(Margin);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawNoteField()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Margin);

            string currentNote = _primaryData != null ? (_primaryData.Note ?? "") : "";
            string newNote = EditorGUILayout.TextField(currentNote,
                GUILayout.Width(WindowWidth - Margin * 2), GUILayout.Height(18));

            if (newNote != currentNote)
            {
                ApplyToAllTargets("Set Hierarchy Note", d => d.SetNote(newNote));
                RepaintHierarchy();
            }

            GUILayout.Space(Margin);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFolderButton()
        {
            bool isFolder = _primaryData != null && _primaryData.IsFolder;
            string label = isFolder ? "\u2714 Virtualized Folder" : "Virtualized Folder";

            var prev = GUI.backgroundColor;
            GUI.backgroundColor = isFolder ? new Color(0.35f, 0.6f, 0.35f) : Color.gray;

            if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.Height(20)))
            {
                bool newValue = !isFolder;
                ApplyToAllTargets("Toggle Hierarchy Folder", d =>
                {
                    d.SetIsFolder(newValue);
                    if (newValue)
                    {
                        if (!d.UseCustomIcon) d.SetCustomIcon(true, 0);
                        if (!d.UseCustomColor) d.SetCustomColor(true, new Color(0.90f, 0.80f, 0.25f));
                    }
                });
                RepaintHierarchy();
            }

            GUI.backgroundColor = prev;
        }

        private void DrawVirtualizedFolderWarning()
        {
            if (_primaryData == null || !_primaryData.IsFolder) return;

            var go = _primaryData.gameObject;
            if (!HierarchyFolderValidation.TryGetStrippedComponents(go, _strippedComponentsBuffer))
                return;

            string list = HierarchyFolderValidation.FormatComponentList(_strippedComponentsBuffer);
            EditorGUILayout.HelpBox(
                $"This virtualized folder has components that will be silently stripped " +
                $"at build time:\n  {list}\n\nMove them to a child GameObject, or remove " +
                "the Virtualized Folder marker.",
                MessageType.Warning);
        }

        private void DrawBookmarkButton()
        {
            bool isBookmarked = _primaryData != null && _primaryData.IsBookmarked;
            string label = isBookmarked ? "\u2605" : "\u2606";

            var prev = GUI.backgroundColor;
            GUI.backgroundColor = isBookmarked ? new Color(0.85f, 0.75f, 0.25f) : Color.gray;

            if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.Width(24), GUILayout.Height(20)))
            {
                bool newValue = !isBookmarked;
                ApplyToAllTargets("Toggle Hierarchy Bookmark", d => d.SetIsBookmarked(newValue));
                HierarchyBookmarkManager.MarkDirty();
                RepaintHierarchy();
            }

            GUI.backgroundColor = prev;
        }

        private void DrawClearButton()
        {
            if (_primaryData == null) return;

            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.5f, 0.3f, 0.3f);

            if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Height(20)))
            {
                for (int i = 0; i < _targets.Length; i++)
                {
                    if (_targets[i] == null) continue;
                    var data = _targets[i].GetComponent<HierarchyInspectorData>();
                    if (data != null)
                        Undo.DestroyObjectImmediate(data);
                }
                _primaryData = null;
                RepaintHierarchy();
                editorWindow.Close();
            }

            GUI.backgroundColor = prev;
        }

        // Returns true on click.
        private static bool DrawChip(Color fillColor, Texture icon, bool isSelected)
        {
            var chipRect = GUILayoutUtility.GetRect(ChipWidth, ChipHeight,
                GUILayout.Width(ChipWidth), GUILayout.Height(ChipHeight));

            bool isHovered = chipRect.Contains(Event.current.mousePosition);
            bool clicked = false;

            if (Event.current.type == EventType.MouseDown && isHovered && Event.current.button == 0)
            {
                clicked = true;
                Event.current.Use();
            }

            if (Event.current.type == EventType.Repaint)
            {
                Color drawColor = isHovered ? Color.Lerp(fillColor, Color.white, 0.15f) : fillColor;

                var prevColor = GUI.color;
                GUI.color = drawColor;
                GUI.DrawTexture(chipRect, _chipFillTex);

                Color outlineCol = isSelected ? Color.white
                    : isHovered ? new Color(1f, 1f, 1f, 0.45f)
                    : new Color(1f, 1f, 1f, 0.12f);
                GUI.color = outlineCol;
                GUI.DrawTexture(chipRect, _chipOutlineTex);

                GUI.color = prevColor;

                if (icon != null)
                {
                    float pad = 2f;
                    float s = Mathf.Min(chipRect.width, chipRect.height) - pad * 2;
                    var iconRect = new Rect(chipRect.x + (chipRect.width - s) / 2f,
                        chipRect.y + (chipRect.height - s) / 2f, s, s);
                    GUI.DrawTexture(iconRect, icon);
                }
            }

            if (isHovered)
                EditorGUIUtility.AddCursorRect(chipRect, MouseCursor.Link);

            return clicked;
        }

        // Returns true on click.
        private static bool DrawXButton()
        {
            var xRect = GUILayoutUtility.GetRect(ClearBtnSize, ClearBtnSize,
                GUILayout.Width(ClearBtnSize), GUILayout.Height(ClearBtnSize));

            bool isHovered = xRect.Contains(Event.current.mousePosition);
            bool clicked = false;

            if (Event.current.type == EventType.MouseDown && isHovered)
            {
                clicked = true;
                Event.current.Use();
            }

            if (Event.current.type == EventType.Repaint)
            {
                var prev = GUI.color;
                GUI.color = isHovered ? new Color(0.7f, 0.25f, 0.25f) : new Color(0.28f, 0.28f, 0.28f);
                GUI.DrawTexture(xRect, _chipFillTex);
                GUI.color = isHovered ? new Color(1f, 0.4f, 0.4f, 0.5f) : new Color(1f, 1f, 1f, 0.12f);
                GUI.DrawTexture(xRect, _chipOutlineTex);
                GUI.color = prev;

                float pad = 4f;
                Handles.color = isHovered ? Color.white : new Color(0.6f, 0.6f, 0.6f);
                Handles.DrawAAPolyLine(1.5f,
                    new Vector3(xRect.x + pad, xRect.y + pad),
                    new Vector3(xRect.xMax - pad, xRect.yMax - pad));
                Handles.DrawAAPolyLine(1.5f,
                    new Vector3(xRect.xMax - pad, xRect.y + pad),
                    new Vector3(xRect.x + pad, xRect.yMax - pad));
            }

            if (isHovered)
                EditorGUIUtility.AddCursorRect(xRect, MouseCursor.Link);

            return clicked;
        }


        /// <summary>
        /// Creates a white Texture2D with anti-aliased rounded corners.
        /// If fill=true, interior is white. If fill=false, only the 1px border ring is white.
        /// Tint with GUI.color when drawing.
        /// </summary>
        private static Texture2D CreateRoundedRectTexture(float chipW, float chipH, int radius, bool fill)
        {
            // Use 2x resolution for crisp rendering on high-DPI
            int scale = 2;
            int w = Mathf.RoundToInt(chipW) * scale;
            int h = Mathf.RoundToInt(chipH) * scale;
            int r = radius * scale;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[w * h];
            var white = new Color32(255, 255, 255, 255);
            var clear = new Color32(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Distance from nearest corner center (only matters in corner regions)
                    float dist = 0f;
                    bool inCorner = false;

                    if (x < r && y < r) // bottom-left (texture y is flipped)
                    {
                        dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                        inCorner = true;
                    }
                    else if (x >= w - r && y < r) // bottom-right
                    {
                        dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(w - r, r));
                        inCorner = true;
                    }
                    else if (x < r && y >= h - r) // top-left
                    {
                        dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, h - r));
                        inCorner = true;
                    }
                    else if (x >= w - r && y >= h - r) // top-right
                    {
                        dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(w - r, h - r));
                        inCorner = true;
                    }

                    if (fill)
                    {
                        if (inCorner)
                        {
                            if (dist > r)
                                pixels[y * w + x] = clear;
                            else if (dist > r - 1)
                            {
                                byte a = (byte)Mathf.Clamp01(r - dist);
                                pixels[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
                            }
                            else
                                pixels[y * w + x] = white;
                        }
                        else
                        {
                            pixels[y * w + x] = white;
                        }
                    }
                    else // outline only
                    {
                        bool onBorder = x == 0 || x == w - 1 || y == 0 || y == h - 1;

                        if (inCorner)
                        {
                            // Draw the arc border
                            float borderOuter = r;
                            float borderInner = r - scale; // 1px border at display scale

                            if (dist > borderOuter)
                                pixels[y * w + x] = clear;
                            else if (dist > borderOuter - 1)
                            {
                                byte a = (byte)(Mathf.Clamp01(borderOuter - dist) * 255);
                                pixels[y * w + x] = new Color32(255, 255, 255, a);
                            }
                            else if (dist > borderInner)
                                pixels[y * w + x] = white;
                            else if (dist > borderInner - 1)
                            {
                                byte a = (byte)(Mathf.Clamp01(dist - borderInner + 1) * 255);
                                pixels[y * w + x] = new Color32(255, 255, 255, a);
                            }
                            else
                                pixels[y * w + x] = clear;
                        }
                        else if (onBorder)
                        {
                            pixels[y * w + x] = white;
                        }
                        else if (x == 1 || x == w - 2 || y == 1 || y == h - 2)
                        {
                            if (scale > 1)
                                pixels[y * w + x] = white;
                            else
                                pixels[y * w + x] = clear;
                        }
                        else
                        {
                            pixels[y * w + x] = clear;
                        }
                    }
                }
            }

            tex.SetPixels32(pixels);
            // Non-readable to free the CPU-side pixel copy.
            tex.Apply(false, true);
            return tex;
        }

        private static HierarchyInspectorData EnsureData(GameObject target)
        {
            var data = target.GetComponent<HierarchyInspectorData>();
            if (data != null) return data;
            data = Undo.AddComponent<HierarchyInspectorData>(target);
            data.hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInBuild;
            return data;
        }

        private void ApplyToAllTargets(string undoName, System.Action<HierarchyInspectorData> action)
        {
            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i] == null) continue;
                var data = EnsureData(_targets[i]);
                Undo.RecordObject(data, undoName);
                action(data);
            }
            // Refresh the cached primary in case EnsureData added one this call.
            if (_targets.Length > 0 && _targets[0] != null)
                _primaryData = _targets[0].GetComponent<HierarchyInspectorData>();
        }

        private static bool ColorsMatch(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.02f &&
                   Mathf.Abs(a.g - b.g) < 0.02f &&
                   Mathf.Abs(a.b - b.b) < 0.02f;
        }

        private void RepaintHierarchy()
        {
            // hierarchyChanged doesn't fire for component-property edits; force a cache refresh.
            if (_targets != null)
            {
                for (int i = 0; i < _targets.Length; i++)
                {
                    if (_targets[i] != null)
                        HierarchyOverlay.InvalidateItem(_targets[i]);
                }
            }
            EditorApplication.RepaintHierarchyWindow();
        }
    }
}
#endif
