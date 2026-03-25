# AI Opponent Balance & Anti-Stalemate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Balance the AI opponent (slower fire, no shadow, unlimited ammo) and prevent stalemate by converting missed player projectiles into collectible trash.

**Architecture:** Four isolated changes across four files. TrashSpawner gets a static accessor for random trash prefabs. TrashPickup gets a `countsForProgress` flag. Projectile spawns trash on lifetime expiry for player-fired shots. AIOpponent gets fire rate, shadow, ammo, and progress-check changes.

**Tech Stack:** Unity 6 / C# / URP

**Spec:** `docs/superpowers/specs/2026-03-25-opponent-balance-anti-stalemate-design.md`

---

### Task 1: Add `GetRandomTrashPrefab()` to TrashSpawner

**Files:**
- Modify: `Assets/_Project/Scripts/Core/TrashSpawner.cs`

- [ ] **Step 1: Add static instance and accessor**

In `TrashSpawner.cs`, add a static instance field, set it in `Awake()`, and add the public accessor:

```csharp
// Add after line 33 (private ObjectPool[] trashPools;)
private static TrashSpawner s_Instance;

// Add new method after the ActiveTrashCount property (line 35)
public static GameObject GetRandomTrashPrefab()
{
    if (s_Instance == null || s_Instance.trashPrefabs == null || s_Instance.trashPrefabs.Length == 0)
        return null;
    return s_Instance.trashPrefabs[Random.Range(0, s_Instance.trashPrefabs.Length)];
}
```

Add `Awake()` before `Start()`:

```csharp
private void Awake()
{
    s_Instance = this;
}
```

- [ ] **Step 2: Recompile and verify**

Run: `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)`
Expected: No compilation errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Core/TrashSpawner.cs
git commit -m "feat: add TrashSpawner.GetRandomTrashPrefab() static accessor"
```

---

### Task 2: Add `countsForProgress` flag to TrashPickup

**Files:**
- Modify: `Assets/_Project/Scripts/Core/TrashPickup.cs`

- [ ] **Step 1: Add the flag field**

In `TrashPickup.cs`, add a public field after line 15 (`IsBeingCollected`):

```csharp
/// <summary>
/// When false, collecting this trash does not count toward level cleanup progress.
/// Used for trash converted from missed player projectiles.
/// </summary>
public bool CountsForProgress { get; set; } = true;
```

- [ ] **Step 2: Guard `RegisterTrashCollected` in `CompleteCollection`**

Change line 67 in `CompleteCollection()`:

```csharp
// Before:
GameManager.Instance?.RegisterTrashCollected();

// After:
if (CountsForProgress)
    GameManager.Instance?.RegisterTrashCollected();
```

- [ ] **Step 3: Reset flag in `OnEnable`**

Add at the end of `OnEnable()` (after line 82), so pooled trash resets correctly:

```csharp
CountsForProgress = true;
```

- [ ] **Step 4: Recompile and verify**

Run: `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)`
Expected: No compilation errors.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Core/TrashPickup.cs
git commit -m "feat: add CountsForProgress flag to TrashPickup"
```

---

### Task 3: Convert player projectiles to trash on lifetime expiry

**Files:**
- Modify: `Assets/_Project/Scripts/Core/Projectile.cs`

- [ ] **Step 1: Add conversion logic in `Update`**

Replace the lifetime expiry block in `Update()` (lines 128-131):

```csharp
// Before:
if (timer <= 0f)
{
    ObjectPool.ReturnOrDestroy(gameObject);
}

// After:
if (timer <= 0f)
{
    SpawnTrashIfPlayerProjectile();
    ObjectPool.ReturnOrDestroy(gameObject);
}
```

- [ ] **Step 2: Add the `SpawnTrashIfPlayerProjectile` method**

Add this method after `OnTriggerEnter` (after line 148):

```csharp
private void SpawnTrashIfPlayerProjectile()
{
    // Only player projectiles (layer 6) convert to trash
    if (shooterLayer != 6) return;

    var trashPrefab = TrashSpawner.GetRandomTrashPrefab();
    if (trashPrefab == null) return;

    var pool = ObjectPool.GetPoolForPrefab(trashPrefab);
    GameObject trash = pool != null
        ? pool.Get(transform.position, Quaternion.identity)
        : Object.Instantiate(trashPrefab, transform.position, Quaternion.identity);

    var pickup = trash.GetComponent<TrashPickup>();
    if (pickup != null)
        pickup.CountsForProgress = false;
}
```

- [ ] **Step 3: Recompile and verify**

Run: `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)`
Expected: No compilation errors.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Scripts/Core/Projectile.cs
git commit -m "feat: convert missed player projectiles to non-progress trash"
```

---

### Task 4: AI opponent balance changes

**Files:**
- Modify: `Assets/_Project/Scripts/Enemies/AIOpponent.cs`

- [ ] **Step 1: Add `using UnityEngine.Rendering;`**

Add after line 2 (`using UnityEngine;`):

```csharp
using UnityEngine.Rendering;
```

- [ ] **Step 2: Change fire rate**

Change line 26:

```csharp
// Before:
[SerializeField] private float shootCooldown = 1.5f;

// After:
[SerializeField] private float shootCooldown = 1.8f;
```

- [ ] **Step 3: Remove shadow in `Start()`**

Add at the end of `Start()`, before the closing brace (after line 78):

```csharp
// Disable shadows on opponent
foreach (var renderer in GetComponentsInChildren<Renderer>())
    renderer.shadowCastingMode = ShadowCastingMode.Off;
```

- [ ] **Step 4: Unlimited ammo — remove ammo check in combat**

Change line 175 in `UpdateCombatBehavior()`:

```csharp
// Before:
else if (shootTimer <= 0f && collectedAmmo > 0)

// After:
else if (shootTimer <= 0f)
```

- [ ] **Step 5: Unlimited ammo — stop decrementing in `Shoot()`**

Remove line 210 in `Shoot()`:

```csharp
// Remove this line:
collectedAmmo--;
```

- [ ] **Step 6: Check `countsForProgress` on vacuum**

Change the `OnTriggerEnter` method (lines 279-289):

```csharp
private void OnTriggerEnter(Collider other)
{
    if (((1 << other.gameObject.layer) & trashLayer) == 0) return;

    var trash = other.GetComponent<TrashPickup>();
    if (trash != null && !trash.IsBeingCollected)
    {
        bool countsForProgress = trash.CountsForProgress;
        ObjectPool.ReturnOrDestroy(other.gameObject);
        collectedAmmo++;
        if (countsForProgress)
            GameManager.Instance?.RegisterTrashCollected();
    }
}
```

Note: Read `CountsForProgress` before pooling the object, since `OnEnable` resets it to `true`.

- [ ] **Step 7: Recompile and verify**

Run: `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)`
Expected: No compilation errors.

- [ ] **Step 8: Commit**

```bash
git add Assets/_Project/Scripts/Enemies/AIOpponent.cs
git commit -m "feat: AI opponent balance — slower fire, no shadow, unlimited ammo, progress-safe vacuum"
```

---

### Task 5: Playtest verification

- [ ] **Step 1: Enter play mode and verify opponent has no shadow**

Visual check: the AI opponent ship should cast no shadow on the planet surface.

- [ ] **Step 2: Verify opponent fires continuously**

Observe: the opponent should keep shooting even after firing many shots — no ammo depletion.

- [ ] **Step 3: Verify fire rate feels slower**

The opponent should fire roughly every 1.8 seconds (noticeably slower than before).

- [ ] **Step 4: Verify missed player shots become trash**

Fire player projectiles that miss. After ~5 seconds, trash pickups should appear where the projectiles expired.

- [ ] **Step 5: Verify converted trash is vacuumable**

Approach the converted trash with the player vacuum. It should be collectible and give 1 ammo.

- [ ] **Step 6: Verify cleanup bar is unaffected by converted trash**

The cleanup progress bar should NOT increase when converted-from-projectile trash is collected.

- [ ] **Step 7: Verify opponent death still gives ammo**

Kill the opponent. Player should receive the opponent's vacuumed trash count as ammo.
