using UnityEngine;
using Firebase.Database;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

/// <summary>
/// מנהל רישום צעדים במצב PVP
/// רושם כל צעד של שחקן (מארח או אורח) לשרת Firebase
/// </summary>
public class PvPMoveLogger : MonoBehaviour
{
    public static PvPMoveLogger Instance;

    private string currentRoomId;
    private bool isHost;
    private DatabaseReference roomRef;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// אתחול הלוגר עם פרטי החדר
    /// </summary>
    public void Initialize(string roomId, bool isHostPlayer)
    {
        currentRoomId = roomId;
        isHost = isHostPlayer;

        if (!string.IsNullOrEmpty(currentRoomId))
        {
            roomRef = FirebaseManager.Instance.DatabaseReference.Child("rooms").Child(currentRoomId);
            UnityEngine.Debug.Log($"[PvPMoveLogger] Initialized for room {roomId}, isHost: {isHost}");
        }
    }

    /// <summary>
    /// רושם צעד של שחקן לשרת בשדה "nextStep"
    /// </summary>
    /// <param name="fromPosition">המיקום שממנו זז החייל</param>
    /// <param name="toPosition">המיקום שאליו זז החייל</param>
    public async void LogPlayerMove(Vector2Int fromPosition, Vector2Int toPosition)
    {
        if (string.IsNullOrEmpty(currentRoomId) || roomRef == null)
        {
            UnityEngine.Debug.LogWarning("[PvPMoveLogger] Cannot log move - room not initialized");
            return;
        }

        try
        {
            // קביעת מי עשה את הצעד
            string playerType = isHost ? "מארח" : "אורח";

            // יצירת מחרוזת המתארת את הצעד
            string moveDescription = $"{playerType} זז מ({fromPosition.x},{fromPosition.y}) ל({toPosition.x},{toPosition.y})";

            // עדכון השדה nextStep בשרת
            var updates = new Dictionary<string, object>
            {
                { "nextStep", moveDescription }
            };

            await roomRef.UpdateChildrenAsync(updates);

            // עדכון מצב הלוח בשרת
            await UpdateBoardState(fromPosition, toPosition);

            UnityEngine.Debug.Log($"[PvPMoveLogger]  רישום צעד הושלם: {moveDescription}");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[PvPMoveLogger]  שגיאה ברישום הצעד: {ex.Message}");
        }
    }

    /// <summary>
    /// מעדכן את מצב הלוח בשרת לאחר צעד
    /// </summary>
    /// <param name="fromPosition">המיקום שממנו זז החייל</param>
    /// <param name="toPosition">המיקום שאליו זז החייל</param>
    private async Task UpdateBoardState(Vector2Int fromPosition, Vector2Int toPosition)
    {
        try
        {
            // מציאת החייל שזז
            RPSUnit movingUnit = FindUnitAtPosition(toPosition);
            if (movingUnit == null)
            {
                UnityEngine.Debug.LogWarning("[PvPMoveLogger] Could not find moving unit for board state update");
                return;
            }

            // עדכון מצב הלוח
            var boardUpdates = new Dictionary<string, object>();

            // עדכון המשבצת המקורית להיות ריקה
            string fromKey = $"boardState/{fromPosition.x}_{fromPosition.y}";
            var emptyState = new Dictionary<string, object>
            {
                { "player", "none" },
                { "role", "None" },
                { "kind", "None" }
            };
            boardUpdates[fromKey] = emptyState;

            // עדכון המשבצת החדשה עם פרטי החייל
            string toKey = $"boardState/{toPosition.x}_{toPosition.y}";
            string playerName = isHost ? "host" : "guest";
            var unitState = new Dictionary<string, object>
            {
                { "player", playerName },
                { "role", movingUnit.role.ToString() },
                { "kind", movingUnit.Kind.ToString() }
            };
            boardUpdates[toKey] = unitState;

            // שליחת העדכון לשרת
            await roomRef.UpdateChildrenAsync(boardUpdates);

            UnityEngine.Debug.Log($"[PvPMoveLogger] עדכון מצב לוח הושלם: {fromPosition} -> {toPosition}");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[PvPMoveLogger] שגיאה בעדכון מצב הלוח: {ex.Message}");
        }
    }

    /// <summary>
    /// מוצא יחידה במיקום מסוים
    /// </summary>
    private RPSUnit FindUnitAtPosition(Vector2Int position)
    {
        RPSUnit[] allUnits = FindObjectsOfType<RPSUnit>();
        foreach (var unit in allUnits)
        {
            if (unit.Position.x == position.x && unit.Position.y == position.y)
            {
                return unit;
            }
        }
        return null;
    }
}