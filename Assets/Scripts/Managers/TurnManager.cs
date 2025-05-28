using System;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    private bool isPlayer1Turn = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    public void StartPlayerTurn()
    {
        TurnTimerManager.Instance?.StartTurn();
    }

    public void StartAITurn()
    {
        TurnTimerManager.Instance?.StartTurn();
        AIPlayerController.Instance?.PlayTurn();
    }

    public void EndTurn()
    {
        TurnTimerManager.Instance?.StopTimer();
        isPlayer1Turn = !isPlayer1Turn;

        if (isPlayer1Turn)
            StartPlayerTurn();
        else
            StartAITurn();
    }

    public bool IsPlayerTurn(int playerId)
    {
        return (playerId == 1 && isPlayer1Turn) || (playerId == 2 && !isPlayer1Turn);
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