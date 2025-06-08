//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using Firebase.Database;
//using System.Threading.Tasks;

//public class PvPTurnManager : MonoBehaviour
//{
//    public static PvPTurnManager Instance;

//    private bool gameActive = true;
//    private string currentRoomId;
//    private bool isHost;
//    private DatabaseReference roomRef;
//    private bool isListening = false;
//    private bool isMyTurn = false;

//    private void Awake()
//    {
//        if (Instance != null && Instance != this)
//        {
//            Destroy(gameObject);
//            return;
//        }

//        Instance = this;
//        DontDestroyOnLoad(gameObject);
//    }

//    public void InitializePvPTurns(string roomId, bool isHostPlayer)
//    {
//        currentRoomId = roomId;
//        isHost = isHostPlayer;

//        UnityEngine.Debug.Log($"[PvPTurnManager] Initializing PvP turns for room: {roomId}, isHost: {isHost}");

//        StartListeningForTurnChanges();
//    }

//    private async void StartListeningForTurnChanges()
//    {
//        if (string.IsNullOrEmpty(currentRoomId)) return;

//        try
//        {
//            roomRef = FirebaseManager.Instance.DatabaseReference.Child("rooms").Child(currentRoomId);

//            // Set initial turn state - host always starts first
//            if (isHost)
//            {
//                var updates = new Dictionary<string, object>
//                {
//                    { "currentTurn", "host" },
//                    { "turnNumber", 1 }
//                };
//                await roomRef.UpdateChildrenAsync(updates);
//                isMyTurn = true;
//                UnityEngine.Debug.Log("[PvPTurnManager] Host's turn - You can play!");
//            }
//            else
//            {
//                isMyTurn = false;
//                UnityEngine.Debug.Log("[PvPTurnManager] Waiting for host to play...");
//            }

//            // Start listening for turn changes
//            roomRef.ValueChanged += HandleTurnChanged;
//            isListening = true;

//            // Start the turn timer
//            TurnTimerManager.Instance?.StartTurn();

//            UnityEngine.Debug.Log("[PvPTurnManager] Started listening for turn changes");
//        }
//        catch (Exception ex)
//        {
//            UnityEngine.Debug.LogError($"[PvPTurnManager] Failed to initialize turn listening: {ex.Message}");
//        }
//    }

//    private void HandleTurnChanged(object sender, ValueChangedEventArgs args)
//    {
//        if (args.DatabaseError != null)
//        {
//            UnityEngine.Debug.LogError($"[PvPTurnManager] Database error: {args.DatabaseError}");
//            return;
//        }

//        DataSnapshot snapshot = args.Snapshot;
//        if (!snapshot.Exists) return;

//        string currentTurn = snapshot.Child("currentTurn").Value?.ToString();
//        int turnNumber = 0;
//        if (snapshot.Child("turnNumber").Value != null)
//        {
//            int.TryParse(snapshot.Child("turnNumber").Value.ToString(), out turnNumber);
//        }

//        UnityEngine.Debug.Log($"[PvPTurnManager] Turn update - Current turn: {currentTurn}, Turn number: {turnNumber}");

//        // Update turn state on main thread
//        UnityMainThreadDispatcher.Instance().Enqueue(() =>
//        {
//            bool newIsMyTurn = (isHost && currentTurn == "host") || (!isHost && currentTurn == "guest");

//            if (newIsMyTurn != isMyTurn)
//            {
//                isMyTurn = newIsMyTurn;

//                if (isMyTurn)
//                {
//                    UnityEngine.Debug.Log($"[PvPTurnManager] Your turn! (Turn #{turnNumber})");
//                    TurnTimerManager.Instance?.StartTurn();
//                }
//                else
//                {
//                    UnityEngine.Debug.Log($"[PvPTurnManager] Waiting for opponent... (Turn #{turnNumber})");
//                }
//            }
//        });
//    }

//    public bool IsMyTurn()
//    {
//        return gameActive && isMyTurn;
//    }

//    public bool IsPlayerTurn(int playerId)
//    {
//        // In PvP mode:
//        // Player 1 = Host (playerId 1), Player 2 = Guest (playerId 2)
//        if (isHost)
//        {
//            return gameActive && isMyTurn && playerId == 1;
//        }
//        else
//        {
//            return gameActive && isMyTurn && playerId == 2;
//        }
//    }

//    public async void EndTurn()
//    {
//        if (!gameActive || !isMyTurn) return;

//        try
//        {
//            // Switch turns
//            string nextTurn = isHost ? "guest" : "host";

//            // Get current turn number and increment it
//            var snapshot = await roomRef.GetValueAsync();
//            int currentTurnNumber = 1;
//            if (snapshot.Child("turnNumber").Value != null)
//            {
//                int.TryParse(snapshot.Child("turnNumber").Value.ToString(), out currentTurnNumber);
//            }

//            var updates = new Dictionary<string, object>
//            {
//                { "currentTurn", nextTurn },
//                { "turnNumber", currentTurnNumber + 1 }
//            };

//            await roomRef.UpdateChildrenAsync(updates);

//            UnityEngine.Debug.Log($"[PvPTurnManager] Turn ended. Next turn: {nextTurn}");

//            // Update local state
//            isMyTurn = false;
//        }
//        catch (Exception ex)
//        {
//            UnityEngine.Debug.LogError($"[PvPTurnManager] Failed to end turn: {ex.Message}");
//        }
//    }

//    public void StopGame()
//    {
//        gameActive = false;
//        TurnTimerManager.Instance?.StopTimer();
//        UnityEngine.Debug.Log("[PvPTurnManager] Game stopped");
//    }

//    public void StartDuel(RPSUnit unit1, RPSUnit unit2)
//    {
//        // Handle duel logic (same as original TurnManager)
//        int winner = UnityEngine.Random.Range(0, 2);

//        if (winner == 0)
//        {
//            BoardManager.Instance.RemoveUnit(unit2);
//            Destroy(unit2.gameObject);
//            unit1.MoveTo(unit2.Position);
//        }
//        else
//        {
//            BoardManager.Instance.RemoveUnit(unit1);
//            Destroy(unit1.gameObject);
//        }

//        // End turn after duel
//        EndTurn();
//    }

//    private void OnDestroy()
//    {
//        if (isListening && roomRef != null)
//        {
//            roomRef.ValueChanged -= HandleTurnChanged;
//            isListening = false;
//        }
//    }

//    // Method to get current player type for UI/debugging
//    public string GetPlayerType()
//    {
//        return isHost ? "Host" : "Guest";
//    }

//    // Method to check if it's the first turn (for special logic if needed)
//    public async Task<bool> IsFirstTurn()
//    {
//        try
//        {
//            var snapshot = await roomRef.GetValueAsync();
//            int turnNumber = 1;
//            if (snapshot.Child("turnNumber").Value != null)
//            {
//                int.TryParse(snapshot.Child("turnNumber").Value.ToString(), out turnNumber);
//            }
//            return turnNumber == 1;
//        }
//        catch
//        {
//            return true;
//        }
//    }
//}
