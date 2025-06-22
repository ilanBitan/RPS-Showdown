using UnityEngine;
using Firebase.Database;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

/// <summary>
/// Handles logging of player moves in PvP matches to Firebase
/// </summary>
public class PvPMoveLogger : MonoBehaviour
{
    public static PvPMoveLogger Instance { get; private set; }

    private string currentRoomId;
    private bool isHost;
    private DatabaseReference roomRef;

    private void Awake()
    {
        // Ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Initialize the logger with room information
    /// </summary>
    public void Initialize(string roomId, bool isHostPlayer)
    {
        currentRoomId = roomId;
        isHost = isHostPlayer;

        if (!string.IsNullOrEmpty(currentRoomId))
        {
            roomRef = FirebaseManager.Instance.DatabaseReference
                .Child("rooms")
                .Child(currentRoomId);

            Debug.Log($"[PvPMoveLogger] Initialized for room {roomId}, Player is {(isHost ? "Host" : "Guest")}");
        }
    }

    /// <summary>
    /// Log a player's move to Firebase
    /// </summary>
    public async void LogPlayerMove(Vector2Int fromPosition, Vector2Int toPosition)
    {
        if (string.IsNullOrEmpty(currentRoomId) || roomRef == null)
        {
            Debug.LogWarning("[PvPMoveLogger] Cannot log move - room not initialized");
            return;
        }

        try
        {
            string playerType = isHost ? "Host" : "Guest";
            string moveDescription = $"{playerType} moved from ({fromPosition.x},{fromPosition.y}) to ({toPosition.x},{toPosition.y})";

            // Update next move in Firebase
            var updates = new Dictionary<string, object>
            {
                { "nextStep", moveDescription },
                { "lastMoveTime", ServerValue.Timestamp }
            };

            await roomRef.UpdateChildrenAsync(updates);
            await UpdateBoardState(fromPosition, toPosition);

            Debug.Log($"[PvPMoveLogger] Logged move: {moveDescription}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PvPMoveLogger] Failed to log move: {ex.Message}");
        }
    }

    /// <summary>
    /// Update the game board state in Firebase after a move
    /// </summary>
    private async Task UpdateBoardState(Vector2Int fromPosition, Vector2Int toPosition)
    {
        try
        {
            RPSUnit movingUnit = FindUnitAtPosition(toPosition);
            if (movingUnit == null)
            {
                Debug.LogWarning("[PvPMoveLogger] Could not find unit at destination position");
                return;
            }

            var boardUpdates = new Dictionary<string, object>
            {
                // Clear the original position
                [$"boardState/{fromPosition.x}_{fromPosition.y}"] = new Dictionary<string, object>
                {
                    { "player", "none" },
                    { "role", "None" },
                    { "kind", "None" }
                },

                // Set the new position
                [$"boardState/{toPosition.x}_{toPosition.y}"] = new Dictionary<string, object>
                {
                    { "player", isHost ? "host" : "guest" },
                    { "role", movingUnit.role.ToString() },
                    { "kind", movingUnit.Kind.ToString() }
                }
            };

            await roomRef.UpdateChildrenAsync(boardUpdates);
            Debug.Log($"[PvPMoveLogger] Updated board state: {fromPosition} -> {toPosition}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PvPMoveLogger] Failed to update board state: {ex.Message}");
        }
    }

    /// <summary>
    /// Find a unit at the specified position
    /// </summary>
    private RPSUnit FindUnitAtPosition(Vector2Int position)
    {
        foreach (var unit in FindObjectsOfType<RPSUnit>())
        {
            if (unit.Position.x == position.x && unit.Position.y == position.y)
            {
                return unit;
            }
        }
        return null;
    }
}