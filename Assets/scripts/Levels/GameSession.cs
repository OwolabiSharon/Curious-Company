using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameMode
{
    SearchAndRescue,
    Survival
}

// Carries what the player picked in the main menu (mode, location, level) into the location scene,
// and remembers which levels have been completed.
public static class GameSession
{
    public const string MainMenuScene = "MainMenu";

    public static GameMode Mode { get; private set; }
    public static LocationData Location { get; private set; }
    // Null in Survival, which has no levels.
    public static LevelData Level { get; private set; }

    public static bool HasSelection => Location != null;

    public static void Play(GameMode mode, LocationData location, LevelData level = null)
    {
        Mode = mode;
        Location = location;
        Level = mode == GameMode.SearchAndRescue ? level : null;
        SceneManager.LoadSceneAsync(location.sceneName);
    }

    public static void RestartLevel()
    {
        if (HasSelection) Play(Mode, Location, Level);
    }

    // Next level in this location; after its last level, the first level of the next available location.
    public static void LoadNextLevel()
    {
        if (!HasSelection) { ReturnToMainMenu(); return; }

        LevelData next = Location.NextLevel(Level);
        if (next != null) { Play(Mode, Location, next); return; }

        GameCatalog catalog = GameCatalog.Load();
        int index = System.Array.IndexOf(catalog.locations, Location);
        for (int i = index + 1; i < catalog.locations.Length; i++)
        {
            LocationData location = catalog.locations[i];
            if (location.available && location.levels.Length > 0) { Play(Mode, location, location.levels[0]); return; }
        }
        ReturnToMainMenu();
    }

    public static void ReturnToMainMenu()
    {
        Location = null;
        Level = null;
        SceneManager.LoadSceneAsync(MainMenuScene);
    }

    // Progress (saved in PlayerPrefs)

    static string CompletedKey(LevelData level) => "level_completed_" + level.name;

    public static bool IsCompleted(LevelData level) => PlayerPrefs.GetInt(CompletedKey(level), 0) == 1;

    public static void MarkCompleted(LevelData level)
    {
        if (IsCompleted(level)) return; // LevelWon runs every frame after a win
        PlayerPrefs.SetInt(CompletedKey(level), 1);
        PlayerPrefs.Save();
    }

    // The first level of a location is always open; every other level opens once the one before it is done.
    public static bool IsUnlocked(LocationData location, LevelData level)
    {
        GameCatalog catalog = GameCatalog.Load();
        if (catalog != null && catalog.unlockAllLevels) return true;

        int index = location.IndexOf(level);
        return index <= 0 || IsCompleted(location.levels[index - 1]);
    }
}
