# Changelog

All notable changes to **Hierarchy Inspector** are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project follows [Semantic Versioning](https://semver.org/).

## [1.0.1]: UPM install path

- Added a `package.json` so Hierarchy Inspector can be installed via Unity Package Manager using a git URL (`https://github.com/OverGast/Hierarchy-Inspector.git?path=Assets/HierarchyInspector`).
- Welcome window now copies the bundled themes into `Assets/HierarchyInspector/Themes/` on first import when running from a UPM install, so theme assets are editable in the user's project regardless of install path. Asset Store (`.unitypackage`) installs are unaffected (no copy is performed).
- "Open Demo Scene" now resolves the demo scene by GUID, so it works under both `Assets/` and `Packages/` install layouts.

## [1.0.0]: Initial release

First public release. Includes:

- Row visuals: alternating stripes, hover highlight, depth shadows, tree connector lines, dim-on-disable, focused/unfocused selection colors with optional glow.
- Per-object gear popup with 8 color presets, a built-in icon picker, custom project icons, notes, folder/bookmark toggles, and multi-select editing.
- Virtualization folders: pure-editor folders that get stripped at build time, with conflict detection for accidentally-attached scripts.
- Bookmarks: per-scene bookmark list, navigated from a star button on each scene's header row.
- Style clipboard: copy and paste color, icon, and folder state with `Ctrl+Shift+C` / `Ctrl+Shift+V`.
- Indicators: prefab tinting, override dot, missing-script highlight, missing-reference indicator, separator detection (auto-detected from `---Section---` naming).
- Animations: hover slide, fade-in on create, rename flash.
- Theme system: a `ScriptableObject` per theme; switch between themes per-user via Edit → Preferences → Hierarchy Inspector. The theme inspector provides a 4-tab editor (Rows, Icons, Indicators, Customization) with show_if dependent fields. Ships with four bundled themes: Default, HighContrast, Minimal, and Vibrant.
- Welcome window on first import, accessible later via Tools → Hierarchy Inspector → Welcome.
