#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the Hierarchy Inspector demo scene programmatically. Used to populate a
/// known-good hierarchy for documentation screenshots without manual setup.
/// </summary>
internal static class DemoSceneBuilder
{
    private const string DemoFolder = "Assets/Plugins/HierarchyInspector/Demo";
    private const string ScenePath  = DemoFolder + "/DemoScene.unity";
    private const string PrefabPath = DemoFolder + "/DemoEnemyPrefab.prefab";

    // Demo color palette (matches the gear popup's color presets).
    private static readonly Color OrangeFolder = new Color(0.90f, 0.55f, 0.20f);
    private static readonly Color BlueFolder   = new Color(0.35f, 0.55f, 0.90f);
    private static readonly Color GreenPlayer  = new Color(0.40f, 0.75f, 0.35f);
    private static readonly Color RedHighlight = new Color(0.85f, 0.30f, 0.30f);

    [MenuItem("Tools/Hierarchy Inspector/Build Demo Scene")]
    public static void BuildDemoScene()
    {
        if (!Directory.Exists(DemoFolder))
        {
            Directory.CreateDirectory(DemoFolder);
            AssetDatabase.Refresh();
        }

        // If the scene already exists on disk, confirm overwrite. This is also a
        // safety prompt in case the user has manual edits in a loaded copy.
        if (File.Exists(ScenePath))
        {
            bool ok = EditorUtility.DisplayDialog(
                "Rebuild Demo Scene?",
                "This will replace the existing demo scene at:\n" + ScenePath +
                "\n\nAny manual edits will be lost. Continue?",
                "Rebuild", "Cancel");
            if (!ok) return;
        }

        // Make a fresh empty scene. NewSceneMode.Single closes everything else;
        // Unity will prompt the user to save any unsaved scenes first.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildHierarchy(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        // The missing-script demo cannot be done reliably in code (it requires a
        // C# file to be created, attached, and then physically deleted to leave
        // a dangling MonoScript reference). Surface a manual step instead.
        EditorUtility.DisplayDialog(
            "Demo Scene Built",
            "Demo scene saved at:\n" + ScenePath +
            "\n\nOne manual step remains for the missing-script demo:\n" +
            "  1. Create a new C# script anywhere (e.g. Demo/TempScript.cs)\n" +
            "  2. Attach it to BrokenObject\n" +
            "  3. Delete the script file (Assets/.../TempScript.cs)\n\n" +
            "BrokenObject will then show as 'Missing (Mono Script)' and the " +
            "missing-script highlight will tint its row.",
            "Got it");
    }

    // ─── HIERARCHY ─────────────────────────────────────────────────────────

    private static void BuildHierarchy(Scene scene)
    {
        CreateSeparator("---ENVIRONMENT---");

        var lighting = Create("Lighting");
        StyleAsFolder(lighting, OrangeFolder);
        var sun = CreateChild(lighting, "Sun");
        sun.AddComponent<Light>().type = LightType.Directional;
        Bookmark(sun);
        CreateChild(lighting, "Ambient Probe").AddComponent<LightProbeGroup>();
        CreateChild(lighting, "Reflection Probe").AddComponent<ReflectionProbe>();

        var cameras = Create("Cameras");
        StyleAsFolder(cameras, BlueFolder);
        var mainCam = CreateChild(cameras, "Main Camera");
        mainCam.AddComponent<Camera>();
        mainCam.AddComponent<AudioListener>();
        Bookmark(mainCam);
        CreateChild(cameras, "UI Camera").AddComponent<Camera>();

        CreateSeparator("---GAMEPLAY---");

        var player = Create("Player");
        player.AddComponent<CharacterController>();
        SetColor(player, GreenPlayer);
        SetNote(player, "Player root: damage logic in PlayerHealth.cs");
        CreateChild(player, "Model").AddComponent<MeshRenderer>();
        CreateChild(player, "Camera").AddComponent<Camera>();
        var weapons = CreateChild(player, "Weapons");
        CreateChild(weapons, "Pistol").AddComponent<MeshRenderer>();
        var rifle = CreateChild(weapons, "Rifle");
        rifle.AddComponent<MeshRenderer>();
        rifle.SetActive(false);

        // Prefab + 2 instances, one with an unsaved override.
        var prefab = EnsureDemoPrefab();
        var enemies = Create("Enemies");
        StyleAsFolder(enemies, default);
        if (prefab != null)
        {
            var spawner = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            spawner.transform.SetParent(enemies.transform);
            spawner.name = "EnemySpawner";
            spawner.transform.position = new Vector3(2f, 0f, 0f);  // creates an override

            var pool = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            pool.transform.SetParent(enemies.transform);
            pool.name = "EnemyPool";
        }

        // Empty placeholder for the manual missing-script step.
        Create("BrokenObject");

        // Missing-reference demo: DemoMissingReference has a public Transform
        // field that defaults to null, which trips the Missing Reference Indicator.
        var unwired = Create("UnwiredObject");
        unwired.AddComponent<DemoMissingReference>();

        var highlighted = Create("HighlightedObject");
        SetColor(highlighted, RedHighlight);

        CreateSeparator("---UI---");

        var canvas = Create("Canvas");
        canvas.AddComponent<Canvas>();
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        CreateChild(canvas, "HUD");
        var pauseMenu = CreateChild(canvas, "Pause Menu");
        pauseMenu.SetActive(false);

        Create("Helpers");
    }

    // ─── HELPERS ───────────────────────────────────────────────────────────

    private static GameObject Create(string name) => new GameObject(name);

    private static GameObject CreateChild(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        return go;
    }

    private static void CreateSeparator(string name)
    {
        // The auto-detect separator feature triggers off the ---Section--- name pattern.
        new GameObject(name);
    }

    private static HierarchyInspectorData EnsureData(GameObject go)
    {
        var data = go.GetComponent<HierarchyInspectorData>();
        if (data == null) data = go.AddComponent<HierarchyInspectorData>();
        data.hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInBuild;
        return data;
    }

    private static void StyleAsFolder(GameObject go, Color color)
    {
        var data = EnsureData(go);
        data.SetIsFolder(true);
        // default(Color) has alpha 0; treat that as "no color" so caller can opt out.
        if (color.a > 0f) data.SetCustomColor(true, color);
    }

    private static void SetColor(GameObject go, Color color) =>
        EnsureData(go).SetCustomColor(true, color);

    private static void SetNote(GameObject go, string note) =>
        EnsureData(go).SetNote(note);

    private static void Bookmark(GameObject go) =>
        EnsureData(go).SetIsBookmarked(true);

    private static GameObject EnsureDemoPrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null) return existing;

        // A trivial source object: an empty GO with a MeshRenderer so the prefab
        // has a recognisable component icon in the gutter.
        var source = new GameObject("_DemoEnemyPrefabSource");
        source.AddComponent<MeshRenderer>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(source, PrefabPath);
        Object.DestroyImmediate(source);
        return prefab;
    }
}
#endif
