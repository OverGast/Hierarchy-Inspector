# The Gear Popup

The gear popup is where you customize one GameObject (or a multi-selection) without leaving the Hierarchy window. It opens from a small gear icon that appears on each row.

> 📷 **[SCREENSHOT: A hierarchy row with the mouse hovering over it. The gear icon is highlighted on the right side of the row, just before the component icons.]**

## Opening it

Hover over a row in the Hierarchy. A gear icon appears next to the GameObject's icon. Click it to open the popup. To customize multiple GameObjects at once, **select them in the hierarchy first** (Ctrl/Cmd-click or Shift-click) and then click the gear on any of the selected rows. The popup will show "N objects selected" at the top and apply edits to all of them.

> 📷 **[SCREENSHOT: The gear popup open over a hierarchy, fully expanded, showing all sections: Color, Icon, Folder/Bookmark/Clear buttons, and the Notes field.]**

## What's in the popup

The popup is divided into named sections.

### Color

Eight preset colors plus a "no color" option. Click a swatch to apply that color to the row. The selected swatch shows a thicker outline. Choose **None** (the leftmost option) to remove a color you previously set.

> 📷 **[SCREENSHOT: Close-up of the Color row in the popup, showing the 8 preset chips with one selected (highlighted outline) and the None chip.]**

The eight presets are tuned to read clearly against both dark and light editor skins.

### Icon

Replaces the GameObject's hierarchy icon with a built-in choice. Browse a curated palette of icons that ship with Hierarchy Decorator (folders, gear, eye, bookmark, etc.). Click an icon to apply it; click it again to clear.

If the curated set isn't enough, the **Project Icon** picker below it accepts any `Texture2D` from your project. Drag a texture in or use the object field. This is great for tagging objects with a logo or category icon you have already authored.

> 📷 **[SCREENSHOT: The Icon section open, showing the rows of built-in icons with one selected, plus the Project Icon picker below it with a custom texture chosen.]**

{% hint style="info" %}
**Icons override the default hierarchy icon, including the "use main component icon" theme behavior.** A row with a custom icon set always shows that icon.
{% endhint %}

### Folder / Bookmark / Clear

Three action buttons:

- **Folder.** Marks this GameObject as a virtualization folder. Folders look like folders in the hierarchy and get stripped at build time. See [Virtualization Folders](folders.md) for the full story.
- **Bookmark.** Adds this GameObject to the per-scene bookmark list. A small star badge appears on the row. Click again to unbookmark. See [Bookmarks](bookmarks.md).
- **Clear.** Removes all customization (color, icon, notes, folder/bookmark flags) and removes the underlying `HierarchyInspectorData` component from the GameObject.

### Notes

A free-form text field. Anything you type here is stored on the GameObject and shows as a tooltip when you hover the row. Useful for "this object is referenced from script X" or "remember to bake before shipping" reminders.

> 📷 **[SCREENSHOT: The Notes field with a few lines of example text typed in, plus a separate screenshot of a hierarchy row showing the tooltip popup with the same notes when hovered.]**

## Multi-select editing

When the popup opens with multiple GameObjects selected, every action applies to all of them. The popup uses the **first** selected object as the source of truth for showing current state (which color/icon is selected). Editing any field writes to all of them.

This is the fastest way to color-tag a whole branch of related GameObjects in one go.

## Where data is stored

Customizations live in a small `HierarchyInspectorData` MonoBehaviour added to the GameObject. The component is hidden in the Inspector by default (it has the `HideInInspector` flag set) so it doesn't clutter the GameObject's component list. It only exists on objects you have actually styled; clicking **Clear** removes it.

{% hint style="warning" %}
The data component is marked `DontSaveInBuild`, so it is automatically stripped from your built game. Color, icon, and bookmark data is editor-only and adds zero runtime cost.
{% endhint %}
