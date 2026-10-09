# Curious Company

A top-down 3D rescue-and-combat prototype made in Unity. Fight your way through zombies, find survivors, and escort them to safety — without losing anyone along the way.

> **Status:** prototype. The core loop (move, shoot, recruit, escort, win/lose) works; several systems are partially built. See [docs/ROADMAP.md](docs/ROADMAP.md).

## Gameplay

- Move around the level and aim with the mouse.
- Shoot enemies; they can see you, hear your shots, and chase you or your survivors.
- Walk near a survivor to recruit them — they follow you via NavMesh.
- Lead survivors into the safe zone. Rescue enough of them to win the level.
- If you die, or any survivor dies, it's game over.

### Controls

| Input | Action |
| --- | --- |
| WASD / Arrow keys | Move |
| Mouse | Aim |
| Left click / Enter | Shoot |
| Space | Dash |
| R / 1 | Restart current scene |
| N / 2 | Next scene (from Scene 1 only) |

## Requirements

- **Unity 6000.0.68f1** (install via Unity Hub — using the exact version avoids reimport/upgrade churn)
- **Git LFS** — textures, models, audio, and some scenes are stored in LFS

## Getting started

1. Install Git LFS (once per machine):

   ```bash
   brew install git-lfs
   ```

   ```bash
   git lfs install
   ```

2. Clone the repository:

   ```bash
   git clone https://github.com/OwolabiSharon/Curious-Company.git
   ```

   If you cloned before installing LFS, run `git lfs pull` inside the repo to download the real asset files.

3. Open the project folder in **Unity Hub → Add → Add project from disk**, then open it with Unity 6000.0.68f1. The first import can take several minutes.
4. Open `Assets/Scenes/Scene 1.unity` and press **Play**, then click the in-game **Play** button.

## Project structure

```text
Assets/
  scripts/                 Custom gameplay code (start here)
  Scenes/                  Scene 1 (menu + level), Scene 2, SampleScene
  assets/scene prefabs/    Player, survivor, camera, manager, UI prefabs
  char/                    Character models, materials, animator controllers
  visuals/                 Environment textures and models
  InputSystem_Actions.inputactions   Input bindings (Controls.cs is generated from this)
  SlimUI/, Hovl Studio/, Vefects/, UnityTechnologies/, Blood decal pack/
                           Third-party asset packs
Packages/                  Package manifest
ProjectSettings/           Unity project settings
docs/                      Architecture and roadmap
```

## Tech

- Universal Render Pipeline (URP) 17
- Input System 1.18
- AI Navigation (NavMesh) 2.0
- uGUI + TextMesh Pro
- ProBuilder for level blockout

## Documentation

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — how the scripts and systems connect
- [docs/ROADMAP.md](docs/ROADMAP.md) — unfinished features and known issues
- [AGENTS.md](AGENTS.md) — conventions for AI coding agents (also useful for human contributors)

## Development notes

- Always commit `.meta` files alongside their assets.
- Don't edit `Assets/scripts/Controls.cs` by hand; edit the `.inputactions` asset and regenerate.
- Object names `Player` / `GameManager` and the tags `Good`, `Enemy`, `Bullet`, `Safe`, `wall` are referenced from code.

## Third-party assets

This project includes assets from the Unity Asset Store and other creators, each under its own license:

- SlimUI — Modern Menu 1
- Hovl Studio — Magic effects pack
- Vefects — Free Blood VFX
- Unity Technologies — Particle Pack
- Blood decal pack
- Mixamo characters and animations

Check each asset's license before redistributing this project.

## Campaign expansion

The Play button now opens a ten-mission rescue campaign with five hospital and five office missions, difficulty selection, randomized deployments, and original Blender furniture. See [CAMPAIGN.md](docs/CAMPAIGN.md) for controls, editing, regeneration, and scope, and [CAMPAIGN_VALIDATION.md](docs/CAMPAIGN_VALIDATION.md) for validation limits.
