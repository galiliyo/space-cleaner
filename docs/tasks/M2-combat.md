# M2: Combat

**Goal:** Advanced AI opponent behavior, Lary boss fight, health system polish, ammo system, score carry-over.

**Status:** In Progress (~30%)

---

## Health System

- [x] Implement player health (20 HP max) — Health.cs
- [x] Take damage on trash projectile hit (1 HP per hit) — Projectile.cs, damage=1
- [x] Take damage on bot projectile hit (1 HP per hit) — all projectiles unified, no separate metal ball type
- [x] Health bar HUD element (top-left, green→red gradient) — GameplayHUD.cs
- [x] Death handling: lose all ammo, keep cleanup %, respawn — PlayerDeathHandler.cs
- [x] Damage visual feedback — post-death invincibility blink (no per-hit flash)

> GDD Ref: §2.6 Health System

## Advanced AI Opponent

- [ ] Improve AI vacuum pathing (efficient trash collection routes)
- [ ] Improve AI combat behavior (aim prediction, dodging)
- [ ] AI difficulty scaling per solar system (speed, accuracy, aggression)
- [ ] Unique AI opponent visuals per planet (distinct ship designs)
- [ ] AI opponent behavior variants (aggressive, defensive, balanced)

> GDD Ref: §4.1 AI Opponents

## Score Carry-Over System

- [x] Track each defeated opponent's vacuum count — AIOpponent.collectedAmmo, transferred to player on death
- [ ] Store defeated opponent data per solar system — needed for Lary minion spawning
- [ ] Spawn defeated opponents as Lary's minions at sun level
- [ ] Minion bots start with stored ammo from their previous defeat
- [ ] Visual indicator showing carry-over minions vs base minions

> GDD Ref: §3.3 Score Carry-Over System

## Lary Boss Fight (Sun Level)

- [ ] Create Lary prefab with placeholder model
- [ ] Create sun boss arena with trash (cleanup still required)
- [ ] Implement Lary attack patterns
- [ ] Lary visible health bar (top-center with name/portrait)
- [ ] Lary minion management: base minions + defeated opponent minions
- [ ] Minion bots shoot projectiles (same type as player/opponent, 1 HP damage)
- [ ] Lary flee behavior at low HP (escape to next solar system)
- [ ] Defeat animation: Lary tantrum → escape animation
- [ ] Achievement trigger on Lary defeat

> GDD Ref: §4.3 Boss Fight Mechanics, §4.5 Lary Scaling

## Lary Personality

- [ ] Speech bubble system for taunts
- [ ] 5-10 taunt lines per solar system
- [ ] Tantrum animation on defeat
- [ ] Taunt trigger logic (periodic, HP threshold, etc.)

> GDD Ref: §4.4 Lary's Personality
