# M1: Prototype

**Goal:** Core gameplay loop playable — spherical ship flight, vacuum mechanic, trash spawning, dual shooting (single + burst), AI opponent, 1 test planet.

**Status:** In Progress (~98%)

---

## Project Setup

- [x] Install TextMeshPro package (included in com.unity.ugui 2.0.0)
- [x] Install Cinemachine package (added 3.1.3)
- [x] Install DOTween (via OpenUPM scoped registry, v1.2.765)
- [x] Update application bundle ID from template default (com.spacecleaner.game)
- [x] Define custom layers (Player=6, Enemy=7, Trash=8, Projectile=9, Planet=10)
- [x] Define custom tags (Trash, PlayerShip, EnemyShip, Planet, Projectile)
- [x] Create Gameplay scene in `Assets/_Project/Scenes/Gameplay/`

> GDD Ref: §10.1 Technology Stack

## Input System

- [x] Replace template InputActions with game-specific actions (SpaceCleaner_Actions.inputactions)
- [x] Define Move action (Vector2, left joystick)
- [x] Define Aim action (Vector2, right joystick — drag to aim)
- [ ] Define separate BurstShot action (currently single unified aim/shoot)
- [x] Configure Touch control scheme for mobile
- [x] Add Gamepad control scheme for testing

> GDD Ref: §2.1 Player Controls

## Spherical Movement System

- [x] Create PlayerShip prefab with placeholder model
- [x] Implement spherical surface movement (fixed radius from planet center) — SphericalMovement.cs
- [x] Implement input mapping on curved surface (joystick → spherical movement)
- [x] Implement ship rotation to face movement direction on sphere
- [x] Add configurable speed parameters (SerializeField)
- [x] Handle wrap-around (no edges, no boundaries)

> GDD Ref: §2.1 Player Controls, §2.2 Movement Model

## Camera System

- [x] Set up follow camera at 60-70 degree angle — SphericalCamera.cs
- [x] Camera follows behind/above ship on spherical surface
- [x] Smooth camera transitions
- [ ] Set up Cinemachine or manual follow camera (using manual follow for now)

> GDD Ref: §2.2 Movement Model, §10.2 Key Systems

## Vacuum Collection System

- [x] Add sphere trigger collider to ship for collection radius — VacuumCollector.cs
- [x] Implement OnTriggerStay to detect trash objects
- [x] Implement trash lerp-toward-ship animation on collect — TrashPickup.cs
- [ ] Add vacuum sound effect trigger
- [x] Add vacuum particle effect — VacuumCollector creates inward-flowing sparkles
- [x] Implement ammo pool with soft cap (50) — PlayerController.cs
- [x] Implement overflow decay when above soft cap
- [x] Visual glow effect for collection radius — procedural sphere with additive URP material, pulses when collecting

> GDD Ref: §2.4 Collection and Ammo, §10.2 Key Systems

## Trash Spawning

- [x] Create base Trash prefab with Rigidbody and collider (Trash_A/B/C)
- [x] Create 3 visual variants (Trash_A, Trash_B, Trash_C)
- [x] Implement TrashSpawner that distributes trash on spherical surface around a planet
- [x] Implement object pooling for trash objects — TrashSpawner creates pools per variant
- [x] Configure spawn parameters per planet (count=200, clusters=25)

> GDD Ref: §2.3 Trash System, §10.2 Key Systems

## Shooting System

- [x] Implement aiming cone visual (Brawl Stars-style) — AimingCone.cs
- [x] Implement single shot: drag to aim, release to fire (flick), short cooldown
- [x] Implement auto-fire: hold to auto-fire (~6 shots/sec)
- [x] Implement burst shot: hold auto-fires 10 shots then 3s cooldown — ShootingSystem.cs
- [x] Decrement ammo pool on fire — PlayerController.TryConsumeAmmo()
- [x] Prevent firing when ammo is zero
- [x] Create projectile prefab with travel speed and collision — Projectile.cs
- [x] Implement projectile damage on hit (1 HP) — Health.cs
- [x] Object pooling for projectiles — LevelSetup creates pool, ShootingSystem/AIOpponent use it

> GDD Ref: §2.7 Shooting System, §10.2 Key Systems

## AI Opponent (Basic)

- [x] Create AI opponent ship prefab with placeholder model — AIShip prefab
- [x] Implement basic vacuum behavior (move toward nearest trash) — AIOpponent.cs
- [x] Implement basic combat behavior (shoot at player when in range)
- [x] AI opponent has one life (dies permanently) — Health + DeathSequence
- [x] On kill: transfer AI's collected trash to player's ammo pool
- [x] Store defeated opponent's vacuum count for carry-over system

> GDD Ref: §4.1 AI Opponents, §3.3 Score Carry-Over System

## Test Planet

- [x] Create placeholder planet (sphere with simple material) — Planet prefab
- [x] Position planet in scene with trash spawned on spherical surface
- [x] Implement space skybox / background — SpaceSkybox.cs procedural 6-sided star field

> GDD Ref: §3.1 Solar System Structure

## HUD (Minimal)

- [x] Ammo counter display with soft cap indicator — GameplayHUD.cs
- [x] Cleanup progress bar (0%→100%)
- [x] Movement joystick (bottom-left) — VirtualJoystick.cs
- [x] Aiming/shooting via right stick (with aiming cone)
- [x] Burst cooldown indicator (radial fill, bottom-right) — GameplayHUD.cs
- [x] Opponent health bar — OpponentBanner.cs

> GDD Ref: §6.1 In-Game HUD

## Level Completion

- [x] Track total trash per planet and collected count — GameManager.cs
- [x] Update cleanup percentage bar in real-time — OnCleanupChanged event
- [x] Track opponent alive/dead state — OnOpponentStateChanged event
- [x] Detect level complete: 100% cleanup AND opponent dead — IsLevelComplete
- [x] Placeholder citizen celebration — confetti particles + animated panel with "PLANET CLEANED!"

> GDD Ref: §3.2 Level Completion, §3.5 Planet Cleaning
