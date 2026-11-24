using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Main menu: Play continues Search & Rescue, Levels picks a location then a level, Survival picks a location.
// Location and level lists are built from Resources/GameCatalog, so new locations and levels show up
// without touching the menu.
public class MainMenuController : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject exitPanel;

    [Header("Select panel")]
    public GameObject selectPanel;
    [Tooltip("Where the generated buttons go")]
    public Transform buttonList;
    [Tooltip("Copied for every entry. Stays hidden itself.")]
    public Button buttonTemplate;
    public TMP_Text selectTitle;

    GameMode pendingMode;

    void Start()
    {
        buttonTemplate.gameObject.SetActive(false);
        ShowMain();
    }

    // Main panel buttons

    public void Play()
    {
        GameCatalog catalog = GameCatalog.Load();
        foreach (LocationData location in catalog.locations)
        {
            if (!location.available) continue;
            foreach (LevelData level in location.levels)
            {
                if (!GameSession.IsCompleted(level) && GameSession.IsUnlocked(location, level))
                {
                    GameSession.Play(GameMode.SearchAndRescue, location, level);
                    return;
                }
            }
        }
        // Everything is done: let them choose.
        ShowLocations(GameMode.SearchAndRescue);
    }

    public void Levels() => ShowLocations(GameMode.SearchAndRescue);

    public void Survival() => ShowLocations(GameMode.Survival);

    public void AreYouSure() => exitPanel.SetActive(true);

    public void CancelExit() => exitPanel.SetActive(false);

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Panels

    void ShowMain()
    {
        selectPanel.SetActive(false);
        exitPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    void ShowLocations(GameMode mode)
    {
        pendingMode = mode;
        OpenSelectPanel(mode == GameMode.Survival ? "Survival" : "Search & Rescue");

        foreach (LocationData location in GameCatalog.Load().locations)
        {
            LocationData picked = location;
            if (!location.available)
                AddButton(location.displayName + " - Coming Soon", false, null);
            else if (mode == GameMode.Survival)
                AddButton(location.displayName, true, () => GameSession.Play(GameMode.Survival, picked));
            else
                AddButton(location.displayName, true, () => ShowLevels(picked));
        }
        AddButton("Back", true, ShowMain);
    }

    void ShowLevels(LocationData location)
    {
        OpenSelectPanel(location.displayName);

        foreach (LevelData level in location.levels)
        {
            LevelData picked = level;
            bool unlocked = GameSession.IsUnlocked(location, level);
            string label = level.levelCode + "  " + level.displayName + (unlocked ? "" : "  (Locked)");
            AddButton(label, unlocked, () => GameSession.Play(pendingMode, location, picked));
        }
        AddButton("Back", true, () => ShowLocations(pendingMode));
    }

    void OpenSelectPanel(string title)
    {
        foreach (Transform child in buttonList)
        {
            if (child == buttonTemplate.transform) continue;
            child.gameObject.SetActive(false); // Destroy waits for the end of the frame
            Destroy(child.gameObject);
        }

        if (selectTitle) selectTitle.text = title;
        mainPanel.SetActive(false);
        exitPanel.SetActive(false);
        // Off then on so the panel's pop-up animation plays again.
        selectPanel.SetActive(false);
        selectPanel.SetActive(true);
    }

    void AddButton(string label, bool interactable, UnityEngine.Events.UnityAction onClick)
    {
        Button button = Instantiate(buttonTemplate, buttonList);
        button.gameObject.SetActive(true);
        button.onClick.RemoveAllListeners();
        button.interactable = interactable;
        if (onClick != null) button.onClick.AddListener(onClick);
        button.GetComponentInChildren<TMP_Text>(true).text = label;
    }
}
