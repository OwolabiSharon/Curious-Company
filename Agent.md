# Curious Company — code guide and unfinished ideas

Reviewed: 9 October 2026. Project: `/Users/sharonowolabi/Desktop/Curious Company`.

This document describes the code and saved Unity assets as they exist today. It is based on reading the gameplay scripts, input definitions, animator controllers, scenes, prefabs, package manifest, and project settings. The game was not run for this review; runtime behavior and Inspector references should be verified in Unity before treating the prototype as complete.

## What the game currently does

Curious Company is a top-down 3D rescue-and-combat prototype. The player moves around an environment, aims with the mouse, shoots enemies, and approaches survivors to recruit them. Recruited survivors follow the player through the NavMesh. A survivor entering a collider tagged `Safe` increases the rescue count. Meeting the rescue target displays the level-won UI. The player dying, or any survivor dying, displays the game-over UI.

The code suggests a broader idea: one survivor could be infected, approach a zombie gate, open it, and cause the other survivors to panic and scatter. Those methods exist, but their gameplay activation is unfinished.

## Project setup and important folders

- Unity version: **6000.0.68f1**, from `ProjectSettings/ProjectVersion.txt`.
- A render pipeline asset is assigned in `GraphicsSettings.asset`; the project includes URP 17.0.4.
- Input System 1.18.0 provides generated action bindings; the project enables both old and new input handling.
- AI Navigation 2.0.10 provides NavMesh support.
- uGUI, animation, Rigidbody physics, audio, and particle systems support gameplay presentation.
- SlimUI Modern Menu 1 is an imported menu/settings package. Blood, magic, and particle packs also include demonstration content; importing an asset does not mean its features are part of the game.
- `Assets/scripts/`: the custom gameplay code.
- `Assets/InputSystem_Actions.inputactions`: editable input action definitions.
- `Assets/scripts/Controls.cs`: generated input wrapper. Change the input asset and regenerate this file rather than editing it by hand.
- `Assets/Scenes/`: the three scenes included in the build.
- `Assets/assets/scene prefabs/`: reusable player, rescue, camera, manager, environment, and UI objects.
- `Assets/assets/`: projectile and older enemy/rescue prefabs. Some differ considerably from the scene-prefab versions.
- `Assets/char/controllers/`: player, zombie, and rescue animator controllers.

The saved build order is:

| Build index | Scene | Startup behavior from code |
| --- | --- | --- |
| 0 | `Assets/Scenes/Scene 1.unity` | Does not automatically enable gameplay; the custom PlayCampaign button enables it in the same scene. |
| 1 | `Assets/Scenes/Scene 2.unity` | GameManager enables gameplay in Awake. |
| 2 | `Assets/Scenes/SampleScene.unity` | GameManager enables gameplay in Awake. |

Scene 1 contains both menu content and gameplay objects. Do not assume it is only a title screen. The Play button is wired to the custom `UIManager.PlayCampaign`, not the imported SlimUI method with the same name.

## How the systems connect

```text
InputSystem_Actions → Controls → InputReader
                                  ├─ Move / Look → CharacterController
                                  ├─ AttackPressed → Shoot → Bullet
                                  │                  └─ ShotFired → EnemyController
                                  └─ JumpPressed → Dash

Enemy trigger contact → TakeDamage → health / knockback / health bar
                                      ├─ player death → GameManager.GameOver
                                      └─ survivor death → GameManager.GameOver

Player proximity → RescueController follows via NavMesh
Safe trigger → GameManager.rescued → LevelWon UI
UIManager.PlayCampaign → GameManager.isPlaying → player / camera behavior
```

GameManager and InputReader each expose a singleton. Most other scripts still obtain them through `GameObject.Find("GameManager")`. Enemy and survivor scripts similarly find `"Player"`. Object names, tags, component placement, and Inspector references are therefore part of the implementation.

## Script-by-script explanation

### GameManager.cs

Owns shared flags and rescue counters. Awake rejects duplicate managers and sets `isPlaying` for scenes whose build index is greater than zero. It is scene-local: `DontDestroyOnLoad` is commented out.

Start forces a 1280 × 720 window. Update checks whether `rescued >= totalRescues` and calls `LevelWon`. `totalRescues` defaults to one. GameOver and LevelWon disable gameplay through `isPlaying` and activate their UI objects. They do not stop time, enemy AI, survivor AI, or physics globally. There is no guarded terminal state, so the victory check keeps running after an outcome.

It also draws an FPS counter using unscaled frame time. `LoadSceneFunc` can load the next scene, but has no call site in the custom scripts and is private. `totalFollowing` is not maintained. `infectedTurned` and `isFree` support the dormant infection/gate feature.

### InputReader.cs and Controls.cs

InputReader creates Controls in Awake, registers Player action callbacks, and enables/disables the Player map with the component. It stores continuous Move and Look values and publishes button events.

| Input | Current result |
| --- | --- |
| WASD / arrow keys | Move across the world X/Z plane. |
| Mouse position | Used as a screen coordinate for aiming. |
| Left mouse button / Enter | Shoot. |
| Space | Dash forward; the action is named Jump, but there is no jump implementation. |
| R / 1 | Reload the current scene through OnPrevious. |
| N / 2 | Load the next scene only while in build index zero. |
| E | Emit InteractPressed; custom gameplay has no subscriber. |
| Left Shift | Sprint callback exists but is empty. |
| C | Crouch callback exists but is empty. |

The input asset includes gamepad, joystick, touch, and XR bindings. The aiming code assumes an absolute screen position, so those bindings do not establish a finished controller or mobile aiming system. Look also has a pointer-delta binding. PausePressed and OnPause exist in InputReader, but the current Player action map has no Pause action.

### CharacterController.cs

This is a custom MonoBehaviour, distinct from UnityEngine.CharacterController. Start finds InputReader and GameManager, gets Rigidbody/audio/collider components, and subscribes Shoot and Dash to input events.

Movement uses `transform.Translate` in Update with world-space X/Z input. It converts movement into the character's local space for animator parameters `hor` and `vert`, allowing animation direction to follow the facing direction. Footsteps play while movement input is nonzero.

Mouse aiming casts from the main camera into the world and points the character toward the hit. Shooting spawns a bullet at `bulletSpawn`, broadcasts the shot position, adds recoil, plays the Shooting animation and audio, and starts a particle effect. Shooting is rejected when gameplay is off, health is depleted, or the current animator state is Shooting. There is no ammo or reload system.

Dash applies a forward Rigidbody impulse. It currently has no cooldown and no gameplay/death check. Player death plays Death, disables the capsule, destroys the Rigidbody, stops footsteps, and calls GameOver.

### Bullet.cs

Moves the projectile along its local forward axis each frame and destroys it after three seconds. The saved Bullet prefabs use speed 35, while the script default is 40.

Trigger contact destroys the bullet unless the other collider is tagged Good. A forward raycast also destroys it on colliders tagged lowercase `wall`. Enemy damage is handled by EnemyController's trigger callback, rather than by Bullet. The raycast runs after movement and looks ahead, so it is not a sweep of the segment already travelled.

### EnemyController.cs

Uses a NavMeshAgent. Start caches objects tagged Good, finds Player and GameManager, and gets audio/collider components. Enemies listen to the player's static ShotFired event and unsubscribe on disable.

Vision checks distance, a forward cone, and whether a raycast is blocked by the configured layer mask. Although `viewAngle` is exposed, the implementation uses a fixed angle of less than 45 degrees on either side. The newer enemy prefab uses mask 64, corresponding to the environment layer.

Visible Good objects can become the closest target. The player receives priority when the enemy has been shot, has heard a shot, or can see the player within playerRange. Hearing is partly implemented: a shot within shotHearingRange sets a destination and permanently sets `heardShot`; subsequent updates pursue the current player position instead of only investigating the sound location.

Caged enemies return early while `gm.isFree` is false. Scene 1 and SampleScene each contain ten `isCaged = 1` prefab overrides. Releasing the gate is therefore a real scene dependency, although its custom-code activation is dormant.

Bullet contact removes one health, plays React and blood particles, and updates the health bar. Good contact plays Attack. Actual friendly damage comes from TakeDamage on enemy contact. At depleted health the enemy plays Death, stops navigation, disables its collider, and is destroyed after two seconds. Animation states tagged Emote temporarily stop navigation. Growl playback is commented out.

### TakeDamage.cs

Shared damage receiver used by the player and survivors. Enemy trigger entry removes one health, applies knockback, plays React, updates the health image, and temporarily prevents further damage. Invoke restores damage eligibility after iFrameDur.

Damage is based on trigger entry, not a timed attack hit or continuous overlap. The script does not itself perform death handling; CharacterController and RescueController monitor its health. Inspector health/maxHealth values matter: the active player prefab has health 8 and maxHealth 9, whereas the scene rescue prefab has 5 and 5.

### RescueController.cs

Uses a NavMeshAgent to follow the player. Survivors not already following are recruited automatically when the player comes within tagDistance. Recruitment enables the health UI and updates the isFollowing animator parameter; it does not require E.

Follow chooses a random offset behind/to the side of the player, samples a nearby NavMesh point, and updates the destination when outside maxRange. The scene rescue prefab uses minRange 2, maxRange 5, and tagDistance 2. Random values are selected every Update. Integer `Random.Range(-1, 1)` produces -1 or 0, so this implementation never selects the positive side.

Safe trigger entry increments the rescue count. There is no already-rescued flag, removal from the escort group, or protection against counting the same survivor again after re-entry. Survivor death triggers GameOver, disables the capsule, and destroys the Rigidbody; it does not explicitly stop the NavMeshAgent.

Spooked and Infected are explained below because they have no active calls.

### CameraController.cs

Runs in LateUpdate when GameManager permits play. For a menu-to-game transition it moves toward the player's offset and rotates toward a 70-degree overhead angle. It then follows the player with MoveTowards. Saved camera settings commonly use offset `(0, 10, -2.5)`, panSpeed 9, and followSpeed 5.

It has a separate local isPlaying flag for completing that transition. Start accesses gm before looking it up by name, so a valid preassigned gm reference is necessary to avoid a null-reference error. The transition calculates rotationReached but only checks positionReached before completing. A location-based Pan method exists, but the branch calling it is commented out.

### UIManager.cs and imported SlimUI code

The custom UIManager hides menu objects, shows gameUI, and sets isPlaying when PlayCampaign is invoked. Other methods show exit/extras panels, return from them, or quit. Quitting stops Play Mode in the Editor and exits the application in a build.

Imported SlimUI UIMenuManager provides additional panel navigation, themes, sound effects, and asynchronous scene loading. UISettingsManager stores preferences and applies some screen/quality settings. Its stored sensitivity, inversion, difficulty, HUD, tooltip, and effect preferences are not read by the custom gameplay scripts. Settings controls therefore do not prove that those features affect gameplay.

Scene 1 retains a LoadScene button call with a null target and the demo name `Demo1Scene2`; that demo scene is not in the configured build list. Audit inherited demo button wiring before relying on it.

### Lighting and diagnostics

- LightFlicker randomizes light brightness, with intervals, dim duration, fades, blackout probability, and optional unscaled timing. It restores intensity on disable. The explicit component found in Scene 1 is disabled, so that placement is dormant.
- lightScript is an empty placeholder.
- FPSTest disables VSync and requests 120 FPS if attached and active. Its script GUID was not found in the inspected game scenes/prefabs, so its presence alone does not establish an active frame cap.

## Existing ideas that are not fully utilized

| Idea and code evidence | Current gap | Practical completion direction |
| --- | --- | --- |
| Infection/betrayal: RescueController.Infected, isParasite, zombieGate, turnRange, GameManager.infectedTurned | Infected is never called; isParasite is unused; the scene rescue prefab has no zombieGate/gate and turnRange is zero. | Choose an infected survivor, assign gate references, add explicit infection states, and decide when the behavior activates. |
| Gate release: Infected rotates gate and sets isFree; enemies read isCaged/isFree | Caged enemies are configured in scenes, but the custom-code method that frees them has no active caller. | Connect a deliberate gate event and update navigation/obstacles so enemies can actually exit. |
| Panic/scattering: Spooked, hidingSpots, maxSpooks, Running Away animation | The Update call is commented out and hidingSpots is empty on the scene rescue prefab. | Assign valid hiding locations, activate panic once per incident, and define how survivors are recovered. |
| Camera tour: Pan, locations, panOffset, index | Pan's calling branch is commented out. | Add an explicit intro phase or use an authored cinematic, then hand control to Follow. |
| Interaction: InteractPressed | No custom subscriber; recruiting is proximity-based. | Use it for deliberate recruitment, gates, or escort commands if desired. |
| Pause, sprint, crouch | Pause has no Player action; sprint/crouch callbacks are empty. | Implement input and gameplay state together, with animation and navigation behavior. |
| Campaign progression: GameManager.LoadSceneFunc and InputReader.OnNext | Victory does not call progression; OnNext only advances from scene zero. | Add an explicit next-level action and final-level behavior. |
| Adjustable enemy vision: viewAngle and objectsWithinRange | viewAngle is ignored and the list is unused. | Use viewAngle/2 and either maintain the list for perception or remove it. |
| Sound investigation: ShotFired and heardShot | Hearing permanently reveals the moving player's position. | Track the last sound position, investigate, and clear alertness after a timeout or search. |
| Escort count: totalFollowing | No code updates it. | Maintain a survivor registry and display recruited/rescued/remaining counts. |
| Gameplay settings: SlimUI PlayerPrefs | Custom movement, aiming, health, and UI do not consume most preferences. | Connect only the settings the game intends to support, and remove misleading demo controls. |
| Alternate platforms | Input bindings exist, but aiming assumes screen coordinates. | Implement platform-specific aiming and validate menus and gameplay on each supported device. |

These are interpretations of existing code, not confirmed design requirements. The strongest connected unfinished concept is an infected escort opening a cage and causing a rescue group to scatter.

## Issues to address before expanding the game

1. **Make outcome handling consistent.** Enemies and survivors do not check isPlaying; damage and dash can also run outside active gameplay. Define menu, playing, won, and lost states, and prevent competing outcomes.
2. **Count each survivor once.** Mark rescue completion, stop or remove the survivor from active escort behavior, and validate the rescue target against the actual level population.
3. **Check references on live scene instances.** Camera Start reads gm too early; standalone prefabs contain intentionally empty scene references and disabled legacy components. Inspect prefab overrides rather than assuming base-prefab values are the final scene values.
4. **Clean up input subscriptions.** CharacterController subscribes in Start but does not unsubscribe when disabled/destroyed. Match subscription lifetimes and guard dash after death.
5. **Handle a missed aiming raycast.** RotateToMouse currently uses hit.point even when Physics.Raycast returns false.
6. **Clarify movement and collision ownership.** Transform movement is mixed with Rigidbody recoil/dash and NavMesh movement. Verify collisions, knockback, and framerate independence before adding abilities.
7. **Improve projectile hit reliability.** Check the actual travelled segment or use a suitable physics approach so fast projectiles cannot skip thin obstacles/targets.
8. **Reset enemy perception deliberately.** closestWR is retained without rebuilding the closest visible target each frame, Good objects are cached only at startup, and heardShot never clears.
9. **Fix and simplify menu wiring.** Remove broken demo callbacks, connect intended settings, and decide whether GameManager should override resolution each scene.

## Guidance for an agent working on this project

- Work in the project directory stated above. This Desktop document is a reference; it is not automatically loaded as a project-root `AGENTS.md`.
- Start with `Assets/scripts/`, then inspect the affected scene/prefab overrides and animator controller. A script change alone may not complete a feature.
- Preserve Unity `.meta` files and GUIDs. Move/rename assets through Unity where practical.
- Do not edit generated Controls.cs directly. Change InputSystem_Actions.inputactions and regenerate.
- Preserve the required object names Player/GameManager and tags Good, Enemy, Bullet, Safe, and lowercase wall unless updating all dependencies together.
- Keep source defaults separate from Inspector overrides. Do not silently copy older enemy/rescue prefab settings into newer scene prefabs.
- Maintain the animator states used by code: Shooting, React, Death, Attack, and Running Away; maintain hor, vert, isPlaying, isMoving, and isFollowing parameters where applicable.
- Keep gameplay additions in the custom scripts unless a change specifically concerns the imported packages. Avoid broad edits to demo/vendor content.
- Treat package availability as tooling, not proof of implemented multiplayer, cinematics, or other gameplay.
- For a behavioral change, run the affected scene in Unity and check the Console. For documentation-only changes, source and asset inspection is sufficient.

Suggested gameplay verification after implementation changes: start through Scene 1's Play button; move/aim/shoot/dash; recruit a survivor; test enemy visibility, hearing, and damage; escort into Safe; leave/re-enter Safe; test player and survivor death; confirm behavior after victory/defeat; restart; and verify intended next-level navigation. Test infection, cage release, and panic separately once their activation paths are implemented.
