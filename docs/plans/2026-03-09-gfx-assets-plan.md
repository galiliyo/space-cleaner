# GFX Assets — Kenney Model Integration Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace all placeholder primitive meshes (capsules, cubes, spheres) with Kenney Space Kit low-poly FBX models across all game prefabs.

**Architecture:** Update the existing `SceneSetup.cs` editor script to load FBX meshes instead of calling `CreatePrimitive()`. Each prefab keeps its current component stack (colliders, scripts, rigidbody) — only the visual mesh and material change. Add new trash variants using additional Kenney models.

**Tech Stack:** Unity 6 (URP), Kenney Space Kit FBX models, Editor scripting (`AssetDatabase`, `PrefabUtility`)

---

### Task 1: Update SceneSetup — Player Ship Mesh

**Files:**
- Modify: `Assets/_Project/Scripts/Editor/SceneSetup.cs` (CreatePlayerShipPrefab method)
- Reference: `Assets/_Project/Models/Ships/craft_speederA.fbx`
- Reference: `Assets/_Project/Materials/Player_Mat.mat`

**Step 1: Read current CreatePlayerShipPrefab method**

Read `SceneSetup.cs` and find the `CreatePlayerShipPrefab()` method. It currently uses `GameObject.CreatePrimitive(PrimitiveType.Capsule)`.

**Step 2: Replace primitive with FBX model instantiation**

Replace the primitive creation with FBX instantiation:

```csharp
private static void CreatePlayerShipPrefab()
{
    // Load FBX model
    var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Ships/craft_speederA.fbx");
    if (modelPrefab == null)
    {
        Debug.LogError("Could not load craft_speederA.fbx");
        return;
    }

    var go = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
    go.name = "PlayerShip";
    go.layer = LayerMask.NameToLayer("Player");
    go.tag = "PlayerShip";

    // Add gameplay components
    go.AddComponent<Rigidbody>().isKinematic = true;

    // Add capsule collider sized to model bounds
    var col = go.AddComponent<CapsuleCollider>();
    var bounds = CalculateBounds(go);
    col.center = bounds.center - go.transform.position;
    col.radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
    col.height = bounds.size.y;
    col.direction = 2; // Z-axis

    go.AddComponent<SpaceCleaner.Player.SphericalMovement>();
    go.AddComponent<SpaceCleaner.Player.PlayerController>();
    go.AddComponent<SpaceCleaner.Player.ShootingSystem>();
    go.AddComponent<SpaceCleaner.Player.VacuumCollector>();
    go.AddComponent<SpaceCleaner.Core.Health>();

    // FirePoint child
    var firePoint = new GameObject("FirePoint");
    firePoint.transform.SetParent(go.transform);
    firePoint.transform.localPosition = new Vector3(0, 0, bounds.extents.z + 0.2f);

    // Apply material to all renderers
    ApplyMaterialToAll(go, "Player_Mat");

    // Save prefab
    PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabRoot}/Player/PlayerShip.prefab");
    Object.DestroyImmediate(go);
    Debug.Log("PlayerShip prefab created with craft_speederA model.");
}
```

**Step 3: Add helper methods — CalculateBounds and ApplyMaterialToAll**

Add these utility methods to SceneSetup.cs:

```csharp
private static Bounds CalculateBounds(GameObject go)
{
    var renderers = go.GetComponentsInChildren<MeshRenderer>();
    if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);

    var bounds = renderers[0].bounds;
    for (int i = 1; i < renderers.Length; i++)
        bounds.Encapsulate(renderers[i].bounds);
    return bounds;
}

private static void ApplyMaterialToAll(GameObject go, string matName)
{
    var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/{matName}.mat");
    if (mat == null) return;
    foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>())
        renderer.sharedMaterial = mat;
}
```

**Step 4: Recompile and verify**

Run: `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)`
Expected: 0 errors

**Step 5: Test — recreate player prefab**

Run: `mcp__mcp-unity__execute_menu_item("SpaceCleaner/1. Create Prefabs")`
Verify PlayerShip.prefab now uses craft_speederA mesh in the scene.

**Step 6: Commit**

```bash
git add Assets/_Project/Scripts/Editor/SceneSetup.cs
git commit -m "feat: replace player ship primitive with craft_speederA FBX model"
```

---

### Task 2: Update SceneSetup — Enemy Ship Mesh

**Files:**
- Modify: `Assets/_Project/Scripts/Editor/SceneSetup.cs` (CreateAIShipPrefab method)
- Reference: `Assets/_Project/Models/Ships/craft_cargoA.fbx`

**Step 1: Replace AI ship primitive with FBX**

Same pattern as Task 1 but for the enemy:

```csharp
private static void CreateAIShipPrefab()
{
    var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Ships/craft_cargoA.fbx");
    if (modelPrefab == null)
    {
        Debug.LogError("Could not load craft_cargoA.fbx");
        return;
    }

    var go = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
    go.name = "AIShip";
    go.layer = LayerMask.NameToLayer("Enemy");
    go.tag = "EnemyShip";

    var rb = go.AddComponent<Rigidbody>();
    rb.isKinematic = true;

    var bounds = CalculateBounds(go);

    // Primary collider
    var col = go.AddComponent<CapsuleCollider>();
    col.center = bounds.center - go.transform.position;
    col.radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
    col.height = bounds.size.y;
    col.direction = 2;

    // Detection sphere trigger
    var trigger = go.AddComponent<SphereCollider>();
    trigger.isTrigger = true;
    trigger.radius = 3f;

    go.AddComponent<SpaceCleaner.Core.Health>();
    go.AddComponent<SpaceCleaner.Enemies.AIOpponent>();

    var firePoint = new GameObject("FirePoint");
    firePoint.transform.SetParent(go.transform);
    firePoint.transform.localPosition = new Vector3(0, 0, bounds.extents.z + 0.2f);

    ApplyMaterialToAll(go, "Enemy_Mat");

    PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabRoot}/Enemies/AIShip.prefab");
    Object.DestroyImmediate(go);
    Debug.Log("AIShip prefab created with craft_cargoA model.");
}
```

**Step 2: Recompile and verify**

Run: `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)`
Expected: 0 errors

**Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Editor/SceneSetup.cs
git commit -m "feat: replace enemy ship primitive with craft_cargoA FBX model"
```

---

### Task 3: Update SceneSetup — Trash Prefab Variants

**Files:**
- Modify: `Assets/_Project/Scripts/Editor/SceneSetup.cs` (CreateTrashPrefabs method)
- Reference: `Assets/_Project/Models/Trash/*.fbx`

**Step 1: Define trash variant mapping**

Replace the 3 primitive trash prefabs with FBX-based variants. Expand from 3 to 6 variants for visual variety:

```csharp
private static readonly (string name, string fbxPath, string matName, float scale)[] TrashVariants = new[]
{
    ("Trash_Barrel",     "Assets/_Project/Models/Trash/barrel.fbx",                "Trash_A_Mat", 0.5f),
    ("Trash_Satellite",  "Assets/_Project/Models/Trash/satelliteDish.fbx",         "Trash_B_Mat", 0.4f),
    ("Trash_Meteor",     "Assets/_Project/Models/Trash/meteor.fbx",               "Trash_C_Mat", 0.35f),
    ("Trash_MeteorHalf", "Assets/_Project/Models/Trash/meteor_half.fbx",          "Trash_A_Mat", 0.4f),
    ("Trash_Barrels",    "Assets/_Project/Models/Trash/barrels.fbx",              "Trash_B_Mat", 0.4f),
    ("Trash_Crystal",    "Assets/_Project/Models/Trash/rock_crystals.fbx",        "Trash_C_Mat", 0.35f),
};
```

**Step 2: Rewrite CreateTrashPrefabs**

```csharp
private static void CreateTrashPrefabs()
{
    foreach (var (name, fbxPath, matName, scale) in TrashVariants)
    {
        var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (modelPrefab == null)
        {
            Debug.LogWarning($"Could not load {fbxPath}, skipping {name}");
            continue;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
        go.name = name;
        go.layer = LayerMask.NameToLayer("Trash");
        go.tag = "Trash";
        go.transform.localScale = Vector3.one * scale;

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        // Use mesh collider for accurate shape, convex for physics
        var meshFilter = go.GetComponentInChildren<MeshFilter>();
        if (meshFilter != null)
        {
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = meshFilter.sharedMesh;
            mc.convex = true;
        }
        else
        {
            // Fallback box collider
            go.AddComponent<BoxCollider>();
        }

        go.AddComponent<SpaceCleaner.Core.TrashPickup>();

        ApplyMaterialToAll(go, matName);

        PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabRoot}/Trash/{name}.prefab");
        Object.DestroyImmediate(go);
        Debug.Log($"Trash prefab created: {name}");
    }
}
```

**Step 3: Update TrashSpawner references**

The TrashSpawner accepts `GameObject[] trashPrefabs` via serialized field. The scene setup script (SetupGameplayScene method) wires these. Update the scene setup to load all 6 new trash prefabs:

Find where trash prefabs are assigned in the scene setup and update to load all variants:

```csharp
// In SetupGameplayScene, where trashSpawner.trashPrefabs is assigned:
var trashPrefabPaths = new[]
{
    $"{PrefabRoot}/Trash/Trash_Barrel.prefab",
    $"{PrefabRoot}/Trash/Trash_Satellite.prefab",
    $"{PrefabRoot}/Trash/Trash_Meteor.prefab",
    $"{PrefabRoot}/Trash/Trash_MeteorHalf.prefab",
    $"{PrefabRoot}/Trash/Trash_Barrels.prefab",
    $"{PrefabRoot}/Trash/Trash_Crystal.prefab",
};
var trashPrefabs = trashPrefabPaths
    .Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p))
    .Where(p => p != null)
    .ToArray();
```

**Step 4: Recompile and verify**

Run: `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)`
Expected: 0 errors

**Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Editor/SceneSetup.cs
git commit -m "feat: replace trash primitives with 6 Kenney FBX variants"
```

---

### Task 4: Update Projectile Mesh

**Files:**
- Modify: `Assets/_Project/Scripts/Editor/SceneSetup.cs` (CreateProjectilePrefab method)
- Reference: `Assets/_Project/Models/Trash/rocks_smallA.fbx` (reuse as projectile)

**Step 1: Replace projectile sphere with small rock mesh**

```csharp
private static void CreateProjectilePrefab()
{
    var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Trash/rocks_smallA.fbx");
    if (modelPrefab == null)
    {
        // Fallback to sphere if model not found
        var fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fallback.name = "Projectile";
        fallback.transform.localScale = Vector3.one * 0.3f;
        PrefabUtility.SaveAsPrefabAsset(fallback, $"{PrefabRoot}/Projectiles/Projectile.prefab");
        Object.DestroyImmediate(fallback);
        return;
    }

    var go = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
    go.name = "Projectile";
    go.layer = LayerMask.NameToLayer("Projectile");
    go.tag = "Projectile";
    go.transform.localScale = Vector3.one * 0.2f;

    var rb = go.AddComponent<Rigidbody>();
    rb.useGravity = false;

    var col = go.AddComponent<SphereCollider>();
    col.isTrigger = true;
    col.radius = 0.5f;

    go.AddComponent<SpaceCleaner.Core.Projectile>();

    PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabRoot}/Projectiles/Projectile.prefab");
    Object.DestroyImmediate(go);
    Debug.Log("Projectile prefab created with rocks_smallA model.");
}
```

**Step 2: Recompile and verify**

Run: `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)`
Expected: 0 errors

**Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Editor/SceneSetup.cs
git commit -m "feat: replace projectile sphere with rock mesh"
```

---

### Task 5: Scale Tuning & Visual Verification

**Files:**
- Modify: `Assets/_Project/Scripts/Editor/SceneSetup.cs` (scale constants if needed)

**Step 1: Recreate all prefabs**

Run: `mcp__mcp-unity__execute_menu_item("SpaceCleaner/1. Create Prefabs")`

**Step 2: Rebuild gameplay scene**

Run: `mcp__mcp-unity__execute_menu_item("SpaceCleaner/2. Setup Gameplay Scene")`

**Step 3: Visual check in Scene view**

Verify in Unity:
- Player ship is visible and appropriately sized relative to planet
- Enemy ship is visually distinct from player
- Trash objects are small enough to look like debris but large enough to spot
- Projectiles are smaller than trash
- No z-fighting or inside-out meshes

**Step 4: Adjust scales if needed**

If models are too big/small, adjust the scale values in the prefab creation methods. Kenney models are typically 1-2 units — the game world may need different scales. Check against `memory/world-scaling.md` for reference values.

**Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Editor/SceneSetup.cs
git commit -m "fix: tune model scales for gameplay scene"
```

---

### Task 6: Clean Up Showcase Script & Unused Primitives

**Files:**
- Delete: `Assets/_Project/Scripts/Editor/ShipShowcase.cs`
- Delete: Old trash prefabs if renamed (Trash_A, Trash_B, Trash_C)

**Step 1: Remove ShipShowcase.cs**

Delete `Assets/_Project/Scripts/Editor/ShipShowcase.cs` — no longer needed.

**Step 2: Remove old trash prefabs**

If old `Trash_A.prefab`, `Trash_B.prefab`, `Trash_C.prefab` still exist alongside new ones, delete the old files.

**Step 3: Recompile**

Run: `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)`
Expected: 0 errors

**Step 4: Commit**

```bash
git add -A Assets/_Project/Scripts/Editor/ Assets/_Project/Prefabs/Trash/
git commit -m "chore: remove showcase script and old placeholder trash prefabs"
```
