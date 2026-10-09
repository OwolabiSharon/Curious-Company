#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Run in a disposable copy. Exercises movement and escort navigation against the real campaign prefabs.
[InitializeOnLoad]
public static class ActorMotionSmokeCheck
{
    const string SessionKey = "CuriousCompany.MotionCheck";
    const string Report = "/tmp/curious-motion-check.txt";
    static IEnumerator routine;
    static float resumeAt;
    static int frame;
    static double deadline;
    static readonly List<string> results = new List<string>();

    static ActorMotionSmokeCheck()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(SessionKey, false)) return;
            results.Clear();
            routine = Check();
            resumeAt = 0;
            frame = -1;
            deadline = EditorApplication.timeSinceStartup + 90;
        };
        EditorApplication.update += Tick;
        Application.logMessageReceived += (message, trace, type) =>
        {
            if (!SessionState.GetBool(SessionKey, false) || !EditorApplication.isPlaying) return;
            if (trace.Contains("UnityEditor.Search.SearchDatabase")) return;
            if (type == LogType.Error || type == LogType.Exception) Finish(message + "\n" + trace);
        };
    }

    [MenuItem("Curious Company/Run Actor Motion Check")]
    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/Campaign/Campaign_01.unity");
        SessionState.SetBool(SessionKey, true);
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if (!SessionState.GetBool(SessionKey, false) || routine == null || !EditorApplication.isPlaying) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Motion check timed out.");
            if (frame == Time.frameCount || Time.time < resumeAt) return;
            frame = Time.frameCount;
            if (!routine.MoveNext()) { Finish(null); return; }
            if (routine.Current is float delay) resumeAt = Time.time + delay;
        }
        catch (Exception ex) { Finish(ex.ToString()); }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    static IEnumerator Check()
    {
        yield return .2f;
        var settings = UnityEngine.Object.Instantiate(InputSystem.settings);
        settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings = settings;
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        Mouse mouse = InputSystem.AddDevice<Mouse>();
        var director = CampaignDirector.Instance;
        director.Begin();
        yield return .2f;
        var player = director.player;
        var body = player.GetComponent<Rigidbody>();
        var enemies = UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        var survivors = UnityEngine.Object.FindObjectsByType<RescueController>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            enemy.enabled = false;
            enemy.GetComponent<NavMeshAgent>().isStopped = true;
            enemy.GetComponent<Collider>().enabled = false;
        }
        foreach (var person in survivors)
        {
            person.tagDistance = 0;
            person.damage.enabled = false;
        }
        player.damage.enabled = false;
        body.position = new Vector3(0, 1, 8);
        body.linearVelocity = Vector3.zero;
        Require(body.interpolation == RigidbodyInterpolation.Interpolate, "Player Rigidbody is not interpolated.");
        Aim(mouse, player.transform.position + Vector3.forward * 6);
        yield return .4f;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
        yield return .04f;
        float initialSpeed = body.linearVelocity.magnitude;
        Require(initialSpeed > 0 && initialSpeed < player.moveSpeed, "Player acceleration snaps to full speed or does not move.");
        yield return .4f;
        Require(Mathf.Abs(body.linearVelocity.magnitude - player.moveSpeed) < .08f, "Player does not reach configured movement speed.");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return .22f;
        Require(body.linearVelocity.magnitude < .03f, "Player fails to settle after releasing input.");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
        yield return .4f;
        Require(body.linearVelocity.magnitude <= player.moveSpeed + .02f, "Diagonal movement exceeds the speed cap.");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return .2f;
        results.Add("Interpolated movement, gradual acceleration, release braking, diagonal speed cap PASS");

        Aim(mouse, player.transform.position + Vector3.forward * 6);
        yield return .4f;
        Quaternion oldRotation = body.rotation;
        Aim(mouse, player.transform.position - Vector3.forward * 6);
        yield return .04f;
        float turn = Quaternion.Angle(oldRotation, body.rotation);
        Require(turn > 1 && turn < 100, $"Aim did not turn progressively: {turn} degrees.");
        yield return .4f;
        Require(Quaternion.Angle(body.rotation, Quaternion.Euler(0,180,0)) < 3, "Aim failed to reach the pointer direction.");
        results.Add("Bounded aim rotation reaches target without an instant 180-degree snap PASS");

        var survivor = survivors[0];
        var agent = survivor.GetComponent<NavMeshAgent>();
        agent.Warp(new Vector3(0,0,8));
        survivor.SendMessage("Recruit");
        yield return .6f;
        Require(agent.isOnNavMesh && agent.hasPath, "Recruited survivor has no follow path.");
        Vector3 destination = agent.destination;
        Aim(mouse, player.transform.position + Vector3.right * 6);
        yield return .5f;
        Require(Vector3.Distance(destination,agent.destination) < .08f, "Stationary mouse aiming moved the escort's destination.");
        Require(survivor.GetComponent<Rigidbody>().isKinematic && !agent.updateRotation,
            "Survivor has competing physics/rotation owners.");
        results.Add("Stationary aiming preserves escort destination; navigation owns NPC motion PASS");

        body.position = new Vector3(-7,1,12);
        body.linearVelocity = Vector3.zero;
        float until = Time.time + 10;
        while (Vector3.Distance(survivor.transform.position,player.transform.position) > 3.2f && Time.time < until)
            yield return .1f;
        Require(Vector3.Distance(survivor.transform.position,player.transform.position) <= 3.2f,
            "Survivor could not follow through a real room doorway.");
        Require(survivor.anim.GetFloat("LocomotionSpeed") >= .44f && survivor.anim.speed == 1f,
            "Locomotion speed adjustment affects the whole animator.");
        results.Add("Real escort route through corridor and doorway; locomotion-only animation scaling PASS");

        var enemyActor = enemies[0];
        var enemyAgent = enemyActor.GetComponent<NavMeshAgent>();
        body.position = new Vector3(0,1,14);
        enemyAgent.Warp(new Vector3(0,0,20));
        enemyActor.range = 20; enemyActor.playerRange = 20; enemyActor.viewAngle = 360;
        enemyActor.enabled = true;
        enemyActor.transform.rotation = Quaternion.Euler(0,90,0);
        float oldDistance = Vector3.Distance(enemyActor.transform.position, player.transform.position);
        Quaternion last = enemyActor.transform.rotation;
        until = Time.time + 1.5f;
        while (Time.time < until)
        {
            yield return null;
            float change = Quaternion.Angle(last,enemyActor.transform.rotation);
            Require(change < enemyActor.turnSpeed * Mathf.Max(Time.deltaTime,.02f) + 4f, "Enemy rotation snapped beyond the angular bound.");
            last = enemyActor.transform.rotation;
        }
        Require(Vector3.Distance(enemyActor.transform.position,player.transform.position) < oldDistance - .5f,
            "Enemy did not pursue its visible target.");
        Require(enemyActor.GetComponent<Rigidbody>().isKinematic && enemyActor.anim.speed == 1f,
            "Enemy navigation or animation ownership is incorrect.");
        results.Add("Enemy pursuit, bounded turning, and locomotion-only speed adjustment PASS");
        CaptureActors(player, enemies, survivors);
        enemyAgent.enabled = true;
        enemyActor.enabled = true;
        enemyActor.health = 3;
        enemyActor.ReceiveBullet(enemyActor.transform.position);
        yield return .12f;
        Require(enemyActor.anim.GetCurrentAnimatorStateInfo(0).IsName("React")
            || enemyActor.anim.GetNextAnimatorStateInfo(0).IsName("React"), "Hit reaction was cut off by locomotion transitions.");
        results.Add("Hit reaction retains its authored exit timing PASS");
        enemyActor.isCaged = true;
        enemyActor.health = 0;
        yield return .15f;
        Require(enemyActor.isDead, "A defeated caged enemy did not enter its death state.");
        results.Add("Defeated caged enemies still enter death state PASS");
        InputSystem.RemoveDevice(keyboard);
        InputSystem.RemoveDevice(mouse);
    }

    static void CaptureActors(CharacterController player, EnemyController[] enemies, RescueController[] survivors)
    {
        for (int i = 1; i < enemies.Length; i++) enemies[i].gameObject.SetActive(false);
        for (int i = 1; i < survivors.Length; i++) survivors[i].gameObject.SetActive(false);
        player.enabled = false;
        player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        player.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;
        player.GetComponent<Rigidbody>().position = new Vector3(-1.7f,1,10);
        player.transform.SetPositionAndRotation(new Vector3(-1.7f,1,10),Quaternion.Euler(0,180,0));
        enemies[0].enabled = false;
        enemies[0].GetComponent<NavMeshAgent>().enabled = false;
        enemies[0].transform.SetPositionAndRotation(new Vector3(0,1,10),Quaternion.Euler(0,180,0));
        survivors[0].enabled = false;
        survivors[0].GetComponent<NavMeshAgent>().enabled = false;
        survivors[0].transform.SetPositionAndRotation(new Vector3(1.7f,1,10),Quaternion.Euler(0,180,0));
        player.anim.Play("Pistol Idle",0,0); player.anim.Update(0);
        enemies[0].anim.Play("Zombie Idle",0,0); enemies[0].anim.Update(0);
        survivors[0].anim.Play("Looking Around",0,0); survivors[0].anim.Update(0);
        var camera = Camera.main;
        camera.transform.position = new Vector3(0,6,4);
        camera.transform.LookAt(new Vector3(0,1,10));
        camera.orthographicSize = 2.5f;
        var target = new RenderTexture(1440,900,24);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
        image.Apply();
        File.WriteAllBytes("/tmp/curious-actors-polished.png",image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = null;
        UnityEngine.Object.Destroy(image);
        UnityEngine.Object.Destroy(target);
    }

    static void Aim(Mouse mouse, Vector3 world)
    {
        Vector3 screen = Camera.main.WorldToScreenPoint(world);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(screen.x, screen.y) });
    }

    static void Finish(string error)
    {
        if (!SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, false);
        File.WriteAllText(Report, error == null ? string.Join("\n",results) : string.Join("\n",results) + "\nFAIL: " + error);
        if (error == null) Debug.Log("ACTOR_MOTION_CHECK_PASS");
        else Debug.LogError("ACTOR_MOTION_CHECK_FAIL: " + error);
        if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
        else EditorApplication.ExitPlaymode();
    }
}
#endif
