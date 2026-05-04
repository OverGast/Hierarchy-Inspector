# Screenshot Capture Guide

This document lists every screenshot referenced in the GitBook docs, plus a demo-scene spec so you can capture most of them without rebuilding setup between shots.

## How to use this guide

1. Build the [demo scene](#demo-scene-setup) once (scripted or by hand).
2. Set Unity to the **dark editor skin** so colors match the docs (Edit → Preferences → General → Editor Theme → Dark).
3. Resize the **Hierarchy window to ~340 pixels wide** for typical capture compositions, wider for screenshots that need many components in the gutter.
4. Take screenshots per the [capture list](#capture-list) below, saving them under `docs/.gitbook/assets/<group>/<name>.png`.
5. After all captures are saved, ping me and I'll do a search/replace through the docs swapping the `> 📷 [SCREENSHOT: ...]` placeholders for `![alt](.gitbook/assets/...)` markdown.

## Naming convention

Files live under `docs/.gitbook/assets/<group>/<NN>-<short-name>.png`. Group names match the doc folder so the relationship is obvious.

```
docs/.gitbook/assets/
├── hero.png
├── populated-scene.png
├── getting-started/
├── gear-popup/
├── folders/
├── bookmarks/
├── style-clipboard/
├── themes/
├── theme-rows/
├── theme-icons/
├── theme-indicators/
└── theme-customization/
```

PNG, transparent background not required (Unity's editor bg is fine).

---

## Demo Scene Setup

One scene supports the majority of demo-scene captures. Put it at `Assets/Plugins/HierarchyInspector/Demo/DemoScene.unity` (the Demo folder already exists).

### Hierarchy structure

```
DemoScene
├── ---ENVIRONMENT---                        ← separator (auto-detected by name)
├── Lighting                                 ← virtualization folder, color: orange
│   ├── Sun (Directional Light)              ← bookmarked, custom Light icon via Use Main Component Icon
│   ├── Ambient Probe (Light Probe Group)
│   └── Reflection Probe (Reflection Probe)
├── Cameras                                  ← virtualization folder, color: blue
│   ├── Main Camera (Camera)                 ← bookmarked
│   └── UI Camera (Camera)
├── ---GAMEPLAY---                           ← separator
├── Player (CharacterController)             ← color: green, has Notes ("Player root: damage logic in PlayerHealth.cs")
│   ├── Model (MeshRenderer)
│   ├── Camera (Camera)
│   └── Weapons
│       ├── Pistol (MeshRenderer)
│       └── Rifle (MeshRenderer)             ← INACTIVE (uncheck the active toggle)
├── Enemies                                  ← virtualization folder
│   ├── EnemySpawner                         ← prefab instance with override dot (modify a property on the instance)
│   └── EnemyPool                            ← prefab instance, no overrides
├── BrokenObject                             ← attach a script then delete the script file (creates Missing Script)
├── UnwiredObject                            ← has a MonoBehaviour with a public Transform field left null
├── HighlightedObject                        ← color: red, no other special state
├── ---UI---                                 ← separator
├── Canvas (Canvas + CanvasScaler + GraphicRaycaster)
│   ├── HUD (RectTransform)
│   └── Pause Menu (RectTransform)           ← INACTIVE
└── Helpers
```

### Per-object setup quick reference

| GameObject | What to set via gear popup |
| --- | --- |
| Lighting | Folder ON, color: orange |
| Cameras | Folder ON, color: blue |
| Enemies | Folder ON, no color |
| Sun | Bookmark ON |
| Main Camera | Bookmark ON |
| Player | Color: green, Notes filled in |
| HighlightedObject | Color: red |

For the prefab demos:

- Create a tiny prefab (an empty GameObject, save it). Drag two instances into the scene as **EnemySpawner** and **EnemyPool**.
- On **EnemySpawner**, change its position or rotation slightly to create an unsaved override (this makes the override dot appear).

For the missing-script demo:

- Create any C# MonoBehaviour, attach it to **BrokenObject**, delete the C# file. Unity now shows "Missing (Mono Script)" on the row, which the missing-script tint highlights.

For the missing-reference demo:

- Create a MonoBehaviour with a public `Transform target;` field, attach to **UnwiredObject**, leave the field null.

{% hint style="info" %}
Want me to write an editor script that builds this scene programmatically? I can drop one in `Assets/Plugins/HierarchyInspector/Demo/Editor/DemoSceneBuilder.cs` that creates a menu item like `Tools → Hierarchy Decorator → Build Demo Scene`. Just say the word.
{% endhint %}

---

## Capture List

The 40 screenshots are grouped by source. Most demo-scene captures share the same scene state; just pick the right framing.

### Hero captures (README.md)

| ID | File | What to capture | Notes |
| --- | --- | --- | --- |
| H-1 | `hero.png` | Side-by-side: stock Unity hierarchy on the left, Hierarchy Decorator on the right, same scene in both | Use the demo scene. To capture the "stock" half, temporarily uncheck Overlay Enabled, screenshot, re-enable, screenshot again, combine in an image editor. ~600px wide each panel. |
| H-2 | `populated-scene.png` | Full demo scene Hierarchy, top to bottom | Show alt rows, prefab tints, both folders collapsed and expanded, separator rows, bookmark badges, the missing-script row in red. ~400px wide × full height. |

### Getting Started (3)

| ID | File | What to capture | Notes |
| --- | --- | --- | --- |
| GS-1 | `getting-started/01-import-dialog.png` | Unity's "Import Unity Package" dialog with the Hierarchy Decorator contents listed | Only needs to happen the first time you re-import. Or fake it by exporting your current package and re-importing. |
| GS-2 | `getting-started/02-first-look.png` | The demo scene Hierarchy, mouse hovered over one row so the gear button is visible | Crop tight to ~6-8 rows. The hovered row should clearly show its gear icon. |
| GS-3 | `getting-started/03-master-toggle-states.png` | Composite: top half = master toggle row with green ENABLED pill, bottom half = same row with red DISABLED pill | Capture the theme inspector header twice (once with overlay on, once off), combine vertically. |

### The Gear Popup (5)

| ID | File | What to capture | Notes |
| --- | --- | --- | --- |
| GP-1 | `gear-popup/01-gear-on-hover.png` | Single hierarchy row with gear icon highlighted on the right | Hover any row in the demo scene. Tight crop. |
| GP-2 | `gear-popup/02-popup-expanded.png` | The gear popup floating over the hierarchy, fully expanded | Open the gear on **Player** (it has a color, notes, etc., so all sections show interesting state). Capture the popup with some hierarchy visible behind it for context. |
| GP-3 | `gear-popup/03-color-section.png` | Close-up of the Color row inside the popup | Crop just the Color section: section label + 8 chips + None chip. Show one chip selected with the highlight outline. |
| GP-4 | `gear-popup/04-icon-section.png` | Close-up of the Icon section + Project Icon picker beneath it | Crop both. Have one built-in icon selected and a custom texture in the Project Icon field. |
| GP-5 | `gear-popup/05-notes-with-tooltip.png` | Composite: top = Notes field with multi-line text, bottom = a hierarchy row with the tooltip popup showing those notes when hovered | Edit Player's notes to a couple of lines, capture the popup, then close it and hover the Player row to capture the tooltip. |

### Virtualization Folders (3)

| ID | File | What to capture | Notes |
| --- | --- | --- | --- |
| F-1 | `folders/01-folders-in-hierarchy.png` | The demo scene with both Lighting and Cameras folders visible, expanded | Make sure the folder rows are clearly distinguishable from regular GameObjects. |
| F-2 | `folders/02-create-folder-flow.png` | 3-frame composite: (1) freshly-created empty GameObject, (2) gear popup open with Folder button highlighted, (3) the same row after, now styled as a folder | Use a temp GameObject in the demo scene; delete it after the capture. |
| F-3 | `folders/03-folder-conflict-warning.png` | Inspector showing a folder GameObject with a Rigidbody attached and the conflict warning helpbox visible | Add a Rigidbody to a temp folder GameObject, capture the Inspector, delete after. The warning is generated by `HierarchyFolderConflictMonitor`. |

### Bookmarks (3)

| ID | File | What to capture | Notes |
| --- | --- | --- | --- |
| B-1 | `bookmarks/01-bookmarked-rows.png` | Hierarchy section showing 2-3 bookmarked rows with their star badges visible | Sun and Main Camera are both bookmarked in the demo scene. |
| B-2 | `bookmarks/02-bookmark-toggle.png` | Close-up of a bookmarked row + the gear popup beside it with the Bookmark button highlighted | Composition: hierarchy on left, popup on right, arrow or visual link between them is fine. |
| B-3 | `bookmarks/03-bookmark-menu.png` | Hierarchy right-click context menu open, with the Bookmarks submenu expanded showing the bookmarked GameObject names | Standard right-click menu capture. |

### Copy & Paste Style (1)

| ID | File | What to capture | Notes |
| --- | --- | --- | --- |
| SC-1 | `style-clipboard/01-copy-paste-flow.png` | 2-frame composite: (1) HighlightedObject (red, custom icon) selected, hint of Ctrl+Shift+C; (2) three other GameObjects selected, after Ctrl+Shift+V applied so they share the same color and icon | Use temp GameObjects; revert after capture. Add the keyboard shortcut overlays in image editor. |

### Themes & Preferences (5)

| ID | File | What to capture | Notes |
| --- | --- | --- | --- |
| T-1 | `themes/01-theme-asset-in-project.png` | Project window with the default HierarchyTheme asset selected, theme inspector visible in the background | Standard Unity layout. |
| T-2 | `themes/02-preferences-pane.png` | Edit → Preferences → Hierarchy Decorator pane, showing the Active Theme picker, action buttons, and the embedded inspector below | Full Preferences window capture. |
| T-3 | `themes/03-rows-tab-open.png` | Theme inspector with the Rows tab active, all 4 sections expanded | Frame the entire inspector top-to-bottom. |
| T-4 | `themes/04-show-if-demo.png` | Composite: top half = Component Icons section with parent toggle ON (children fully visible); bottom half = same section with parent OFF (children indented and grayed) | Showcases the show_if behavior. |
| T-5 | `themes/05-header-bar-pill.png` | Tight close-up of the theme inspector's header bar: title, ENABLED pill, Reset button | ~80px tall. |

### Theme: Rows Tab (5)

| ID | File | What to capture | Notes |
| --- | --- | --- | --- |
| TR-1 | `theme-rows/01-all-sections-expanded.png` | Rows tab open, all 4 sections (Background, Effects, Inactive State, Selection) expanded | Same as T-3 but cropped to just the tab content. |
| TR-2 | `theme-rows/02-alternating-rows.png` | Demo scene hierarchy showing 8+ rows with the alternating two-tone clearly visible | Tight crop. Use a section of the demo scene without bookmarks/colors so the alternation is the only thing standing out. |
| TR-3 | `theme-rows/03-tree-lines-depth.png` | Player → Weapons → Pistol/Rifle expanded, showing tree lines connecting parent-child and depth shadows at each indent | Tight crop on that nested branch. |
| TR-4 | `theme-rows/04-active-inactive-mix.png` | Hierarchy section with active and inactive GameObjects side by side, showing the dim effect | Rifle and Pause Menu are inactive in the demo scene; capture a section that includes them plus active rows above. |
| TR-5 | `theme-rows/05-selection-focused-unfocused.png` | Side-by-side: same row selected with Hierarchy focused (loud color) vs. Scene view focused (quiet color) | Two captures, combine. |

### Theme: Icons Tab (4)

| ID | File | What to capture | Notes |
| --- | --- | --- | --- |
| TI-1 | `theme-icons/01-tab-open.png` | Icons tab with all 3 sections expanded | Frame the tab content. |
| TI-2 | `theme-icons/02-component-icon-hover.png` | Close-up of one hierarchy row with multiple component icons in the gutter, one icon hovered and visibly brighter | Player works well (CharacterController + others). |
| TI-3 | `theme-icons/03-main-component-icons.png` | Hierarchy section showing several rows whose primary icon comes from their first component: a camera icon for Main Camera, light icon for Sun, etc. | Use Cameras and Lighting folders expanded. |
| TI-4 | `theme-icons/04-ui-tints-comparison.png` | Two-frame composite: (1) Default tints (foldout arrow + gear at default colors); (2) Bright accent tints (e.g. cyan foldout arrow, magenta gear) | Capture a few rows with foldouts, switch tints in theme, capture again. |

### Theme: Indicators Tab (4)

| ID | File | What to capture | Notes |
| --- | --- | --- | --- |
| TIN-1 | `theme-indicators/01-tab-open.png` | Indicators tab with all 3 sections expanded | Frame the tab content. |
| TIN-2 | `theme-indicators/02-prefab-tints-overrides.png` | Hierarchy section with the Enemies folder expanded, showing the two prefab instances tinted blue. EnemySpawner has the override dot visible | Tight crop. |
| TIN-3 | `theme-indicators/03-missing-script-and-reference.png` | Hierarchy section showing BrokenObject (red highlight) and UnwiredObject (yellow indicator) | Side by side. |
| TIN-4 | `theme-indicators/04-separator-rendering.png` | Hierarchy section showing 2-3 separator rows with the divider styling and centered labels (---ENVIRONMENT---, ---GAMEPLAY---, ---UI---) | Tight crop on a separator with a few normal rows above and below for context. |

### Theme: Customization Tab (5)

| ID | File | What to capture | Notes |
| --- | --- | --- | --- |
| TC-1 | `theme-customization/01-tab-open.png` | Customization tab with all 4 sections expanded | Frame the tab content. |
| TC-2 | `theme-customization/02-styling-on-vs-off.png` | 2-frame composite: (1) demo hierarchy with Per-Object Styling ON (gear visible on hover, colors visible); (2) same hierarchy with the toggle OFF (no gear, no colors) | Compelling before/after. |
| TC-3 | `theme-customization/03-color-effects-comparison.png` | 4-row composite showing the same colored row rendered with each effect: (1) plain flood, (2) gradient, (3) left stripe, (4) accent line | Use HighlightedObject (red), toggle each effect on alone for each frame. |
| TC-4 | `theme-customization/04-active-toggle-column.png` | Hierarchy section showing the Active Toggle Column visible on the right edge of every row, mix of on/off states | Toggle the Pause Menu's active flag for visual variety. |
| TC-5 | `theme-customization/05-rename-flash-frames.png` | 2-frame composite: (1) GameObject mid-rename with the rename field active; (2) same row immediately after, with the rename flash highlight visible | Animation freeze-frame. May need to spam screenshots to catch the flash. |

---

## Capture tips

- **Window size matters.** Inspector and Hierarchy windows can be resized by dragging the dock edges; pick a width that frames the labels well (~340-400px is a good default). The same width across all captures keeps the docs visually consistent.
- **Dark theme.** Switch to Unity's dark editor theme before capturing. The doc palette assumes dark.
- **Hide the Inspector tab bar** when capturing the standalone theme inspector, by tearing it out into a floating window. Less visual clutter.
- **DPI.** Capture at 1x or 2x; GitBook handles both. Stay consistent.
- **Composites** (the multi-frame combinations like TR-5 or TC-3) can be done in any image editor (Photoshop, GIMP, Figma, etc.). Even a quick side-by-side via a slack screenshot works.
- **Sensitive content.** None of these screenshots include user-identifying data. Use the demo scene; don't accidentally capture an unrelated project.

When the captures are ready (or even some of them, if you want to ship docs incrementally), tell me and I'll wire them into the markdown.
