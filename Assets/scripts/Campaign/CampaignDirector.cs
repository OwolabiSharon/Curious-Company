using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-50)]
public class CampaignDirector : MonoBehaviour
{
    public static CampaignDirector Instance { get; private set; }
    [Range(0, 9)] public int mission;
    public GameManager manager;
    public CharacterController player;
    public GameObject enemyPrefab;
    public GameObject survivorPrefab;
    public Transform[] spawnPoints;
    public Light[] roomLights;
    public Vector3 extractionPosition;
    public bool Briefing { get; private set; } = true;
    public string Conditions { get; private set; }
    public string SetupError { get; private set; }
    bool transitioning;
    readonly List<GameObject> population = new List<GameObject>();

    void Awake()
    {
        Instance = this;
        CampaignSession.EnsureRun();
    }

    void Start()
    {
        manager.isPlaying = false;
        ApplyConditions();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void ApplyConditions()
    {
        bool reducedPower = mission > 1 && (CampaignSession.MissionSeed(mission) & 1) == 1;
        Conditions = reducedPower ? "Emergency power • reduced room lighting" : "Mains power • standard visibility";
        foreach (Light lamp in roomLights)
        {
            if (lamp) lamp.intensity = reducedPower ? 7f : 18f;
        }
    }

    public void Begin()
    {
        if (!Briefing || transitioning) return;
        // Validate every location before spawning anyone; a partial population cannot win.
        int count = CampaignSession.EnemyCount(mission) + CampaignSession.SurvivorCount(mission);
        var candidates = new List<Vector3>();
        foreach (Transform point in spawnPoints)
        {
            if (NavMesh.SamplePosition(point.position, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                candidates.Add(hit.position);
        }
        if (candidates.Count < count)
        {
            SetupError = "The mission has insufficient reachable spawn points. Rebuild it using Curious Company > Build Campaign.";
            Debug.LogError(SetupError);
            return;
        }
        var random = new System.Random(CampaignSession.MissionSeed(mission));
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }
        int survivors = CampaignSession.SurvivorCount(mission);
        manager.totalRescues = survivors;
        player.damage.health = player.damage.maxHealth = CampaignSession.Difficulty == CampaignDifficulty.Easy ? 12 : 9;
        for (int i = 0; i < count; i++)
        {
            bool friendly = i < survivors;
            GameObject actor = Instantiate(friendly ? survivorPrefab : enemyPrefab,
                candidates[i] + Vector3.up, Quaternion.Euler(0, random.Next(360), 0));
            population.Add(actor);
            var agent = actor.GetComponent<NavMeshAgent>();
            agent.Warp(candidates[i]);
            if (friendly)
            {
                var rescue = actor.GetComponent<RescueController>();
                rescue.isFollowing = false;
                rescue.minRange = 1;
                rescue.maxRange = 2;
                rescue.tagDistance = 3;
                rescue.damage.health = rescue.damage.maxHealth = 7;
                agent.speed = 4.2f;
                agent.stoppingDistance = 0.6f;
            }
            else
            {
                var enemy = actor.GetComponent<EnemyController>();
                enemy.isCaged = false;
                enemy.health = enemy.maxHealth = 2 + mission / 3 + (CampaignSession.Difficulty == CampaignDifficulty.Hard ? 1 : 0);
                enemy.range = 7 + mission * 0.3f;
                enemy.shotHearingRange = 8 + mission * 0.45f + (int)CampaignSession.Difficulty;
                agent.speed = CampaignSession.EnemySpeed(mission);
                agent.stoppingDistance = 0.65f;
            }
        }
        CampaignSession.Started = true;
        Briefing = false;
        manager.isPlaying = true;
    }

    public void Restart()
    {
        if (transitioning || Briefing) return;
        Load(SceneManager.GetActiveScene().name);
    }

    public void Next()
    {
        if (transitioning || !manager.HasWon || mission >= CampaignSession.MissionCount - 1) return;
        Load(CampaignSession.SceneName(mission + 1));
    }

    public void NewRun()
    {
        CampaignSession.NewRun();
        Load(CampaignSession.SceneName(0));
    }

    public void Menu() => Load("Scene 1");

    void Load(string scene)
    {
        if (transitioning) return;
        transitioning = true;
        SceneManager.LoadSceneAsync(scene);
    }
}
