#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Run in a disposable validation copy: Unity -batchmode -executeMethod CampaignSmokeCheck.Run.
// Uses Play Mode, real actors, NavMesh and Safe triggers. Does not change saved scene contents.
[InitializeOnLoad]
public static class CampaignSmokeCheck
{
    const string Key = "CuriousCompany.Smoke";
    static int stage;
    static int mission;
    static double deadline;
    static EnemyController shotTarget;
    static float targetHealth;
    static Vector3 moveStart;
    static UnityEngine.InputSystem.Keyboard testKeyboard;
    static float nextCheck;
    static int rescued;
    static float health;
    static RescueController[] survivors;
    static readonly List<string> results = new List<string>();

    static CampaignSmokeCheck()
    {
        EditorApplication.playModeStateChanged += OnPlayState;
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
    }

    [MenuItem("Curious Company/Preview Campaign")]
    public static void OpenPreview()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/Campaign/Campaign_01.unity");
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("Curious Company/Run Campaign Smoke Check")]
    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene("Assets/Scenes/Scene 1.unity");
        EditorApplication.EnterPlaymode();
    }

    static void OnPlayState(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key,false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            stage = -1; mission = 0; nextCheck = 0;
            deadline = EditorApplication.timeSinceStartup + 180;
            CampaignSession.Difficulty = CampaignDifficulty.Hard;
            var inputSettings = UnityEngine.Object.Instantiate(UnityEngine.InputSystem.InputSystem.settings);
            inputSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            UnityEngine.InputSystem.InputSystem.settings = inputSettings;
        }
    }

    static void OnLog(string message, string trace, LogType type)
    {
        // Unity 6000.6 can throw during editor search indexing in a fresh validation copy.
        // Keep that editor-only diagnostic in the log; gameplay errors still fail this check.
        if (trace.Contains("UnityEditor.Search.SearchDatabase")) return;
        if (SessionState.GetBool(Key,false) && EditorApplication.isPlaying && (type == LogType.Exception || type == LogType.Error))
            Fail(message + "\n" + trace);
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (deadline == 0) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Smoke check timed out.");
            if (stage == -1)
            {
                var menu = UnityEngine.Object.FindFirstObjectByType<UIManager>();
                if (menu == null) return;
                menu.PlayCampaign();
                results.Add("Scene 1 Play button routes into campaign PASS");
                stage = 0;
                return;
            }
            var director = CampaignDirector.Instance;
            if (!director || director.mission != mission) return;
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + .35f;
            if (stage == 0)
            {
                Require(director.Briefing && !director.manager.isPlaying, "Briefing must gate gameplay.");
                director.Begin();
                Require(string.IsNullOrEmpty(director.SetupError),director.SetupError);
                stage = 1;
            }
            else if (stage == 1)
            {
                survivors = UnityEngine.Object.FindObjectsByType<RescueController>(FindObjectsSortMode.None);
                var enemies = UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
                Require(survivors.Length == CampaignSession.SurvivorCount(mission),"Incorrect survivor population.");
                Require(enemies.Length == CampaignSession.EnemyCount(mission),"Incorrect enemy population.");
                foreach (var actor in enemies)
                {
                    Require(actor.GetComponent<NavMeshAgent>().isOnNavMesh,"Enemy off NavMesh.");
                    actor.GetComponent<NavMeshAgent>().speed = 0;
                }
                foreach (var actor in survivors)
                    Require(actor.GetComponent<NavMeshAgent>().isOnNavMesh,"Survivor off NavMesh.");
                if (mission < 2) Capture(director,mission % 2 == 0 ? "hospital" : "office");
                if (mission == 0) ScreenCapture.CaptureScreenshot("/tmp/curious-hud.png");
                rescued = 0;
                if (mission == 0)
                {
                    shotTarget = enemies[0];
                    shotTarget.GetComponent<NavMeshAgent>().Warp(new Vector3(0,0,5));
                    targetHealth = shotTarget.health;
                    director.player.transform.forward = Vector3.forward;
                    director.player.SendMessage("Shoot");
                    stage = 9;
                }
                else stage = 2;
            }
            else if (stage == 9)
            {
                Require(shotTarget.health == targetHealth-1,"A fired projectile did not damage its target exactly once.");
                results.Add("Real fired projectile damages enemy exactly once PASS");
                // Inject the actual input asset's keyboard binding and observe Rigidbody movement.
                testKeyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
                moveStart = director.player.transform.position;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(testKeyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.W));
                stage = 10;
            }
            else if (stage == 10)
            {
                Require(director.player.transform.position.z > moveStart.z + .3f,$"W input did not move the player: input={director.player.InputReader.Move}, start={moveStart}, now={director.player.transform.position}, velocity={director.player.GetComponent<Rigidbody>().linearVelocity}.");
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(testKeyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
                UnityEngine.InputSystem.InputSystem.RemoveDevice(testKeyboard);
                moveStart = director.player.transform.position;
                director.player.SendMessage("Dash");
                stage = 14;
            }
            else if (stage == 14)
            {
                Require(Vector3.Distance(director.player.transform.position,moveStart) > .15f,"Dash did not produce motion.");
                results.Add("Input asset W movement and active-game dash PASS");
                stage = 2;
            }
            else if (stage == 2)
            {
                if (rescued < survivors.Length)
                {
                    var survivor = survivors[rescued];
                    // Crossing the trigger physically verifies the one-time rescue flag.
                    survivor.isFollowing = true;
                    survivor.GetComponent<NavMeshAgent>().Warp(director.extractionPosition + Vector3.forward*.5f);
                    stage = 3;
                }
                else stage = 5;
            }
            else if (stage == 3)
            {
                Require(survivors[rescued].IsRescued,$"Safe trigger did not rescue survivor at {survivors[rescued].transform.position}; playing={director.manager.isPlaying}, health={survivors[rescued].damage.health}, following={survivors[rescued].isFollowing}.");
                Require(director.manager.rescued == rescued+1,"Incorrect rescue count.");
                survivors[rescued].SendMessage("OnTriggerEnter",GameObject.Find("Extraction • Safe").GetComponent<Collider>());
                Require(director.manager.rescued == rescued+1,"Survivor counted twice.");
                rescued++;
                stage = 2;
            }
            else if (stage == 5)
            {
                Require(director.manager.HasWon && !director.manager.isPlaying,"Victory did not end gameplay.");
                health = director.player.damage.health;
                var enemy = UnityEngine.Object.FindFirstObjectByType<EnemyController>();
                director.player.damage.SendMessage("OnTriggerEnter",enemy.GetComponent<Collider>());
                Require(director.player.damage.health == health,"Damage continued after victory.");
                director.manager.GameOver();
                Require(director.manager.HasWon,"Loss overwrote victory.");
                results.Add($"Mission {mission+1}: Hard population, agent placement, actual Safe trigger, duplicate rescue guard, victory guard PASS");
                if (mission < 9)
                {
                    director.Next(); mission++; stage = 0;
                }
                else
                {
                    // Check final-mission boundary before restarting into a loss test.
                    director.Next();
                    Require(!director.manager.isPlaying,"Final mission resumed unexpectedly.");
                    director.Restart(); stage = 6;
                }
            }
            else if (stage == 6)
            {
                if (!director.Briefing) return;
                director.Begin(); stage = 7;
            }
            else if (stage == 7)
            {
                director.player.damage.health = 0;
                stage = 8;
            }
            else if (stage == 8)
            {
                Require(director.manager.HasEnded && !director.manager.HasWon,"Player death did not cause game over.");
                director.manager.LevelWon();
                Require(!director.manager.HasWon,"Victory overwrote loss.");
                results.Add("Player death and competing terminal states PASS");
                director.Restart();
                stage = 11;
            }
            else if (stage == 11)
            {
                if (!director.Briefing) return;
                director.Begin(); stage = 12;
            }
            else if (stage == 12)
            {
                UnityEngine.Object.FindFirstObjectByType<RescueController>().damage.health = 0;
                stage = 13;
            }
            else if (stage == 13)
            {
                Require(director.manager.HasEnded && !director.manager.HasWon,"Survivor death did not end the mission.");
                results.Add("Survivor death causes game over PASS");
                File.WriteAllLines("/tmp/curious-campaign-smoke.txt",results);
                SessionState.SetBool(Key,false);
                Debug.Log("CAMPAIGN_SMOKE_PASS");
                if (Application.isBatchMode) EditorApplication.Exit(0);
                else EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception ex) { Fail(ex.ToString()); }
    }

    static void Require(bool value,string message)
    {
        if (!value) throw new Exception(message);
    }

    static void Fail(string message)
    {
        SessionState.SetBool(Key,false);
        File.WriteAllText("/tmp/curious-campaign-smoke.txt","FAIL: " + message);
        Debug.LogError("CAMPAIGN_SMOKE_FAIL: " + message);
        if (Application.isBatchMode) EditorApplication.Exit(1);
        else EditorApplication.ExitPlaymode();
    }

    static void Capture(CampaignDirector director, string name)
    {
        var camera = Camera.main;
        Vector3 original = camera.transform.position;
        camera.transform.position = new Vector3(0,17,-5.9f);
        var target = new RenderTexture(1584,1000,24);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
        image.Apply();
        File.WriteAllBytes($"/tmp/curious-{name}.png",image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = null;
        UnityEngine.Object.Destroy(image);
        UnityEngine.Object.Destroy(target);
        camera.transform.position = original;
    }
}
#endif
