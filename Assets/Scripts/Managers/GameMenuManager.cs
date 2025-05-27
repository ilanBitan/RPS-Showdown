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
        // ����� ����� �������
        SceneManager.LoadScene("LoginScene");
    }

    public void OnLocalBattlePressed()
    {
        GameModeManager.Instance.SelectedMode = GameMode.PvP;
        SceneManager.LoadScene("GameScene");
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
