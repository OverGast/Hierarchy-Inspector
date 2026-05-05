# Bookmarks

Bookmarks let you mark important GameObjects and jump to them from a per-scene menu. Useful for the player, the main camera, the UI canvas, the level director, or anything else you find yourself selecting all the time.

![Bookmarked GameObjects with star badges visible on their rows](../.gitbook/assets/bookmarks/01-bookmarked-rows.png)

## Bookmarking a GameObject

1. Click the gear icon on the row.
2. Click the **Bookmark** button.

A small star badge appears on the row to confirm the bookmark. Click **Bookmark** in the popup again to remove it.

## Jumping to a bookmark

Right-click anywhere in the Hierarchy window. The context menu includes a **Bookmarks** submenu listing every bookmarked GameObject in the active scene. Pick one to select and frame it.

The list is grouped by scene, so working in a multi-scene setup is fine; each scene has its own bookmarks.

## Behavior details

- **Inactive GameObjects stay bookmarkable.** A bookmark on a disabled GameObject still appears in the menu so you can find and re-enable it. Most other tools hide inactive objects from search.
- **Bookmark order follows the order you set them.** The menu is not sorted alphabetically; bookmarks appear in the order they were created. This makes a simple ordering scheme work: bookmark in priority order.
- **The badge survives selection.** Selecting a bookmarked GameObject keeps the star visible. Unity's selection highlight redraws the row, but the badge is drawn afterwards so it stays on top.

{% hint style="info" %}
**Bookmarks are stored on the GameObject**, not in editor preferences. This means a bookmark moves with its scene and survives across machines, branches, and pulls. Every collaborator on the project sees the same bookmarks for that scene.
{% endhint %}

## Removing all bookmarks

There is no "clear all bookmarks" button. Open the gear popup on each bookmarked object and click **Bookmark** again to remove individually, or click **Clear** to wipe all customization on that GameObject.
