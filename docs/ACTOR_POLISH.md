# Character movement and presentation pass

This pass changes the existing gameplay scripts, two Animator controllers, and four character materials. It does not regenerate campaign scenes, navigation data, models, textures, or prefab GUIDs. It can be applied alongside the target-Editor navigation repair described in `UNITY_REPAIR_HANDOFF.md`.

## Movement

- Player Rigidbody interpolation smooths rendered movement between physics ticks. Acceleration and braking have separate tunable rates. Rigidbody damping is zeroed because the movement controller supplies braking explicitly.
- Mouse aiming uses a horizontal plane through the player rather than the first collider under the pointer. Crossing furniture, walls, or characters therefore does not change the height of the aim surface. Rotation advances toward that direction at a capped rate in FixedUpdate.
- The player Animator's horizontal/vertical parameters follow actual horizontal velocity with damping. Footsteps follow actual motion and stop during the dash. Shooting and hit reactions use short animation crossfades.
- The camera uses SmoothDamp during gameplay; the existing menu-to-game transition remains.
- `AgentMotion` lets NavMeshAgent own NPC translation, with a kinematic Rigidbody and disabled root motion. Facing eases toward travel velocity. NPC animation start/stop uses separate speed thresholds to reduce flicker from small avoidance movements.
- Enemies update perception at staggered intervals, retain a visible target until another is substantially closer, and throttle destination changes. A short memory uses the last seen position rather than tracking unseen moving targets. Existing finite shot-location investigation remains.
- Survivors occupy stable escort slots aligned with the player's last walking direction. Mouse aiming while stationary no longer swings the formation around. A reachable player position is the fallback when a formation offset falls behind an obstacle. Disabling, rescuing, or losing a survivor releases its slot and updates the following count.

## Character appearance

Existing models, rigs, diffuse textures, and normal maps are retained. This is a material and animation polish pass, not a replacement character-art set.

The survivor body/hair and zombie body/hair materials have brighter base tints and lower smoothness, retaining texture detail with less shiny fabric. Survivor and zombie locomotion transitions use short fixed-time blends without waiting for a full idle/run loop to end. A new float parameter, `LocomotionSpeed`, drives only the running states; attacks, reactions, death, and idle retain their normal playback speed. Running states also enable the Animator's humanoid foot IK option.

## Main tuning controls

| Component | Fields | Initial values |
| --- | --- | --- |
| CharacterController | moveAcceleration / moveBraking | 24 / 32 m/s² |
| CharacterController | aimTurnSpeed / animationDamping | 900°/s / 0.12 s |
| CameraController | followSmoothTime | 0.14 s |
| EnemyController | movementAcceleration / turnSmoothTime / turnSpeed | 7 / 0.12 s / 540°/s |
| EnemyController | perceptionInterval / referenceRunSpeed | 0.12 s / 3.2 m/s |
| RescueController | movementAcceleration / turnSmoothTime / turnSpeed | 10 / 0.10 s / 600°/s |
| RescueController | referenceRunSpeed | 4.2 m/s |

Existing movement speeds, difficulty curves, tags, state names, damage values, and rescue objectives are preserved. `minRange` controls formation spacing; the legacy `maxRange` field is retained for serialization but no longer causes stop/start following at that distance. Live scene Inspector overrides still take precedence for serialized fields.

## Applying and checking on the partner's machine

Bring across the changed scripts, the new `AgentMotion.cs` with its .meta, both changed controllers under `Assets/char/controllers/`, and all four changed materials under `Assets/char/mats/`. Copy their existing .meta files unchanged if transferring files manually. Include the Editor check script and its .meta if running that check.

Let Unity compile and reimport. There is no need to invoke Build Campaign for this pass. The separate navigation compatibility issue must already be repaired before the campaign can run in Unity 6000.0.68f1.

Use **Curious Company → Run Actor Motion Check** in a disposable validation copy. It opens Campaign_01, enters Play Mode, tests movement and real escort navigation, then exits Play Mode. It temporarily injects synthetic keyboard/mouse devices and uses an in-memory input settings copy. It does not save scene modifications. The check writes `/tmp/curious-motion-check.txt`; in batch mode it exits Unity with success/failure status.

Also run the existing Campaign Smoke Check and manually assess the feel at normal Game-view scale, including abrupt input reversals, rapid aim changes, dash into walls, several escorts in a doorway, enemy crowd turns, and win/loss transitions.

## Validation record

Verified on 2026-10-09 in Unity **6000.6.5f1**, using the separate `/tmp/curious-motion-validation` project:

- Actor Motion Check passed in Play Mode: player acceleration/braking and diagonal cap, bounded aim turns, stationary-aim escort stability, a real doorway route, enemy pursuit/turning, locomotion-only playback scaling, retained hit reactions, and caged-enemy death. Final run exited successfully with `ACTOR_MOTION_CHECK_PASS`.
- The existing Campaign Smoke Check passed all ten missions on Hard, including Scene 1 entry, actual projectile damage, movement/dash, populations and navigation placement, actual Safe trigger/duplicate rescue guards, victory/next-level flow, and player/survivor death. This run preceded the final restoration of authored zombie reaction exit timings; the final focused check covered that restoration and caged death handling.
- Inspected the rendered existing character models/materials. [Close-up preview](previews/actors-polished.png) uses a temporary inspection camera and actor positions; it is not the normal gameplay camera.
- No compilation or gameplay errors occurred in the checks. The validation Editor separately logged an existing `UnityEditor.Search.SearchDatabase` indexing exception. The harness excludes that Editor-only stack from gameplay failures; the Console was not completely error-free.

Target-version compatibility in **Unity 6000.0.68f1 remains unverified**. No newer-version baked navigation, scenes, prefabs, package settings, or existing .meta files were copied back during this pass. The partner's navigation load problem remains a separate repair. Human playtesting of feel, crowded doorways, hearing/combat, and performance is still required; these checks do not establish a frame-rate improvement.
