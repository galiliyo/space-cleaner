# Larry Boss Fight — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the sun boss arena where Larry fires trash balls at the player, taunts via speech bubbles, and is supported by carry-over minions from defeated planet opponents.

**Architecture:** Separate BossFight scene with stationary boss (LarryBoss) on a sun sphere. LarryTrashBall is a dual-behavior projectile: deals 5 HP on player hit, spawns a high-value TrashPickup (10 ammo) on planet-surface landing. BossFightManager orchestrates spawning Larry + base minions + carry-over minions, tracks win condition, and exposes static events for HUD integration. CarryOverData is a static class accumulating defeated opponent data across the planet run.

**Tech Stack:** Unity 6 (URP 17.3.0), C# (.NET Standard 2.1), New Input System, TextMeshPro

**Spec:** `docs/superpowers/specs/2026-04-04-larry-boss-fight-design.md`

---

## File Structure

**Create:**
| File | Responsibility |
|------|---------------|
| `Assets/_Project/Tests/EditMode/SpaceCleaner.Tests.EditMode.asmdef` | Test assembly definition for EditMode tests |
| `Assets/_Project/Tests/EditMode/CarryOverDataTests.cs` | Tests for Record/GetAndClear/Reset |
| `Assets/_Project/Scripts/Boss/CarryOverData.cs` | Static carry-over data between planet run and boss fight |
| `Assets/_Project/Scripts/Boss/LarryTrashBall.cs` | Dual-behavior projectile: flight (damage) → landing (collectible) |
| `Assets/_Project/Scripts/Boss/LarryBoss.cs` | Stationary boss: fires trash balls, speech bubble taunts, OnDefeated event |
| `Assets/_Project/Scripts/Boss/BossFightManager.cs` | Arena lifecycle: spawning, win condition, static events for HUD |

**Modify:**
| File | Change |
|------|--------|
| `Assets/_Project/Scripts/Core/Health.cs` | Add `SetMaxHealth(int)` method for runtime health configuration |
| `Assets/_Project/Scripts/Core/TrashPickup.cs` | Add `ammoValue` field/property, use in `CompleteCollection()`, reset in `OnEnable()` |
| `Assets/_Project/Scripts/Enemies/AIOpponent.cs` | Add carry-over fields, `Configure()` method, use `AmmoValue`, record to CarryOverData |
| `Assets/_Project/Scripts/UI/GameplayHUD.cs` | Add boss health bar (top-center, hidden by default), subscribe to BossFightManager static events |

**Scene:**
| Path | Contents |
|------|----------|
| `Assets/_Project/Scenes/BossFight/BossFight.unity` | Sun (Planet layer 10), Player, Camera, GameplayHUD, BossFightManager |

---

## Task 1: Test Infrastructure

**Files:**
- Create: `Assets/_Project/Tests/EditMode/SpaceCleaner.Tests.EditMode.asmdef`

- [ ] **Step 1: Create test assembly definition**

Create directory `Assets/_Project/Tests/EditMode/` and write the asmdef:

```json
{
    "name": "SpaceCleaner.Tests.EditMode",
    "rootNamespace": "SpaceCleaner.Tests",
    "references": [
        "GUID:27619889-8ba5-4292-831c-b4b050ade1ea",
        "GUID:0acc523941302664db1f4e527237feb3"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Recompile and verify**

Run: `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)`
Expected: No errors. The test assembly should compile (may have 0 tests).

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Tests/EditMode/SpaceCleaner.Tests.EditMode.asmdef
git add Assets/_Project/Tests/EditMode/SpaceCleaner.Tests.EditMode.asmdef.meta
git commit -m "chore: add EditMode test assembly definition"
```

---

## Task 2: TrashPickup.ammoValue

**Files:**
- Modify: `Assets/_Project/Scripts/Core/TrashPickup.cs`

The spec requires a configurable ammo value (default 1) so Larry's trash balls can yield 10 ammo when collected.

- [ ] **Step 1: Add ammoValue field and property**

In `TrashPickup.cs`, after the `CountsForProgress` property (line 22), add:

```csharp
[SerializeField] private int ammoValue = 1;
public int AmmoValue { get => ammoValue; set => ammoValue = value; }
```

- [ ] **Step 2: Reset ammoValue in OnEnable**

In `TrashPickup.OnEnable()` (line 84), add after `CountsForProgress = true;` (line 96):

```csharp
ammoValue = 1;
```

This ensures pooled objects don't carry over a previous ammoValue (e.g., 10 from a LarryTrashBall landing).

- [ ] **Step 3: Use ammoValue in CompleteCollection**

In `TrashPickup.CompleteCollection()` (line 65), change line 71:

Old:
```csharp
player.AddAmmo(1);
```

New:
```csharp
player.AddAmmo(ammoValue);
```

- [ ] **Step 4: Recompile and verify**

Run: `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)`
Expected: No errors. All 30+ scripts compile.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Core/TrashPickup.cs
git commit -m "feat(TrashPickup): add configurable ammoValue field (default 1)"
```

---

## Task 3: CarryOverData

**Files:**
- Create: `Assets/_Project/Scripts/Boss/CarryOverData.cs`
- Create: `Assets/_Project/Tests/EditMode/CarryOverDataTests.cs`

Static class that accumulates defeated opponent data (name + ammo) during a planet run. BossFightManager calls `GetAndClear()` to retrieve entries and reset.

- [ ] **Step 1: Write failing tests**

Create `Assets/_Project/Tests/EditMode/CarryOverDataTests.cs`:

```csharp
using NUnit.Framework;
using SpaceCleaner.Boss;

namespace SpaceCleaner.Tests
{
    public class CarryOverDataTests
    {
        [SetUp]
        public void SetUp()
        {
            // Ensure clean state before each test
            CarryOverData.GetAndClear();
        }

        [Test]
        public void Record_StoresEntry()
        {
            CarryOverData.Record("Buzz", 25);
            var entries = CarryOverData.GetAndClear();
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("Buzz", entries[0].name);
            Assert.AreEqual(25, entries[0].ammo);
        }

        [Test]
        public void GetAndClear_ClearsAfterRetrieval()
        {
            CarryOverData.Record("Buzz", 25);
            CarryOverData.GetAndClear(); // first call retrieves
            var entries = CarryOverData.GetAndClear(); // second call should be empty
            Assert.AreEqual(0, entries.Count);
        }

        [Test]
        public void MultipleRecords_Accumulate()
        {
            CarryOverData.Record("Buzz", 25);
            CarryOverData.Record("Rex", 40);
            var entries = CarryOverData.GetAndClear();
            Assert.AreEqual(2, entries.Count);
        }

        [Test]
        public void GetAndClear_OnEmpty_ReturnsEmptyList()
        {
            var entries = CarryOverData.GetAndClear();
            Assert.IsNotNull(entries);
            Assert.AreEqual(0, entries.Count);
        }
    }
}
```

- [ ] **Step 2: Run tests — verify they fail**

Run: `mcp__mcp-unity__run_tests(testMode="EditMode", testFilter="SpaceCleaner.Tests.CarryOverDataTests")`
Expected: Compilation error — `CarryOverData` does not exist yet.

- [ ] **Step 3: Implement CarryOverData**

Create `Assets/_Project/Scripts/Boss/CarryOverData.cs`:

```csharp
using System.Collections.Generic;

namespace SpaceCleaner.Boss
{
    /// <summary>
    /// Accumulates defeated opponent data during a planet run.
    /// BossFightManager retrieves and clears entries at boss arena start.
    /// No persistence — data lives in memory for the duration of a solar system run.
    /// </summary>
    public static class CarryOverData
    {
        private static readonly List<(string name, int ammo)> s_Entries = new();

        public static void Record(string name, int ammo)
        {
            s_Entries.Add((name, ammo));
        }

        /// <summary>
        /// Returns all stored entries and clears the list.
        /// </summary>
        public static List<(string name, int ammo)> GetAndClear()
        {
            var result = new List<(string name, int ammo)>(s_Entries);
            s_Entries.Clear();
            return result;
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearStaticState()
        {
            s_Entries.Clear();
        }
    }
}
```

- [ ] **Step 4: Recompile and run tests — verify they pass**

Run: `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)`
Then: `mcp__mcp-unity__run_tests(testMode="EditMode", testFilter="SpaceCleaner.Tests.CarryOverDataTests")`
Expected: All 4 tests pass.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Boss/CarryOverData.cs
git add Assets/_Project/Tests/EditMode/CarryOverDataTests.cs
git commit -m "feat(Boss): add CarryOverData static class with tests"
```

---

## Task 4: AIOpponent Changes

**Files:**
- Modify: `Assets/_Project/Scripts/Enemies/AIOpponent.cs`

Add carry-over fields, a `Configure()` method for BossFightManager, use `AmmoValue` from TrashPickup, and record to CarryOverData on death.

- [ ] **Step 1: Add using directive and fields**

At the top of `AIOpponent.cs`, add after `using SpaceCleaner.Player;` (line 5):

```csharp
using SpaceCleaner.Boss;
```

After the `[Header("Collision")]` block (line 42), add:

```csharp
[Header("Boss Arena")]
public bool isCarryOver = false;
public bool recordToCarryOver = true;
```

- [ ] **Step 2: Add Configure method**

After the `CollectedAmmo` property (line 59), add:

```csharp
/// <summary>
/// Configures this opponent for boss arena use. Call after Instantiate, before first Update.
/// </summary>
public void Configure(Transform planet, float orbitRadius, int startingAmmo, bool recordToCarryOver, GameObject projectilePrefab = null)
{
    this.planet = planet;
    this.orbitRadius = orbitRadius;
    this.startingAmmo = startingAmmo;
    this.collectedAmmo = startingAmmo;
    this.recordToCarryOver = recordToCarryOver;
    if (projectilePrefab != null)
        this.projectilePrefab = projectilePrefab;

    // Auto-find or create fire point (needed for Shoot() to work)
    if (firePoint == null)
    {
        var fp = transform.Find("FirePoint");
        if (fp == null)
        {
            var fpGO = new GameObject("FirePoint");
            fpGO.transform.SetParent(transform, false);
            fpGO.transform.localPosition = Vector3.forward * 2f;
            fp = fpGO.transform;
        }
        firePoint = fp;
    }
}
```

- [ ] **Step 3: Update OnTriggerEnter to use AmmoValue**

In `OnTriggerEnter` (line 285), change the trash collection block. The current code (lines 290-298):

```csharp
if (trash != null && !trash.IsBeingCollected)
{
    bool countsForProgress = trash.CountsForProgress;
    ObjectPool.ReturnOrDestroy(other.gameObject);
    collectedAmmo++;
    SFXManager.Instance?.Play(SFXType.AICollectTrash);
    if (countsForProgress)
        GameManager.Instance?.RegisterTrashCollected();
}
```

Replace with:

```csharp
if (trash != null && !trash.IsBeingCollected)
{
    bool countsForProgress = trash.CountsForProgress;
    int ammo = trash.AmmoValue;
    ObjectPool.ReturnOrDestroy(other.gameObject);
    collectedAmmo += ammo;
    SFXManager.Instance?.Play(SFXType.AICollectTrash);
    if (countsForProgress)
        GameManager.Instance?.RegisterTrashCollected();
}
```

Key change: read `AmmoValue` before pool return, then `collectedAmmo += ammo` instead of `collectedAmmo++`.

- [ ] **Step 4: Update OnDeath to record carry-over**

In `OnDeath()` (line 301), add carry-over recording before the ammo transfer. Change from:

```csharp
private void OnDeath()
{
    SFXManager.Instance?.Play(SFXType.AIDeath);
    // Transfer collected ammo to player
    var player = FindAnyObjectByType<PlayerController>();
    if (player != null)
        player.AddAmmo(collectedAmmo);
```

To:

```csharp
private void OnDeath()
{
    SFXManager.Instance?.Play(SFXType.AIDeath);

    if (recordToCarryOver)
        CarryOverData.Record(opponentName, collectedAmmo);

    // Transfer collected ammo to player
    var player = FindAnyObjectByType<PlayerController>();
    if (player != null)
        player.AddAmmo(collectedAmmo);
```

- [ ] **Step 5: Recompile and verify**

Run: `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)`
Expected: No errors. Existing behavior unchanged (recordToCarryOver defaults to true, but CarryOverData.Record is a no-impact call when no boss fight follows).

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Scripts/Enemies/AIOpponent.cs
git commit -m "feat(AIOpponent): add carry-over fields, Configure(), use AmmoValue"
```

---

## Task 5: LarryTrashBall

**Files:**
- Create: `Assets/_Project/Scripts/Boss/LarryTrashBall.cs`

Larry's projectile. Moves toward the player via Rigidbody velocity. On player hit: 5 HP damage. On planet surface hit: spawns a high-value TrashPickup (10 ammo, does not count for progress) and self-destructs. Lifetime: 8s max.

- [ ] **Step 1: Create LarryTrashBall script**

Create `Assets/_Project/Scripts/Boss/LarryTrashBall.cs`:

```csharp
using UnityEngine;
using SpaceCleaner.Core;

namespace SpaceCleaner.Boss
{
    [RequireComponent(typeof(Rigidbody))]
    public class LarryTrashBall : MonoBehaviour
    {
        [SerializeField] private float lifetime = 8f;
        [SerializeField] private int playerDamage = 5;
        [SerializeField] private int landingAmmoValue = 10;

        private float timer;
        private int shooterLayer = -1;

        public void SetShooterLayer(int layer) => shooterLayer = layer;

        private void OnEnable()
        {
            timer = lifetime;
            shooterLayer = -1;

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (TryGetComponent<TrailRenderer>(out var trail))
                trail.Clear();
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                ObjectPool.ReturnOrDestroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            int otherLayer = other.gameObject.layer;

            // Don't hit the entity that fired us (Larry, Enemy layer 7)
            if (otherLayer == shooterLayer) return;

            // Player hit (layer 6): deal damage
            if (otherLayer == 6)
            {
                var health = other.GetComponentInParent<Health>();
                if (health != null)
                    health.TakeDamage(playerDamage);

                SFXManager.Instance?.PlayAtPosition(SFXType.ProjectileImpact, transform.position);
                ObjectPool.ReturnOrDestroy(gameObject);
                return;
            }

            // Planet/sun surface hit (layer 10): convert to collectible trash
            if (otherLayer == 10)
            {
                SpawnLandingPickup();
                ObjectPool.ReturnOrDestroy(gameObject);
                return;
            }
        }

        private void SpawnLandingPickup()
        {
            var trashPrefab = TrashSpawner.GetRandomTrashPrefab();
            if (trashPrefab == null) return;

            var pool = ObjectPool.GetPoolForPrefab(trashPrefab);
            GameObject trash = pool != null
                ? pool.Get(transform.position, transform.rotation)
                : Object.Instantiate(trashPrefab, transform.position, transform.rotation);

            // Scale up for visibility (spec: 2x)
            trash.transform.localScale = Vector3.one * 2f;

            var pickup = trash.GetComponent<TrashPickup>();
            if (pickup != null)
            {
                pickup.CountsForProgress = false;
                pickup.AmmoValue = landingAmmoValue;
            }
        }
    }
}
```

- [ ] **Step 2: Recompile and verify**

Run: `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)`
Expected: No errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Boss/LarryTrashBall.cs
git commit -m "feat(Boss): add LarryTrashBall dual-behavior projectile"
```

---

## Task 6: LarryBoss

**Files:**
- Create: `Assets/_Project/Scripts/Boss/LarryBoss.cs`

Stationary boss placed on the sun's surface. Does not move. Fires LarryTrashBall at the player on a configurable timer. Shows speech bubble taunts on a separate timer. Fires OnDefeated event on death with placeholder escape (scale to zero).

- [ ] **Step 1: Create LarryBoss script**

Create `Assets/_Project/Scripts/Boss/LarryBoss.cs`:

```csharp
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;
using SpaceCleaner.Core;
using SpaceCleaner.Player;

namespace SpaceCleaner.Boss
{
    [RequireComponent(typeof(Health))]
    public class LarryBoss : MonoBehaviour
    {
        [Header("Attack")]
        [SerializeField] private float fireRate = 3f;
        [SerializeField] private float trashBallSpeed = 20f;
        [SerializeField] private int poolSize = 8;

        [Header("Taunts")]
        [SerializeField] private float tauntMinInterval = 8f;
        [SerializeField] private float tauntMaxInterval = 12f;
        [SerializeField] private float tauntDisplayTime = 3f;
        [SerializeField] private string[] tauntLines = new[]
        {
            "You call that cleaning?!",
            "My minions will take out the trash\u2014and by trash, I mean YOU!",
            "This galaxy was boring when it was clean!",
            "Give up already!",
            "You'll never beat me!"
        };

        public event Action OnDefeated;

        private Health health;
        private Transform playerTransform;
        private float fireTimer;
        private float tauntTimer;
        private ObjectPool trashBallPool;
        private GameObject trashBallTemplate;

        // Speech bubble references
        private Canvas speechCanvas;
        private TextMeshProUGUI speechText;
        private Coroutine hideSpeechCoroutine;

        public Health Health => health;

        private void Awake()
        {
            health = GetComponent<Health>();
            health.OnDeath += HandleDeath;
        }

        private void Start()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                playerTransform = player.transform;

            CreateTrashBallPool();
            CreateSpeechBubble();

            fireTimer = fireRate;
            tauntTimer = UnityEngine.Random.Range(tauntMinInterval, tauntMaxInterval);

            // Disable shadows
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = ShadowCastingMode.Off;
        }

        private void Update()
        {
            if (health.IsDead || playerTransform == null) return;

            // Attack timer
            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f)
            {
                FireTrashBall();
                fireTimer = fireRate;
            }

            // Taunt timer
            tauntTimer -= Time.deltaTime;
            if (tauntTimer <= 0f)
            {
                ShowRandomTaunt();
                tauntTimer = UnityEngine.Random.Range(tauntMinInterval, tauntMaxInterval);
            }
        }

        private void FireTrashBall()
        {
            if (trashBallPool == null || playerTransform == null) return;

            Vector3 firePos = transform.position + transform.up * 3f;
            Vector3 dir = (playerTransform.position - firePos).normalized;
            Quaternion rot = Quaternion.LookRotation(dir);

            GameObject ball = trashBallPool.Get(firePos, rot);

            var trashBall = ball.GetComponent<LarryTrashBall>();
            if (trashBall != null)
                trashBall.SetShooterLayer(gameObject.layer); // Enemy layer 7

            var rb = ball.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = dir * trashBallSpeed;

            SFXManager.Instance?.Play(SFXType.AIShoot);
        }

        private void ShowRandomTaunt()
        {
            if (tauntLines == null || tauntLines.Length == 0) return;
            string line = tauntLines[UnityEngine.Random.Range(0, tauntLines.Length)];

            if (speechText != null)
            {
                speechText.text = line;
                speechCanvas.gameObject.SetActive(true);

                if (hideSpeechCoroutine != null)
                    StopCoroutine(hideSpeechCoroutine);
                hideSpeechCoroutine = StartCoroutine(HideSpeechAfterDelay());
            }
        }

        private IEnumerator HideSpeechAfterDelay()
        {
            yield return new WaitForSeconds(tauntDisplayTime);
            if (speechCanvas != null)
                speechCanvas.gameObject.SetActive(false);
        }

        private void HandleDeath()
        {
            OnDefeated?.Invoke();
            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            // Placeholder escape: scale to zero over 0.5s, then deactivate (M4 replaces with animation)
            float duration = 0.5f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            SFXManager.Instance?.Play(SFXType.AIDeath);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            gameObject.SetActive(false);
        }

        // --- Pool & UI creation ---

        private void CreateTrashBallPool()
        {
            // Create template object
            trashBallTemplate = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            trashBallTemplate.name = "LarryTrashBall_Template";
            trashBallTemplate.layer = 9; // Projectile layer
            trashBallTemplate.transform.localScale = Vector3.one * 1.5f;

            // Rigidbody
            var rb = trashBallTemplate.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Trigger collider (replace default non-trigger)
            var collider = trashBallTemplate.GetComponent<SphereCollider>();
            collider.isTrigger = true;

            // LarryTrashBall component
            trashBallTemplate.AddComponent<LarryTrashBall>();

            // Emissive green material (distinct from player projectiles)
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                var mat = new Material(shader);
                Color baseColor = new Color(0.3f, 1f, 0.2f, 1f);
                mat.SetColor("_BaseColor", baseColor);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", baseColor * 2.5f);
                var meshRenderer = trashBallTemplate.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.sharedMaterial = mat;
                    meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                }
            }

            // Trail renderer (same pattern as Projectile.cs)
            var trail = trashBallTemplate.AddComponent<TrailRenderer>();
            trail.time = 0.5f;
            trail.startWidth = 0.6f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;

            var trailShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (trailShader != null)
            {
                var trailMat = new Material(trailShader);
                trailMat.SetColor("_BaseColor", new Color(0.4f, 1f, 0.3f, 1f));
                trailMat.SetFloat("_Surface", 1f);
                trailMat.SetFloat("_Blend", 1f);
                trailMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                trailMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                trailMat.SetFloat("_ZWrite", 0f);
                trailMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                trailMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                trail.sharedMaterial = trailMat;
            }

            var colorGrad = new Gradient();
            colorGrad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.4f, 1f, 0.3f), 0f),
                    new GradientColorKey(new Color(0.2f, 0.6f, 0.1f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            trail.colorGradient = colorGrad;

            trashBallTemplate.SetActive(false);

            // Create pool
            var poolGO = new GameObject("LarryTrashBallPool");
            poolGO.SetActive(false);
            poolGO.transform.SetParent(transform);

            trashBallPool = poolGO.AddComponent<ObjectPool>();
            trashBallPool.InitializeRuntime(trashBallTemplate, poolSize, poolGO.transform);
            poolGO.SetActive(true);
        }

        private void CreateSpeechBubble()
        {
            // World-space canvas parented to Larry, above him
            var canvasGO = new GameObject("SpeechBubble");
            canvasGO.transform.SetParent(transform, false);
            canvasGO.transform.localPosition = Vector3.up * 5f;

            speechCanvas = canvasGO.AddComponent<Canvas>();
            speechCanvas.renderMode = RenderMode.WorldSpace;
            speechCanvas.sortingOrder = 10;

            var canvasRT = canvasGO.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(6f, 2f);
            canvasRT.localScale = Vector3.one * 0.5f;

            // Background panel
            var bgGO = new GameObject("Background");
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.SetParent(canvasRT, false);
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;
            var bgImg = bgGO.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

            // Text
            var textGO = new GameObject("TauntText");
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.SetParent(canvasRT, false);
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(0.2f, 0.1f);
            textRT.offsetMax = new Vector2(-0.2f, -0.1f);

            speechText = textGO.AddComponent<TextMeshProUGUI>();
            speechText.fontSize = 1.2f;
            speechText.color = new Color(1f, 0.9f, 0.3f, 1f);
            speechText.alignment = TextAlignmentOptions.Center;
            speechText.fontStyle = FontStyles.Bold;
            speechText.enableWordWrapping = true;

            canvasGO.SetActive(false); // Hidden until first taunt
        }

        private void OnDestroy()
        {
            if (health != null)
                health.OnDeath -= HandleDeath;
        }
    }
}
```

- [ ] **Step 2: Recompile and verify**

Run: `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)`
Expected: No errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Boss/LarryBoss.cs
git commit -m "feat(Boss): add LarryBoss with trash ball attack and speech bubble taunts"
```

---

## Task 7: BossFightManager

**Files:**
- Create: `Assets/_Project/Scripts/Boss/BossFightManager.cs`

Manages boss arena lifecycle. Spawns Larry + 2 base minions + carry-over minions. Tracks alive count. Fires static events for HUD integration. Sets up player orbit around the sun.

- [ ] **Step 1: Add SetMaxHealth to Health.cs**

In `Assets/_Project/Scripts/Core/Health.cs`, after the `Revive()` method (line 45), add:

```csharp
/// <summary>
/// Sets max health and resets current health to the new max.
/// Use for runtime-created entities (e.g. boss with configurable HP).
/// </summary>
public void SetMaxHealth(int max)
{
    maxHealth = max;
    currentHealth = max;
    OnHealthChanged?.Invoke(currentHealth, maxHealth);
}
```

- [ ] **Step 2: Create BossFightManager script**

Create `Assets/_Project/Scripts/Boss/BossFightManager.cs`:

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SpaceCleaner.Core;
using SpaceCleaner.Player;
using SpaceCleaner.Camera;
using SpaceCleaner.Enemies;
using SpaceCleaner.UI;

namespace SpaceCleaner.Boss
{
    public class BossFightManager : MonoBehaviour
    {
        [Header("Sun")]
        [SerializeField] private Transform sun;
        [SerializeField] private float sunRadius = 60f;
        [SerializeField] private float hoverHeight = 2f;

        [Header("Larry")]
        [SerializeField] private int larryMaxHealth = 50;
        [SerializeField] private Vector3 larryOffset = Vector3.up;

        [Header("Minions")]
        [SerializeField] private GameObject minionPrefab;
        [SerializeField] private int baseMinionCount = 2;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private int projectilePoolSize = 20;

        [Header("Player Setup")]
        [SerializeField] private PlayerController player;
        [SerializeField] private SphericalCamera sphericalCamera;

        // Static events — subscribe in any script's Start(), fired one frame after BossFightManager.Start()
        public static event Action<Health, string> OnBossHealthReady;
        public static event Action OnBossArenaComplete;

        private LarryBoss larry;
        private readonly List<Health> aliveEntities = new();
        private bool arenaComplete;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearStaticState()
        {
            OnBossHealthReady = null;
            OnBossArenaComplete = null;
        }

        private void Start()
        {
            float orbitRadius = sunRadius + hoverHeight;

            SetupPlayer(orbitRadius);
            SetupProjectilePool();

            // Retrieve carry-over data from planet run
            var carryOvers = CarryOverData.GetAndClear();

            // Spawn Larry on the sun surface
            SpawnLarry(orbitRadius);

            // Spawn base minions + carry-over minions
            SpawnMinions(orbitRadius, carryOvers);

            // Enable Projectile-Planet collision for LarryTrashBall landing detection
            Physics.IgnoreLayerCollision(9, 10, false);

            // Fire ready event one frame later (so all Start() subscriptions are in place)
            StartCoroutine(FireReadyEvent());
        }

        private void SetupPlayer(float orbitRadius)
        {
            if (player == null) return;

            // SphericalMovement around the sun
            var movement = player.GetComponent<SphericalMovement>();
            if (movement != null)
                movement.SetPlanet(sun, orbitRadius);

            // Position on top of the sun
            player.transform.position = sun.position + Vector3.up * orbitRadius;
            player.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

            // Aiming cone
            var aimingCone = player.GetComponentInChildren<AimingCone>();
            if (aimingCone != null)
                aimingCone.SetPlanet(sun);

            // Shooting system
            var shooting = player.GetComponent<ShootingSystem>();
            if (shooting != null)
            {
                if (aimingCone != null)
                    shooting.SetAimingCone(aimingCone);
                if (projectilePrefab != null)
                    shooting.SetProjectilePrefab(projectilePrefab);
            }

            // Disable shadows
            foreach (var r in player.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = ShadowCastingMode.Off;

            // Death handler
            if (player.GetComponent<PlayerDeathHandler>() == null)
                player.gameObject.AddComponent<PlayerDeathHandler>();

            // Camera
            if (sphericalCamera != null)
                sphericalCamera.SetTarget(player.transform, sun);
        }

        private void SetupProjectilePool()
        {
            if (projectilePrefab == null) return;
            if (ObjectPool.GetPoolForPrefab(projectilePrefab) != null) return;

            var poolGO = new GameObject("ProjectilePool");
            poolGO.SetActive(false);
            poolGO.transform.SetParent(transform);

            var pool = poolGO.AddComponent<ObjectPool>();
            pool.InitializeRuntime(projectilePrefab, projectilePoolSize, poolGO.transform);
            poolGO.SetActive(true);
        }

        private void SpawnLarry(float orbitRadius)
        {
            // Create Larry as a placeholder sphere (M4 will add proper model)
            var larryGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            larryGO.name = "Larry";
            larryGO.layer = 7; // Enemy
            larryGO.transform.localScale = Vector3.one * 4f;

            // Position on sun surface at configured offset
            Vector3 surfaceDir = larryOffset.normalized;
            if (surfaceDir.sqrMagnitude < 0.01f) surfaceDir = Vector3.up;
            larryGO.transform.position = sun.position + surfaceDir * orbitRadius;
            larryGO.transform.rotation = Quaternion.LookRotation(
                Vector3.Cross(surfaceDir, Vector3.right).normalized, surfaceDir);

            // Collider as trigger for projectile hits
            var collider = larryGO.GetComponent<BoxCollider>();
            collider.isTrigger = true;

            // Health component — AddComponent triggers Awake (sets 20 HP default),
            // then we override via SetMaxHealth to the configured value
            var health = larryGO.AddComponent<Health>();
            health.SetMaxHealth(larryMaxHealth);

            // Larry boss component
            larry = larryGO.AddComponent<LarryBoss>();
            larry.OnDefeated += () => HandleEntityDeath(health);

            // Emissive red material for visibility
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                var mat = new Material(shader);
                Color c = new Color(0.9f, 0.2f, 0.2f, 1f);
                mat.SetColor("_BaseColor", c);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", c * 2f);
                var meshRenderer = larryGO.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.sharedMaterial = mat;
                    meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                }
            }

            aliveEntities.Add(health);
        }

        private void SpawnMinions(float orbitRadius, List<(string name, int ammo)> carryOvers)
        {
            if (minionPrefab == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[BossFightManager] minionPrefab is null — no minions spawned.");
#endif
                return;
            }

            int totalMinions = baseMinionCount + carryOvers.Count;

            for (int i = 0; i < totalMinions; i++)
            {
                // Distribute evenly around the sun at equator
                float angle = (360f / totalMinions) * i;
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
                Vector3 pos = sun.position + dir * orbitRadius;

                var minionGO = Instantiate(minionPrefab, pos, Quaternion.identity);
                minionGO.name = i < baseMinionCount ? $"Minion_Base_{i}" : $"Minion_CarryOver_{i - baseMinionCount}";
                minionGO.layer = 7; // Enemy

                var opponent = minionGO.GetComponent<AIOpponent>();
                if (opponent != null)
                {
                    int ammo = 30; // default for base minions
                    bool isCarryOver = false;

                    if (i >= baseMinionCount)
                    {
                        var co = carryOvers[i - baseMinionCount];
                        ammo = co.ammo;
                        isCarryOver = true;
                    }

                    opponent.Configure(sun, orbitRadius, ammo, false, projectilePrefab);
                    opponent.isCarryOver = isCarryOver;
                }

                // Track minion health
                var health = minionGO.GetComponent<Health>();
                if (health != null)
                {
                    aliveEntities.Add(health);
                    health.OnDeath += () => HandleEntityDeath(health);
                }

                // Add opponent banner
                if (minionGO.GetComponent<OpponentBanner>() == null)
                    minionGO.AddComponent<OpponentBanner>();
            }
        }

        private void HandleEntityDeath(Health entity)
        {
            aliveEntities.Remove(entity);

            if (aliveEntities.Count == 0 && !arenaComplete)
            {
                arenaComplete = true;
                OnBossArenaComplete?.Invoke();
                SFXManager.Instance?.Play(SFXType.LevelComplete);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[BossFightManager] Boss arena complete! All enemies defeated.");
#endif
            }
        }

        private IEnumerator FireReadyEvent()
        {
            yield return null; // Wait one frame so all Start() subscriptions are in place
            if (larry != null)
                OnBossHealthReady?.Invoke(larry.Health, "Larry");
        }
    }
}
```

- [ ] **Step 3: Recompile and verify**

Run: `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)`
Expected: No errors. Verify no namespace issues.

**Note:** `SphericalCamera`, `SphericalMovement`, `AimingCone`, `ShootingSystem`, `PlayerDeathHandler`, and `OpponentBanner` are referenced. Ensure `using SpaceCleaner.Camera;` covers `SphericalCamera` and the rest are in `SpaceCleaner.Player` or `SpaceCleaner.UI`.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Scripts/Core/Health.cs
git add Assets/_Project/Scripts/Boss/BossFightManager.cs
git commit -m "feat(Boss): add BossFightManager with spawning, win condition, and events"
```

---

## Task 8: GameplayHUD Boss Health Bar

**Files:**
- Modify: `Assets/_Project/Scripts/UI/GameplayHUD.cs`

Add a boss health bar (top-center, hidden by default). Subscribes to BossFightManager static events. Shows bar + Larry's name while boss is alive. Hides on OnBossArenaComplete.

- [ ] **Step 1: Add using directive and fields**

In `GameplayHUD.cs`, add after `using SpaceCleaner.Player;` (line 9):

```csharp
using SpaceCleaner.Boss;
```

After the `private int lastDisplayedCleanupPercent = -1;` field (line 61), add:

```csharp
// Boss health bar (hidden outside boss fights)
private Slider bossHealthBar;
private TextMeshProUGUI bossHealthText;
private TextMeshProUGUI bossNameText;
private GameObject bossHealthContainer;
private Health bossHealth;
```

- [ ] **Step 2: Subscribe to BossFightManager events in Start()**

In `GameplayHUD.Start()`, after the `CreateBurstCooldownIndicator()` call (line 137), add:

```csharp
// Subscribe to boss fight events (static — safe even if BossFightManager doesn't exist)
BossFightManager.OnBossHealthReady += ShowBossHealthBar;
BossFightManager.OnBossArenaComplete += HideBossHealthBar;
```

- [ ] **Step 3: Unsubscribe in OnDestroy()**

In `GameplayHUD.OnDestroy()` (line 1063), add at the end before the closing brace:

```csharp
BossFightManager.OnBossHealthReady -= ShowBossHealthBar;
BossFightManager.OnBossArenaComplete -= HideBossHealthBar;

if (bossHealth != null)
    bossHealth.OnHealthChanged -= UpdateBossHealth;
```

- [ ] **Step 4: Add boss health bar methods**

Add the following methods after the `SetHUDVisible` method (around line 1061):

```csharp
// --- Boss Health Bar ---

private void ShowBossHealthBar(Health health, string bossName)
{
    bossHealth = health;
    bossHealth.OnHealthChanged += UpdateBossHealth;

    if (bossHealthContainer == null)
        CreateBossHealthBar();

    if (bossNameText != null)
        bossNameText.text = bossName;

    bossHealthContainer.SetActive(true);
    UpdateBossHealth(health.CurrentHealth, health.MaxHealth);
}

private void HideBossHealthBar()
{
    if (bossHealthContainer != null)
        bossHealthContainer.SetActive(false);

    if (bossHealth != null)
    {
        bossHealth.OnHealthChanged -= UpdateBossHealth;
        bossHealth = null;
    }
}

private void UpdateBossHealth(int current, int max)
{
    if (bossHealthBar != null)
        bossHealthBar.value = max > 0 ? (float)current / max : 0f;

    if (bossHealthText != null)
        bossHealthText.text = $"{current}/{max}";
}

private void CreateBossHealthBar()
{
    var parentRT = GetComponent<RectTransform>();
    if (parentRT == null) return;

    // Container — top-center
    bossHealthContainer = new GameObject("BossHealthContainer");
    var containerRT = bossHealthContainer.AddComponent<RectTransform>();
    containerRT.SetParent(parentRT, false);
    containerRT.anchorMin = new Vector2(0.5f, 1f);
    containerRT.anchorMax = new Vector2(0.5f, 1f);
    containerRT.pivot = new Vector2(0.5f, 1f);
    containerRT.anchoredPosition = new Vector2(0f, -10f);
    containerRT.sizeDelta = new Vector2(320f, 40f);

    var borderImage = bossHealthContainer.AddComponent<UnityEngine.UI.Image>();
    borderImage.color = new Color(0.15f, 0.05f, 0.05f, 0.95f);

    // Boss name label (above the bar)
    var nameGO = new GameObject("BossName");
    var nameRT = nameGO.AddComponent<RectTransform>();
    nameRT.SetParent(containerRT, false);
    nameRT.anchorMin = new Vector2(0f, 1f);
    nameRT.anchorMax = new Vector2(1f, 1f);
    nameRT.pivot = new Vector2(0.5f, 0f);
    nameRT.anchoredPosition = new Vector2(0f, 2f);
    nameRT.sizeDelta = new Vector2(0f, 18f);

    bossNameText = nameGO.AddComponent<TextMeshProUGUI>();
    bossNameText.text = "BOSS";
    bossNameText.fontSize = 14f;
    bossNameText.color = new Color(1f, 0.3f, 0.3f, 1f);
    bossNameText.alignment = TextAlignmentOptions.Center;
    bossNameText.fontStyle = FontStyles.Bold;

    // Slider bar
    var barGO = new GameObject("BossHealthBar");
    var barRT = barGO.AddComponent<RectTransform>();
    barRT.SetParent(containerRT, false);
    barRT.anchorMin = Vector2.zero;
    barRT.anchorMax = Vector2.one;
    barRT.offsetMin = new Vector2(6f, 4f);
    barRT.offsetMax = new Vector2(-6f, -4f);

    bossHealthBar = barGO.AddComponent<Slider>();
    bossHealthBar.minValue = 0f;
    bossHealthBar.maxValue = 1f;
    bossHealthBar.interactable = false;

    // Background
    var bgGO = new GameObject("Background");
    var bgRT = bgGO.AddComponent<RectTransform>();
    bgRT.SetParent(barRT, false);
    bgRT.anchorMin = Vector2.zero;
    bgRT.anchorMax = Vector2.one;
    bgRT.sizeDelta = Vector2.zero;
    var bgImg = bgGO.AddComponent<UnityEngine.UI.Image>();
    bgImg.color = new Color(0.3f, 0.05f, 0.05f, 0.8f);

    // Fill area
    var fillAreaGO = new GameObject("Fill Area");
    var fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
    fillAreaRT.SetParent(barRT, false);
    fillAreaRT.anchorMin = Vector2.zero;
    fillAreaRT.anchorMax = Vector2.one;
    fillAreaRT.sizeDelta = Vector2.zero;

    var fillGO = new GameObject("Fill");
    var fillRT = fillGO.AddComponent<RectTransform>();
    fillRT.SetParent(fillAreaRT, false);
    fillRT.anchorMin = Vector2.zero;
    fillRT.anchorMax = Vector2.one;
    fillRT.sizeDelta = Vector2.zero;

    // Reuse the health gradient (red → yellow → green)
    var fillImg = fillGO.AddComponent<UnityEngine.UI.Image>();
    fillImg.sprite = GenerateGradientSprite(s_HealthGradientTex ??= GenerateGradientTexture(256,
        new Color(1f, 0.2f, 0.2f), new Color(1f, 0.85f, 0f), new Color(0.2f, 1f, 0.5f)));
    fillImg.color = Color.white;

    bossHealthBar.fillRect = fillRT;

    // HP text overlay
    var textGO = new GameObject("BossHealthText");
    var textRT = textGO.AddComponent<RectTransform>();
    textRT.SetParent(containerRT, false);
    textRT.anchorMin = Vector2.zero;
    textRT.anchorMax = Vector2.one;
    textRT.sizeDelta = Vector2.zero;

    bossHealthText = textGO.AddComponent<TextMeshProUGUI>();
    bossHealthText.fontSize = 13f;
    bossHealthText.color = Color.white;
    bossHealthText.alignment = TextAlignmentOptions.Center;
    bossHealthText.text = "0/0";

    bossHealthContainer.SetActive(false); // Hidden by default
}
```

- [ ] **Step 5: Include boss bar in HUD visibility toggle**

In the `SetHUDVisible` method (line 1043), add after the `ammoText` line:

```csharp
if (bossHealthContainer != null) bossHealthContainer.SetActive(visible);
```

- [ ] **Step 6: Recompile and verify**

Run: `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)`
Expected: No errors.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Scripts/UI/GameplayHUD.cs
git commit -m "feat(HUD): add boss health bar with BossFightManager event integration"
```

---

## Task 9: BossFight Scene Setup

**Files:**
- Create: `Assets/_Project/Scenes/BossFight/BossFight.unity`

Create the boss arena scene with a sun, player, camera, HUD, and BossFightManager. For M2, this is loaded manually via the editor. M3 will add proper planet-to-boss transitions.

- [ ] **Step 1: Create the scene**

Use MCP:
```
mcp__mcp-unity__create_scene(scenePath="Assets/_Project/Scenes/BossFight/BossFight.unity")
mcp__mcp-unity__load_scene(scenePath="Assets/_Project/Scenes/BossFight/BossFight.unity")
```

- [ ] **Step 2: Create the sun**

Use MCP to create a sphere for the sun:
```
mcp__mcp-unity__update_gameobject(
    name="Sun",
    tag="Planet",
    layer=10,
    primitiveType="Sphere",
    position={"x":0,"y":0,"z":0},
    scale={"x":120,"y":120,"z":120}
)
```

The sun radius is 60 (scale 120 = diameter). Layer 10 = Planet so LarryTrashBall detects surface collision. Tag "Planet" for any tag-based lookups.

Then add an emissive material via `update_component`:
```
mcp__mcp-unity__update_component(
    gameObjectName="Sun",
    componentType="MeshRenderer",
    properties={"material":{"color":{"r":1,"g":0.85,"b":0.3,"a":1}}}
)
```

Add a SphereCollider trigger (for LarryTrashBall detection):
```
mcp__mcp-unity__update_component(
    gameObjectName="Sun",
    componentType="SphereCollider",
    properties={"isTrigger":true}
)
```

- [ ] **Step 3: Create player, camera, and HUD**

The player ship, spherical camera, and GameplayHUD should be set up as scene objects (prefab instances or new GameObjects with components). Follow the Gameplay scene pattern:

1. Add the player ship (copy from Gameplay scene or create fresh)
2. Add SphericalCamera
3. Add a Canvas with GameplayHUD component

These require manual scene setup or prefab instantiation. The BossFightManager's serialized fields (`player`, `sphericalCamera`) will be wired to these objects.

- [ ] **Step 4: Create BossFightManager object**

Create an empty GameObject named "BossFightManager" and add the BossFightManager component:
```
mcp__mcp-unity__update_gameobject(
    name="BossFightManager",
    position={"x":0,"y":0,"z":0}
)
```

Wire the serialized fields in the inspector:
- `sun` → Sun Transform
- `sunRadius` → 60
- `player` → PlayerController reference
- `sphericalCamera` → SphericalCamera reference
- `minionPrefab` → AIOpponent prefab (from Gameplay scene)
- `projectilePrefab` → Projectile prefab
- `larryMaxHealth` → 50
- `baseMinionCount` → 2

- [ ] **Step 5: Add SpaceSkybox**

Add a SpaceSkybox component to the BossFightManager or a separate object:
```
mcp__mcp-unity__update_component(
    gameObjectName="BossFightManager",
    componentType="SpaceSkybox"
)
```

- [ ] **Step 6: Save the scene**

```
mcp__mcp-unity__save_scene()
```

- [ ] **Step 7: Play-test the boss fight**

1. Open the BossFight scene in Unity
2. Enter Play mode
3. Verify:
   - Player spawns on the sun surface and can move (spherical movement)
   - Larry spawns as a red cube on the sun
   - Larry fires green trash balls at the player every 3s
   - Trash balls deal 5 HP damage on player hit
   - Missed trash balls hitting the sun surface become collectible trash (2x scale)
   - Collecting landed trash gives 10 ammo
   - Larry shows speech bubble taunts every 8-12s
   - 2 base minions spawn and behave like standard AI opponents
   - Boss health bar appears at top-center of HUD
   - Defeating all enemies triggers arena complete event
   - Larry scales to zero on death

- [ ] **Step 8: Commit scene and all remaining files**

```bash
git add Assets/_Project/Scenes/BossFight/
git commit -m "feat(Boss): add BossFight scene with sun arena setup"
```

---

## Integration Checklist

After all tasks, verify end-to-end:

- [ ] All scripts compile: `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)`
- [ ] EditMode tests pass: `mcp__mcp-unity__run_tests(testMode="EditMode")`
- [ ] Gameplay scene still works (TrashPickup/AIOpponent changes are backwards-compatible)
- [ ] BossFight scene: full play-through from start to boss defeat
- [ ] Boss health bar shows/hides correctly
- [ ] Carry-over system: defeat opponents in Gameplay, then load BossFight — verify extra minions spawn with correct ammo

---

## Physics Layer Note

`LarryTrashBall` is on Projectile layer (9) and must collide with:
- Player (6) — for damage on hit
- Planet (10) — for landing conversion

`BossFightManager.Start()` calls `Physics.IgnoreLayerCollision(9, 10, false)` to ensure Projectile ↔ Planet collision is enabled. If it's already enabled in the project's physics matrix, this is a no-op.

Normal projectiles (Projectile.cs) ignore Planet hits because their `hitLayers` LayerMask doesn't include layer 10. No side effects.
