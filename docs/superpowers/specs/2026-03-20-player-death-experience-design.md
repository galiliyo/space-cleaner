# Player Death Experience — Design Spec

**Date:** 2026-03-20
**Status:** Approved
**Scope:** What happens when the player's HP reaches zero during gameplay

---

## Overview

When the player dies (HP reaches 0), the game plays a dramatic launch-off animation, transitions to an in-scene death lobby overlay, and lets the player retry with a soft penalty. No scene changes, no hard resets. The goal is a brief emotional beat — not a frustration wall.

---

## Death Animation Sequence (~3 seconds)

### 1. Freeze Frame (~50ms)
- Set `Time.timeScale = 0` for 2-3 frames using `WaitForSecondsRealtime`
- Creates hit-stop impact feel on the killing blow

### 2. Ship Launch Off Planet (~2 seconds)
- Disable `SphericalMovement` (stop surface snapping)
- Apply outward velocity along the planet surface normal (away from planet center)
- Add slight random angular spin for tumbling effect
- Ship drifts into space, planet shrinks behind it

### 3. Camera Tracks
- Existing gameplay camera follows the ship as it launches off
- Optional: subtle star-streak particle effect for motion feel

### 4. Fade to Overlay (~0.5 seconds)
- At ~2 seconds into the sequence, a full-screen dark panel fades in
- `Time.timeScale = 0` to pause gameplay behind the overlay

**Total duration:** ~3 seconds from death to lobby visible

---

## Death Lobby Overlay (In-Scene UI)

An in-scene UI Canvas panel covering the full screen. Gameplay is paused behind it.

### Layout (top to bottom)

1. **Background** — Full-screen dark overlay (black, 90% opacity). Subtle slow-drifting star particles for atmosphere.

2. **Damaged ship sprite** (center) — Pre-made 2D image of the ship looking beat up. Slight bobbing animation (sin-wave or DOTween). Smoke particle effects layered around it.

3. **Single stat line** (below ship) — Cleanup percentage only: "Planet: 47% Clean". No other stats. Minimal reading time.

4. **Retry button** (center-bottom) — Large, prominent, glowing. Label: "Retry". Triggers respawn sequence.

5. **Main Menu button** (small text, below Retry) — For players who want to quit. Loads main menu scene.

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
- HP restored to maximum (20)
- Ammo set to 0 (death penalty)
- Cleanup percentage **preserved** (unchanged)
- Opponent state **preserved** (HP, position, alive/dead unchanged)

### 3. Drop-In Animation (0.5s)
- Ship drops back onto the planet surface **at the position where the player died**
- Mirrors the death launch in reverse — flew off, now flies back
- Re-enable `SphericalMovement` once landed

### 4. Invincibility Window (2 seconds)
- Player is invulnerable for 2 seconds after respawn
- Ship blinks/flashes to communicate invincibility visually
- Prevents spawn-death loops from nearby opponent

### 5. Unpause
- `Time.timeScale = 1`
- Controls go live
- Camera follows player at new position

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
- Subscribe to player's `Health.OnDeath` event
- Run freeze frame (timeScale manipulation with real-time waits)
- Disable `SphericalMovement`, apply launch velocity + spin
- Trigger camera follow during launch
- Tell `DeathOverlayUI` to show after animation completes
- Handle retry: reset player state, play drop-in animation, enable invincibility, unpause
- Handle invincibility timer + blinking visual

#### `DeathOverlayUI.cs` (Scripts/UI/)
Manages the death lobby UI panel. Responsibilities:
- Fade panel in/out
- Display current cleanup percentage (read from `GameManager`)
- Retry button click → notify `PlayerDeathHandler`
- Main Menu button click → load main menu scene

### Modified Scripts

#### `SphericalMovement.cs`
- Use `component.enabled = false/true` to disable surface snapping during launch and re-enable on respawn

#### `PlayerController.cs`
- Add `ResetAmmo()` method to zero out ammo count and fire `OnAmmoChanged`
- Support disabling input during death sequence (disable/enable the component or input actions)

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
