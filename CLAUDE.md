# CLAUDE.md

**Space Cleaner** — dual-joystick mobile space shooter in Unity 6 (6000.3.10f1) with URP. Player vacuums space trash and shoots it as projectiles. Target: Android (primary), iOS (later).

## Project map

@docs/CLAUDE-structure.md

- `Assets/_Project/` — all game code and assets
- `Assets/Settings/` — URP pipeline assets and volume profiles (rendering config only)
- `docs/GDD.md` — single source of truth for game design
- `docs/tasks/` — M1–M5, M7 milestones; `overview.md` is the status dashboard
- `docs/plans/` — design and implementation plans
- `docs/superpowers/specs/` — approved but unimplemented specs

## Tech stack

Unity 6 LTS · URP 17.3.0 · IL2CPP · New Input System 1.19.0 · .NET Standard 2.1 · Android ARM64 min SDK 25

<important if="you need to run, test, or recompile code">

Unity Editor is connected via MCP Unity on `localhost:8090` (auto-starts). Use `mcp__mcp-unity__*` tools — prefer them over manual file edits for scene/prefab changes.

| Operation | MCP call |
|---|---|
| Recompile after C# edits | `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)` |
| Run EditMode tests | `mcp__mcp-unity__run_tests(testMode="EditMode")` |
| Run PlayMode tests | `mcp__mcp-unity__run_tests(testMode="PlayMode")` |
| Run specific test | `mcp__mcp-unity__run_tests(testFilter="Namespace.Class.Method")` |

**MCP quirks:**
- Listens on IPv6 (`::1:8090`), not IPv4 — curl needs `http://[::1]:8090`
- Times out during package imports / domain reloads — retry after Unity finishes
</important>

<important if="you are creating or modifying C# scripts">

- Place scripts in `Assets/_Project/Scripts/{domain}/`
- Use namespace `SpaceCleaner.{Domain}` (e.g., `SpaceCleaner.Player`, `SpaceCleaner.Core`)
- After editing, recompile: `mcp__mcp-unity__recompile_scripts(returnWithLogs=true)`
</important>

<important if="you are working with physics, collisions, raycasts, or layer masks">

Layers: Player(6), Enemy(7), Trash(8), Projectile(9), Planet(10)
Tags: Trash, PlayerShip, EnemyShip, Planet, Projectile
</important>

<important if="you are writing rendering, shader, or visual effect code">

- Render pipeline: URP 17.3.0 — use URP-compatible APIs (`UniversalRenderPipelineAsset`, `ScriptableRendererFeature`, etc.)
- Two quality tiers: `Mobile_RPAsset` (render scale 0.8, per-vertex lighting) and `PC_RPAsset`
- Color space: Linear
</important>

<important if="you are working with player input or touch controls">

- New Input System only (`activeInputHandler = 1`) — legacy Input is disabled
- Input actions defined in `Assets/_Project/Input/SpaceCleaner_Actions.inputactions`
</important>

<important if="you are creating or modifying scenes, prefabs, or GameObjects">

- Active gameplay scene: `Assets/_Project/Scenes/Gameplay/Gameplay.unity`
- Game scenes live in `Assets/_Project/Scenes/{Gameplay,MainMenu,BossFight}/`
- `Assets/Scenes/SampleScene.unity` exists but is unused
- Use MCP tools (`create_scene`, `load_scene`, `save_scene`) for scene operations
</important>

<important if="you are adding, removing, or updating packages or dependencies">

Key packages (do not reinstall unless broken):
- `com.unity.render-pipelines.universal` 17.3.0
- `com.unity.inputsystem` 1.19.0
- `com.unity.ugui` 2.0.0 — includes TextMeshPro (no separate install needed)
- `com.unity.test-framework` 1.6.0
- `com.unity.cinemachine` 3.1.3
- `com.gamelovers.mcp-unity` — MCP bridge (git package)
- DOTween — **NOT on UPM**; install via Unity Asset Store `.unitypackage` only
</important>

<important if="you are committing, staging, or managing binary assets with git">

@docs/CLAUDE-git.md
</important>

<important if="you are reading requirements, checking task status, or planning features">

- Game design requirements: `docs/GDD.md`
- Milestone tasks: `docs/tasks/overview.md` (dashboard), `docs/tasks/M1`–`M5`, `M7`
- Unimplemented approved specs: player death experience (`docs/superpowers/specs/2026-03-20-player-death-experience-design.md`), opponent balance/anti-stalemate (`docs/superpowers/specs/2026-03-25-opponent-balance-anti-stalemate-design.md`)
- When code and docs conflict, code is newer — update docs to match
</important>
