# Virtualization Folders

Virtualization folders are a way to group GameObjects in the Hierarchy purely for organization. They look like folders, behave like folders, and disappear at build time so they cost nothing in your shipped game.

![Hierarchy with virtualization folders containing several GameObjects](../../.gitbook/assets/01-folders-in-hierarchy.png)

## The problem they solve

In Unity, the only way to group GameObjects in the Hierarchy is to make a parent GameObject and put your real objects underneath it. That works, but every parent adds a Transform, an extra step in coordinate calculations, and lifecycle overhead. Worse, if you only want the parent for organization, the children's world positions get tied to the parent's transform.

Virtualization folders give you the visual grouping without the runtime cost. At build time, the folder GameObject is destroyed and its children are reparented to the folder's parent. Their world positions stay exactly the same.

## Creating a folder

1. Create a GameObject in the hierarchy (right-click → **Create Empty**).
2. Click the gear icon on the new GameObject's row.
3. Click the **Folder** button.

The row instantly changes to look like a folder: a folder icon, a folder-styled row body, and the GameObject is now treated as a folder by the rest of the system.

To unmark something as a folder, click the gear and press **Folder** again. To remove all customization including the folder flag, use **Clear**.

## Build behavior

Hierarchy Inspector hooks Unity's `IProcessSceneWithReport` callback. When you build a player, every folder in every scene is processed in this order:

1. Find all folders in the scene, deepest-nested first.
2. Move each folder's children up to the folder's parent (or scene root if the folder was a root object).
3. Delete the folder GameObject.

The result: the runtime hierarchy is identical to what you would have built manually without folders, but you got to keep the visual grouping during development.

{% hint style="success" %}
**World positions are preserved.** Children are reparented with `worldPositionStays: true`, so their final transforms are exactly where they appear in the editor.
{% endhint %}

## What about folders inside folders?

Nesting works. Deepest-first processing means an inner folder unwinds before the outer one, so children always end up at the correct level.

```
Scene Root
├── Outer Folder (will be stripped)
│   ├── Inner Folder (will be stripped)
│   │   ├── EnemyPrefab
│   │   └── EnemyPrefab
│   └── PickupPrefab
└── Player
```

After build:

```
Scene Root
├── EnemyPrefab
├── EnemyPrefab
├── PickupPrefab
└── Player
```

## Things to know

{% hint style="warning" %}
**Don't put scripts on folder GameObjects.** Folders are intended to be empty containers. If you put a script on a folder, that script will be destroyed along with the folder at build time. The included `HierarchyFolderConflictMonitor` will warn you in the Inspector if it detects this case.
{% endhint %}

* **Folders are intact in play mode.** The strip only runs during a player build (when Unity invokes `IProcessSceneWithReport` with a non-null `BuildReport`). Press Play in the editor and your folder GameObjects stay where they are.
* **Prefabs as folders work too.** You can mark a prefab instance as a folder; the strip will still process it correctly at build.
* **Renaming a folder is fine.** The folder's name doesn't affect anything; rename freely.
