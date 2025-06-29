using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ForfeitManager : MonoBehaviour
{
    [SerializeField] private GameObject forfeitPopup;
    [SerializeField] private TextMeshProUGUI popupText;

    private void Start()
    {
        // Make sure the popup is hidden at the start
        if (forfeitPopup != null)
        {
            forfeitPopup.SetActive(false);
        }
    }

    public void ShowForfeitPopup()
    {
        if (forfeitPopup != null)
        {
            popupText.text = "Are you sure you want to forfeit?";
            forfeitPopup.SetActive(true);
        }
    }

    public void OnYesButtonClick()
    {
        // Check if the game has ended - if so, allow exit without penalty
        if (PlayerController.gameEnded)
        {
            Debug.Log("Game has ended. Player can exit without penalty.");
            // Load the main menu scene without incrementing losses
            SceneManager.LoadScene("MainMenuScene");
            return;
        }

        // Game is still active - increment the loss count in Firebase
        FirebaseManager.Instance.DatabaseService.IncrementUserLossesAsync();

        // Load the main menu scene
        Debug.Log("Player forfeited during active game. Returning to Main Menu.");
        SceneManager.LoadScene("MainMenuScene");
    }

    public void OnNoButtonClick()
    {
        if (forfeitPopup != null)
        {
            forfeitPopup.SetActive(false);
        }
    }
} 