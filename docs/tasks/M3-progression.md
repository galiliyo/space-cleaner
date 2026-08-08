# M3: Progression

**Goal:** Full solar system playable — multiple planet types, progression system, save/load, citizen rewards, combo system.

**Status:** In Progress (core foundation)

---

## Combo System

- [x] Implement combo counter (2-second window between pickups) — ComboManager.cs
- [x] Combo multiplier display on HUD (center) — ComboUI.cs
- [x] Escalating visual effects (x2, x5, x10, etc.) — ComboUI.cs tier color/font escalation
- [x] Combo reset after 2 seconds of no collection — ComboManager.cs
- [x] Combo bonus applied to citizen currency rewards — ProgressionManager.CalculateCitizenReward

> GDD Ref: §2.5 Combo System

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
- [x] Currency calculation based on planet size and performance — ProgressionManager.CalculateCitizenReward (cleanup% + combo)
- [ ] Currency award and display

> GDD Ref: §3.6 Citizen Rewards

## In-Game Currency

- [x] Currency tracking (persistent across sessions) — CurrencyManager.cs + SaveData
- [ ] Currency display on HUD (coin icon)
- [x] Currency earned from citizen rewards — ProgressionManager.HandleLevelComplete
- [x] Currency storage in save system — SaveSystem (JSON)

> GDD Ref: §3.7 In-Game Currency

## Save/Load System

- [x] Define save data structure (solar system, planet, currency, achievements, opponent carry-over) — SaveData.cs
- [x] Implement save (JSON serialization or PlayerPrefs) — SaveSystem.cs (JSON, atomic write)
- [x] Implement load on game start — ProgressionManager.Awake
- [x] Auto-save on planet completion and boss defeat — ProgressionManager (HandleLevelComplete, RecordBossDefeated)

> GDD Ref: §10.1 Technology Stack

## Progression Manager

- [x] Track current solar system and planet index — SaveData.currentSolarSystem / currentPlanetIndex
- [x] Track per-planet cleanup progress (combined player + opponent) — GameManager.CleanupPercentage
- [x] Track opponent defeated state and carry-over data — SaveData.carryOver + RecordOpponentDefeated
- [ ] Handle progression unlocks
- [ ] Manage game state transitions (planet → planet → boss → next system)

> GDD Ref: §10.2 Key Systems
