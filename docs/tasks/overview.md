# Milestone Overview

| Milestone | Status | Progress |
|---|---|---|
| [M1: Prototype](M1-prototype.md) | Complete ✓ | 100% |
| [M2: Combat](M2-combat.md) | In Progress | ~70% |
| [M3: Progression](M3-progression.md) | Not Started | 0% |
| [M4: Polish + Ship Customization](M4-polish.md) | In Progress | ~35% |
| [M5: Content](M5-content.md) | Not Started | 0% |
| [M7: Release](M7-release.md) | Not Started | 0% |

## Current Focus

**M3: Progression** — the empty core-loop piece. No currency, save/load, or planet-to-planet progression yet (`Assets/_Project/Scripts/Progression/` exists but is empty). M2 boss/buff/combat and M4 game-feel polish are largely done in code (see below).

## Implemented beyond tracker (verified 2026-08-08)

- **M2 Combat (~70%):** Larry boss fight (`Boss/LarryBoss.cs`, `BossFightManager`, `LarryTrashBall`), score carry-over (`Boss/CarryOverData.cs`), minion management, speech-bubble taunts. Still missing: advanced AI pathing/dodging/difficulty scaling, Larry flee + defeat tantrum.
- **Buff/powerup system:** `Core/BuffManager.cs`, `BuffPickup`, `BuffReceiver`, `BuffHUD`, `BuffIcons`.
- **Combo system (GDD §2.4):** `Core/ComboManager.cs` + `UI/ComboUI.cs`.
- **M4 game-feel polish (~35%):** full Gauntlet pass (11 pieces), haptics, fresnel planet atmosphere, SFX pitch variation, HUD micro-animations. Still missing: menus, music, ship color customization, achievements, toon art.

## Changes from v1.0

- M6 (Ship Customization) merged into M4 (Polish)
- Combo system deferred from M1 to M3/M4
- Shooting redesigned: two buttons (single + burst) with Brawl Stars-style aiming cones
- Movement redesigned: spherical surface at fixed elevation around planets
- AI opponent added to core loop (1v1 per planet)
- Level completion: 100% cleanup + kill opponent (both required)
- Score carry-over: defeated opponents become Lary's armed minions at sun boss fight
