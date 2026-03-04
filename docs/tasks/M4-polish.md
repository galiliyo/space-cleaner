# M4: Polish + Ship Customization

**Goal:** Art, audio, UI complete, ship color customization — toon shading, VFX, SFX, music, HUD, menus, achievements, color picker.

**Status:** Not Started

---

## Toon Shading / Art

- [ ] Custom toon/cel-shading shader (URP compatible)
- [ ] Soft outline shader for ships and trash
- [ ] Bright saturated color palette applied to all assets
- [ ] Space skybox with vibrant nebula colors
- [ ] Planet surface detail (visible from orbit)
- [ ] Dynamic sun lighting for boss fights
- [ ] Unique AI opponent ship designs (final art)
- [ ] Aiming cone visual polish

> GDD Ref: §9.1 Visual Style, §9.2 Rendering

## VFX

- [ ] Vacuum collection particle effect (polished)
- [ ] Projectile trail effects (single shot vs burst differentiation)
- [ ] Combo multiplier escalating visual effects
- [ ] Citizen celebration confetti/particles
- [ ] Bot metal ball impact effects
- [ ] Ship damage effects
- [ ] Boss defeat explosion/tantrum effect
- [ ] Burst cooldown power-up animation (polished)

> GDD Ref: §9.2 Rendering

## SFX

- [ ] Vacuum whooshing crescendo
- [ ] Trash impact cartoon bonk sounds
- [ ] Combo ascending musical tones
- [ ] Citizen cheering and party horns
- [ ] Ship engine hum
- [ ] Single shot fire and impact sounds
- [ ] Burst shot fire sounds (rapid sequence)
- [ ] Bot metal ball heavy impact sound (10 HP)
- [ ] UI interaction sounds

> GDD Ref: §9.3 Audio Direction

## Music

- [ ] Main theme (catchy upbeat, space synth)
- [ ] Gameplay ambient track
- [ ] Boss fight track (intense)
- [ ] Celebration jingle
- [ ] Menu music

> GDD Ref: §9.3 Audio Direction

## Lary Voice

- [ ] Voice-acted taunt lines (bratty, nasal delivery)
- [ ] Defeat tantrum voice clips
- [ ] Taunt audio playback system

> GDD Ref: §9.3 Audio Direction

## Full HUD

- [ ] Health bar with heart icon and color gradient
- [ ] Ammo counter with trash bag icon and soft cap indicator
- [ ] Cleanup progress bar (polished, shows combined %)
- [ ] Combo multiplier with visual flair
- [ ] Currency display with coin icon
- [ ] Opponent health bar
- [ ] Boss health bar with Lary portrait (sun level)
- [ ] Aiming cone polish for both shoot buttons
- [ ] Cooldown indicators for both shoot buttons
- [ ] Responsive layout for different screen sizes

> GDD Ref: §6.1 In-Game HUD

## Menus

- [ ] Main Menu (Play, Ship Customization, Achievements, Settings)
- [ ] Pause Menu (Resume, Restart Planet, Settings, Quit)
- [ ] Settings (Sound, Music, Controls Sensitivity, Graphics Quality)
- [ ] Menu transitions and animations

> GDD Ref: §6.2 Menus

## Ship Color Customization

- [ ] Ship material setup supporting primary, secondary, accent color slots
- [ ] Runtime color application to ship model
- [ ] Default color set available to all players
- [ ] Premium colors locked behind currency cost
- [ ] Color definitions data (ScriptableObject or config)
- [ ] Ship Customization menu accessible from Main Menu
- [ ] Color picker widget for primary, secondary, accent colors
- [ ] Ship preview showing selected colors in real-time
- [ ] Locked/unlocked state display per color
- [ ] Purchase flow: select locked color → confirm spend → unlock
- [ ] Display player's current currency in customization screen
- [ ] Deduct currency on color purchase
- [ ] Persist unlocked colors in save data

> GDD Ref: §7.1 Phase 1: Colors

## Achievements System

- [ ] Achievement data definitions (boss defeats, milestones, combos, collection, currency, PvP)
- [ ] Achievement tracking and unlock logic
- [ ] Achievement unlock animation per tier
- [ ] Achievements gallery UI (earned/locked with progress bars)
- [ ] Equippable achievement display on ship/profile

> GDD Ref: §8.1 Achievement Types, §8.2 Achievement Display

## Opening Cinematic

- [ ] Camera sweep across clean solar system
- [ ] Lary's ship enters and dumps trash
- [ ] Citizens look up in dismay
- [ ] Player ship powers up
- [ ] Mission briefing text
- [ ] Transition to playable tutorial

> GDD Ref: §5.1 Opening Cinematic
