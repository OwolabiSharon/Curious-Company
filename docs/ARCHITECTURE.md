# Architecture

How the custom gameplay code in `Assets/scripts/` fits together. Based on reading the scripts, input definitions, animator controllers, scenes, prefabs, package manifest, and project settings (last reviewed 9 October 2026). Runtime behavior and Inspector references should be verified in Unity.

## Campaign expansion (9 October 2026)

The project now includes ten campaign scenes appended after the three prototype scenes. Scene 1's custom PlayCampaign handler routes into Campaign_01 when it is in Build Settings. The campaign owns its briefing, difficulty, seeded population, HUD, and next-mission flow. See [CAMPAIGN.md](CAMPAIGN.md) for the active expansion architecture.

The script descriptions below document the original prototype baseline. They predate the campaign changes to shared gameplay: guarded terminal states, one-time extraction, gated AI/damage/dash, finite sound investigation, fixed view-angle use, safe camera initialization, subscription cleanup, Rigidbody movement, and swept projectile hits. Do not use those earlier bug descriptions as the current behavior contract.

## Original scenes and build order

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

## Script reference

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

`Controls.cs` is generated from `Assets/InputSystem_Actions.inputactions`. Change the input asset and regenerate it rather than editing it by hand.

### CharacterController.cs

This is a custom MonoBehaviour, distinct from `UnityEngine.CharacterController`. Start finds InputReader and GameManager, gets Rigidbody/audio/collider components, and subscribes Shoot and Dash to input events.

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

Spooked and Infected have no active calls; see [ROADMAP.md](ROADMAP.md).

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
