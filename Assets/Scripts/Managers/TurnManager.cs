using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public enum PlayerTurn { Player1, Player2 }
    public PlayerTurn currentTurn = PlayerTurn.Player1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    public bool IsPlayerTurn(int playerId)
    {
        return (playerId == 1 && currentTurn == PlayerTurn.Player1)
            || (playerId == 2 && currentTurn == PlayerTurn.Player2);
    }

    public void EndTurn()
    {
        currentTurn = (currentTurn == PlayerTurn.Player1) ? PlayerTurn.Player2 : PlayerTurn.Player1;
        Debug.Log($"🔄 Turn switched to: {currentTurn}");
        TurnTimerManager.Instance?.StartTurn();
    }
}
