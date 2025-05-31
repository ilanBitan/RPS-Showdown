using UnityEngine;
using System.Collections;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    private bool isPlayer1Turn = true;
    private bool gameActive = true;
    private static bool lastGameStartedWithPlayer1 = true;
    private static bool isFirstGame = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        if (isFirstGame)
        {
            // במשחק ראשון - הגרלה
            isPlayer1Turn = UnityEngine.Random.Range(0, 2) == 0;
            lastGameStartedWithPlayer1 = isPlayer1Turn;
            isFirstGame = false;
        }
        else
        {
            // במשחקים הבאים - הפוך ממי שהתחיל קודם
            isPlayer1Turn = !lastGameStartedWithPlayer1;
            lastGameStartedWithPlayer1 = isPlayer1Turn;
        }
    }

    public void StartPlayerTurn()
    {
        if (!gameActive) return;
        isPlayer1Turn = true;
        TurnTimerManager.Instance?.StartTurn();
    }

    public void StartAITurn()
    {
        if (!gameActive) return;
        isPlayer1Turn = false;
        TurnTimerManager.Instance?.StartTurn();
    }

    public void EndTurn()
    {
        if (!gameActive) return;
        isPlayer1Turn = !isPlayer1Turn;
        TurnTimerManager.Instance?.StartTurn();

        if (!isPlayer1Turn)
        {
            AIPlayerController ai = FindObjectOfType<AIPlayerController>();
            if (ai != null)
            {
                StartCoroutine(StartAITurnWithDelay(ai));
            }
        }
    }

    private IEnumerator StartAITurnWithDelay(AIPlayerController ai)
    {
        yield return new WaitForSeconds(0.5f);
        ai.PlayTurn();
    }

    public bool IsPlayerTurn(int playerId)
    {
        return gameActive && ((playerId == 1 && isPlayer1Turn) || (playerId == 2 && !isPlayer1Turn));
    }

    public void StopGame()
    {
        gameActive = false;
        TurnTimerManager.Instance?.StopTimer();
    }

    public void StartDuel(RPSUnit unit1, RPSUnit unit2)
    {
        int winner = UnityEngine.Random.Range(0, 2);

        if (winner == 0)
        {
            BoardManager.Instance.RemoveUnit(unit2);
            Destroy(unit2.gameObject);
            unit1.MoveTo(unit2.Position);
        }
        else
        {
            BoardManager.Instance.RemoveUnit(unit1);
            Destroy(unit1.gameObject);
        }

        EndTurn();
    }
}