# Rescue campaign

The expansion adds ten editable mission scenes, original Blender furniture, difficulty selection, and randomized deployments. The existing Scene 1 menu's Play button opens the campaign. The three prototype scenes and source actor prefabs remain available.

## Play

Open `Assets/Scenes/Scene 1.unity`, enter Play Mode, and press Play. Choose Easy, Medium, or Hard on the briefing screen, then Deploy. You can also open `Assets/Scenes/Campaign/Campaign_01.unity` directly.

Move with WASD, aim with the mouse, shoot with the left mouse button, and dash with Space. Approach a survivor to recruit them, then escort them to the green extraction zone at the entrance. Every survivor must survive. R retries the current mission with the same deployment seed; N or the Next mission button advances after victory. The final mission offers a new run rather than loading past the build list.

Difficulty is locked once the run starts. New randomized run changes the seed and permits a new difficulty choice. Progress and seed persist across scene transitions in the current session, but are **not saved across application restarts**.

## Missions

| # | Location | Required survivors | Medium enemies |
| --- | --- | --- | --- |
| 1 | Hospital — Admissions | 2 | 7 |
| 2 | Office — Reception | 2 | 9 |
| 3 | Hospital — General Ward | 2 | 11 |
| 4 | Office — Open Plan | 3 | 13 |
| 5 | Hospital — Intensive Care | 3 | 15 |
| 6 | Office — Archives | 3 | 17 |
| 7 | Hospital — Isolation | 4 | 19 |
| 8 | Office — Executive Floor | 4 | 21 |
| 9 | Hospital — Last Evacuation | 4 | 23 |
| 10 | Office — Rooftop Approach | 5 | 25 |

The names describe mission themes, not distinct objective mechanics. Every mission currently uses rescue-and-escort. These are separate mission scenes, not connected physical storeys. The layouts use a shared modular corridor-and-wing design, with more rooms in later tiers and connecting room passages. They are a first environment pass, not ten wholly bespoke architectural layouts.

Enemy count, speed, hearing range, and health increase with mission index. Easy adds player health and reduces incoming damage; Hard increases damage and enemy health. The balance values are centralized in `CampaignSession.cs` and are initial tuning values rather than a completed balance pass.

A seed shuffles enemy and survivor locations among validated points. Later missions can have reduced room lighting. Geometry, mission order, and the rescue objective stay fixed. Retries retain the seed and difficulty. A new run changes the deployment, but randomization does not guarantee every resulting combination is unique.

## Art and scene editing

`Assets/CampaignArt/Models` contains ten original FBX models: hospital bed, privacy curtain, medical trolley, waiting bench, office desk, office chair, cabinet, plant, reception counter, and washbasin. `Tools/Art/FurnitureLibrary.blend` contains their editable Blender source. `Tools/build_campaign_props.py` reproduces the library:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background \
  --python Tools/build_campaign_props.py -- /absolute/path/to/Curious-Company
```

The scenes use the existing floor textures, original characters and animation controllers, two-tone walls, restrained floor labels, warm practical lights, a player flashlight, and a URP post-processing profile. Room lights have no real-time shadows; the overhead fill and flashlight supply shadows. Production light baking, more detailed material work, occlusion culling, and device performance profiling remain future art/performance work.

Edit the generated scenes normally in Unity. **Curious Company → Build Campaign overwrites the campaign scene layouts and generated materials/prefabs**, so save a separate scene variant if retaining hand edits. It preserves asset GUIDs on rebuild and does not overwrite the original three scenes or their source prefabs. The builder also appends the campaign scenes to Build Settings and verifies paths from the entrance to every population point.

Campaign actor variants fix missing scene references, remove disabled duplicate player components, configure physics/navigation ownership, and add a correctly placed muzzle and flashlight. Source prefab Inspector values remain intact.

## Code and behavior

- `CampaignSession`: mission catalog, difficulty curves, run seed.
- `CampaignDirector`: briefing, seeded spawning, population configuration, retries and transitions.
- `CampaignHUD`: briefing, difficulty selector, health/extraction HUD and outcome screens.
- `CampaignBuilder` (Editor): furniture and actor variants, scene construction, navigation baking and reachability checks.
- `CampaignSmokeCheck` (Editor): Play Mode checks using real actors and Safe trigger crossings. Run in a disposable validation copy; it changes the open scene and enters/exits Play Mode.

Shared gameplay fixes include one-time rescue counting, guarded win/loss outcomes, stopping AI/damage/dash outside gameplay, finite sound investigation, the exposed enemy view angle, camera initialization, input event cleanup, collision-based player movement, a dash cooldown, and swept projectile hits. These changes also apply to the original prototype scenes and require their own regression smoke pass.

## Validation

See `docs/CAMPAIGN_VALIDATION.md` for the executed checks and remaining limitations. The project remains configured for Unity **6000.0.68f1**. A separate copy is used for validation in installed Unity **6000.6.5f1**; this does not establish compatibility or visual parity in the older editor.
