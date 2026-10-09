# Roadmap

Unfinished ideas already present in the code, and known issues to fix before expanding the game. These are interpretations of existing code, not confirmed design requirements. See [ARCHITECTURE.md](ARCHITECTURE.md) for how each system works today.

## Campaign expansion status

See [CAMPAIGN.md](CAMPAIGN.md) and [CAMPAIGN_VALIDATION.md](CAMPAIGN_VALIDATION.md). The expansion implements campaign progression, difficulty, randomized deployments, one-time extraction, guarded outcomes, finite hearing, view-angle use, camera initialization, event cleanup, Rigidbody movement, and swept shots. The original issue list below is retained as a prototype baseline; those implemented items now need regression and balance testing rather than first implementation. Infection, gate release, panic, alternate objectives, save/resume, and connected building storeys remain unimplemented.

## Dormant features

The strongest connected unfinished concept is an infected escort opening a cage and causing a rescue group to scatter.

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

## Known issues

1. **Make outcome handling consistent.** Enemies and survivors do not check isPlaying; damage and dash can also run outside active gameplay. Define menu, playing, won, and lost states, and prevent competing outcomes.
2. **Count each survivor once.** Mark rescue completion, stop or remove the survivor from active escort behavior, and validate the rescue target against the actual level population.
3. **Check references on live scene instances.** Camera Start reads gm too early; standalone prefabs contain intentionally empty scene references and disabled legacy components. Inspect prefab overrides rather than assuming base-prefab values are the final scene values.
4. **Clean up input subscriptions.** CharacterController subscribes in Start but does not unsubscribe when disabled/destroyed. Match subscription lifetimes and guard dash after death.
5. **Handle a missed aiming raycast.** RotateToMouse currently uses hit.point even when Physics.Raycast returns false.
6. **Clarify movement and collision ownership.** Transform movement is mixed with Rigidbody recoil/dash and NavMesh movement. Verify collisions, knockback, and framerate independence before adding abilities.
7. **Improve projectile hit reliability.** Check the actual travelled segment or use a suitable physics approach so fast projectiles cannot skip thin obstacles/targets.
8. **Reset enemy perception deliberately.** closestWR is retained without rebuilding the closest visible target each frame, Good objects are cached only at startup, and heardShot never clears.
9. **Fix and simplify menu wiring.** Remove broken demo callbacks, connect intended settings, and decide whether GameManager should override resolution each scene.
