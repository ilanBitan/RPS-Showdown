using UnityEngine;
using TMPro;
using System.Diagnostics;

public class TurnTimerManager : MonoBehaviour
{
    public static TurnTimerManager Instance;

    public TextMeshProUGUI timerText;
    public float turnDuration = 10f;

    private float currentTime;
    private bool timerRunning = false;
    private bool activePhaseStarted = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
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

    private void Update()
    {
        if (!timerRunning || !activePhaseStarted)
            return;

        if (BattleManager.Instance != null && BattleManager.Instance.IsBattleActive())
            return;

        if (PlayerController.gameEnded)
        {
            // אם המשחק נגמר - להציג הודעה במקום טיימר
            if (timerText != null)
            {
                if (TurnManager.Instance.IsPlayerTurn(1))
                    timerText.text = "YOU WIN";
                else
                    timerText.text = "YOU LOST";
            }
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
            timerText.text = Mathf.CeilToInt(currentTime).ToString();
    }
}
//using UnityEngine;
//using TMPro;
//using System.Diagnostics;

//public class TurnTimerManager : MonoBehaviour
//{
//    public static TurnTimerManager Instance;

//    public TextMeshProUGUI timerText;
//    public float turnDuration = 10f;

//    private float currentTime;
//    private bool timerRunning = false;
//    private bool activePhaseStarted = false;
//    private FirebaseDatabaseService dbService;

//    private void Awake()
//    {
//        if (Instance != null && Instance != this)
//            Destroy(gameObject);
//        else
//            Instance = this;

//        // Initialize database service
//        dbService = new FirebaseDatabaseService(FirebaseManager.Instance);
//    }

//    public void ActivateGameTimer()
//    {
//        activePhaseStarted = true;
//    }

//    public void StartTurn()
//    {
//        if (!activePhaseStarted) return;

//        currentTime = turnDuration;
//        timerRunning = true;
//        UpdateDisplay();
//    }

//    public void StopTimer()
//    {
//        timerRunning = false;
//    }

//    private void Update()
//    {
//        if (!timerRunning || !activePhaseStarted)
//            return;

//        if (BattleManager.Instance != null && BattleManager.Instance.IsBattleActive())
//            return;

//        if (PlayerController.gameEnded)
//        {
//            // אם המשחק נגמר - לעדכן סטטיסטיקות ולשנות את הטקסט
//            if (timerText != null)
//            {
//                if (TurnManager.Instance.IsPlayerTurn(1)) // Assuming Player 1 is the local player
//                {
//                    timerText.text = "YOU WIN";
//                    // Get current stats and update them
//                    dbService.GetUserStats((userData) =>
//                    {
//                        if (userData != null)
//                        {
//                            dbService.UpdateUserWins(userData.wins + 1);
//                            dbService.UpdateUserScore(userData.score + 10);
//                        }
//                    });
//                }
//                else
//                {
//                    timerText.text = "YOU LOST";
//                    // Get current stats and update them
//                    dbService.GetUserStats((userData) =>
//                    {
//                        if (userData != null)
//                        {
//                            dbService.UpdateUserLosses(userData.losses + 1);
//                        }
//                    });
//                }
//            }
//            timerRunning = false; // Stop the timer when the game ends
//            return;
//        }

//        currentTime -= Time.deltaTime;
//        UpdateDisplay();

//        if (currentTime <= 0f)
//        {
//            timerRunning = false;
//            UnityEngine.Debug.Log("⏰ Time's up! Auto-switching turn...");
//            TurnManager.Instance?.EndTurn();
//        }
//    }

//    void UpdateDisplay()
//    {
//        if (timerText != null)
//            timerText.text = Mathf.CeilToInt(currentTime).ToString();
//    }
//}