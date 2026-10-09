using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public bool infectedTurned = false;
    public bool isFree = false;
    public bool isPlaying = false;
    public int totalRescues = 1;
    public int totalFollowing = 0;

    public int rescued = 0;

    public GameObject gameOverUI;
    public GameObject LevelWonUI;
    float deltaTime;
    public bool HasEnded { get; private set; }
    public bool HasWon { get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (SceneManager.GetActiveScene().buildIndex > 0)
        {
            isPlaying = true;
        }


        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Respect the player's display settings.
    }

    // Update is called once per frame
    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        if (isPlaying && !HasEnded && totalRescues > 0 && rescued >= totalRescues)
        {
            LevelWon();
        }
    }

    public void GameOver()
    {
        if (HasEnded) return;
        HasEnded = true;
        isPlaying = false;
        if (gameOverUI) gameOverUI.SetActive(true);
    }

    public void LevelWon()
    {
        if (HasEnded) return;
        HasEnded = true;
        HasWon = true;
        isPlaying = false;
        if (LevelWonUI) LevelWonUI.SetActive(true);
    }


    void LoadSceneFunc()
    {
        int nextScene = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextScene < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadSceneAsync(nextScene);
        }
    }
    void OnGUI()
    {
        if (CampaignDirector.Instance != null) return;
        float fps = 1.0f / deltaTime;

        GUI.Label(
            new Rect(10, 10, 300, 30),
            $"FPS: {fps:F1}"
        );
    }
}
