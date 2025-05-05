using UnityEngine;
using TMPro;

public class GameManage : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    private int currentScore = 0;

    private void Start()
    {
        // Load user score when game starts
        LoadUserScore();

        // Subscribe to authentication events to handle user changes
        FirebaseManager.Instance.OnUserAuthenticated += (success, message) => {
            if (success) LoadUserScore();
        };

        FirebaseManager.Instance.OnUserSignedOut += () => {
            // Reset score or handle sign out
            currentScore = 0;
            UpdateScoreDisplay();
        };
    }

    private void LoadUserScore()
    {
        if (FirebaseManager.Instance.IsUserSignedIn)
        {
            FirebaseManager.Instance.GetUserScore((score) => {
                if (score >= 0)
                {
                    currentScore = score;
                    UpdateScoreDisplay();
                }
            });
        }
    }

    public void AddPoints(int points)
    {
        currentScore += points;
        UpdateScoreDisplay();

        // Save updated score to Firebase
        if (FirebaseManager.Instance.IsUserSignedIn)
        {
            FirebaseManager.Instance.UpdateUserScore(currentScore);
        }
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore}";
        }
    }
}