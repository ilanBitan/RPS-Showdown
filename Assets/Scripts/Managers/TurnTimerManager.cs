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