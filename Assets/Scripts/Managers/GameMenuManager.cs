using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMenuManager : MonoBehaviour
{
    public GameObject gameModePanel;

    public void OnPlayGamePressed()
    {
        if (gameModePanel != null)
        {
            gameModePanel.SetActive(true);
        }
    }

    public void OnSettingsPressed()
    {
        // נעבור לסצנת הפרופיל
        SceneManager.LoadScene("LoginScene");
    }

    public void OnEasyPressed()
    {
        GameModeManager.Instance.SelectEasy();
        SceneManager.LoadScene("GameScene");
    }

    public void OnMediumPressed()
    {
        GameModeManager.Instance.SelectMedium();
        SceneManager.LoadScene("GameScene");
    }

    public void OnHardPressed()
    {
        GameModeManager.Instance.SelectHard();
        SceneManager.LoadScene("GameScene");
    }
}
