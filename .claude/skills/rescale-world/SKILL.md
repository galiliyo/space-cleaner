---
name: rescale-world
description: Use when changing planet size, world scale, orbit distances, or any spatial proportions in Space Cleaner. Triggers on "resize", "rescale", "make planet bigger/smaller", "change scale", "adjust proportions".
---

# Rescale World

## Overview

Rescales all distance-dependent values in Space Cleaner proportionally when the planet size changes. One missed value breaks the visual balance.

## When to Use

- User wants to change planet size
- Ship/trash/camera proportions look wrong after a scale change
- Orbit heights, speeds, or ranges need proportional adjustment

## Process

1. **Calculate ratio:** `new_planet_scale / current_planet_scale`
2. **Multiply ALL values below by ratio**
3. **Recalculate FirePoint counter-scale:** `(1/shipScaleX, 1/shipScaleY, 1/shipScaleZ)`
4. **Check trash prefab scales** — adjust if too small/large relative to new ship size

## All Scale-Dependent Values

Edit these in `Assets/_Project/Scenes/Gameplay/Gameplay.unity` (YAML). Read each value from the scene/prefab YAML before multiplying — don't rely on a remembered number, it drifts the moment anyone edits the scene.

| Property | Component / Location |
|---|---|
| Planet scale | Planet Transform `m_LocalScale` |
| PlayerShip scale | PlayerShip Transform `m_LocalScale` |
| PlayerShip position | PlayerShip Transform `m_LocalPosition` (y) |
| AIShip scale | AIShip Transform `m_LocalScale` |
| AIShip position | AIShip Transform `m_LocalPosition` (y) |
| Player orbitRadius | `SphericalMovement` |
| Player moveSpeed | `SphericalMovement` |
| Player collectRadius | `VacuumCollector` |
| Player projectileSpeed | `ShootingSystem` |
| AI orbitRadius | `AIOpponent` |
| AI moveSpeed | `AIOpponent` |
| AI vacuumRadius | `AIOpponent` |
| AI shootRange | `AIOpponent` |
| AI aggressionRange | `AIOpponent` |
| AI projectileSpeed | `AIOpponent` |
| AI trigger collider | `SphereCollider` (isTrigger) on AIShip, radius |
| Camera distance | `SphericalCamera` |
| Camera elevation | `SphericalCamera` (usually keep as-is) |
| Camera far clip | `Camera` component `far clip plane` |
| LevelSetup planetRadius | `LevelSetup` |
| LevelSetup hoverHeight | `LevelSetup` |
| TrashSpawner planetRadius | `TrashSpawner` |
| TrashSpawner spawnHeight | `TrashSpawner` |
| FirePoint counter-scale | Both FirePoint Transforms |
| Trash prefabs | `Trash_A/B/C.prefab` `m_LocalScale` |

## Key Relationships

- **Planet radius** = planet scale / 2
- **Orbit radius** = planet radius + hover height above surface
- **Spawn height** should equal orbit height (trash at same altitude as ship)
- **VacuumCollector** creates its own SphereCollider at runtime — no scene collider needed for player
- **MCP changes in play mode are lost** — edit YAML directly or use MCP outside play mode

## Design Guidelines

- Ship: ~1/250 of planet radius (visible but planet feels massive)
- Orbit height: 5-10% of planet radius (see surface curvature)
- Trash: 25-50% of ship size (visible floating debris)
- Camera distance: 3-5x ship length behind
- Planet covers ~70% of screen at these proportions

## Common Mistakes

- Forgetting TrashSpawner spawnHeight (trash spawns at wrong altitude = invisible)
- Not updating FirePoint counter-scale after changing ship scale
- Editing via MCP during play mode (changes are discarded on stop)
- Missing AI values (orbitRadius, moveSpeed, ranges all need scaling)
