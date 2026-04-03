# Player Death Experience — Design Spec

**Date:** 2026-03-20
**Status:** Implemented ✓
**Scope:** What happens when the player's HP reaches zero during gameplay

---

## Overview

When the player dies (HP reaches 0), the game plays a dramatic launch-off animation, transitions to an in-scene death lobby overlay, and lets the player retry with a soft penalty. No scene changes, no hard resets. The goal is a brief emotional beat — not a frustration wall.

**GDD note:** The GDD Section 2.5 states "Death Penalty: None." This spec refines that to include ammo loss as a soft penalty. Update GDD after implementation per project convention ("code is newer").

---

## Death Animation Sequence (~3 seconds)

### 1. Freeze Frame (~50ms)
- Set `Time.timeScale = 0` for 2-3 frames using `WaitForSecondsRealtime`
- Creates hit-stop impact feel on the killing blow
- `PlayerDeathHandler` caches `transform.position` and `transform.rotation` at this moment (the death position, before launch moves the ship)

### 2. Ship Launch Off Planet (~2 seconds)
- Restore `Time.timeScale = 1` after freeze frame ends
- Disable `SphericalMovement` (stop surface snapping)
- Apply outward velocity along the planet surface normal (away from planet center) via **direct transform manipulation using `Time.deltaTime`** (not Rigidbody physics, to avoid timeScale complications)
- Add slight random angular spin for tumbling effect
- Ship drifts into space, planet shrinks behind it

### 3. Camera Tracks
- Temporarily override `SphericalCamera` with a **simple smooth-follow** behind the ship during the launch sequence (the normal orbit camera is tuned for surface-level framing and will produce bad angles as the ship flies away)
- `PlayerDeathHandler` directly drives camera position during this ~2s window
- Optional: subtle star-streak particle effect for motion feel

### 4. Fade to Overlay (~0.5 seconds)
- At ~2 seconds into the sequence, a full-screen dark panel fades in
- `Time.timeScale = 0` to pause gameplay behind the overlay (this is the only sustained timeScale=0, applied after the launch animation completes)

**Total duration:** ~3 seconds from death to lobby visible

**timeScale summary:** `1 → 0 (50ms freeze) → 1 (2s launch animation) → 0 (overlay pause)`

---

## Death Lobby Overlay (In-Scene UI)

An in-scene UI Canvas panel covering the full screen. Gameplay is paused behind it.

### Layout (top to bottom)

1. **Background** — Full-screen dark overlay (black, 90% opacity). Subtle slow-drifting star particles for atmosphere.

2. **Damaged ship sprite** (center) — Pre-made 2D image of the ship looking beat up. Slight bobbing animation using `Mathf.Sin` (DOTween is not installed; do not add it solely for this feature). Smoke particle effects layered around it. **Asset note:** No damaged ship sprite exists yet — use a placeholder tinted/darkened version of the ship icon until art is created.

3. **Single stat line** (below ship) — Cleanup percentage only: "Planet: 47% Clean" (computed as `Mathf.RoundToInt(GameManager.Instance.CleanupPercentage * 100)`). No other stats. Minimal reading time.

4. **Retry button** (center-bottom) — Large, prominent, glowing. Label: "Retry". Triggers respawn sequence.

5. **Main Menu button** (small text, below Retry) — For players who want to quit. **Stub for now** — logs a warning ("Main menu not yet implemented") until the main menu scene is created.

### Design Rationale
- No RenderTexture/second camera — too expensive for a 2-3 second mobile screen
- Single stat reduces decision time — player glances and taps
- No Larry taunt in v1 (trivial to add later)

---

## Respawn Sequence

When the player taps "Retry":

### 1. Fade Out Overlay (0.3s)
- Death lobby panel fades out

### 2. Reset Player State
- HP restored to maximum via `Health.Revive()` (new method — see Architecture)
- Ammo set to 0 via `PlayerController.ResetAmmo()` (death penalty)
- Cleanup percentage **preserved** (unchanged)
- Opponent state **preserved** (HP, position, alive/dead unchanged)

### 3. Drop-In Animation (0.5s)
- Reposition ship at the **cached death position** (stored before launch)
- Ship drops back onto the planet surface from slightly above
- Mirrors the death launch in reverse — flew off, now flies back
- Re-enable `SphericalMovement` once landed
- Restore `SphericalCamera` to normal orbit-follow mode

### 4. Invincibility Window (2 seconds)
- Player is invulnerable for 2 seconds after respawn
- Ship blinks/flashes to communicate invincibility visually
- Prevents spawn-death loops from nearby opponent
- **Spawn-death fallback:** If playtesting shows 2s invincibility is insufficient (repeated deaths before ammo can be collected), apply one of: (a) grant 3-5 starting ammo on respawn, or (b) respawn on opposite side of the planet from the opponent. Prefer (a) first as it's simpler.

### 5. Unpause
- `Time.timeScale = 1`
- Controls go live
- Camera follows player at respawned position

---

## Audio Hooks

SFX call sites to add (actual audio assets deferred):

| Moment | Placeholder SFX |
|---|---|
| Death impact (freeze frame) | Heavy thud / crunch |
| Ship launch off planet | Whoosh / rocket sound |
| Overlay fade in | Low rumble or silence |
| Retry button click | UI confirm click |
| Drop-in / respawn landing | Thud + engine hum |

Implementation: Add `AudioSource.PlayOneShot()` call sites with serialized `AudioClip` fields (null by default, no error if unassigned).

---

## Penalty Summary

| Attribute | On Death |
|---|---|
| HP | Restored to max |
| Ammo | Reset to 0 (all lost) |
| Cleanup % | Preserved |
| Opponent state | Preserved |
| Player position | Same as death location |
| Game over | None — unlimited retries |

---

## Architecture

### New Scripts

#### `PlayerDeathHandler.cs` (Scripts/Player/)
Central orchestrator for the death experience. Responsibilities:
- Subscribe to player's `Health.OnDeath` event (must be idempotent — handle duplicate calls gracefully)
- Cache death position + rotation before launch animation
- Run freeze frame (timeScale manipulation with `WaitForSecondsRealtime`)
- Disable `SphericalMovement`, apply launch velocity via transform manipulation
- Override `SphericalCamera` to simple smooth-follow during launch
- Tell `DeathOverlayUI` to show after animation completes
- Handle retry: reset player state via `Health.Revive()` + `PlayerController.ResetAmmo()`, play drop-in animation, enable invincibility, unpause
- Handle invincibility timer + blinking visual
- Audio hook call sites for each phase

#### `DeathOverlayUI.cs` (Scripts/UI/)
Manages the death lobby UI panel. Responsibilities:
- Fade panel in/out (use `CanvasGroup.alpha` for smooth fading — works with `Time.timeScale = 0` via `WaitForSecondsRealtime`)
- Display current cleanup percentage (read from `GameManager`)
- Retry button click → notify `PlayerDeathHandler`
- Main Menu button click → stub for now (log warning)

### Modified Scripts

#### `Health.cs`
- Add `Revive()` method: resets `currentHealth = maxHealth`, fires `OnHealthChanged`. Must bypass the `IsDead` guard that blocks `Heal()` on dead entities. This is the critical fix — without it, HP cannot be restored after death.

#### `SphericalMovement.cs`
- Use `component.enabled = false/true` to disable surface snapping during launch and re-enable on respawn

#### `PlayerController.cs`
- Add `ResetAmmo()` method to zero out ammo count and fire `OnAmmoChanged`
- Add `bool isDead` flag checked in `Update()` to block input processing during death — do NOT disable the component (disabling triggers `OnDisable` which calls `inputActions.Disable()`, breaking all input including UI buttons)

#### `GameManager.cs`
- Add `PauseGame()` / `ResumeGame()` wrapping `Time.timeScale`
- Optionally track death count

#### `GameplayHUD.cs`
- Hide HUD elements (joysticks, ammo counter) while death overlay is active
- Re-show on respawn

### No New Scenes
Everything operates within the existing Gameplay scene. The death lobby is a UI overlay, not a separate scene.

---

## Open Questions (for playtesting)

- Exact launch velocity and spin values — tune during implementation
- Invincibility duration (2s) — may need adjustment based on opponent aggression
- Whether to add Larry taunt in a future iteration
- Drop-in animation style — straight down vs. arcing approach
