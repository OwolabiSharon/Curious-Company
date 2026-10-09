# AGENTS.md

Instructions for AI coding agents working in this repository. Humans should start with [README.md](README.md).

## Project at a glance

Curious Company is a top-down 3D rescue-and-combat prototype built in **Unity 6000.0.68f1** (URP 17.0.4). The player moves, aims with the mouse, shoots enemies, and recruits survivors by walking near them. Recruited survivors follow via NavMesh; a survivor entering a `Safe` trigger counts as rescued. Hitting the rescue target shows the level-won UI; the player or any survivor dying shows game over.

Deeper references — read the relevant one before changing a system:

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — scene build order, system wiring, script-by-script behavior, input map.
- [docs/ROADMAP.md](docs/ROADMAP.md) — dormant features (infection, cage release, panic) and known bugs.

## Where things live

| Path | What it is |
| --- | --- |
| `Assets/scripts/` | All custom gameplay code. Start here. |
| `Assets/scripts/Controls.cs` | **Generated** from `Assets/InputSystem_Actions.inputactions`. Never hand-edit. |
| `Assets/Scenes/` | `Scene 1` (menu + gameplay, build index 0), `Scene 2`, `SampleScene`. |
| `Assets/assets/scene prefabs/` | Current player, rescue, camera, manager, environment, and UI prefabs. |
| `Assets/assets/` | Projectile and older enemy/rescue prefabs (these differ from the scene-prefab versions). |
| `Assets/char/controllers/` | Player, zombie, and rescue animator controllers. |
| `Assets/SlimUI`, `Hovl Studio`, `Vefects`, `UnityTechnologies`, `Blood decal pack` | Imported third-party / demo content. Don't edit unless the task is about them. |
| `ProjectSettings/`, `Packages/manifest.json` | Engine settings and package versions. |

## Rules

- **Preserve `.meta` files and GUIDs.** Never delete or regenerate a `.meta`. Move/rename assets in pairs (asset + `.meta`) or through the Unity Editor.
- **Input changes go through the asset.** Edit `InputSystem_Actions.inputactions`, then regenerate `Controls.cs` in Unity.
- **Names and tags are load-bearing.** Code uses `GameObject.Find("Player")` / `Find("GameManager")` and the tags `Good`, `Enemy`, `Bullet`, `Safe`, and lowercase `wall`. Don't rename any of them without updating every dependency.
- **Animator contract.** Code relies on states `Shooting`, `React`, `Death`, `Attack`, `Running Away` and parameters `hor`, `vert`, `isPlaying`, `isMoving`, `isFollowing`. Keep them in sync with the controllers.
- **Script changes are often not enough.** A feature may also need scene/prefab Inspector references or animator changes. Check prefab overrides on the live scene instance, not just the base prefab.
- **Source defaults ≠ Inspector values.** Don't copy older prefab settings into newer scene prefabs, and don't assume a script default is what runs.
- **Keep gameplay code in `Assets/scripts/`.** Avoid broad edits to vendor/demo packages.
- **Installed packages are tooling, not features.** Multiplayer Center, Timeline, Visual Scripting, etc. being present doesn't mean those features exist in the game.
- **Large binaries use Git LFS** (see `.gitattributes`). New large textures, models, audio, or scenes should be LFS-tracked.
- Match the existing C# style in the file you're editing (see `.editorconfig`).

## Verifying changes

There is no automated test suite or CI. Scene/YAML edits and gameplay behavior can only be verified in the Unity Editor.

- **Docs-only changes:** reading source and assets is sufficient.
- **Code changes:** open the affected scene, enter Play Mode, and check the Console for errors.
- **Gameplay smoke test:** start from Scene 1's Play button → move / aim / shoot / dash → recruit a survivor → test enemy sight, hearing, and damage → escort into `Safe` → leave and re-enter `Safe` → test player and survivor death → confirm behavior after win/loss → restart (R) → check next-level navigation.
- Test infection, cage release, and panic separately once their activation paths exist.

If you cannot run Unity, say so explicitly in your summary rather than claiming the change works.
