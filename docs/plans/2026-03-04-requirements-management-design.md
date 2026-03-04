# Requirements Management Design

**Date:** 2026-03-04
**Status:** Approved

## Decision

Manage all game design requirements and development tasks in markdown files within `docs/`.

## Structure

```
docs/
├── GDD.md                    # Full game design document (source of truth)
├── tasks/
│   ├── overview.md           # Milestone status dashboard
│   ├── M1-prototype.md       # Core gameplay loop
│   ├── M2-combat.md          # Enemy and boss systems
│   ├── M3-progression.md     # Solar system progression
│   ├── M4-polish.md          # Art, audio, UI
│   ├── M5-content.md         # Level design, balancing
│   ├── M6-ship.md            # Ship customization
│   └── M7-release.md         # Optimization, store launch
```

## Conventions

- `GDD.md` is the single source of truth for requirements. Update it when specs change.
- Each `M*.md` contains concrete tasks as `- [ ]` / `- [x]` checklists grouped by system/feature.
- Tasks reference GDD sections with `> GDD Ref: §X.X Section Name`.
- `overview.md` shows milestone status at a glance.
