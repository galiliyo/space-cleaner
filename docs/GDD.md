# SPACE CLEANER — Game Design Document

**Version:** 1.0
**Date:** March 2026
**Platform:** Android & iOS
**Engine:** Unity (C#) with Universal Render Pipeline
**Genre:** 3D Action / Arcade / Space Cleanup

---

## 1. Game Overview

### 1.1 Concept Statement

Space Cleaner is a 3D cartoony action-arcade game where the player pilots a customizable spaceship through solar systems, vacuuming up space trash to clean the galaxy. The galaxy has been littered by Larry, a mischievous villain whose sole purpose is to make every solar system dirty. The player must clean planets, fight Larry in boss battles at each sun, and progress through increasingly challenging solar systems to restore the galaxy to its former glory.

### 1.2 Genre and Platform

| Attribute       | Detail                                                           |
| --------------- | ---------------------------------------------------------------- |
| Genre           | 3D Action / Arcade / Space Cleanup                               |
| Platform        | Android and iOS (mobile)                                         |
| Engine          | Unity (C#) with Universal Render Pipeline (URP)                  |
| Art Style       | Cartoony / stylized 3D (similar to Solar Smash but more playful) |
| Connectivity    | No internet required for single-player (Phase 1)                 |
| Target Audience | Casual to mid-core gamers, ages 10+                              |
| Monetization    | TBD (Phase 2 — no real-money purchases in Phase 1)               |

### 1.3 Elevator Pitch

Larry has trashed the galaxy. You are the Space Cleaner — pilot your ship, vacuum the debris, blast trash at enemies, defeat Larry in epic boss fights at every sun, and restore peace to the cosmos. Clean planets, earn rewards from grateful citizens, customize your ship, and progress from rocky worlds to gas giants to ice worlds and beyond. No internet needed. Pure galactic cleanup action.

### 1.4 Unique Selling Points

- Dual mechanic: vacuum trash for cleanup AND use it as ammunition against enemies and bosses
- Risk-reward boss fights: Larry's attacks become your ammo if you can dodge and collect
- Fully offline single-player campaign with a charming villain and story progression
- Cartoony 3D art style that is approachable and visually distinctive
- Galactic progression through scientifically-inspired planet types
- Grateful planet citizens celebrate when you clean their world

---

## 2. Core Gameplay Mechanics

### 2.1 Player Controls

Dual-control scheme optimized for mobile touch screens:

- **Movement Control (left side):** Virtual joystick for 3D flight. The ship moves freely in all directions around planetary bodies.
- **Aim/Shoot Control (right side):** Second joystick for shooting collected trash at enemies. Drag to aim, release to fire.

The vacuum mechanic is passive and radius-based. Any trash within the ship's collection radius is automatically sucked in. No manual vacuum button — fly near trash to collect it.

### 2.2 Trash System

Trash serves a dual purpose: it is both the objective to clean and the ammunition to fight. This creates tension between cleaning efficiently and stockpiling ammo.

| Trash Type        | Visual                          | Damage | Found In                        |
| ----------------- | ------------------------------- | ------ | ------------------------------- |
| Plastic Debris    | Colorful floating bags, bottles | 1 HP   | All planet types                |
| Broken Satellites | Metallic chunks, solar panels   | 1 HP   | Rocky planets, orbits           |
| Space Junk        | Bolts, panels, wires            | 1 HP   | All planet types                |
| Comet Debris      | Icy rocks, dust trails          | 1 HP   | Ice giants, outer orbits        |
| Alien Fast Food   | Cartoon wrappers, cups          | 1 HP   | All planet types (comic relief) |

All trash types deal identical damage (1 HP per piece). Shots are drawn randomly from the collected pool.

### 2.3 Collection and Ammo

- **Collection Radius:** Glowing circular field surrounds the ship. Trash entering the radius is auto-collected with vacuum sound and particle animation.
- **Ammo Pool:** Collected trash becomes ammo. No capacity limit.
- **Zero Ammo State:** Cannot shoot with zero trash. Must collect more first.
- **Ammo Counter:** On-screen HUD element shows current trash/ammo count.

### 2.4 Combo System

Collecting trash quickly builds a combo multiplier:

- Collecting within 2 seconds of previous pickup increases the combo counter
- Higher combos award bonus in-game currency from planet citizens
- Combo multiplier displayed on-screen with escalating visual effects (x2, x5, x10, etc.)
- Going 2+ seconds without collecting resets the combo

### 2.5 Health System

| Attribute              | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Player Max HP          | 20 HP                                                 |
| Damage per Trash Hit   | 1 HP                                                  |
| Health Packs           | Awarded after completing a planet or group of planets |
| Death Penalty          | None — respawn at boss fight (no game over)           |
| Defeated Player Reward | Earn all of the defeated enemy's trash points         |

---

## 3. Progression System

### 3.1 Solar System Structure

Linear progression through solar systems. Each contains multiple planets of increasing complexity, culminating in a boss fight against Larry at the sun.

| Phase | Planet Type          | Difficulty                        | Enemies                  |
| ----- | -------------------- | --------------------------------- | ------------------------ |
| 1     | Rocky Planets        | Easy — intro mechanics            | None (tutorial zone)     |
| 2     | Gas Giants           | Medium — more trash, larger areas | Alien bots introduced    |
| 3     | Ice Giants           | Hard — dense trash fields         | More aggressive bots     |
| 4+    | Exotic types (TBD)   | Escalating                        | Advanced bot patterns    |
| Final | The Sun (Boss Arena) | Boss fight                        | Larry + summoned minions |

After defeating Larry at the sun, a short animation shows Larry escaping to the next solar system. Player earns an achievement and follows.

### 3.2 Planet Cleaning

Each planet has a cleanup percentage bar (0% → 100%). Tracks ratio of trash collected vs total. At 100%, citizen celebration triggers and player receives currency reward, then moves to next planet.

### 3.3 Citizen Rewards

At 100% cleanup, camera shifts to show citizens on the planet surface. Unique cartoony alien designs per planet type throw a celebration with confetti, cheering, and "Thank You!" message. Currency earned based on planet size and combo score.

### 3.4 In-Game Currency

Primary economy in Phase 1. Earned from citizen rewards. Spent on ship color customization. Phase 2 expands to speed upgrades, vacuum radius upgrades, health capacity upgrades, and cosmetics.

---

## 4. Enemies and Combat

### 4.1 Alien Bots

Larry's minions. Appear from gas giant phase onward, becoming more numerous and aggressive. Purpose: eliminate the player. Defeating a bot earns all of its accumulated trash points as ammo. Bots patrol trash fields, pursue player when in range, shoot trash projectiles (1 HP damage per hit). Density and aggression increase per solar system.

### 4.2 Larry — The Main Antagonist

Central villain. Bratty, trash-obsessed character whose motivation is making the galaxy dirty. Appears in opening cinematic littering the first solar system. Boss at end of every solar system.

### 4.3 Boss Fight Mechanics

Takes place near the sun. No environmental trash — must use stockpiled ammo OR collect Larry's attacks:

- Larry shoots large trash balls. If dodged and collected, gives player 10 ammo each.
- Larry has visible health bar scaling up each solar system.
- Larry summons alien bot minions during fight.
- Player respawns immediately if defeated — no game over or progression loss.
- Defeating Larry triggers escape animation: jumps into ship, flies to next solar system.
- After escape animation, player earns a new achievement.

### 4.4 Larry's Personality

Taunts throughout boss fight via speech bubbles. Examples:

- "You call that cleaning?!"
- "My minions will take out the trash — and by trash, I mean YOU!"
- "This galaxy was boring when it was clean!"

Throws tantrum when defeated before fleeing.

### 4.5 Larry Scaling

| Solar System | Larry HP      | Attack Speed | Minions  |
| ------------ | ------------- | ------------ | -------- |
| 1            | 50 HP (TBD)   | Slow         | 0-2      |
| 2            | 80 HP (TBD)   | Medium       | 2-4      |
| 3            | 120 HP (TBD)  | Fast         | 4-6      |
| 4+           | Scaling (TBD) | Very fast    | 6+ (TBD) |

All numeric values TBD — finalized during playtesting.

---

## 5. Narrative and Story

### 5.1 Opening Cinematic

Animated cutscene: camera sweeps across clean solar system → Larry's ship enters dumping trash → citizens look up in dismay → player's ship powers up → mission briefing: "The galaxy needs you. Clean it up. Stop Larry." → transitions into playable tutorial.

### 5.2 Tutorial Flow

Opening cinematic transitions directly into gameplay on first rocky planet. Teaches:

1. Fly using movement control
2. Approach trash to auto-collect via vacuum radius
3. Check cleanup percentage bar
4. Fire collected trash using aim/shoot control (at practice target)
5. Complete first planet to trigger citizen celebration

Tutorial is integrated into first few planets, not a separate mode.

### 5.3 Ongoing Story

Each Larry defeat shows escape animation with muttering about next plan. Taunts become more desperate and funny over time. Later planet citizens reference Larry by name — word of the Space Cleaner's heroics spreads.

---

## 6. User Interface and HUD

### 6.1 In-Game HUD

- **Health Bar (top-left):** Current HP / 20, heart icon, green→red gradient
- **Ammo Counter (top-right):** Current trash/ammo count, trash bag icon
- **Cleanup Progress Bar (top-center):** Planet cleanup 0%→100%
- **Combo Multiplier (center):** Appears when combos active, escalating visual flair
- **Currency Display (below ammo):** Current money, coin icon
- **Movement Joystick (bottom-left):** Semi-transparent virtual joystick
- **Aim/Shoot Joystick (bottom-right):** Semi-transparent joystick
- **Boss Health Bar (top-center, boss fights only):** Larry's HP with name and portrait

### 6.2 Menus

- **Main Menu:** Play, Ship Customization, Achievements, Settings
- **Ship Customization:** Color picker and material previews (Phase 1). Upgrades placeholder (Phase 2).
- **Achievements Gallery:** Grid of earned/locked achievements with progress indicators
- **Settings:** Sound, music, controls sensitivity, graphics quality
- **Pause Menu:** Resume, restart planet, settings, quit to main menu

---

## 7. Ship Customization

### 7.1 Phase 1: Colors

Player can change primary, secondary, and accent colors using a color picker. Some colors default, premium colors unlocked with in-game currency. Fixed base ship model for all players.

### 7.2 Phase 2: Full Customization (Future)

Ship shape/body modifications, decals and patterns, particle trail effects, vacuum visual effects, materials from chests, performance buffs (speed, vacuum radius, health capacity).

---

## 8. Achievements System

### 8.1 Achievement Types

- **Boss Defeat:** Earned after defeating Larry per solar system. Unique short animation each.
- **Cleaning Milestones:** For cleaning specific numbers of objects (10, 50, 100 planets).
- **Combo Achievements:** For reaching specific combo thresholds.
- **Collection Achievements:** For total trash collected.
- **Currency Achievements:** For accumulating wealth.

### 8.2 Achievement Display

Equippable and visible on ship/profile. Unlock accompanied by celebratory animation per tier. Gallery shows all earned/locked with progress bars.

---

## 9. Art Direction

### 9.1 Visual Style

Cartoony, stylized 3D. Solar Smash's scale but with bright colors, exaggerated proportions, playful designs. Planets with visible surface details and cartoon citizens. Colorful varied trash. Vibrant nebula space backgrounds. Larry: bratty space gremlin in a junky ship.

### 9.2 Rendering

URP with custom toon/cel-shading post-processing. Key elements:

- Soft outline shader on ships and trash
- Bright saturated color palette
- Particle effects for vacuum, combos, celebrations
- Dynamic lighting from sun during boss fights

### 9.3 Audio Direction

- Catchy upbeat main theme with space synth
- Satisfying vacuum whooshing crescendo
- Cartoon bonk sounds for trash impacts
- Ascending musical tones for combo builds
- Voice-acted bratty nasal delivery for Larry's taunts
- Cheering and party horns for citizen celebrations

---

## 10. Technical Architecture

### 10.1 Technology Stack

| Component         | Technology                                          |
| ----------------- | --------------------------------------------------- |
| Game Engine       | Unity 6 LTS (6000.3.10f1)                           |
| Language          | C#                                                  |
| Render Pipeline   | Universal Render Pipeline (URP)                     |
| Physics           | Unity Physics (trigger colliders for vacuum radius) |
| Target FPS        | 60 FPS on mid-range devices, 30 FPS minimum         |
| Target Platforms  | Android (API 25+), iOS (15+)                        |
| Build Size Target | Under 200 MB (Phase 1)                              |
| Save System       | Local save (PlayerPrefs or JSON serialization)      |
| Version Control   | Git with Git LFS for assets                         |

### 10.2 Key Systems

- **Trash Spawning System:** Procedural placement around planets by type/difficulty. Object pooling.
- **Vacuum Collection System:** Sphere trigger collider on ship. On trigger enter, trash lerps toward ship with animation, adds to ammo pool.
- **Projectile System:** Trash fired in aimed direction with travel time. Collision for damage.
- **AI System:** Bot pathfinding (NavMesh or steering behaviors). Patrol, pursue, attack.
- **Boss AI:** Larry's attack patterns, minion summoning, phase transitions by HP thresholds.
- **Progression Manager:** Tracks solar system, planet, cleanup progress, unlocks. Save/load.
- **UI Manager:** HUD updates, menu navigation, achievement notifications.
- **Camera System:** Third-person follow with smooth transitions for cutscenes/celebrations.

### 10.3 Performance Considerations

- Object pooling for trash and projectiles (avoid GC spikes)
- LOD on planets and large objects by camera distance
- Occlusion culling for off-screen objects
- Texture atlasing to reduce draw calls
- Particle system budgeting (max particle counts)
- Audio compression and streaming for music

---

## 11. Phase 2 Roadmap (Future)

Not included in initial release. Documented for planning.

### 11.1 Multiplayer

- Online multiplayer with friend search by in-game name
- Different game modes
- Defeating a player earns their trash points
- Player health: 20 HP, 1 damage per hit

### 11.2 Events

- Time-limited events (e.g., Red vs Blue team cleanup race)
- Unique rewards and leaderboards

### 11.3 Expanded Customization

- Full ship design editor: shape, decals, patterns, particle trails
- Materials/parts from chests
- Performance buffs: speed, vacuum radius, max health

### 11.4 Skins System

- Complete skin system with materials from gameplay chests
- Body shapes, wing configs, engine styles, cockpit designs

### 11.5 Expanded Economy

- Ship upgrades, chest purchases with earned currency
- Potential battle pass or seasonal rewards
- Monetization strategy TBD based on Phase 1 data

---

## 12. Development Plan

### 12.1 Phase 1 Milestones

| Milestone       | Deliverable                 | Key Tasks                                                             |
| --------------- | --------------------------- | --------------------------------------------------------------------- |
| M1: Prototype   | Core gameplay loop playable | Ship flight, vacuum mechanic, trash spawning, shooting, 1 test planet |
| M2: Combat      | Enemy and boss systems      | Bot AI, Larry boss fight, health system, ammo system                  |
| M3: Progression | Full solar system playable  | Multiple planet types, progression system, save/load, citizen rewards |
| M4: Polish      | Art, audio, UI complete     | Toon shading, VFX, SFX, music, HUD, menus, achievements               |
| M5: Content     | 3-5 solar systems           | Level design, difficulty balancing, Larry scaling, playtesting        |
| M6: Ship        | Color customization         | Color picker UI, currency spending, shop system                       |
| M7: Release     | App store launch            | Performance optimization, bug fixes, store listings, device testing   |
