using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMenuManager : MonoBehaviour
{
    public GameObject gameModePanel;
    [SerializeField] private RoomManager roomManager;

    private void Start()
    {
        if (roomManager == null)
        {
            roomManager = FindObjectOfType<RoomManager>();
        }
    }

    public void OnPlayGamePressed()
    {
        if (gameModePanel != null)
        {
            gameModePanel.SetActive(true);
        }
    }

    public void OnSettingsPressed()
    {
        //   
        SceneManager.LoadScene("LoginScene");
    }

    public void OnLocalBattlePressed()
    {
        if (roomManager == null)
        {
            roomManager = FindObjectOfType<RoomManager>();
        }

        if (roomManager == null)
        {
            UnityEngine.Debug.LogError("RoomManager not found! Make sure it's attached to the RoomPanel.");
            return;
        }

        UnityEngine.Debug.Log("Local Battle pressed - showing room panel");
        roomManager.ShowRoomPanel();
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
