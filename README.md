**If you like this asset, feel free to leave a star, thank you!**

# Hierarchy Inspector

A Unity Editor extension that turns the Hierarchy window into a workspace: row visuals, per-object styling, virtualization folders, bookmarks, and a style clipboard.

## Get the asset

Hierarchy Inspector ships on the **Unity Asset Store**:

> [Get it on the Asset Store →](https://assetstore.unity.com/packages/tools/utilities/hierarchy-inspector-378440)

A `.unitypackage` is also attached to each [GitHub Release](https://github.com/OverGast/Hierarchy-Inspector/releases) for users who want to evaluate or install outside the Asset Store. Download the latest release, then drag the `.unitypackage` into your project.

## Install via Unity Package Manager (UPM)

You can also install Hierarchy Inspector directly from this repository through Unity's Package Manager:

1. In Unity, open `Window → Package Manager`.
2. Click the `+` button in the top-left and choose `Add package from git URL`.
3. Paste:
   ```
   https://github.com/OverGast/Hierarchy-Inspector.git?path=Assets/HierarchyInspector
   ```
4. To pin to a specific release, append a tag, for example:
   ```
   https://github.com/OverGast/Hierarchy-Inspector.git?path=Assets/HierarchyInspector#v1.0.3
   ```

On first import, the welcome window will copy the bundled themes into `Assets/HierarchyInspector/Themes/` so you can edit colors and toggle settings normally; the package's runtime and editor code stay in `Packages/`.

Updates can be pulled later by re-opening the package in the manager and clicking `Update`.

## Documentation

Full user documentation is published at [overgast.github.io/spacewhale-docs](https://overgast.github.io/spacewhale-docs/hierarchy-inspector/getting-started/).

## Repo layout

- `Assets/HierarchyInspector/` houses the runtime data component, the editor overlay, the theme assets, and the demo scene.

User documentation lives in its own repository, [spacewhale-docs](https://github.com/OverGast/spacewhale-docs), and is published at the [docs site](https://overgast.github.io/spacewhale-docs/hierarchy-inspector/getting-started/).

## Support, feature requests, and bug reports

Two channels:

- **Open a ticket** on this repository: [github.com/OverGast/Hierarchy-Inspector/issues](https://github.com/OverGast/Hierarchy-Inspector/issues). Best for reproducible bugs and concrete feature requests.
- **Email** [spacewhale.assets@gmail.com](mailto:spacewhale.assets@gmail.com) for license questions, account or purchase issues, or anything you'd rather not discuss in public.
