# AI Opponent Balance & Anti-Stalemate Design

**Date:** 2026-03-25
**Status:** Implemented ✓
**Implementation:** `docs/superpowers/plans/2026-03-25-opponent-balance-anti-stalemate.md`

---

## Problem Statement

Two stalemate failure modes exist in the current M1 prototype:

1. **Ammo exhaustion:** All trash is vacuumed. Player fires all projectiles at the opponent, misses, and has no way to acquire more ammo. Neither entity can damage the other.
2. **Overwhelming opponent:** The opponent fires too fast (1.5s cooldown) and runs out of ammo too quickly (ammo-gated shooting). This creates an on/off pressure pattern rather than sustained tension.

This spec describes four targeted changes to eliminate both failure modes while maintaining the core vacuum-race-then-combat loop.

---

## Design Intent

The player and opponent are rival cleaners competing for the same resource pool. The opponent should:
- Apply **constant, predictable threat** rather than feast-or-famine pressure
- Deny resources through vacuuming, not through ammo withholding
- Reinforce the incentive to kill it early (hoarded ammo transfers on death)

The anti-stalemate escape hatch: **the player's own missed shots replenish the resource pool.** Ammo is never permanently lost from the ecosystem.

---

## Changes

### 1. Fire Rate Reduction

**File:** `AIOpponent.cs`
**Change:** `shootCooldown` 1.5s → 1.8s (20% slower)

**Rationale:** The 1.5s rate felt too rapid and was contributing to unavoidable damage bursts. Slower fire maintains threat while giving the player reaction windows. Spread (4 degrees `shootInaccuracy`) is **not changed** — inaccuracy was not part of this balance pass and remains at its tuned value.

**Not in scope:** Adjusting `aggressionRange` (currently 30u on a ~52u radius planet). Raising the aggression range to make the opponent "always hunt" was considered but rejected — it would collapse the opponent's dual identity as both rival cleaner and combatant. Tuning `aggressionRange` is deferred to a separate balance pass after playtesting the fire rate change.

---

### 2. Shadow Removal

**File:** `AIOpponent.cs` (runtime workaround — see Tech Debt below)
**Prefab:** `Assets/_Project/Prefabs/Enemies/AIOpponent.prefab` (authoritative location)

**Rationale:** The shadow created visual confusion about the opponent's position on the planet surface. On mobile (per-vertex lighting, lower shadow quality), the shadow was more distracting than informative.

**Correct implementation:** Disable `ShadowCastingMode` directly on the prefab's Renderer components in the Inspector. This is the authoritative, persistent, and pool-safe approach.

**Tech Debt:** The current implementation disables shadows at runtime in `Start()` via `GetComponentsInChildren<Renderer>()`. This is incorrect because:
- It casts a shadow for one frame before `Start()` runs
- If the opponent is ever pooled/re-enabled, `Start()` does not re-run — `OnEnable()` would need to be updated
- The intent is invisible to Inspector-level inspection

**Required follow-up:** Move shadow disable from `Start()` to the prefab Inspector. Remove the runtime loop from code.

---

### 3. Unlimited Ammo

**File:** `AIOpponent.cs`

**Change:** Remove the `collectedAmmo > 0` gate before shooting. Stop decrementing `collectedAmmo` in `Shoot()`. The opponent fires whenever the cooldown expires, unconditionally.

**What is kept:**
- Vacuum behavior remains — opponent still chases and collects trash
- `collectedAmmo` still increments on each trash pickup (opponent's internal count grows)
- Death transfer remains — on death, `collectedAmmo` is added to the player's ammo supply
- `startingAmmo = 30` is retained as a guaranteed baseline death reward, even if the opponent dies before vacuuming anything

**Death transfer semantics:** The opponent begins with 30 ammo. Each piece of trash they vacuum adds 1. On death the player receives all of it. The 30 baseline is an intentional incentive: killing the opponent always yields a meaningful ammo bonus regardless of how much trash they managed to hoard. This is a reward for combat victory, not a representation of infinite rounds.

**Why vacuum with unlimited ammo?** The opponent chases trash for resource denial — every piece they collect is one fewer piece available for the player's ammo. It also contributes to the shared 80% cleanup threshold (see Win Condition below).

**Opponent targets all trash:** `FindNearestTrash()` uses `TrashPickup.ActiveInstances` without filtering by `CountsForProgress`. This is **intentional** — the opponent will chase and vacuum converted-from-projectile trash (non-progress pieces) as readily as real trash. This creates natural competition: player fires, projectile converts to trash, both race to collect it. The denial pressure is consistent regardless of trash origin.

---

### 4. Player Projectile → Trash Conversion

**File:** `Projectile.cs` (primary), `TrashSpawner.cs`, `TrashPickup.cs`

#### Trigger Condition

Player projectiles (identified by `shooterLayer == 6`, the Player layer) that reach end of lifetime (`timer <= 0f`) spawn a trash pickup at their current position before returning to the pool.

Planet collision is **not** used as a trigger because layer 10 (Planet) is excluded from the projectile's `hitLayers` mask and uses a solid collider, not a trigger. The 5-second lifetime is sufficient for all trajectories.

Opponent projectiles (layer 7) expire and pool normally with no conversion.

#### Conversion Behavior

1. Check `shooterLayer == 6` **before** calling `ObjectPool.ReturnOrDestroy()` — `OnEnable()` resets `shooterLayer` to -1 when the pool recycles the object
2. Call `TrashSpawner.GetRandomTrashPrefab()` to pick a random prefab from `Trash_A`, `Trash_B`, `Trash_C`
3. Get the pool via `ObjectPool.GetPoolForPrefab(trashPrefab)` — pools were initialized by `TrashSpawner.InitializeRuntime()`
4. Fallback to `Object.Instantiate()` if pool returns null (execution order edge case)
5. Set `CountsForProgress = false` on the spawned `TrashPickup`

#### Supporting Changes

**TrashSpawner:** Add static instance (`s_Instance`, set in `Awake()`) and public static accessor `GetRandomTrashPrefab()`. This avoids putting trash data on the shared projectile prefab and ensures pool compatibility.

**TrashPickup:** Add `public bool CountsForProgress { get; set; } = true;`. Reset to `true` in `OnEnable()` so pooled instances don't carry forward the false value. Both `CompleteCollection()` (player vacuum) and `AIOpponent.OnTriggerEnter()` (opponent vacuum) check this flag before calling `GameManager.RegisterTrashCollected()`.

#### Spawn Position

Trash spawns at `transform.position` when the projectile expires, with `Quaternion.identity`. No initial velocity. Known edge cases (surface clipping, ammo-farming by deliberately shooting into empty space) are accepted as low-probability risks — not in scope for this pass.

#### Conversion Feedback (In Scope — Not Yet Implemented)

When a player projectile converts to trash, both a particle burst and a short audio cue should play at the conversion point:

- **Visual:** Small particle burst at the spawn position (distinct from projectile impact particles)
- **Audio:** Brief, positional audio (3D spatialised) — nearby conversions are clearly audible, distant ones are soft

**Annoyance mitigation:** Players cannot spam-fire indefinitely — ammo is gated behind vacuuming. The maximum practical conversion rate is self-limited by the resource loop. Sound should be short (< 0.5s) and non-looping. A "clink" or "pop" rather than a sustained effect.

This feedback is marked **in scope but not yet implemented** — it should be added before the M1 milestone is closed.

---

## Win Condition Safety

`IsLevelComplete = CleanupPercentage >= 0.8f && !opponentAlive`

Both conditions must be true simultaneously. Critically, when the opponent vacuums **real** trash (`CountsForProgress == true`), `RegisterTrashCollected()` is called — so the opponent's vacuuming contributes to the 80% cleanup threshold alongside the player's. The level can reach 80% through the combined efforts of both entities.

**No soft-lock is possible** provided the player eventually kills the opponent: once the opponent dies, `opponentAlive = false`, and if cumulative cleanup (player + opponent) has reached 80%, the level completes.

The player "getting the loser's trash" refers to the ammo transfer on death, not a trash-count restoration. The 80% gate is reachable through cooperative vacuuming before the kill.

---

## Anti-Stalemate Loop

```
Player fires projectile
    → Hits opponent: damage (fight to the kill)
    → Hits planet/expires: converts to trash pickup

Both player and opponent race to vacuum the converted trash:
    → Player vacuums: +1 ammo, CountsForProgress=false (no cleanup credit)
    → Opponent vacuums: +1 to collectedAmmo, no cleanup credit
    → Either way: trash is gone, but more will appear from the next shot

Player kills opponent:
    → +collectedAmmo transferred as player ammo
    → opponentAlive = false
    → Win checks against 80% threshold
```

The resource pool is self-replenishing through the player's own firing behavior. Stalemate requires the player to stop shooting, which is not a rational strategy when the opponent has unlimited ammo.

**Open question (future work):** What makes the player miss? The spec assumes misses arise naturally from spherical geometry (shooting across the planet surface) and touch-input imprecision (dual-stick aim on mobile). A dedicated dodge mechanic or obstacle system for the opponent is not designed here.

---

## Files Modified

| File | Change |
|------|--------|
| `Assets/_Project/Scripts/Enemies/AIOpponent.cs` | Fire rate 1.8s, shadow (see tech debt), unlimited ammo, `CountsForProgress` check on vacuum |
| `Assets/_Project/Scripts/Core/Projectile.cs` | `SpawnTrashIfPlayerProjectile()` on lifetime expiry |
| `Assets/_Project/Scripts/Core/TrashPickup.cs` | `CountsForProgress` flag, guarded `RegisterTrashCollected()`, reset in `OnEnable()` |
| `Assets/_Project/Scripts/Core/TrashSpawner.cs` | `s_Instance` static ref, `GetRandomTrashPrefab()` accessor |
| `Assets/_Project/Prefabs/Enemies/AIOpponent.prefab` | Shadow casting disabled (tech debt — pending) |

---

## Out of Scope

- Conversion VFX/SFX implementation (flagged in-scope but not yet built — follow-up required)
- Shadow disable migration from `Start()` code to prefab (flagged as tech debt — follow-up required)
- `aggressionRange` tuning (deferred to post-playtesting balance pass)
- Visual transition/animation for projectile-to-trash conversion
- Changes to vacuum speed, collection radius, or player fire rate
- Ammo soft cap adjustments
- Opponent dodge/movement evasion mechanics
