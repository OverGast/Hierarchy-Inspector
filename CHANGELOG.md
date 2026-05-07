# Changelog

All notable changes to **Hierarchy Inspector** are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project follows [Semantic Versioning](https://semver.org/).

## [1.0.1]: UPM install path

- Added a `package.json` so Hierarchy Inspector can be installed via Unity Package Manager using a git URL (`https://github.com/OverGast/Hierarchy-Inspector.git?path=Assets/HierarchyInspector`).
- On first load, the welcome flow now copies the four bundled themes from the package into `Assets/HierarchyInspector/Themes/` and switches the active theme to the writable Default copy. Asset Store (`.unitypackage`) installs are unaffected (no copy is performed). The bootstrap is gated on the actual filesystem state (does Assets/ already contain a theme?) rather than an EditorPrefs flag, so it is self-healing across upgrades, re-installs, and stale per-machine prefs ; users upgrading from v1.0.0 get themes copied automatically on the next domain reload.
- "Open Demo Scene" now resolves the demo scene by GUID, so it works under both `Assets/` and `Packages/` install layouts.
- The active-theme fallback (used when no theme is selected in EditorPrefs or the stored reference is stale) now prefers themes that live under `Assets/` over those under `Packages/`, so UPM users do not silently end up with a read-only theme selected when an editable copy exists.
- Repo housekeeping: `*.unitypackage` builds are no longer committed to the repository; they ship as GitHub Release assets only.

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
