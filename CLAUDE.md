# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Summary

**Space Cleaner** — A dual-joystick mobile space shooter built in Unity 6 (6000.3.10f1) with URP. The player vacuums space trash and shoots it as projectiles. Target platforms: Android (primary), iOS (later).

## Unity & Build Configuration

- **Engine:** Unity 6 LTS (6000.3.10f1)
- **Render Pipeline:** URP 17.3.0 — two quality tiers: `Mobile_RPAsset` (render scale 0.8, per-vertex lighting) and `PC_RPAsset`
- **Scripting Backend:** IL2CPP (Android), .NET Standard 2.1
- **Input:** New Input System only (`activeInputHandler = 1`). Legacy Input is disabled.
- **Color Space:** Linear
- **Android:** ARM64 only, min SDK 25 (Android 7.1)
- **Asset Serialization:** Force Text (YAML)

## MCP Unity Integration

Unity Editor is connected to Claude Code via MCP Unity on `localhost:8090` (auto-starts). Use the `mcp__mcp-unity__*` tools to interact with scenes, GameObjects, components, materials, and the build pipeline directly. Always prefer MCP tools over manual file editing for scene/prefab changes.

## Project Structure

All game assets live under `Assets/_Project/`:

```
Assets/_Project/
├── Scripts/{Boss,Camera,Core,Enemies,Player,Progression,UI}/
├── Scenes/{BossFight,Gameplay,MainMenu}/
├── Prefabs/{Enemies,Projectiles,UI}/
├── Models/{Characters,Planets,Ships,Trash}/
├── Materials/
├── Animations/
├── Audio/{Music,SFX}/
├── Shaders/
├── Textures/
└── UI/
```

Keep all new game code and assets inside `Assets/_Project/`. The top-level `Assets/Settings/` contains URP pipeline assets and volume profiles — edit these for rendering config, not game logic.

## Key Installed Packages

- `com.unity.render-pipelines.universal` 17.3.0 — URP
- `com.unity.inputsystem` 1.19.0 — New Input System
- `com.unity.ugui` 2.0.0 — Canvas UI
- `com.unity.test-framework` 1.6.0 — Test Runner
- `com.gamelovers.mcp-unity` — MCP Unity bridge (git package)

**Not yet installed but planned:** TextMeshPro, Cinemachine, DOTween.

## Running Tests

Use the MCP Unity test runner tool:
```
mcp__mcp-unity__run_tests(testMode="EditMode")
mcp__mcp-unity__run_tests(testMode="PlayMode")
mcp__mcp-unity__run_tests(testFilter="Namespace.ClassName.TestMethod")
```

## Recompiling Scripts

After writing or editing C# files, trigger recompilation:
```
mcp__mcp-unity__recompile_scripts(returnWithLogs=true)
```
Always check compilation logs for errors before proceeding.

## C# Coding Conventions

- Place scripts in the appropriate `Assets/_Project/Scripts/{domain}/` subfolder
- Use namespace `SpaceCleaner.{Domain}` (e.g., `SpaceCleaner.Player`, `SpaceCleaner.Core`)
- Scripts that touch URP shaders or rendering must use URP-compatible APIs (`UniversalRenderPipelineAsset`, `ScriptableRendererFeature`, etc.)
- Input actions are defined in `Assets/InputSystem_Actions.inputactions` — this is currently the generic template and needs to be replaced with game-specific actions

## Git & LFS

- Binary assets (`.fbx`, `.png`, `.jpg`, `.wav`, `.mp3`, `.ogg`, `.psd`, `.tga`, `.tif`, `.tiff`, `.exr`, `.obj`, `.mp4`, `.jpeg`) are tracked by Git LFS
- `.unity`, `.prefab`, `.asset`, `.mat` files are text-serialized YAML — not LFS tracked (correct)
- Generated files (`.csproj`, `.sln`, `Library/`, `Temp/`) are gitignored

## Scene Management

- Template scene at `Assets/Scenes/SampleScene.unity` is the only registered scene
- Game scenes should be created in `Assets/_Project/Scenes/{Gameplay,MainMenu,BossFight}/`
- Use MCP tools (`create_scene`, `load_scene`, `save_scene`) for scene operations

## Requirements and Task Tracking

- **GDD:** `docs/GDD.md` — single source of truth for all game design requirements
- **Tasks:** `docs/tasks/M1-prototype.md` through `M7-release.md` — checklist-based task tracking per milestone
- **Overview:** `docs/tasks/overview.md` — milestone status dashboard
- When code and docs conflict, code is newer — update docs to match

## Known Gaps

- Bundle ID is still the template default (`com.UnityTechnologies.com.unity.template.urpblank`)
- No custom Tags or Layers defined yet (will need Player, Enemy, Trash, etc.)
- `Assets/TutorialInfo/` and `Assets/MobileDependencyResolver/` are template artifacts — safe to remove when ready
