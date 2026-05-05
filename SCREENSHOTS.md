# Screenshot Capture Guide

The 10 screenshots referenced from the GitBook docs, with capture instructions.

## How to use this guide

1. Build the demo scene: **Tools → Hierarchy Inspector → Build Demo Scene** (one-time).
2. Set Unity to the **dark editor skin** so colors match the docs (Edit → Preferences → General → Editor Theme → Dark).
3. Resize the Hierarchy / Inspector windows to **~340 pixels wide** for the typical capture composition.
4. Take the screenshots below, saving each as `docs/.gitbook/assets/<path>` per the file column.
5. Once captured, tell me and I'll wire them into the docs by swapping the `> 📷 [SCREENSHOT: ...]` placeholders for image links.

## Asset folder layout

```
docs/.gitbook/assets/
├── hero.png
├── getting-started/
├── folders/
├── bookmarks/
├── themes/
├── theme-rows/
├── theme-icons/
├── theme-indicators/
└── theme-customization/
```

PNG, transparent background not required (Unity's editor bg is fine).

---

## Capture list

| ID | File | Source page | What to capture |
| --- | --- | --- | --- |
| 1 | `hero.png` | README | Side-by-side: stock Unity hierarchy on the left, Hierarchy Inspector on the right, both showing the demo scene. To capture the "stock" half, temporarily uncheck Overlay Enabled, screenshot, re-enable, screenshot the right half, combine in an image editor. Aim for ~600px wide each panel. |
| 2 | `getting-started/02-first-look.png` | Getting Started | The demo scene Hierarchy, mouse hovering over one row so the gear button is visible on it. Crop tight to ~6-8 rows. |
| 3 | `folders/01-folders-in-hierarchy.png` | Virtualization Folders | The demo scene with both the **Lighting** and **Cameras** folders expanded so their contents are visible. Make sure the folder rows look clearly distinct from regular GameObjects. |
| 4 | `bookmarks/01-bookmarked-rows.png` | Bookmarks | Section of the demo scene showing 2-3 bookmarked rows with their star badges visible. Sun and Main Camera are bookmarked; expand the Lighting and Cameras folders to show both. |
| 5 | `themes/02-preferences-pane.png` | Themes & Preferences | Edit → Preferences → Hierarchy Inspector pane. Frame the full Preferences window so the left sidebar (Hierarchy Inspector selected) and the right panel (Active Theme picker, Create New / Reveal Asset / Reset buttons, embedded inspector) are all visible. |
| 6 | `themes/03-rows-tab-open.png` | Themes & Preferences | The full theme inspector with the Rows tab active and all 4 sections expanded. Tear the Inspector tab out into a floating window for less visual clutter. Frame top-to-bottom. |
| 7 | `theme-rows/01-all-sections-expanded.png` | Theme Reference: Rows Tab | Same as #6, but cropped to just the tab content (no Unity tab bar above it). |
| 8 | `theme-icons/01-tab-open.png` | Theme Reference: Icons Tab | Theme inspector with the **Icons** tab active and all 3 sections (Component Icons, GameObject Icon, UI Tints) expanded. Crop to tab content. |
| 9 | `theme-indicators/01-tab-open.png` | Theme Reference: Indicators Tab | Theme inspector with the **Indicators** tab active and all 3 sections (Prefabs, Warnings, Separators) expanded. Crop to tab content. |
| 10 | `theme-customization/01-tab-open.png` | Theme Reference: Customization Tab | Theme inspector with the **Customization** tab active and all 4 sections (Per-Object Styling, Color Effects, Toolbar, Animations) expanded. Crop to tab content. |

---

## What the demo scene contains

The builder produces this hierarchy. Most of the variety here exists to support the kept captures and (optionally) future marketing material; you don't need all of it for the 10 shots above.

```
DemoScene
├── ---ENVIRONMENT---       ← separator
├── Lighting                ← folder, orange
│   ├── Sun                 ← bookmarked, Directional Light
│   ├── Ambient Probe
│   └── Reflection Probe
├── Cameras                 ← folder, blue
│   ├── Main Camera         ← bookmarked
│   └── UI Camera
├── ---GAMEPLAY---          ← separator
├── Player                  ← green, has Notes, CharacterController
│   ├── Model
│   ├── Camera
│   └── Weapons
│       ├── Pistol
│       └── Rifle           ← INACTIVE
├── Enemies                 ← folder
│   ├── EnemySpawner        ← prefab instance with override
│   └── EnemyPool           ← prefab instance
├── BrokenObject            ← (optional) attach + delete a script for missing-script demo
├── UnwiredObject           ← DemoMissingReference component, null Transform field
├── HighlightedObject       ← red color
├── ---UI---                ← separator
├── Canvas
│   ├── HUD
│   └── Pause Menu          ← INACTIVE
└── Helpers
```

---

## Capture tips

- **Window width matters.** Inspector and Hierarchy widths are draggable. Pick one width and stay with it across all captures so the docs read consistently.
- **Dark theme.** Switch to Unity's dark editor theme before capturing (the doc palette assumes dark).
- **Tear out the Inspector** into a floating window for the standalone theme-inspector captures (#6, #7, #8, #9, #10). Cleaner crops without the tab bar above.
- **DPI.** Capture at 1x or 2x; GitBook handles both. Stay consistent.
- **Hero composite (#1).** Toggle Overlay Enabled off, screenshot, toggle back on, screenshot, then combine in any image editor (GIMP, Photoshop, Figma, even paste-into-PowerPoint works).
