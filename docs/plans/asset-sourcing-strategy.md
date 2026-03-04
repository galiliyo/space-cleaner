# Asset Sourcing Strategy

**Decision:** Use free/cheap placeholder assets for prototyping (M1–M3), replace with custom art during polish (M4–M5).

**Prefab structure rule:** Keep meshes as child objects in prefabs so model swaps don't break references.

---

## Phase 1: Prototype (M1–M3) — Free Placeholder Assets

### 3D Models

| Need | Source | License | Notes |
|------|--------|---------|-------|
| Player & enemy ships | [Kenney Space Kit](https://kenney.nl/assets/space-kit) (150 assets) | CC0 | Low-poly ships, turrets, characters. OBJ/FBX/GLB. |
| More ship variety | [OpenGameArt LowPoly Spaceships](https://opengameart.org/content/lowpoly-spaceships-pack) | CC-BY | 10 ships × 5 color variants. FBX/OBJ/glTF. |
| Planets, moons, asteroids | [OpenGameArt Low Poly Space Pack](https://opengameart.org/content/low-poly-space-pack) | CC0 | 9 planets, 3 moons, 3 asteroids, 2 rings. |
| Trash/debris | Unity primitives + Kenney Space Kit rocks | CC0 | Color-coded cubes/spheres work fine for prototype. |
| Modular ship parts | [OpenGameArt LowPoly Ship Components](https://opengameart.org/content/3d-lowpoly-spaceships-and-components) | CC0 | Modular parts for Larry's junky ship. |

### Skybox & Environment

| Need | Source | License | Notes |
|------|--------|---------|-------|
| Space skybox | [FREE Skybox Extended Shader](https://assetstore.unity.com/packages/vfx/shaders/free-skybox-extended-shader-107400) | Asset Store EULA | Procedural stars + nebula. URP compatible. |
| Planet textures | [Solar System Scope](https://www.solarsystemscope.com/textures/) | CC BY 4.0 | 2K/4K planet maps. Apply with toon shader. |
| Backup skyboxes | [Polyverse Skies](https://assetstore.unity.com/packages/vfx/shaders/polyverse-skies-low-poly-skybox-shaders-104017) | Asset Store EULA | Low-poly faceted style matches art direction. |

### Sound Effects

| Need | Source | License | Notes |
|------|--------|---------|-------|
| Lasers, weapons | [Kenney Sci-Fi Sounds](https://kenney.nl/assets/sci-fi-sounds) (70 SFX) | CC0 | Weapon shots, energy blasts, beeps. |
| Explosions, impacts | [Kenney Impact Sounds](https://kenney.nl/assets/impact-sounds) (130 SFX) | CC0 | Hits, collisions, crashes. |
| UI sounds | [Kenney Digital Audio](https://kenney.nl/assets/digital-audio) (60 SFX) | CC0 | Blips, confirmations, alerts. |
| Extra laser/phaser FX | [OpenGameArt 63 Digital SFX](https://opengameart.org/content/63-digital-sound-effects-lasers-phasers-space-etc) | CC0 | 63 synthesized space sounds. WAV. |
| Vacuum/suction sounds | [Mixkit Space Shooter SFX](https://mixkit.co/free-sound-effects/space-shooter/) | Mixkit Free License | 17+ effects, no attribution needed. |

### UI

| Need | Source | License | Notes |
|------|--------|---------|-------|
| UI elements | [Kenney UI Pack (Space)](https://kenney.nl/assets/ui-pack-space-expansion) | CC0 | Buttons, panels, icons in space theme. |
| 2D ship sprites (UI/HUD) | [Kenney Space Shooter Redux](https://kenney.nl/assets/space-shooter-redux) (295 sprites) | CC0 | Ship icons, bullet sprites, HUD numbers, fonts. |

---

## Phase 2: Polish (M4–M5) — Custom / Purchased Assets

### Approach Options (decide during M3)

1. **AI-generated models** — Use Meshy/Tripo3D for base meshes, clean up in Blender. Good for unique stylized look.
2. **Commissioned artist** — Hire for key hero assets (player ship, Larry, boss ships). Fiverr/ArtStation.
3. **Premium asset packs** — Unity Asset Store paid packs for bulk content (planet packs, VFX).
4. **Hand-modeled in Blender** — Full control, most time-intensive.

### Priority Assets for Custom Art

1. Player ship (hero asset, seen constantly)
2. Larry + Larry's ship (main antagonist)
3. Trash types (5 distinct visual categories)
4. Planet types (define each world's identity)
5. Toon shader + outline shader (URP custom)
6. VFX (vacuum beam, combo celebrations, explosions)

---

## License Tracking

| License | Requirements | Assets Using |
|---------|-------------|--------------|
| CC0 | None | Kenney (all), OpenGameArt (selected) |
| CC-BY | Credit in game/docs | OpenGameArt spaceships, Solar System Scope |
| CC BY 4.0 | Credit in game/docs | Solar System Scope textures |
| Mixkit Free | None | Mixkit SFX |
| Unity Asset Store EULA | No redistribution of raw assets | Skybox shader, any Asset Store packs |

**Action:** Maintain a `CREDITS.md` file at project root for attribution requirements.

---

## Download Priority (Do First)

1. Kenney Space Kit — ships and structures for immediate prototyping
2. Kenney Sci-Fi Sounds + Impact Sounds — audio for gameplay feel
3. OpenGameArt Low Poly Space Pack — planets and asteroids
4. FREE Skybox Extended Shader — space environment
5. Kenney Digital Audio — UI feedback sounds
