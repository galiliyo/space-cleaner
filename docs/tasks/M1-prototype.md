# M1: Prototype

**Goal:** Core gameplay loop playable — spherical ship flight, vacuum mechanic, trash spawning, dual shooting (single + burst), AI opponent, 1 test planet.

**Status:** Not Started

---

## Project Setup

- [ ] Install TextMeshPro package
- [ ] Install Cinemachine package
- [ ] Install DOTween (via OpenUPM or .unitypackage)
- [ ] Update application bundle ID from template default
- [ ] Define custom layers (Player, Enemy, Trash, Projectile)
- [ ] Define custom tags as needed
- [ ] Create Gameplay scene in `Assets/_Project/Scenes/Gameplay/`

> GDD Ref: §10.1 Technology Stack

## Input System

- [ ] Replace template InputActions with game-specific actions
- [ ] Define Move action (Vector2, left joystick)
- [ ] Define SingleShot action (drag to aim, release to fire)
- [ ] Define BurstShot action (drag to aim, hold to auto-fire)
- [ ] Configure Touch control scheme for mobile
- [ ] Add Gamepad control scheme for testing

> GDD Ref: §2.1 Player Controls

## Spherical Movement System

- [ ] Create PlayerShip prefab with placeholder model
- [ ] Implement spherical surface movement (fixed radius from planet center)
- [ ] Implement input mapping on curved surface (joystick → spherical movement)
- [ ] Implement ship rotation to face movement direction on sphere
- [ ] Add configurable speed parameters (SerializeField)
- [ ] Handle wrap-around (no edges, no boundaries)

> GDD Ref: §2.1 Player Controls, §2.2 Movement Model

## Camera System

- [ ] Set up follow camera at 60-70 degree angle
- [ ] Camera follows behind/above ship on spherical surface
- [ ] Smooth camera transitions
- [ ] Set up Cinemachine or manual follow camera

> GDD Ref: §2.2 Movement Model, §10.2 Key Systems

## Vacuum Collection System

- [ ] Add sphere trigger collider to ship for collection radius
- [ ] Implement OnTriggerEnter to detect trash objects
- [ ] Implement trash lerp-toward-ship animation on collect
- [ ] Add vacuum sound effect trigger
- [ ] Add vacuum particle effect
- [ ] Implement ammo pool with soft cap (placeholder: 50)
- [ ] Implement overflow decay when above soft cap
- [ ] Visual glow effect for collection radius

> GDD Ref: §2.4 Collection and Ammo, §10.2 Key Systems

## Trash Spawning

- [ ] Create base Trash prefab with Rigidbody and collider
- [ ] Create 3-5 visual variants (placeholder meshes/colors)
- [ ] Implement TrashSpawner that distributes trash on spherical surface around a planet
- [ ] Implement object pooling for trash objects
- [ ] Configure spawn parameters per planet (count, spread radius)

> GDD Ref: §2.3 Trash System, §10.2 Key Systems

## Shooting System

- [ ] Implement aiming cone visual (Brawl Stars-style) for both buttons
- [ ] Implement single shot: drag to aim, release to fire, short cooldown
- [ ] Implement burst shot: drag to aim, hold to auto-fire ~10 shots, ~3 sec cooldown with power-up animation
- [ ] Decrement ammo pool on fire
- [ ] Prevent firing when ammo is zero
- [ ] Create projectile prefab with travel speed and collision
- [ ] Implement projectile damage on hit (1 HP)
- [ ] Object pooling for projectiles

> GDD Ref: §2.7 Shooting System, §10.2 Key Systems

## AI Opponent (Basic)

- [ ] Create AI opponent ship prefab with placeholder model
- [ ] Implement basic vacuum behavior (move toward nearest trash)
- [ ] Implement basic combat behavior (shoot at player)
- [ ] AI opponent has one life (dies permanently)
- [ ] On kill: transfer AI's collected trash to player's ammo pool
- [ ] Store defeated opponent's vacuum count for carry-over system

> GDD Ref: §4.1 AI Opponents, §3.3 Score Carry-Over System

## Test Planet

- [ ] Create placeholder planet (sphere with simple material)
- [ ] Position planet in scene with trash spawned on spherical surface
- [ ] Implement space skybox / background

> GDD Ref: §3.1 Solar System Structure

## HUD (Minimal)

- [ ] Ammo counter display (top-right) with soft cap indicator
- [ ] Cleanup progress bar (top-center, 0%→100%)
- [ ] Movement joystick (bottom-left, on-screen)
- [ ] Single shot button (bottom-right, with aiming cone)
- [ ] Burst shot button (bottom-right, with aiming cone and cooldown indicator)
- [ ] Opponent health bar

> GDD Ref: §6.1 In-Game HUD

## Level Completion

- [ ] Track total trash per planet and collected count (both players combined)
- [ ] Update cleanup percentage bar in real-time
- [ ] Track opponent alive/dead state
- [ ] Detect level complete: 100% cleanup AND opponent dead
- [ ] Placeholder citizen celebration trigger at completion

> GDD Ref: §3.2 Level Completion, §3.5 Planet Cleaning
