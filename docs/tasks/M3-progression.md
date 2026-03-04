# M3: Progression

**Goal:** Full solar system playable — multiple planet types, progression system, save/load, citizen rewards, combo system.

**Status:** Not Started

---

## Solar System Structure

- [ ] Create SolarSystem data structure (variable planet count + sun boss)
- [ ] Implement planet sequence (content-driven, unique per solar system)
- [ ] Automatic transition between planets (cutscene/animation of ship flying)
- [ ] Transition to sun boss arena after final planet
- [ ] Post-boss transition: Lary escape animation → next solar system

> GDD Ref: §3.1 Solar System Structure, §3.4 Planet Transitions

## Planet Types

- [ ] Rocky planet variant (easy, weak AI opponent)
- [ ] Gas giant variant (medium, larger area, smarter AI)
- [ ] Ice giant variant (hard, dense trash fields, aggressive AI)
- [ ] Visual differentiation per planet type (materials, scale, skybox tint)
- [ ] Planet-specific trash distribution parameters
- [ ] Unique AI opponent design per planet

> GDD Ref: §3.1 Solar System Structure

## Citizen Rewards

- [ ] Camera shift to planet surface on level completion
- [ ] Citizen character designs per planet type (placeholder)
- [ ] Celebration animation (confetti, cheering, "Thank You!")
- [ ] Currency calculation based on planet size and performance
- [ ] Currency award and display

> GDD Ref: §3.6 Citizen Rewards

## In-Game Currency

- [ ] Currency tracking (persistent across sessions)
- [ ] Currency display on HUD (coin icon)
- [ ] Currency earned from citizen rewards
- [ ] Currency storage in save system

> GDD Ref: §3.7 In-Game Currency

## Combo System

- [ ] Implement combo counter (2-second window between pickups)
- [ ] Combo multiplier display on HUD (center)
- [ ] Escalating visual effects (x2, x5, x10, etc.)
- [ ] Combo reset after 2 seconds of no collection
- [ ] Combo bonus applied to citizen currency rewards

> GDD Ref: §2.5 Combo System

## Save/Load System

- [ ] Define save data structure (solar system, planet, currency, achievements, opponent carry-over)
- [ ] Implement save (JSON serialization or PlayerPrefs)
- [ ] Implement load on game start
- [ ] Auto-save on planet completion and boss defeat

> GDD Ref: §10.1 Technology Stack

## Progression Manager

- [ ] Track current solar system and planet index
- [ ] Track per-planet cleanup progress (combined player + opponent)
- [ ] Track opponent defeated state and carry-over data
- [ ] Handle progression unlocks
- [ ] Manage game state transitions (planet → planet → boss → next system)

> GDD Ref: §10.2 Key Systems
