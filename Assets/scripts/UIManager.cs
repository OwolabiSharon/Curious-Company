using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameManager gm;
    public GameObject playMenu;
    public GameObject gameUI;
    public GameObject exitMenu;
    public GameObject extrasMenu;
    public GameObject one;
    public GameObject two;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gm = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    public void PlayCampaign()
    {
        if (UnityEngine.Application.CanStreamedLevelBeLoaded(CampaignSession.SceneName(0)))
        {
            CampaignSession.NewRun();
            UnityEngine.SceneManagement.SceneManager.LoadScene(CampaignSession.SceneName(0));
            return;
        }
        exitMenu.SetActive(false);
        if (extrasMenu) extrasMenu.SetActive(false);
        playMenu.SetActive(false);
        one.SetActive(false);
        two.SetActive(false);
        gameUI.SetActive(true);
        gm.isPlaying = true;
    }

    public void AreYouSure()
    {
        exitMenu.SetActive(true);
        if (extrasMenu) extrasMenu.SetActive(false);
        // DisablePlayCampaign();
    }

    public void ExtrasMenu()
    {
        // playMenu.SetActive(false);
        if (extrasMenu) extrasMenu.SetActive(true);
        exitMenu.SetActive(false);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
                        Application.Quit();
#endif
    }

    public void ReturnMenu()
    {
        // playMenu.SetActive(false);
        if (extrasMenu) extrasMenu.SetActive(false);
        exitMenu.SetActive(false);
        // mainMenu.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
