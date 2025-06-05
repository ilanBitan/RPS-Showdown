using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TurnTimerManager : MonoBehaviour
{
    public static TurnTimerManager Instance;

    public TextMeshProUGUI timerText;
    public float turnDuration = 10f;
    public GameObject playAgainContainer;

    private float currentTime;
    private bool timerRunning = false;
    private bool activePhaseStarted = false;
    private bool playerWon = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        // וודא שכפתור Play Again תמיד מוצג אבל לא לחיץ בהתחלה
        if (playAgainContainer != null)
        {
            playAgainContainer.SetActive(true);
            var button = playAgainContainer.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.interactable = false;
            }
        }
    }

    public void ActivateGameTimer()
    {
        activePhaseStarted = true;
    }

    public void StartTurn()
    {
        if (!activePhaseStarted) return;

        currentTime = turnDuration;
        timerRunning = true;
        UpdateDisplay();
    }

    public void StopTimer()
    {
        timerRunning = false;
    }

    public void SetPlayerWon(bool won)
    {
        playerWon = won;
        // הפוך את הכפתור ללחיץ כשהמשחק נגמר
        if (playAgainContainer != null)
        {
            var button = playAgainContainer.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.interactable = true;
            }
        }

        // עדכון סטטיסטיקות בשרת רק במשחקי PvE
        if (GameModeManager.Instance != null && IsPlayerVsComputer())
        {
            UpdatePlayerStatistics(won);
        }
    }

    private bool IsPlayerVsComputer()
    {
        var mode = GameModeManager.Instance.SelectedMode;
        return mode == GameMode.PvE_Easy || mode == GameMode.PvE_Medium || mode == GameMode.PvE_Hard;
    }

    private void UpdatePlayerStatistics(bool playerWon)
    {
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsInitialized)
        {
            UnityEngine.Debug.LogWarning("Firebase not initialized, cannot update player statistics");
            return;
        }

        var dbService = FirebaseManager.Instance.DatabaseService;
        if (dbService == null)
        {
            UnityEngine.Debug.LogWarning("Database service not available, cannot update player statistics");
            return;
        }

        // קבלת הסטטיסטיקות הנוכחיות מהשרת
        dbService.GetUserStats((userData) => {
            if (userData == null)
            {
                UnityEngine.Debug.LogError("Could not get user data to update statistics");
                return;
            }

            if (playerWon)
            {
                // עדכון נצחונות
                int newWins = userData.wins + 1;
                dbService.UpdateUserWins(newWins);

                // עדכון נקודות בהתאם לרמת קושי
                int pointsToAdd = GetPointsForDifficulty();
                int newScore = userData.score + pointsToAdd;
                dbService.UpdateUserScore(newScore);

                UnityEngine.Debug.Log($"Player won! Added {pointsToAdd} points. New wins: {newWins}, New score: {newScore}");
            }
            else
            {
                // עדכון הפסדים
                int newLosses = userData.losses + 1;
                dbService.UpdateUserLosses(newLosses);

                UnityEngine.Debug.Log($"Player lost! New losses: {newLosses}");
            }
        });
    }

    private int GetPointsForDifficulty()
    {
        var mode = GameModeManager.Instance.SelectedMode;
        return mode switch
        {
            GameMode.PvE_Easy => 10,
            GameMode.PvE_Medium => 50,
            GameMode.PvE_Hard => 100,
            _ => 0
        };
    }

    private void Update()
    {
        if (PlayerController.gameEnded)
        {
            // אם המשחק נגמר - להציג הודעה במקום טיימר
            if (timerText != null)
            {
                timerText.text = playerWon ? "YOU WIN!" : "YOU LOSE!";
            }
            return;
        }

        if (!timerRunning || !activePhaseStarted)
            return;

        if (BattleManager.Instance != null && BattleManager.Instance.IsBattleActive())
            return;

        currentTime -= Time.deltaTime;
        UpdateDisplay();

        if (currentTime <= 0f)
        {
            timerRunning = false;
            UnityEngine.Debug.Log("⏰ Time's up! Auto-switching turn...");
            TurnManager.Instance?.EndTurn();
        }
    }

    void UpdateDisplay()
    {
        if (timerText != null)
        {
            if (PlayerController.gameEnded)
            {
                timerText.text = playerWon ? "YOU WIN!" : "YOU LOSE!";
            }
            else
            {
                timerText.text = Mathf.CeilToInt(currentTime).ToString();
            }
        }
    }
}