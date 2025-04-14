using UnityEngine;

public static class GameLogger
{
    public static void LogMove(Unit unit, Vector2Int from, Vector2Int to)
    {
        Debug.Log($"[{unit.UnitType}] moved from {from} to {to}");
    }

    public static void LogInvalidMove(Unit unit, Vector2Int attemptedPos, string reason)
    {
        Debug.Log($"[{unit.UnitType}] tried to move to {attemptedPos} – FAILED: {reason}");
    }
}
