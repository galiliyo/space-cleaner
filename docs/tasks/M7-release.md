# M7: Release

**Goal:** App store launch — performance optimization, bug fixes, store listings, device testing.

**Status:** Not Started

---

## Performance Optimization

- [ ] Profile on mid-range Android device (target 60 FPS)
- [ ] Profile on low-end device (target 30 FPS minimum)
- [ ] Object pooling audit (trash, projectiles, effects)
- [ ] LOD setup on planets and large objects
- [ ] Occlusion culling verification
- [ ] Texture atlasing / draw call optimization
- [ ] Particle system budget enforcement
- [ ] Audio compression and streaming for music
- [ ] Build size under 200 MB

> GDD Ref: §10.3 Performance Considerations

## Bug Fixes

- [ ] Full regression playthrough
- [ ] Edge case testing (zero ammo, max combo, rapid scene transitions)
- [ ] Save/load integrity testing
- [ ] Memory leak profiling
- [ ] Crash testing on target devices

## Store Preparation

- [ ] Update bundle ID to final value
- [ ] App icons (all required sizes)
- [ ] Screenshots for store listing
- [ ] Store description and metadata
- [ ] Age rating questionnaire
- [ ] Privacy policy (no data collection in Phase 1)

## Device Testing

- [ ] Test on 3+ Android devices (different screen sizes, chipsets)
- [ ] Test on iOS device(s) if targeting iOS
- [ ] Touch input validation on all test devices
- [ ] Battery/thermal testing during extended play
