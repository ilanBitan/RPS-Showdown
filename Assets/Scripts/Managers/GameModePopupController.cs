using UnityEngine;

public class GameModePopupController : MonoBehaviour
{
    public GameObject gameModePanel;

    public void HideGameModePanel()
    {
        gameModePanel.SetActive(false);
    }
} 