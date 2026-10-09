# Prompt for the Unity-connected repair agent

You have MCP access to my running Unity Editor. Repair the Curious Company campaign in this project and verify the result inside this Editor. Perform the repair; do not stop at diagnosis or give me instructions to do the work myself.

## Context and observed failure

This is a Unity 6000.0.68f1 URP rescue-and-combat game. Read the actual Editor version, project path, package versions, Git status, and Console through your tools before changing anything. Use the connected project; do not assume another machine's paths exist.

A previous agent added ten campaign missions (five hospital, five office), Easy/Medium/Hard, seeded enemy/survivor placements, furniture, and shared gameplay fixes. It generated scenes and baked NavMesh data in a separate Unity 6000.6.5f1 copy, then copied the campaign assets into a repository still configured for 6000.0.68f1. The automated checks passed in the newer copy only. Compatibility in 6000.0.68f1 was not established.

My screenshot shows Unity 6000.0.68f1 with Campaign_01 running and these Console messages:
- Failed to load an asset ending in Assets/Scenes/Campaign/Campaign_01_Navigation.asset.
- “The mission has insufficient reachable spawn points. Rebuild it using Curious Company > Build Campaign.”
- A separate warning about reducing additional punctual-light shadow resolution to fit seven shadow maps into a 2048x2048 atlas.

The navigation load failure is the first blocker to investigate. Version-incompatible baked data is a strong hypothesis, not a confirmed diagnosis. Verify missing files, incomplete Git LFS checkout, import errors, corrupt assets, references, and package compatibility before deciding. Treat the shadow warning separately.

## Read these project files

- AGENTS.md
- docs/CAMPAIGN.md and docs/CAMPAIGN_VALIDATION.md
- docs/ARCHITECTURE.md and docs/ROADMAP.md (their original prototype descriptions are marked as historical)
- Assets/scripts/Editor/CampaignBuilder.cs
- Assets/scripts/Editor/CampaignSmokeCheck.cs
- Assets/scripts/Campaign/CampaignDirector.cs
- Assets/scripts/Campaign/CampaignSession.cs
- Assets/scripts/Campaign/CampaignHUD.cs
- Relevant shared scripts under Assets/scripts/

Campaign scenes are Assets/Scenes/Campaign/Campaign_01.unity through Campaign_10.unity. Each scene references a baked *_Navigation.asset. Campaign actors and furniture are under Assets/CampaignArt/Prefabs/. Original prototype scenes are Scene 1, Scene 2, and SampleScene.

## Repair procedure

1. Capture the full Console errors and their stack traces. Exit Play Mode before editing or baking. Preserve unsaved work and existing local changes. Keep Unity 6000.0.68f1 as the target unless I explicitly authorize upgrading it.

2. Inspect Campaign_01's live hierarchy and Inspector references. Its NavMeshSurface is on “Architecture and furnishings”. The builder configures CollectObjects.Children, PhysicsColliders, environment layer 6, and the default agent type. Verify actual floor/obstacle colliders, layer assignments, surface settings, actor agent types, and CampaignDirector.spawnPoints. Confirm whether surface.navMeshData can load and whether the navigation asset actually exists on disk.

3. Prefer rebaking and saving navigation for the existing scenes inside this Editor. Preserve hand-edited geometry, lighting, furniture, and prefab overrides. Do not immediately invoke Curious Company > Build Campaign: that command regenerates all ten scenes, campaign materials, and actor/furniture variants, potentially overwriting scene edits. Full regeneration is only appropriate if those changes are understood and preserved.

4. Handle unreadable existing navigation assets safely. CampaignBuilder currently calls LoadAssetAtPath<NavMeshData>, copies into a loaded asset, or calls CreateAsset at the original path when the load returns null. An existing file that cannot deserialize needs explicit handling. Do not delete or regenerate .meta files. If necessary, create a fresh NavMeshData asset at a new unique project path using Unity's asset APIs, assign it to the surface, and save the scene while retaining the old asset for diagnosis. When updating a valid existing asset, preserve its GUID, mark it dirty, and save it. Add a focused Editor repair/rebake command if needed, and make repeated execution safe.

5. Repair all ten missions in the target Editor. Verify complete navigable routes from the entrance/extraction area to every usable spawn, not just successful NavMesh.SamplePosition calls. Validate enough distinct usable locations for Hard: EnemyCount(i) = 5 + 2*i + 4 and SurvivorCount(i) = 2 + integer(i/3), with i from 0 to 9. Mission 10 needs 32 actor locations. Check agent placement/Warp success and retain the guard that prevents deploying an incomplete population. Do not lower required counts or bypass the error to hide a broken NavMesh.

6. Fix any remaining import, serialization, compilation, or Inspector-reference errors revealed in this version. Keep source edits focused on the repair. Inspect scene instances and campaign variants as well as source prefabs. Preserve the original prototype scenes, generated Controls.cs, object names, tags, and animator contracts. Input changes must go through the inputactions asset and Unity regeneration. Avoid vendor/demo edits and wholesale Library deletion.

7. Investigate the shadow-atlas warning after gameplay works. Inspect active lights and the actual URP asset. Prefer reducing unnecessary shadow-casting lights or appropriate shadow resolution over blindly increasing the atlas. Preserve readable interiors and the player flashlight.

## Verify inside Unity

- Save, close/reopen a repaired scene, and confirm its navigation asset reloads. Restart the Editor or perform an equivalent fresh import/reopen verification before claiming persistence is fixed.
- Start with Scene 1's Play button, select difficulty, and Deploy successfully.
- Verify Easy, Medium, and Hard, including expected populations and difficulty locking during a run.
- Exercise move, mouse aim, shooting, dash, and wall/furniture collisions.
- Recruit and escort a survivor through doorways into Safe; leaving/re-entering must not count twice.
- Check enemy sight, hearing, attacks, player death, survivor death, and that gameplay stops after win/loss.
- Retry with R: preserve the run seed/difficulty. Advance after victory with Next/N through all ten missions. Verify the final ending and new-run behavior.
- Inspect briefing/HUD readability and click behavior in the actual Game view.
- Use CampaignSmokeCheck as supplementary evidence after inspecting it. Its existing tests were written for a newer Editor and are not a substitute for target-version testing or manual combat checks. Do not suppress runtime errors to make tests pass.
- Verify repaired asset references persist on disk and Build Settings includes all ten campaign scenes.

Report the confirmed root cause, exact files/assets changed, actual Editor/package versions used, verification results, and anything still failing or untested. Include a working Game-view screenshot and relevant final Console evidence. Update the validation document with results from this machine. Do not claim the earlier newer-version checks prove this repair.
