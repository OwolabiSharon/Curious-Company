# Campaign validation — 9 October 2026

## Environment

- Main repository remains configured for Unity 6000.0.68f1; its package manifest and original asset GUIDs were preserved.
- Installed Unity 6000.6.5f1 compiled, generated scenes, and ran Play Mode checks in `/tmp/curious-company-validation`.
- Blender 5.2.1 generated ten original FBX furniture models and the editable furniture library.
- Generated campaign assets were copied into the main repository after validation. The validation copy's upgraded project/package settings were **not** copied back.
- Opening the desktop Editor exposed an unaccepted Unity Editor Software Terms dialog. No agreement was accepted on the user's behalf. The manual briefing/HUD inspection remains pending.

## Passed checks

- C# compilation and all ten scene builds.
- Baked NavMesh paths from the entrance to every candidate population point in each mission.
- Scene 1's actual `UIManager.PlayCampaign` entry point loads the campaign.
- A real fired projectile damages an enemy exactly once.
- The input asset's W binding drives Rigidbody movement; an active-game dash produces motion.
- All ten missions on Hard spawn the expected enemy and survivor populations on the NavMesh.
- Actual survivor crossings of the Safe trigger count correctly; repeated callbacks cannot count an already extracted survivor again.
- Every mission's win state disables gameplay; the Next action reaches the following mission.
- The final mission does not advance outside the campaign.
- Damage is rejected after victory; a late loss cannot overwrite victory.
- Restart loads the final mission again; player death causes loss, and a late victory cannot overwrite loss.
- Survivor death causes loss.
- Camera captures of hospital and office scenes were visually inspected. The initial lighting was too dark and was revised; labels were reduced and the camera moved closer. The saved previews reflect the revised environments.

The harness configures a temporary in-memory input settings instance so synthetic keyboard input reaches the game while Unity runs without focus. It does not persist that setting. A Unity 6000.6 editor search-indexing exception occurred in the fresh validation copy; it remains in the log but is explicitly excluded from gameplay-error failure detection. Gameplay exceptions and errors still fail the harness.

## Not established by these checks

- Compatibility of the generated assets and scripts in Unity 6000.0.68f1. Open and regenerate with **Curious Company → Build Campaign** in that editor before relying on the generated NavMesh data there.
- Manual mouse aiming, enemy combat feel, wall collisions during dash, hearing/vision balance, or a complete human playthrough of each difficulty.
- Manual briefing/HUD layout and interaction inspection; the desktop Editor is awaiting license acceptance.
- Full regression playthroughs of Scene 2 and SampleScene. Shared gameplay changes apply there too.
- Target-device frame rate, memory budget, standalone player builds, or production light baking.
- Visual equivalence to the supplied final reference. This is a modular furnished environment pass; more detailed materials, bespoke layouts, and art direction remain.

## Reproduce

In a disposable project copy, with the correct Unity version and terms/license setup complete:

```sh
Unity -batchmode -nographics -projectPath /path/to/copy \
  -executeMethod CampaignBuilder.Build -quit -logFile /tmp/curious-campaign-build.log

Unity -batchmode -projectPath /path/to/copy \
  -executeMethod CampaignSmokeCheck.Run -logFile /tmp/curious-campaign-smoke.log
```

The smoke runner enters Play Mode, traverses the campaign, writes `/tmp/curious-campaign-smoke.txt`, and exits with 0 on success or 1 on failure. In an interactive Editor, the equivalent **Curious Company → Run Campaign Smoke Check** menu exits Play Mode instead of quitting Unity.

The required manual smoke pass remains: Scene 1 Play → select difficulty → Deploy → move / aim / shoot / dash → recruit → test enemy sight, hearing, damage → escort into Safe → leave/re-enter Safe → player/survivor death → outcome gating → R restart → Next → final ending.
