/*using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameEndHandler : MonoBehaviour
{
    public GameObject endPanel; // Assign EmptyContainer in Inspector
    public Button playAgainButton;
    public TextMeshProUGUI victoryText;

    public static bool gameEnded = false; // 💥 Indicates if the game has ended

    private void Start()
    {
        if (endPanel != null) endPanel.SetActive(false);

        if (playAgainButton != null)
        {
            playAgainButton.interactable = false; // 🛑 Cannot click at the start
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        }

        gameEnded = false; // 🛑 At the start of the scene, not finished
    }

    public void ShowVictory(string winnerName)
    {
        victoryText.text = $"{winnerName} wins the game!";
        endPanel.SetActive(true);
        if (playAgainButton != null)
            playAgainButton.interactable = true;

        gameEnded = true; // 🔒 End the game
    }

    private void OnPlayAgainClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
*/