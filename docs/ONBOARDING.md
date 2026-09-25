# Space Cleaner — Developer Onboarding (Gameplay Edition)

Welcome! This document explains **what the game is and how it plays**. Read it before you open any code. Technical setup is covered in `CLAUDE.md`; this is the "what are we actually building?" guide.

> **Source-of-truth note:** `docs/GDD.md` (v1.0, March 2026) is the original vision, but the game has evolved. Where this doc and the GDD disagree, this doc reflects the current code. Differences are called out in [GDD vs. reality](#8-gdd-vs-reality).

---

## 1. The One-Minute Pitch

Larry, a bratty space gremlin, has littered the galaxy. You are the **Space Cleaner**, a small ship that **vacuums up space trash** and then **fires that trash as ammunition** at rival bots and at Larry himself.

The core idea is that **trash is both your objective and your ammo**:

- Cleaning the planet is how you win.
- Trash is also how you fight.
- So you're always weighing "collect everything" against "keep enough ammo to survive."

**Platform:** mobile (Android first), dual-touch-joystick controls, no internet needed, cartoony 3D look.

---

## 2. The Core Loop

Each **planet** is one self-contained level:

```
Fly around the planet ─► vacuum trash (auto) ─► build combos
        │                        │
        │                        ▼
        │              trash becomes ammo
        │                        │
        ▼                        ▼
 Fight the AI rival  ◄──── shoot trash at it
        │
        ▼
 Planet ≥ 80% clean  AND  rival defeated  ──►  LEVEL COMPLETE
        │
        ▼
 Citizens celebrate, you get currency, move on
        │
        ▼
 After all planets in a solar system ──► BOSS FIGHT vs. Larry at the sun
```

**Both conditions are required to finish a planet.** Cleaning to 80% isn't enough, and neither is killing the rival alone. This is enforced in `GameManager.IsLevelComplete`.

---

## 3. What the Player Does

### 3.1 Movement (left joystick)
- The ship **flies around the planet's surface** at a fixed hover height. The camera follows in third person.
- Steering is planet-relative: think "skating around a ball," not free 3D flight.
- Planets are **radius 50**; the ship orbits at about 52.

### 3.2 Vacuum (automatic, no button)
- A glowing radius around the ship **auto-collects any trash** that enters it. You just fly near stuff.
- Collected trash flies into the ship and adds **+1 ammo** (no ammo cap).
- Trash also counts toward the planet's **cleanup %**.

### 3.3 Shooting (right joystick, Brawl-Stars-style aim)
The aim stick shows an **aiming cone** and supports two firing styles:

| Gesture | Result |
|---|---|
| **Quick flick** (release in under ~0.3s) | Single shot (short cooldown) |
| **Hold and aim** (past ~0.3s) | Auto-fire stream (~6 shots/sec) |

- Holding fires a **burst of 10 shots**, then a **3-second cooldown** before the next burst.
- Every shot costs 1 ammo. **No ammo = can't shoot** — go vacuum more.
- Every projectile deals **1 damage**.

### 3.4 Health
- **20 HP**; every hit does 1 damage (Larry's trash balls hit harder — see §5).
- **Death is soft:** no game over. See [Dying](#35-dying-and-respawning).

### 3.5 Dying and respawning
When your HP hits 0:
1. A brief freeze-frame, then your ship is **launched off the planet**, tumbling into space.
2. A dark "lobby" overlay shows your planet's cleanup %, plus a big **Retry** button.
3. On Retry you drop back in at the spot you died, with **full HP but 0 ammo** (the penalty) and **2 seconds of invincibility** (ship blinks).
4. **Planet progress and the opponent's state are preserved.**

Design intent: dying should sting a little (you lose ammo) but never make the player quit.

---

## 4. Trash: The Central Idea

- Trash is scattered over the planet in **clusters** (~200 pieces, ~20 clusters on the test planet), in a few cartoony flavors (plastic, junk, satellite bits, alien fast food).
- All trash is **mechanically identical**; variety is visual/comic.
- Trash comes in two kinds:
  - **Real trash**: counts toward cleanup %.
  - **Converted trash**: created from your own **missed shots** (a player projectile that expires without hitting anything turns back into a trash pickup). It gives ammo but **does not** count toward cleanup %.

**Why converted trash exists (anti-stalemate):** if you fire all your ammo and miss, the ammo isn't lost from the world; it becomes trash you can re-collect. The rival can also grab it, which creates a fun tug-of-war over stray shots.

### Combos
- Collect trash within **2 seconds** of the previous pickup to build a combo.
- The displayed multiplier escalates at combo counts **3 / 6 / 10 / 15 / 20 → x2 / x3 / x5 / x8 / x10**.
- Going 2 seconds without a pickup **resets** the combo.
- Your **best combo** in a level boosts the currency reward at the end.

### Buffs (power-ups)
Three floating pickups spawn at random spots around the planet, each on a ~30s respawn cooldown (staggered so they don't all appear at once). Each lasts **15 seconds** once grabbed:

| Buff | Effect |
|---|---|
| **Speed** | Ship moves 1.5x faster |
| **Vacuum Radius** | Collection radius 3x bigger |
| **Ammo Strength** | 2x damage and 2x ammo gained per pickup |

Buffs are a small "go get it!" detour that rewards map awareness (there's a **radar minimap** for this).

---

## 5. Enemies

### 5.1 The AI Rival (one per planet)
A rival cleaner shares your planet and competes with you for the trash.

- **It vacuums trash too**, so it denies you ammo and contributes to (or steals from) the cleanup %.
- **It shoots at you** when you're within its aggression range, slowly but steadily and with deliberately sloppy aim (lots of random spread) so it's threatening, not oppressive.
- **It has effectively unlimited ammo**, so it never goes quiet and gives you a "breather." Constant, predictable pressure was a deliberate design choice.
- **Killing it pays off:** it starts with a baseline stash of ammo, plus whatever it vacuumed. **All of it transfers to you** on death.
- It uses **hysteresis** on its aggression (enters/exits "hunt" mode at different distances) so it doesn't flicker between behaviors.

**Design intent:** the rival is a *rival cleaner first, combatant second*. Killing it early is rewarded; ignoring it means it hoards resources and pesters you.

### 5.2 Larry, the Boss (end of each solar system)
Fought at the **sun**. No environmental trash here, so ammo is the whole game.

- Larry is **stationary** and lobs **big trash balls** at you.
- A trash ball that **hits you** hurts (5 HP).
- A trash ball that **misses** lands on the sun and becomes **collectible ammo worth 10**. **Dodging is how you refuel**: the risk/reward heart of the fight.
- Larry **summons minions** and **taunts you** with speech bubbles ("You call that cleaning?!").
- He has a boss health bar at the top of the screen. Win by defeating **Larry and all minions**.

**Score carry-over (the cool part):** every AI rival you defeated during the planet run **returns as an armed minion** in Larry's arena, carrying the ammo they'd hoarded. Your earlier choices come back to haunt you. (Minions that die in the boss arena don't carry over further.)

> Boss numbers (HP, fire rate, minion count) are all **placeholders pending playtesting.** Larry's escape/tantrum animation is not built yet.

---

## 6. Progression and Rewards

- **Solar system** = several planets, then a Larry boss fight at the sun.
- Planet types escalate: **Rocky** (easy, tutorial-ish) → **Gas Giants** (more trash, bots get introduced) → **Ice Giants** (dense, aggressive) → exotic types (TBD).
- When a planet is completed, the **citizens celebrate** (confetti, "Thank You!") and you're paid **currency**:

  `reward = 100 base + (2 x cleanup %) + (10 x best combo)`

- Currency will be spent on **ship color customization** (Phase 1) and upgrades (Phase 2).
- Progress **saves locally** (offline only).

---

## 7. What Exists Today

| Area | Status |
|---|---|
| Core loop (fly, vacuum, shoot, one planet, AI rival) | ✅ Done |
| Death / respawn experience | ✅ Done |
| Combo system, buffs, radar, HUD polish, haptics, SFX | ✅ Done (game-feel pass) |
| Larry boss fight + carry-over minions + taunts | ✅ Mostly done (no flee animation, balance TBD) |
| Progression core (save/load, currency, citizen reward formula) | 🟡 Foundation in place; planet-to-planet flow and boss transition not wired |
| Main menu, music, achievements, ship customization, toon art | ❌ Not started |
| Tutorial and opening cinematic | ❌ Not started |
| Multiplayer, events, skins, upgrades | 🔮 Phase 2 (not now) |

Check `docs/tasks/overview.md` for the live dashboard.

---

## 8. GDD vs. Reality

The GDD is still good for tone and long-term vision, but these points changed:

| GDD v1.0 says | The game does now |
|---|---|
| One aim joystick, "drag to aim, release to fire" | Flick = single shot, hold = auto-fire burst (10 shots, 3s cooldown) |
| Free 3D flight | Spherical surface movement at fixed hover height |
| Bots appear from gas giants onward | One AI rival per planet from the start (1v1) |
| Finish a planet at 100% cleanup | **80% cleanup AND rival defeated** |
| Death penalty: none | Soft penalty: ammo reset to 0 |
| Larry HP 50 / attacks 1 HP | Placeholders in code (30 HP, trash balls 5 damage); to be tuned |
| Players' trash types have different damage | All identical (1 damage) |

When you find another mismatch, **code wins**: update the doc.

---

## 9. How to Get a Feel for the Game

Do this before reading any code:

1. Open `Assets/_Project/Scenes/Gameplay/Gameplay.unity` and press Play.
2. **Just vacuum.** Notice how satisfying the combo chain feels.
3. **Fire the aim stick** with flicks, then holds. Feel the burst → cooldown rhythm.
4. **Let the rival hit you.** Die once and watch the death sequence, then Retry.
5. **Miss shots on purpose** and watch them turn back into trash.
6. Chase a **buff** (use the radar).
7. Open `Assets/_Project/Scenes/BossFight/` and fight Larry. Try dodging his trash balls and vacuuming the misses.

### Golden rules of the design (keep these in mind when changing anything)
1. **Trash is both goal and weapon.** Any change that breaks that tension is suspect.
2. **Never stalemate the player.** There should always be a way to get more ammo.
3. **Never punish too hard.** Death is a setback, not a game over.
4. **Pressure should be steady, not spiky.** That's why the rival never runs out of ammo.
5. **It's a casual, cartoony, mobile game.** Readable at a glance, one thumb per stick.

---

## 10. Where to Read Next

| If you want to know... | Read |
|---|---|
| Full vision, story, art direction | `docs/GDD.md` |
| What's done and what's next | `docs/tasks/overview.md` |
| Why the rival behaves as it does | `docs/superpowers/specs/2026-03-25-opponent-balance-anti-stalemate-design.md` |
| How the boss fight works | `docs/superpowers/specs/2026-04-04-larry-boss-fight-design.md` |
| Death and respawn details | `docs/superpowers/specs/2026-03-20-player-death-experience-design.md` |
| Build, tools, project layout | `CLAUDE.md` and `docs/CLAUDE-structure.md` |
