using UnityEngine;
using UnityEngine.SceneManagement;

// Button actions for the Level Won and Game Over screens.
public class LevelEndMenu : MonoBehaviour
{
    public void NextLevel() => GameSession.LoadNextLevel();

    public void Retry()
    {
        // Opened straight from the editor there's no chosen level, so just reload the scene.
        if (GameSession.HasSelection) GameSession.RestartLevel();
        else SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu() => GameSession.ReturnToMainMenu();
}
