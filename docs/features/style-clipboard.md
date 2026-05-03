# Copy & Paste Style

Once you have styled one GameObject the way you like (color, icon, folder flag), copying that styling to others takes one shortcut.

## Shortcuts

| Action | Shortcut |
| --- | --- |
| Copy style from selected GameObject | `Ctrl+Shift+C` (Windows/Linux), `Cmd+Shift+C` (macOS) |
| Paste style onto selected GameObject(s) | `Ctrl+Shift+V` (Windows/Linux), `Cmd+Shift+V` (macOS) |

The shortcuts are scoped to the Hierarchy window. They only fire when the Hierarchy has keyboard focus.

> 📷 **[SCREENSHOT: Two-step composite. (1) A styled GameObject (red color, custom folder icon) selected with Ctrl+Shift+C visualised. (2) Three other GameObjects selected, then Ctrl+Shift+V applied, showing all three now share the same color and icon.]**

## What gets copied

The clipboard captures:

- **Color** (or "no color")
- **Icon** (built-in palette index, if one is set)
- **Folder flag** (whether the source is a virtualization folder)

It does **not** copy:

- **Notes.** These are usually GameObject-specific and would be wrong to duplicate.
- **Bookmarks.** Bookmarking the same GameObject twice would be meaningless.
- **Project icons.** The custom-texture icon path is treated as object-specific.

## Multi-paste

Select any number of GameObjects before pressing `Ctrl+Shift+V`. The same clipboard contents get applied to every selected row.

## Clipboard scope

The clipboard is per-Unity-session. Closing Unity clears it. If you need a re-usable styling preset, save the styled GameObject as a prefab variant or template; the clipboard is for quick mid-edit copies.
