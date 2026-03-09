# GFX Assets Design — Low-Poly Visual Assets

**Date:** 2026-03-09
**Status:** Approved

## Goal

Replace placeholder geometry with low-poly stylized 3D models from the Kenney Space Kit, establishing the game's visual identity.

## Art Direction

- **Style:** Low-poly stylized (Astroneer / Poly Bridge aesthetic)
- **Shading:** Flat-color URP Lit materials (toon shading deferred to later milestone)
- **Vertex budget:** ~500 verts per trash piece, ~2000 verts per ship
- **Consistent palette:** Bright, saturated colors per GDD section 9.1

## Asset Source

**Primary:** Kenney Space Kit (CC0, free) — 28 FBX models imported to `Assets/_Project/Models/`

**Future (not this pass):**
- AI-generated unique Larry boss ship (Meshy.ai / Tripo3D)
- Hand-modeled cartoon trash (plastic bags, alien fast food)

## Model Assignments

### Ships (`Assets/_Project/Models/Ships/`)

| Role | FBX File | Rationale |
|------|----------|-----------|
| Player Ship | `craft_speederA.fbx` | Sleek, heroic silhouette |
| Enemy Bot | `craft_cargoA.fbx` | Bulky, industrial — visually distinct from player |
| Larry (placeholder) | `craft_miner.fbx` | Chunky, "cobbled together" look |

Unused ships kept for potential future use: speederB/C/D, racer, cargoB.

### Trash (`Assets/_Project/Models/Trash/`)

| GDD Trash Type | FBX File(s) |
|----------------|-------------|
| Broken Satellites | `satelliteDish.fbx`, `satelliteDish_detailed.fbx` |
| Space Junk | `barrel.fbx`, `barrels.fbx`, `barrels_rail.fbx`, `machine_barrel.fbx`, `machine_barrelLarge.fbx` |
| Comet Debris | `meteor.fbx`, `meteor_detailed.fbx`, `meteor_half.fbx` |
| Rocky Debris | `rock_largeA/B.fbx`, `rocks_smallA/B.fbx`, `rock_crystals*.fbx` |
| Plastic Debris | Not in pack — use barrel variants as placeholder |
| Alien Fast Food | Not in pack — future AI-gen or hand-model |

### Characters (`Assets/_Project/Models/Characters/`)

| Role | FBX File |
|------|----------|
| Planet Citizens | `alien.fbx` |
| Astronaut variants | `astronautA.fbx`, `astronautB.fbx` |

### Environment

- **Planets:** Keep procedural spheres with improved materials
- **Sun/Star:** Shader-based (emissive sphere + bloom, future pass)
- **Asteroids:** `rock_largeA/B.fbx` reused as background decoration

## Implementation Steps

1. Create colored URP Lit materials for each model category (player=blue, enemy=red, trash=gray variants)
2. Update existing prefabs to use new meshes instead of primitive geometry
3. Wire mesh colliders or simplified box/sphere colliders for gameplay
4. Update trash spawner to randomly select from trash model variants
5. Test scale — models may need uniform scaling to match world proportions
6. Clean up editor showcase script (`ShipShowcase.cs`) after done

## Not In Scope (Later Milestones)

- Toon/cel-shading shader
- Outline rendering
- Post-processing (bloom, vignette)
- Particle effects overhaul
- Unique Larry boss model
- Cartoon trash types (bags, fast food)
