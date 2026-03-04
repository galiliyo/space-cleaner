# M5: Content

**Goal:** 3-5 solar systems — level design, unique AI opponents, difficulty balancing, Lary scaling, playtesting.

**Status:** Not Started

---

## Solar System Design

- [ ] Design Solar System 1 (tutorial, rocky planets, weak AI, easy Lary fight)
- [ ] Design Solar System 2 (gas giants, smarter AI, medium Lary)
- [ ] Design Solar System 3 (ice giants, aggressive AI, hard Lary)
- [ ] Design Solar Systems 4-5 (exotic types TBD, scaling difficulty)
- [ ] Planet count per system (variable, content-driven)
- [ ] Trash density curves per system

> GDD Ref: §3.1 Solar System Structure

## AI Opponent Design

- [ ] Design unique AI opponent per planet (appearance + behavior)
- [ ] Configure AI difficulty per solar system (speed, accuracy, aggression)
- [ ] AI behavior variants: aggressive vacuum, defensive, balanced, ambush
- [ ] Playtest AI difficulty curve across solar systems

> GDD Ref: §4.1 AI Opponents

## 3D Models

- [ ] Player ship model (final or near-final)
- [ ] Lary character model
- [ ] Lary's ship model
- [ ] Unique AI opponent ship models (per planet)
- [ ] Planet models per type (rocky, gas, ice, exotic)
- [ ] Citizen character models per planet type
- [ ] Trash models (5 types: plastic, satellites, junk, comet debris, alien fast food)
- [ ] Minion bot models for boss fights

> GDD Ref: §2.3 Trash System, §9.1 Visual Style

## Lary Scaling

- [ ] Configure Lary HP per solar system (50→80→120→scaling TBD)
- [ ] Configure attack speed per system (slow→medium→fast→very fast)
- [ ] Configure base minion count per system (0-2→2-4→4-6→6+ TBD)
- [ ] Test dynamic minion count (base + defeated opponents with carry-over ammo)
- [ ] Playtest and balance all values

> GDD Ref: §4.5 Lary Scaling

## Tutorial Integration

- [ ] First rocky planet teaches movement (spherical surface)
- [ ] Second section teaches vacuum collection
- [ ] Cleanup bar tutorial prompt
- [ ] Single shot tutorial with aiming cone
- [ ] Burst shot tutorial with aiming cone
- [ ] Defeat weak AI opponent tutorial
- [ ] Citizen celebration as tutorial conclusion
- [ ] No separate tutorial mode — all integrated

> GDD Ref: §5.2 Tutorial Flow

## Playtesting

- [ ] Full playthrough solar system 1
- [ ] Full playthrough solar system 2
- [ ] Full playthrough solar system 3
- [ ] Boss difficulty assessment per system (with carry-over minions)
- [ ] Ammo soft cap tuning (placeholder 50)
- [ ] Burst shot balance (shot count, cooldown duration)
- [ ] Single shot cooldown tuning
- [ ] Session length validation (target: TBD per planet)
- [ ] Score carry-over impact on boss difficulty
