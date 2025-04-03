using UnityEngine;
using UnityEngine.SceneManagement; // Needed to change scenes

public class SceneLoader : MonoBehaviour
{
    // Loads a scene by name (must match name in Build Settings)
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Quits the game (works in builds, not in the editor)
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Quit");
    }
}
