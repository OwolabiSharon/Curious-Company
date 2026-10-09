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
        Screen.SetResolution(1280, 720, false);
    }

    // Update is called once per frame
    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        if (rescued >= totalRescues)
        {
            LevelWon();
        }
    }

    public void GameOver()
    {
        isPlaying = false;
        gameOverUI.SetActive(true);
    }

    public void LevelWon()
    {
        isPlaying = false;
        LevelWonUI.SetActive(true);
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
        float fps = 1.0f / deltaTime;

        GUI.Label(
            new Rect(10, 10, 300, 30),
            $"FPS: {fps:F1}"
        );
    }
}
