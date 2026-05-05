# Themes & Preferences

A **theme** is a `ScriptableObject` asset that holds every setting Hierarchy Inspector uses to draw the hierarchy. You can have multiple themes in your project and switch between them per-user. The default install ships one theme to get you started.

## The Preferences pane

Open **Edit → Preferences → Hierarchy Inspector**. This is the per-user settings pane.

> 📷 **[SCREENSHOT: Unity's Preferences window with the Hierarchy Inspector section selected on the left sidebar, and the right panel showing the Active Theme picker, action buttons, and the embedded theme inspector below.]**

The pane has:

- **Theme Asset.** The currently active theme. Drop in a different `HierarchyInspectorTheme` asset to switch. The selection is stored per-user (in `EditorPrefs`), not in the project, so different team members can have different active themes without stepping on each other.
- **Create New Theme.** Generates a new theme asset in `Assets/Editor/Themes/Hierarchy/` (the folder is created if missing) and switches to it. The new theme starts with the same defaults as the bundled one.
- **Reveal Asset.** Pings the active theme in the Project window so you can find it on disk.
- **Reset to Defaults.** Restores every field on the active theme to its built-in default. Undoable.
- **Embedded inspector.** The active theme's full inspector renders right inside the Preferences pane so you can tweak settings without leaving the dialog.

## Editing a theme

You can edit a theme two ways:

1. **From Preferences** as described above.
2. **Selecting the asset directly.** Find the theme in the Project window, click it, and the Inspector shows the same tabbed editor.

Both routes are equivalent; pick whichever flow fits your hands. Either way, **changes update every open Hierarchy window in real-time**. There is no "Apply" button. The overlay re-reads settings on every paint.

> 📷 **[SCREENSHOT: The full theme inspector with the Rows tab open, showing the 4-tab bar, accent strip at top, and the section foldouts for Background, Effects, Inactive State, and Selection.]**

## The theme inspector at a glance

The inspector is organized into **4 tabs**:

| Tab | What it controls |
| --- | --- |
| [Rows](../theme/rows.md) | Backgrounds, hover effects, depth, tree lines, selection |
| [Icons](../theme/icons.md) | Component icons in the gutter, GameObject icons, UI tints |
| [Indicators](../theme/indicators.md) | Prefab tints, missing-script warnings, separator detection |
| [Customization](../theme/customization.md) | Per-object styling, color effects, toolbar, animations |

Each tab is divided into named sections (foldouts). Settings inside a section are usually a parent toggle followed by detail fields. When you turn off a parent toggle, the detail fields gray out and indent so you can see they exist but are not currently in effect. This is **show_if** behavior; the detail fields stay visible for discovery, they just become read-only until you re-enable the parent.

## The master toggle

At the very top of the theme inspector is the **Overlay Enabled** toggle. Turn it off to disable Hierarchy Inspector entirely; you see Unity's stock hierarchy until you re-enable. This is a master switch above every other setting.

A status pill in the header shows the current state at a glance: green "● ENABLED" or red "● DISABLED".


## Creating a new theme

If you want a second theme (for example, a high-contrast version, or one with no animations for performance work):

1. Open **Edit → Preferences → Hierarchy Inspector**.
2. Click **Create New Theme**. A new theme asset appears in `Assets/Editor/Themes/Hierarchy/` and becomes active.
3. Edit the new theme's settings as you want.
4. Switch back to the old theme any time by dragging it into the Theme Asset field.

You can also duplicate any theme asset in the Project window (Ctrl+D) to fork an existing configuration.

{% hint style="info" %}
**Theme assets travel with the project.** Anyone who pulls the project gets the same themes. The user's *active* selection is the only thing that's per-user.
{% endhint %}
