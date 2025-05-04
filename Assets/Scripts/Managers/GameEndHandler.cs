/*using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameEndHandler : MonoBehaviour
{
    public GameObject endPanel; // Assign EmptyContainer in Inspector
    public Button playAgainButton;
    public TextMeshProUGUI victoryText;

    public static bool gameEnded = false; // 💥 הוספנו נעילה כללית למשחק

    private void Start()
    {
        if (endPanel != null) endPanel.SetActive(false);

        if (playAgainButton != null)
        {
            playAgainButton.interactable = false; // 🛑 לא לחיץ בהתחלה
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        }

        gameEnded = false; // 🛑 בתחילת סצנה אין ניצחון
    }

    public void ShowVictory(string winnerName)
    {
        victoryText.text = $"{winnerName} wins the game!";
        endPanel.SetActive(true);
        if (playAgainButton != null)
            playAgainButton.interactable = true;

        gameEnded = true; // 🔒 נעילת המשחק
    }

    private void OnPlayAgainClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
*/