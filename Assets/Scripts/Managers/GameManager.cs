using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public List<RPSUnit> Player1Units { get; private set; }
    public List<RPSUnit> Player2Units { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void SetPlayersUnits(List<RPSUnit> p1Units, List<RPSUnit> p2Units)
    {
        Player1Units = p1Units;
        Player2Units = p2Units;

        Debug.Log($"✅ GameManager set {p1Units.Count} units for Player 1 and {p2Units.Count} for Player 2.");
    }

    public List<RPSUnit> GetEnemyUnits(int forPlayerId)
    {
        return forPlayerId == 1 ? Player2Units : Player1Units;
    }
}
