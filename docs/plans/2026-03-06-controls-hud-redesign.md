# Controls & HUD Redesign

**Date:** 2026-03-06
**Status:** Approved

## Controls

### Movement Joystick (Bottom-Left)
- Virtual joystick, fixed position
- Controls ship direction on spherical surface
- Existing `SphericalMovement.cs` handles input

### Aim/Fire Joystick (Bottom-Right)
- **Replaces** the separate SingleShot and BurstShot buttons
- One unified joystick for aiming and firing:
  - **Flick (release < 0.3s):** Single shot in aim direction, short cooldown
  - **Hold (> 0.3s):** Auto-fire burst in aim direction, continues while held
- Direction determined by joystick position at moment of release (flick) or continuously while held (burst)
- Requires ammo to fire; no-ammo feedback (visual flash or sound)

### Input Actions Update
- Remove: `SingleShot` (Button), `BurstShot` (Button + Hold)
- Add: `Aim` (Vector2, right stick / right side touch)
- Keep: `Move` (Vector2, left stick / left side touch)

## HUD Layout

```
┌──────────────────────────────────────┐
│ [ammo: 23/50]          [████░░ 45%] │  top-right: ammo, top-center: cleanup bar
│                                      │
│                                      │
│            gameplay area              │
│                                      │
│                                      │
│  ┌───────┐                           │
│  │ radar │                           │
│  │  map  │                           │
│  └───────┘                           │
│  ┌───────┐              ┌───────┐    │
│  │ move  │              │  aim  │    │
│  │  joy  │              │  joy  │    │
│  └───────┘              └───────┘    │
└──────────────────────────────────────┘
```

### Top-Right: Ammo Counter (keep existing)
- Shows current ammo / soft cap
- Overflow indicator when above soft cap

### Top-Center: Cleanup Progress Bar (keep existing)
- 0% → 100% progress bar with percentage text

### Bottom-Left Above Joystick: Radar Minimap
- Semi-transparent circular overlay (~15% screen width)
- 90° spherical range around player
- Player: fixed center, triangle pointing movement direction
- Trash clusters: green dots
- Opponent: red diamond
- Planet-fixed orientation (north stays north, map doesn't rotate)
- 40-50% alpha background

### World-Space: Opponent HP Bar
- Floating bar above AI ship mesh
- Only visible when opponent is on-screen / nearby
- Always faces camera (billboard)
- Replaces the old top-left `OpponentHealthPanel`

## Removed
- Top-left opponent health panel → replaced by world-space bar
- Separate SingleShot / BurstShot buttons → replaced by aim joystick

## Implementation Files

| File | Action |
|------|--------|
| `Scripts/Player/ShootingSystem.cs` | Refactor: accept aim Vector2, flick vs hold logic |
| `Scripts/Player/PlayerController.cs` | Update: wire new Aim input, remove old shot bindings |
| `Scripts/UI/GameplayHUD.cs` | Update: remove opponent panel, add radar reference |
| `Scripts/UI/RadarMinimap.cs` | **New:** radar UI component |
| `Scripts/UI/WorldSpaceHealthBar.cs` | **New:** billboard HP bar for opponent |
| `Input/SpaceCleaner_Actions.inputactions` | Update: replace shot actions with Aim |
| `Scenes/Gameplay/Gameplay.unity` | Update: rewire HUD, add radar, add world HP |
