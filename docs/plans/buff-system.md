# Buff System

A pickup-based power-up system: large spinning rings spawn on the planet orbit; flying through one grants a temporary multiplier to either the player or an AI opponent.

## Behavior

- **Three buff types** (one instance of each can exist at a time):
  | Type | Color | Effect | Multiplier |
  |---|---|---|---|
  | `Speed` | Cyan | Movement speed | 1.5× |
  | `VacuumRadius` | Green | Vacuum collection range | 3× |
  | `AmmoStrength` | Orange | Damage dealt + ammo gained per trash | 2× |

- **Lifecycle per ring:**
  - Spawns at a random point on the orbit shell
  - Stays active for 30s (auto-despawns if uncollected)
  - On collection: applies the buff for 15s, plays SFX, despawns
  - After despawn (collected or expired): 30s cooldown before respawning
  - Initial spawns are staggered: `Speed` at t=0, `VacuumRadius` at t=10s, `AmmoStrength` at t=20s

- **Collection rule:** any entity with a `BuffReceiver` component within 3.5 units of a ring's center picks it up. Both player and AI can collect.

## Architecture

### Components

| Script | Role |
|---|---|
| `Core/BuffType.cs` | Enum: `Speed`, `VacuumRadius`, `AmmoStrength` |
| `Core/BuffReceiver.cs` | Per-entity component holding active timers and exposing multipliers. Maintains a static `All` list so `BuffPickup` can scan receivers without `FindObjectsByType` |
| `Core/BuffPickup.cs` | The visual ring: `LineRenderer` circle in local space, additive emissive material, spins around the surface normal. Distance-checks `BuffReceiver.All` each frame |
| `Core/BuffManager.cs` | Singleton that tracks per-type cooldowns and spawns rings. Auto-discovers planet via `"Planet"` tag |

### Data flow

```
BuffManager.Update()
  └─ for each type, tick cooldown → Spawn() when ≤ 0
       └─ creates GameObject + BuffPickup, calls Initialize(type, pos, normal)

BuffPickup.Update()
  ├─ rotate around surface normal
  ├─ tick despawn timer
  └─ for each BuffReceiver in BuffReceiver.All:
       └─ if within CollectRadius → receiver.ApplyBuff(type), Despawn(collected:true)

BuffReceiver.ApplyBuff(type)
  └─ sets _timers[type] = 15s → Recalculate() → multipliers update
```

### Multiplier consumers

| System | Reads | Effect |
|---|---|---|
| `Player/SphericalMovement` | `SpeedMultiplier` | `effectiveSpeed = moveSpeed * mult` |
| `Enemies/AIOpponent` (in `MoveToward`) | `SpeedMultiplier` | Same as above |
| `Player/VacuumCollector` (in `LateUpdate`) | `VacuumRadiusMultiplier` | Resizes `SphereCollider.radius` and VFX shape radius when multiplier changes |
| `Player/ShootingSystem` (in `FireProjectile`) | `DamageMultiplier` | Calls `projectile.OverrideDamage(...)` |
| `Enemies/AIOpponent` (in `Shoot`) | `DamageMultiplier` | Same as above |
| `Core/TrashPickup.CompleteCollection` | `AmmoGainMultiplier` | Multiplies `ammoValue` before `AddAmmo` |
| `Enemies/AIOpponent.OnTriggerEnter` | `AmmoGainMultiplier` | Same |

### Auto-attachment

`BuffReceiver` is added in `Awake()` of any consumer that needs it:

```csharp
buffReceiver = GetComponent<Core.BuffReceiver>() ?? gameObject.AddComponent<Core.BuffReceiver>();
```

This avoids manual prefab editing — works for both player and AI without scene changes.

## Scene Setup Required

`BuffManager` is **not** auto-created. To enable the system in the Gameplay scene:

1. Hierarchy → right-click → **Create Empty** → name it `BuffManager`
2. Inspector → **Add Component** → `BuffManager`
3. Planet is auto-discovered via the `"Planet"` tag — no Inspector wiring needed

## Known Issues

### 1. `BuffReceiver` not found in namespace `SpaceCleaner.Core`

**Symptom:** Pressing Play does nothing; Console shows four CS0234 errors:

```
AIOpponent.cs(51,22): error CS0234: BuffReceiver does not exist in namespace 'SpaceCleaner.Core'
ShootingSystem.cs(30,22): same
SphericalMovement.cs(22,22): same
VacuumCollector.cs(33,22): same
```

**Cause:** Buff scripts were created externally (via Claude). Unity's Bee build system cached the assembly's file list before they appeared. The files exist with valid `.meta` files but aren't included in the active build graph.

**Fix:** In Unity, press **Ctrl+R** (Assets → Refresh). Wait for the compilation indicator in the bottom-right to finish.

### 2. Skybox not visible in Android builds

**Symptom:** Skybox renders correctly in the editor, but Android builds show a black background.

**Cause:** URP ignores `Camera.clearFlags`. It reads `backgroundType` from `UniversalAdditionalCameraData` instead. The reflection call in `SpaceSkybox.ApplyLighting()` was passing `int 0` to a property of type `CameraBackgroundType` (an enum). `PropertyInfo.SetValue` does not auto-convert int → enum and was throwing `ArgumentException` silently.

**Fix:** Convert via `Enum.ToObject(prop.PropertyType, 0)`:

```csharp
var prop = camData.GetType().GetProperty("backgroundType");
if (prop != null)
    prop.SetValue(camData, Enum.ToObject(prop.PropertyType, 0)); // 0 = Skybox
```

(Reflection is used to avoid a hard assembly dependency on `UniversalAdditionalCameraData`.)

### 3. AI opponent aggressive immediately after respawn

**Symptom:** Right after respawning, the AI is already moving and shooting before the player can react.

**Cause:** In `PlayerDeathHandler.RespawnSequence()`, `GameManager.ResumeGame()` ran *before* the drop-in animation, but the `FreezeUntilPlayerReady()` call was placed *after* the drop-in. Result: ~0.5s window where the AI was unfrozen and free to chase.

**Fix:** Moved the freeze call to immediately follow `ResumeGame()`, before the drop-in animation begins:

```csharp
movement.enabled = true;
GameManager.Instance?.ResumeGame();

// Freeze opponents BEFORE the drop-in so they can't attack during the animation
foreach (var opp in FindObjectsByType<AIOpponent>(FindObjectsSortMode.None))
    opp.FreezeUntilPlayerReady(minAmmo: 3, maxDuration: 30f);
```

The freeze unblocks once the player has collected ≥3 ammo, with a 30s safety fallback.

## Tuning Constants

All in `BuffReceiver` and `BuffPickup`:

```csharp
// BuffReceiver
SpeedMult        = 1.5f
VacuumMult       = 3f
DamageMult       = 2f
AmmoGainMult     = 2f
BuffDuration     = 15f  // seconds

// BuffPickup
RingRadius       = 5f
CollectRadius    = 3.5f
ActiveDuration   = 30f  // ring lifetime if uncollected
RotateSpeed      = 25f  // deg/s

// BuffManager
Cooldown         = 30f  // post-despawn cooldown per type
```
