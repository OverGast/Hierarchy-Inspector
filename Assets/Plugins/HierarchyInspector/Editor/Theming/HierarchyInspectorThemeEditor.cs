#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace FoundFootage.Editor.Hierarchy
{
    [CustomEditor(typeof(HierarchyInspectorTheme))]
    internal sealed class HierarchyInspectorThemeEditor : UnityEditor.Editor
    {
        private const string TabPrefKey = "FoundFootage.HierarchyTheme.SelectedTab";
        private const string FoldoutPrefPrefix = "FoundFootage.HierarchyTheme.Foldout.";

        // ─── palette ──────────────────────────────────────────────────────────
        // Tuned for Unity's dark editor; light-skin equivalents kick in via the
        // PickSkin helper at the bottom of this file.
        private static readonly Color AccentBlue   = new Color(0.345f, 0.510f, 0.827f, 1f);
        private static readonly Color AccentOn     = new Color(0.427f, 0.780f, 0.478f, 1f);
        private static readonly Color AccentOff    = new Color(0.851f, 0.400f, 0.400f, 1f);
        private static readonly Color HeaderBg     = new Color(0.235f, 0.235f, 0.235f, 1f);
        private static readonly Color MasterBg     = new Color(0.259f, 0.259f, 0.259f, 1f);
        private static readonly Color SectionHdrBg = new Color(0.176f, 0.176f, 0.176f, 1f);
        private static readonly Color TabBarBg     = new Color(0.208f, 0.208f, 0.208f, 1f);
        private static readonly Color PillBg       = new Color(0f, 0f, 0f, 0.30f);
        private static readonly Color HairlineDark = new Color(0f, 0f, 0f, 0.55f);

        // ─── data shapes ──────────────────────────────────────────────────────
        private struct Field
        {
            public string Property;
            public GUIContent Label;
            // When set, the field is indented + disabled while the named bool prop is false.
            public string DependsOn;
        }

        private struct Section
        {
            public string Title;
            public Field[] Fields;
            public Action<HierarchyInspectorThemeEditor> CustomDraw;
            // Override Fields.Length when the section uses CustomDraw to render extra rows.
            // 0 (the struct default) means "auto = Fields.Length".
            public int CountOverride;
            public string Help;
            // When set, the entire section body is disabled while the named bool prop is false.
            public string DependsOn;
        }

        private struct Tab
        {
            public GUIContent Label;
            public Section[] Sections;
        }

        private static readonly Tab[] Tabs = BuildTabs();
        private int _selectedTab;

        // Cached styles — EditorStyles isn't safe to read at static-construction time.
        private static GUIStyle s_pillStyle;
        private static GUIStyle s_tabStyle;
        private static GUIStyle s_tabActiveStyle;
        private static GUIStyle s_sectionTitleStyle;
        private static GUIStyle s_sectionCountStyle;
        private static GUIStyle s_masterLabelStyle;

        private static readonly GUIContent s_pillOn  = new GUIContent("● ENABLED");
        private static readonly GUIContent s_pillOff = new GUIContent("● DISABLED");
        private static readonly GUIContent s_resetButton = new GUIContent(
            "Reset to Defaults",
            "Restore every field on this theme to its built-in default value.");
        private static readonly GUIContent s_overlayLabel = new GUIContent(
            "Overlay Enabled",
            "Master switch. When off, the overlay unsubscribes its callbacks and renders nothing — Unity's stock hierarchy is shown.");

        private void OnEnable()
        {
            _selectedTab = Mathf.Clamp(EditorPrefs.GetInt(TabPrefKey, 0), 0, Tabs.Length - 1);
        }

        // ═══ ENTRY POINT ══════════════════════════════════════════════════════

        public override void OnInspectorGUI()
        {
            EnsureStyles();
            serializedObject.Update();

            DrawTopAccentStrip();
            DrawHeaderBar();
            DrawMasterRow();
            EditorGUILayout.Space(8f);     // breathing room between header area and tabs

            DrawTabBar();
            EditorGUILayout.Space(8f);     // breathing room between tabs and the first section

            // Sections gate on master "enabled". Tab bar stays interactive so the
            // user can still browse tabs while the overlay is off.
            var enabledProp = serializedObject.FindProperty("enabled");
            bool overlayOn = enabledProp == null || enabledProp.boolValue;

            using (new EditorGUI.DisabledScope(!overlayOn))
            {
                var tab = Tabs[_selectedTab];
                for (int i = 0; i < tab.Sections.Length; i++)
                    DrawSection(tab.Sections[i]);
            }

            serializedObject.ApplyModifiedProperties();
        }

        // ═══ STYLE INIT ═══════════════════════════════════════════════════════

        private static void EnsureStyles()
        {
            if (s_pillStyle != null) return;

            s_pillStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 0, 0),
            };

            s_tabStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                padding = new RectOffset(14, 14, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };
            s_tabStyle.normal.textColor = new Color(0.55f, 0.55f, 0.55f);
            s_tabStyle.hover.textColor = new Color(0.83f, 0.83f, 0.83f);

            s_tabActiveStyle = new GUIStyle(s_tabStyle)
            {
                fontStyle = FontStyle.Bold,
            };
            s_tabActiveStyle.normal.textColor = new Color(0.92f, 0.92f, 0.92f);
            s_tabActiveStyle.hover.textColor = new Color(0.92f, 0.92f, 0.92f);

            s_sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                padding = new RectOffset(0, 0, 0, 0),
            };
            s_sectionTitleStyle.normal.textColor = new Color(0.88f, 0.88f, 0.88f);

            s_sectionCountStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 9,
            };
            s_sectionCountStyle.normal.textColor = new Color(0.42f, 0.42f, 0.42f);

            s_masterLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
            };
            s_masterLabelStyle.normal.textColor = new Color(0.92f, 0.92f, 0.92f);
        }

        // ═══ CHROME DRAWING ═══════════════════════════════════════════════════

        // 2px accent strip at the very top of the inspector — gives the tool a
        // visual identity. Mirrors the mockup's --accent border-top.
        private static void DrawTopAccentStrip()
        {
            var r = GUILayoutUtility.GetRect(0, 2, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, AccentBlue);
        }

        private void DrawHeaderBar()
        {
            var enabledProp = serializedObject.FindProperty("enabled");
            bool overlayOn = enabledProp == null || enabledProp.boolValue;

            // Painted background — flat rather than CSS-style gradient (IMGUI gradients
            // cost significantly more for negligible visual gain at this scale).
            var bgRect = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(bgRect, PickSkin(HeaderBg, new Color(0.78f, 0.78f, 0.78f)));
            DrawHairline(bgRect, bottom: true);

            // Inner content rect (padded).
            var inner = new Rect(bgRect.x + 10, bgRect.y + 4, bgRect.width - 20, bgRect.height - 8);

            // Reset button (right-aligned).
            const float btnW = 130f;
            var btnRect = new Rect(inner.xMax - btnW, inner.y + 1, btnW, 20f);
            if (GUI.Button(btnRect, s_resetButton))
            {
                foreach (var t in targets)
                {
                    if (t is HierarchyInspectorTheme theme)
                    {
                        Undo.RecordObject(theme, "Reset Hierarchy Theme");
                        theme.ResetToDefaults();
                        EditorUtility.SetDirty(theme);
                    }
                }
                serializedObject.Update();
                GUIUtility.ExitGUI();
            }

            // Status pill (just left of the button).
            const float pillW = 92f;
            var pillRect = new Rect(btnRect.x - pillW - 8, inner.y + 2, pillW, 18f);
            DrawStatusPill(pillRect, overlayOn);

            // Title (fills remaining space on the left).
            var titleRect = new Rect(inner.x, inner.y, pillRect.x - inner.x - 8, inner.height);
            GUI.Label(titleRect, "Hierarchy Inspector Theme", EditorStyles.boldLabel);
        }

        private static void DrawStatusPill(Rect rect, bool on)
        {
            EditorGUI.DrawRect(rect, PillBg);
            // 1px hairline border. Top/bottom only — sides hide naturally against header bg.
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), HairlineDark);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), HairlineDark);

            var prev = GUI.contentColor;
            GUI.contentColor = on ? AccentOn : AccentOff;
            GUI.Label(rect, on ? s_pillOn : s_pillOff, s_pillStyle);
            GUI.contentColor = prev;
        }

        private void DrawMasterRow()
        {
            var enabledProp = serializedObject.FindProperty("enabled");
            if (enabledProp == null) return;

            bool overlayOn = enabledProp.boolValue;

            var rowRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rowRect, PickSkin(MasterBg, new Color(0.82f, 0.82f, 0.82f)));
            DrawHairline(rowRect, bottom: true);

            // 3px status strip on the left — green when on, red when off. Mirrors
            // the mockup's master-strip and gives the user an instant read.
            var stripRect = new Rect(rowRect.x, rowRect.y, 3f, rowRect.height);
            EditorGUI.DrawRect(stripRect, overlayOn ? AccentOn : AccentOff);

            // Toggle is rendered manually so we can place it precisely after the strip
            // and have a single, prominent label rather than Unity's stock layout.
            const float padLeft = 14f;
            const float toggleSize = 16f;
            var toggleRect = new Rect(rowRect.x + padLeft, rowRect.y + 6f, toggleSize, toggleSize);
            EditorGUI.BeginChangeCheck();
            bool next = EditorGUI.Toggle(toggleRect, overlayOn);
            if (EditorGUI.EndChangeCheck())
                enabledProp.boolValue = next;

            var labelRect = new Rect(toggleRect.xMax + 8f, rowRect.y, 200f, rowRect.height);
            EditorGUI.LabelField(labelRect, s_overlayLabel, s_masterLabelStyle);
        }

        private void DrawTabBar()
        {
            var barRect = GUILayoutUtility.GetRect(0, 26, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(barRect, PickSkin(TabBarBg, new Color(0.74f, 0.74f, 0.74f)));
            DrawHairline(barRect, bottom: true);

            // Equal-width tabs across the inspector width.
            float tabW = barRect.width / Tabs.Length;
            var e = Event.current;

            for (int i = 0; i < Tabs.Length; i++)
            {
                var tabRect = new Rect(barRect.x + i * tabW, barRect.y, tabW, barRect.height);

                // Active tab: slightly lighter bg + accent underline at the bottom.
                if (i == _selectedTab)
                {
                    EditorGUI.DrawRect(tabRect, PickSkin(
                        new Color(0.224f, 0.224f, 0.224f, 1f),
                        new Color(0.84f, 0.84f, 0.84f, 1f)));
                    var underRect = new Rect(tabRect.x, tabRect.yMax - 2f, tabRect.width, 2f);
                    EditorGUI.DrawRect(underRect, AccentBlue);
                }

                // Vertical separator on the right (skip after last tab).
                if (i < Tabs.Length - 1)
                {
                    var sep = new Rect(tabRect.xMax - 1f, tabRect.y + 4f, 1f, tabRect.height - 8f);
                    EditorGUI.DrawRect(sep, HairlineDark);
                }

                var style = i == _selectedTab ? s_tabActiveStyle : s_tabStyle;
                GUI.Label(tabRect, Tabs[i].Label, style);

                if (e.type == EventType.MouseDown && e.button == 0 && tabRect.Contains(e.mousePosition))
                {
                    if (i != _selectedTab)
                    {
                        _selectedTab = i;
                        EditorPrefs.SetInt(TabPrefKey, i);
                        GUI.FocusControl(null);
                        Repaint();
                    }
                    e.Use();
                }
            }
        }

        // ═══ SECTION + FIELD ══════════════════════════════════════════════════

        private void DrawSection(Section section)
        {
            string key = FoldoutPrefPrefix + section.Title;
            bool open = EditorPrefs.GetBool(key, true);

            int count = section.CountOverride > 0
                ? section.CountOverride
                : (section.Fields != null ? section.Fields.Length : 0);

            bool next = DrawSectionHeader(section.Title, count, open);
            if (next != open) EditorPrefs.SetBool(key, next);
            if (!next) return;

            bool sectionEnabled = true;
            if (!string.IsNullOrEmpty(section.DependsOn))
            {
                var gate = serializedObject.FindProperty(section.DependsOn);
                sectionEnabled = gate == null || gate.boolValue;
            }

            using (new EditorGUI.IndentLevelScope())
            using (new EditorGUI.DisabledScope(!sectionEnabled))
            {
                if (section.Fields != null)
                {
                    for (int i = 0; i < section.Fields.Length; i++)
                        DrawField(section.Fields[i]);
                }

                section.CustomDraw?.Invoke(this);

                if (!string.IsNullOrEmpty(section.Help))
                    EditorGUILayout.HelpBox(section.Help, MessageType.Info);
            }
            EditorGUILayout.Space(6f);     // gap between foldout sections
        }

        private static bool DrawSectionHeader(string title, int count, bool open)
        {
            var rect = GUILayoutUtility.GetRect(1f, 24f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, PickSkin(SectionHdrBg, new Color(0.70f, 0.70f, 0.70f)));

            // 3px left accent stripe shown only when the section is expanded — visual
            // weight matches the open/closed state.
            if (open)
            {
                var stripeRect = new Rect(rect.x, rect.y, 3f, rect.height);
                EditorGUI.DrawRect(stripeRect, AccentBlue);
            }

            // Arrow glyph. Drawn as a plain label so we own the click handling for the
            // whole header rect — EditorGUI.Foldout consumes click events asymmetrically
            // and conflicts with the row-wide click handler below.
            var arrowRect = new Rect(rect.x + 7f, rect.y, 16f, rect.height);
            var prevColor = GUI.contentColor;
            GUI.contentColor = new Color(0.65f, 0.65f, 0.65f);
            GUI.Label(arrowRect, open ? "▼" : "▶", EditorStyles.label);
            GUI.contentColor = prevColor;

            // Title — width budget leaves room for the right-aligned count badge.
            var titleRect = new Rect(rect.x + 24f, rect.y + 4f, rect.width - 130f, rect.height - 8f);
            GUI.Label(titleRect, title, s_sectionTitleStyle);

            // Right-aligned count badge.
            if (count > 0)
            {
                string badge = count == 1 ? "1 SETTING" : count + " SETTINGS";
                var countRect = new Rect(rect.xMax - 100f, rect.y, 92f, rect.height);
                GUI.Label(countRect, badge, s_sectionCountStyle);
            }

            // Click anywhere on the header bar toggles the foldout.
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                open = !open;
                e.Use();
            }
            return open;
        }

        private void DrawField(Field f)
        {
            var prop = serializedObject.FindProperty(f.Property);
            if (prop == null) return;

            if (string.IsNullOrEmpty(f.DependsOn))
            {
                EditorGUILayout.PropertyField(prop, f.Label, true);
                return;
            }

            // show_if: indent + disable when the gating bool is off. Visible-but-disabled
            // is preferred over hidden — users can still discover the field.
            var parent = serializedObject.FindProperty(f.DependsOn);
            bool parentOn = parent != null && parent.boolValue;

            using (new EditorGUI.IndentLevelScope())
            using (new EditorGUI.DisabledScope(!parentOn))
            {
                EditorGUILayout.PropertyField(prop, f.Label, true);
            }
        }

        // ═══ CUSTOM DRAWS ═════════════════════════════════════════════════════

        private void DrawBackgroundColors()
        {
            EditorGUILayout.LabelField("Dark Skin", EditorStyles.miniBoldLabel);
            DrawField(new Field
            {
                Property = "rowColorDarkPrimary",
                Label = new GUIContent("Even Rows",
                    "Row body color used on even rows when Unity's editor is set to the dark skin.")
            });
            DrawField(new Field
            {
                Property = "rowColorDarkSecondary",
                Label = new GUIContent("Odd Rows",
                    "Row body color used on odd (alternating) rows when Unity's editor is set to the dark skin."),
                DependsOn = "alternatingRows"
            });

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Light Skin", EditorStyles.miniBoldLabel);
            DrawField(new Field
            {
                Property = "rowColorLightPrimary",
                Label = new GUIContent("Even Rows",
                    "Row body color used on even rows when Unity's editor is set to the light skin.")
            });
            DrawField(new Field
            {
                Property = "rowColorLightSecondary",
                Label = new GUIContent("Odd Rows",
                    "Row body color used on odd (alternating) rows when Unity's editor is set to the light skin."),
                DependsOn = "alternatingRows"
            });
        }

        // ═══ TAB / SECTION DEFINITIONS ════════════════════════════════════════

        private static Tab[] BuildTabs()
        {
            return new[]
            {
                // ──────────── ROWS ────────────
                new Tab
                {
                    Label = new GUIContent("Rows", "Row backgrounds, effects, inactive state, and selection."),
                    Sections = new[]
                    {
                        new Section
                        {
                            Title = "Background",
                            CountOverride = 5, // alternatingRows + 4 row colors
                            Fields = new[]
                            {
                                F("alternatingRows", "Alternating Rows",
                                    "Tint odd rows with the secondary row color so consecutive rows are easier to scan."),
                            },
                            CustomDraw = self => self.DrawBackgroundColors(),
                            Help = "Even = primary color, Odd = secondary (alternating) color. The Dark or Light pair is selected automatically based on Unity's current editor skin (Edit → Preferences → General → Editor Theme)."
                        },
                        new Section
                        {
                            Title = "Effects",
                            Fields = new[]
                            {
                                F("rowHover", "Highlight on Hover",
                                    "Brighten the row that the mouse is currently over."),
                                F("rowBorders", "Row Borders",
                                    "Draw a subtle 1px line between rows."),
                                F("depthShadow", "Depth Shadow",
                                    "Soft drop-shadow at the left edge of each nesting level so depth reads at a glance."),
                                F("treeLines", "Tree Lines",
                                    "Draw connector lines between parent and child rows."),
                                F("treeLineColor", "Tree Line Color",
                                    "Color of the parent/child connector lines.",
                                    dependsOn: "treeLines"),
                            }
                        },
                        new Section
                        {
                            Title = "Inactive State",
                            Fields = new[]
                            {
                                F("inactiveDimming", "Dim Inactive Objects",
                                    "Fade rows whose GameObject is disabled in the hierarchy."),
                                F("inactiveDimAlpha", "Inactive Opacity",
                                    "Multiplier applied to row contents when the GameObject is inactive (0 = fully dimmed, 1 = no dim).",
                                    dependsOn: "inactiveDimming"),
                            }
                        },
                        new Section
                        {
                            Title = "Selection",
                            Fields = new[]
                            {
                                F("selectionColorFocused", "Focused Color",
                                    "Row body color used when the Hierarchy window has keyboard focus."),
                                F("selectionColorUnfocused", "Unfocused Color",
                                    "Row body color used when the Hierarchy window does not have keyboard focus."),
                                F("selectedRowGlow", "Selected Row Glow",
                                    "Soft glow drawn at the bottom edge of the selected row."),
                            }
                        },
                    }
                },

                // ──────────── ICONS ────────────
                new Tab
                {
                    Label = new GUIContent("Icons", "Component gutter icons, GameObject icon, and chrome tints."),
                    Sections = new[]
                    {
                        new Section
                        {
                            Title = "Component Icons",
                            Fields = new[]
                            {
                                F("componentIcons", "Show Component Icons",
                                    "Render component icons in the right-side gutter of each row."),
                                F("maxIcons", "Max Icons per Row",
                                    "Maximum number of component icons to render per row.",
                                    dependsOn: "componentIcons"),
                                F("iconSize", "Icon Size",
                                    "Pixel size of each component icon in the gutter.",
                                    dependsOn: "componentIcons"),
                                F("iconGlow", "Glow on Hover",
                                    "Brighten component icons under the mouse cursor.",
                                    dependsOn: "componentIcons"),
                                F("componentQuickToggle", "Click to Toggle",
                                    "Click a component icon to toggle that component's enabled state.",
                                    dependsOn: "componentIcons"),
                            }
                        },
                        new Section
                        {
                            Title = "GameObject Icon",
                            Fields = new[]
                            {
                                F("useMainComponentIcon", "Use Main Component Icon",
                                    "If a GameObject has no custom icon, use the icon of its first non-Transform component."),
                                F("mainIconSize", "Main Icon Size",
                                    "Pixel size of the GameObject's primary icon. Affects override-dot, label start, and bookmark badge positions."),
                            }
                        },
                        new Section
                        {
                            Title = "UI Tints",
                            Fields = new[]
                            {
                                F("foldoutArrowColor", "Foldout Arrow",
                                    "Tint applied to the parent/child foldout arrow."),
                                F("gearIconColor", "Gear Icon",
                                    "Tint applied to the per-object settings (gear) icon."),
                            },
                            Help = "Foldout arrows and the per-object gear button are tinted with these colors. Use them to brighten chrome on dark backgrounds."
                        },
                    }
                },

                // ──────────── INDICATORS ────────────
                new Tab
                {
                    Label = new GUIContent("Indicators", "Prefab tints, missing-script warnings, separator detection."),
                    Sections = new[]
                    {
                        new Section
                        {
                            Title = "Prefabs",
                            Fields = new[]
                            {
                                F("prefabTinting", "Tint Prefab Instances",
                                    "Auto-blend a tint into rows whose GameObject is a prefab instance."),
                                F("prefabTintColor", "Tint Color",
                                    "Color blended into prefab instance rows when prefab tinting is enabled.",
                                    dependsOn: "prefabTinting"),
                                F("prefabOverrideDot", "Show Override Dot",
                                    "Show a small dot next to prefab instances that have unsaved overrides."),
                                F("prefabOverrideDotColor", "Override Dot Color",
                                    "Color of the prefab override dot drawn next to the GameObject icon.",
                                    dependsOn: "prefabOverrideDot"),
                            }
                        },
                        new Section
                        {
                            Title = "Warnings",
                            Fields = new[]
                            {
                                F("missingScriptWarning", "Missing Script Highlight",
                                    "Tint rows whose GameObject has a Missing Script component."),
                                F("missingScriptTintColor", "Missing Script Color",
                                    "Tint blended into rows that have a missing script component.",
                                    dependsOn: "missingScriptWarning"),
                                F("missingReferences", "Missing Reference Indicator",
                                    "Show an indicator when a serialized reference field on the row's components is null."),
                            }
                        },
                        new Section
                        {
                            Title = "Separators",
                            Fields = new[]
                            {
                                F("separatorDetection", "Auto-Detect Separators",
                                    "Treat GameObjects named like ---Section--- as visual dividers."),
                            },
                            Help = "Treats GameObjects named like ---Section--- as visual dividers."
                        },
                    }
                },

                // ──────────── CUSTOMIZATION ────────────
                new Tab
                {
                    Label = new GUIContent("Customization", "User-driven coloring, color effects, toolbar, and animations."),
                    Sections = new[]
                    {
                        new Section
                        {
                            Title = "Per-Object Styling",
                            Fields = new[]
                            {
                                F("perObjectStyling", "Per-Object Styling",
                                    "Allow individual GameObjects to be styled via the gear popup."),
                                F("folderStyling", "Folder Styling",
                                    "Auto-style rows that contain only child objects (treat as folders).",
                                    dependsOn: "perObjectStyling"),
                            }
                        },
                        new Section
                        {
                            Title = "Color Effects",
                            Fields = new[]
                            {
                                F("gradientBackground", "Gradient on Colored Rows",
                                    "When a row has a custom color, fade it left-to-right back to the row body color."),
                                F("leftColorStripe", "Left Color Stripe",
                                    "Draw a thin colored bar on the left edge of rows that have a custom color."),
                                F("accentLine", "Accent Line",
                                    "Draw a thin accent line at the bottom of rows that have a custom color."),
                                F("iconTinting", "Tint Icons with Row Color",
                                    "Tint component icons with the row's custom color when one is set."),
                            },
                            Help = "Effects in this section only apply to rows that have a custom color set via the gear popup."
                        },
                        new Section
                        {
                            Title = "Toolbar",
                            Fields = new[]
                            {
                                F("activeToggle", "Active Toggle Column",
                                    "Show the eye/lock GameObject-active toggle column."),
                            }
                        },
                        new Section
                        {
                            Title = "Animations",
                            Fields = new[]
                            {
                                F("hoverSlide", "Hover Slide",
                                    "Slight indent animation when a row is hovered."),
                                F("fadeInAnimation", "Fade In on Create",
                                    "Fade-in animation for newly created GameObjects."),
                                F("renameFlash", "Rename Flash",
                                    "Brief flash highlight when a GameObject is renamed."),
                            }
                        },
                    }
                },
            };
        }

        // ═══ HELPERS ══════════════════════════════════════════════════════════

        private static Field F(string property, string label, string tooltip) =>
            new Field { Property = property, Label = new GUIContent(label, tooltip) };

        private static Field F(string property, string label, string tooltip, string dependsOn) =>
            new Field { Property = property, Label = new GUIContent(label, tooltip), DependsOn = dependsOn };

        private static void DrawHairline(Rect rect, bool bottom)
        {
            float y = bottom ? rect.yMax - 1f : rect.y;
            EditorGUI.DrawRect(new Rect(rect.x, y, rect.width, 1f), HairlineDark);
        }

        private static Color PickSkin(Color dark, Color light) =>
            EditorGUIUtility.isProSkin ? dark : light;
    }
}
#endif
