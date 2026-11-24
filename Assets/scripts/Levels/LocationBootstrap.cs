using UnityEngine;
using UnityEngine.AI;

// Goes in every location scene. When the scene is loaded from the main menu it sets the scene up
// for the chosen mode and level and starts play straight away.
// Opened on its own (e.g. pressing Play in the editor) it leaves the scene's own menu up, as before.
[DefaultExecutionOrder(100)] // after GameManager.Awake and UIManager.Start
public class LocationBootstrap : MonoBehaviour
{
    public LocationData location;
    public UIManager uiManager;

    [Tooltip("Switched on only in Search & Rescue")]
    public GameObject[] rescueOnly = new GameObject[0];
    [Tooltip("Switched on only in Survival")]
    public GameObject[] survivalOnly = new GameObject[0];

    bool selected;

    void Awake()
    {
        // GameManager starts play on its own in any scene after build index 0. Location scenes
        // now come after MainMenu, so hold play until the scene's menu (or Start below) begins it.
        if (GameManager.Instance != null) GameManager.Instance.isPlaying = false;

        selected = GameSession.HasSelection && GameSession.Location == location;
        if (!selected) return;

        // Set the level up before any zombie or rescue runs Start: zombies collect their
        // targets there, so other levels' rescues must already be switched off.
        ApplyMode(GameSession.Mode);
        if (GameSession.Level != null) ApplyLevel(GameSession.Level);
    }

    void Start()
    {
        // Same as clicking Play on the scene's menu.
        if (selected && uiManager != null) uiManager.PlayCampaign();
    }

    void ApplyMode(GameMode mode)
    {
        foreach (GameObject go in rescueOnly) if (go) go.SetActive(mode == GameMode.SearchAndRescue);
        foreach (GameObject go in survivalOnly) if (go) go.SetActive(mode == GameMode.Survival);

        if (mode == GameMode.Survival)
            Debug.LogWarning($"Survival isn't built yet. {location.displayName} is running its normal setup.");
    }

    void ApplyLevel(LevelData level)
    {
        var variants = FindObjectsByType<LevelVariant>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (LevelVariant variant in variants)
            variant.gameObject.SetActive(variant.IsInLevel(level));

        if (level.rescueCount > 0 && GameManager.Instance != null)
            GameManager.Instance.totalRescues = level.rescueCount;

        if (level.zombieSpeedBonus != 0f)
            foreach (EnemyCombat zombie in FindObjectsByType<EnemyCombat>(FindObjectsSortMode.None))
            {
                var agent = zombie.GetComponent<NavMeshAgent>();
                if (agent != null) agent.speed += level.zombieSpeedBonus;
            }
    }
}
