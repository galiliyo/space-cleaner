# Larry Boss Fight — Design Spec

**Date:** 2026-04-04
**Status:** Approved
**Scope:** Larry boss fight, score carry-over system, Larry personality (taunts). Covers the sun-level boss arena for M2.

---

## Overview

After clearing all planets in a solar system, the player enters the sun boss arena. Larry is stationary at the sun's surface. He fires trash balls at the player and summons minions. The player must defeat Larry and all minions to complete the solar system. Defeated planet opponents carry over as armed minions in the boss fight.

---

## 1. Score Carry-Over

A static `CarryOverData` class accumulates defeated opponent data across the planet run.

**API:**
```csharp
CarryOverData.Record(string name, int ammo)  // called from AIOpponent.OnDeath
CarryOverData.GetAndClear()                  // called by BossFightManager at arena start
                                             // returns List<(string name, int ammo)> and clears
```

- Called from `AIOpponent.OnDeath` alongside the existing ammo-transfer-to-player logic
- No persistence — M3 will handle save/load
- Data lives in memory for the duration of a solar system run

---

## 2. `LarryTrashBall.cs`

Larry's projectile. Dual-behavior: damages the player on direct hit, converts to a collectible trash pickup if it misses and lands on the sun surface.

**Flight phase (projectile):**
- Spawned by `LarryBoss` aimed at the player's current position
- Moves via `Rigidbody.linearVelocity` (same pattern as `Projectile.cs`)
- Configurable speed (`SerializeField`, default 20f)
- On collision with player (layer 6): deals 5 HP damage via `Health.TakeDamage(5)`, then returns to pool / destroys
- Lifetime: 8s max — if no collision, returns to pool
- Ignores Larry (layer check, same as `Projectile.SetShooterLayer`)

**Landing phase (collectible):**
- On collision with sun surface (layer 10 — Planet): disables `Rigidbody`, snaps to surface, enables vacuum collection
- Becomes a standard `TrashPickup` with `CountsForProgress = false` and `ammoValue = 10`
- The player's existing `VacuumCollector` handles collection — no changes needed to vacuum system

**Visual:** Uses one of the existing Trash prefab meshes (Trash_A/B/C) but scaled 2x for visibility. Reuses `Projectile`'s trail renderer pattern for in-flight visibility.

---

## 3. `LarryBoss.cs`

Stationary GameObject placed on the sun's surface. Does not move. Namespace: `SpaceCleaner.Boss`.

**Components:** `Health` (configurable, default 50 HP for solar system 1), `AudioSource`
**Layer:** Enemy (7) — same as AI opponents

**Attack — Trash Ball:**
- Fires a `LarryTrashBall` at the player's current position
- Fire rate: configurable `SerializeField`, default 3s between shots
- Uses `ObjectPool` if available, else `Instantiate`

**Taunts — Speech Bubble:**
- World-space canvas parented to Larry, positioned above him
- TextMeshPro label, auto-hides after 3s
- `string[]` of taunt lines (serialized, default lines provided)
- Taunt timer: randomised 8–12s interval, independent of attack timer
- Default taunt lines:
  - "You call that cleaning?!"
  - "My minions will take out the trash — and by trash, I mean YOU!"
  - "This galaxy was boring when it was clean!"
  - "Give up already!"
  - "You'll never beat me!"

**Events:**
```csharp
public event Action OnDefeated;  // fired when Health.OnDeath triggers
```

On defeat: fires `OnDefeated`, plays escape placeholder (a simple scale-to-zero + deactivate — M4 will replace with animation).

---

## 4. `BossFightManager.cs`

Manages boss arena lifecycle. Lives in the BossFight scene at `Assets/_Project/Scenes/BossFight/`. Namespace: `SpaceCleaner.Boss`.

**Scene setup:**
- A separate scene containing the sun (sphere with emissive material), Larry, and spawn points
- For M2, loaded manually via editor or a temporary "Start Boss Fight" button — M3 will add proper planet-to-boss transitions
- The sun uses the Planet layer (10) so `LarryTrashBall` can detect surface collision

**On Start:**
1. Calls `CarryOverData.GetAndClear()` to retrieve planet opponents
2. Spawns Larry at a fixed position on the sun surface (configurable `SerializeField` offset)
3. Spawns 2 base minions (`AIOpponent` prefab) at symmetric positions around Larry
4. Assigns the sun `Transform` to each minion's `planet` field (for spherical movement)
5. Sets `recordToCarryOver = false` on all arena minions
6. For each carry-over entry: spawns an additional `AIOpponent` with `startingAmmo` set to the stored value, `isCarryOver = true`, and `recordToCarryOver = false`
7. Subscribes to `Health.OnDeath` for Larry and each minion

**Win Condition:**
- Tracks alive count (Larry + all minions)
- When all are dead → fires `OnBossArenaComplete`
- `BossFightManager` owns boss completion independently from `GameManager` — `GameManager` is not involved in boss state. M3 will unify them under a progression manager.

**Boss Health Bar:**
- On arena start, raises `OnBossHealthReady(Health larryHealth, string larryName)`
- `GameplayHUD` subscribes and shows a top-center boss bar (hidden outside boss fights)
- Boss bar reuses the same gradient logic as the player health bar

---

## 5. `TrashPickup` changes

Add a configurable ammo value:
```csharp
[SerializeField] private int ammoValue = 1;
public int AmmoValue => ammoValue;
```

Two hardcoded ammo references must be updated:
- `TrashPickup.CompleteCollection()` line 71: change `player.AddAmmo(1)` → `player.AddAmmo(ammoValue)`
- `AIOpponent.OnTriggerEnter()` line 295: change `collectedAmmo++` → `collectedAmmo += trash.AmmoValue`

Default is 1, so all existing trash behavior is unchanged. `LarryTrashBall` sets `ammoValue = 10` when converting to landing phase.

---

## 6. `AIOpponent` changes (minimal)

Add two fields:
```csharp
public bool isCarryOver = false;       // stub for M4 visual distinction
public bool recordToCarryOver = true;  // false for boss arena minions
```

In `OnDeath`, add carry-over recording (before existing ammo-transfer-to-player logic):
```csharp
if (recordToCarryOver)
    CarryOverData.Record(opponentName, collectedAmmo);
```

`BossFightManager` sets `recordToCarryOver = false` on all arena minions — otherwise defeated boss minions would feed into the *next* solar system's boss fight.

---

## 7. `GameplayHUD` changes (minimal)

Add a boss health bar section (top-center, hidden by default):
- Subscribes to `BossFightManager.OnBossHealthReady`
- Shows bar + Larry's name while boss is alive
- Hides on `OnBossArenaComplete`

---

## Out of Scope

- Larry escape animation (M4 art)
- Visual distinction for carry-over minions (M4 art)
- Larry attack phase transitions (M5 balancing)
- Larry voice acting (M4 audio)
- Boss fight scene transition from planet (M3 progression)
- Larry 3D model — uses placeholder geometry for M2
