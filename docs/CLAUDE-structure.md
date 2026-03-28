# Project Structure

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
