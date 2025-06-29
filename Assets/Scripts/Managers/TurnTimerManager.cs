using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TurnTimerManager : MonoBehaviour
{
    public static TurnTimerManager Instance;

    public TextMeshProUGUI timerText;
    public float turnDuration = 10f;
    public GameObject playAgainContainer;
    public TextMeshProUGUI playAgainText;
    public Button playAgainButton;

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

        // Auto-assign playAgainButton if not set
        if (playAgainButton == null && playAgainContainer != null)
        {
            playAgainButton = playAgainContainer.GetComponentInChildren<Button>();
            Debug.Log($"[DEBUG] playAgainButton auto-assigned: {playAgainButton?.gameObject.name}");
        }
        else
        {
            Debug.Log($"[DEBUG] playAgainButton assigned in inspector: {playAgainButton?.gameObject.name}");
        }

        // Make sure the Play Again button is always visible but not interactable at start
        if (playAgainButton != null)
        {
            playAgainButton.interactable = false;
            Debug.Log($"[DEBUG] playAgainButton.interactable = false (Awake)");
        }
        if (playAgainContainer != null)
        {
            playAgainContainer.SetActive(true);
        }
        // Auto-find PlayAgainText if not assigned manually
        if (playAgainText == null && playAgainContainer != null)
        {
            playAgainText = playAgainContainer.GetComponentInChildren<TextMeshProUGUI>();
        }
        // Clear button text at game start
        if (playAgainText != null)
            playAgainText.text = "";
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
        // Update turn button text
        if (TurnManager.Instance != null && playAgainText != null)
        {
            bool isPlayerTurn = TurnManager.Instance.IsPlayerTurn(1); // 1 = always local player
            UpdateTurnButtonText(isPlayerTurn, false);
        }
        UnityEngine.Debug.Log("[TurnTimerManager] Timer started - new turn begins");
    }

    public void StopTimer()
    {
        timerRunning = false;
        UnityEngine.Debug.Log("[TurnTimerManager] Timer stopped");
    }

    public void SetPlayerWon(bool won)
    {
        playerWon = won;
        // Update button text to end game state
        UpdateTurnButtonText(false, true);
        // Update player statistics on server only in PvE games
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

        // Get current statistics from server
        dbService.GetUserStats((userData) => {
            if (userData == null)
            {
                UnityEngine.Debug.LogError("Could not get user data to update statistics");
                return;
            }

            if (playerWon)
            {
                // Update wins
                int newWins = userData.wins + 1;
                dbService.UpdateUserWins(newWins);

                // Update points based on difficulty
                int pointsToAdd = GetPointsForDifficulty();
                int newScore = userData.score + pointsToAdd;
                dbService.UpdateUserScore(newScore);

                UnityEngine.Debug.Log($"Player won! Added {pointsToAdd} points. New wins: {newWins}, New score: {newScore}");
            }
            else
            {
                // Update losses
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
            // If the game ends, show a message instead of the timer
            if (timerText != null)
            {
                timerText.text = playerWon ? "YOU WIN!" : "YOU LOSE!";
            }
            return;
        }

        if (!timerRunning || !activePhaseStarted)
            return;

        if (BattleManager.Instance != null && BattleManager.Instance.IsBattleActive())
        {
            // Timer is paused during battles - this is expected behavior
            return;
        }

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

    public void UpdateTurnButtonText(bool isPlayerTurn, bool isGameEnded)
    {
        // Try to find PlayAgainText dynamically if it's still null
        if (playAgainText == null)
        {
            var found = GameObject.Find("PlayAgainText");
            if (found != null)
            {
                playAgainText = found.GetComponent<TMPro.TextMeshProUGUI>();
                Debug.Log("[DEBUG] playAgainText was null, found and assigned dynamically!");
            }
        }
        if (playAgainButton == null && playAgainContainer != null)
        {
            playAgainButton = playAgainContainer.GetComponentInChildren<Button>();
            Debug.Log($"[DEBUG] playAgainButton was null, auto-assigned: {playAgainButton?.gameObject.name}");
        }
        Debug.Log($"[DEBUG] UpdateTurnButtonText called. isPlayerTurn={isPlayerTurn}, isGameEnded={isGameEnded}");
        if (playAgainText == null)
        {
            Debug.LogError("[DEBUG] playAgainText is STILL NULL after dynamic search!");
            return;
        }
        if (isGameEnded)
        {
            playAgainText.text = "PLAY AGAIN!";
            // Make button interactable only at game end
            if (playAgainButton != null)
            {
                playAgainButton.interactable = true;
                Debug.Log("[DEBUG] playAgainButton.interactable = true (game ended)");
            }
        }
        else
        {
            playAgainText.text = isPlayerTurn ? "YOUR TURN" : "ENEMY TURN";
            // Make button NOT interactable during the game
            if (playAgainButton != null)
            {
                playAgainButton.interactable = false;
                Debug.Log("[DEBUG] playAgainButton.interactable = false (game active)");
            }
        }
        Debug.Log($"[DEBUG] playAgainText.text set to: {playAgainText.text}");
    }
}