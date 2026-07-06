# Hierarchy Inspector

A Unity Editor extension that enhances the Hierarchy window with row visuals, per-object styling, virtualization folders, bookmarks, and a style clipboard.

## Documentation

Full user documentation, with screenshots, is published at:

**[overgast.github.io/spacewhale-docs](https://overgast.github.io/spacewhale-docs/hierarchy-inspector/)**

The same link is available in-editor:

- The `?` button in the theme inspector header.
- The **Open Documentation** button in the welcome window.
- Or open the welcome window any time from `Tools → Hierarchy Inspector → Welcome`.

## Quick start

1. After import, open any scene. Rows now alternate, hover, and show a gear button on the right.
2. Click the gear on any row to give it a color, custom icon, mark it as a folder, bookmark it, or attach a note.
3. Open `Edit → Preferences → Hierarchy Inspector` to switch between the bundled themes (Default, HighContrast, Minimal, Vibrant) or create your own.
4. Use `Ctrl+Shift+C` / `Ctrl+Shift+V` to copy and paste a row's styling onto other rows.

## Folder layout

- `Editor/` is the editor overlay, theme system, gear popup, bookmarks, and folder build-stripping logic.
- `Runtime/` holds the small `HierarchyInspectorData` MonoBehaviour. The component is automatically stripped from player builds via Unity's `DontSaveInBuild` flag.
- `Themes/` contains the four bundled theme assets.
- `Demo/` has the demo scene, an enemy prefab, and a tiny `DemoMissingReference` script used to demonstrate the missing-reference indicator.
