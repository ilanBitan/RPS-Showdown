//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using UnityEngine;
//using Firebase.Database;

//public class PvPSetupManager : MonoBehaviour
//{
//    private List<RPSUnit> player1Units;
//    private List<RPSUnit> player2Units;
//    private int selectionStep = 0;
//    private bool isHostTurn = true;
//    private string currentRoomId;
//    private bool isHost;
//    private DatabaseReference roomRef;
//    private bool isListening = false;

//    public void Initialize(List<RPSUnit> p1Units, List<RPSUnit> p2Units, string roomId, bool isHostPlayer)
//    {
//        player1Units = p1Units;
//        player2Units = p2Units;
//        currentRoomId = roomId;
//        isHost = isHostPlayer;
//        selectionStep = 0;
//        isHostTurn = true;

//        SetupInitialSelections();
//        if (!isHost)
//        {
//            StartListeningForHostSelections();
//        }
//    }

//    private void SetupInitialSelections()
//    {
//        if (isHost)
//        {
//            // Host can only select in bottom rows
//            foreach (var unit in player1Units)
//            {
//                if (unit.Position.y >= 4) // Bottom rows
//                {
//                    unit.EnableSetupSelection();
//                }
//                else
//                {
//                    unit.DisableSetupSelection();
//                }
//                unit.ResetVisual();
//            }
//            foreach (var unit in player2Units)
//            {
//                unit.DisableSetupSelection();
//                unit.ResetVisual();
//            }
//            UnityEngine.Debug.Log($"[GameSetup] Setup started. You are the HOST. Select FLAG for your pieces (bottom rows). Room ID: {currentRoomId}");
//        }
//        else
//        {
//            // Guest can only select in top rows
//            foreach (var unit in player1Units)
//            {
//                unit.DisableSetupSelection();
//                unit.ResetVisual();
//            }
//            foreach (var unit in player2Units)
//            {
//                if (unit.Position.y < 2) // Top rows
//                {
//                    unit.EnableSetupSelection();
//                }
//                else
//                {
//                    unit.DisableSetupSelection();
//                }
//                unit.ResetVisual();
//            }
//            UnityEngine.Debug.Log($"[GameSetup] Setup started. You are the GUEST. Waiting for host to complete their setup. Room ID: {currentRoomId}");
//        }
//    }

//    private async void StartListeningForHostSelections()
//    {
//        if (string.IsNullOrEmpty(currentRoomId)) return;

//        roomRef = FirebaseManager.Instance.DatabaseReference.Child("rooms").Child(currentRoomId);

//        try
//        {
//            DataSnapshot snapshot = await roomRef.GetValueAsync();
//            if (snapshot.Exists)
//            {
//                string hostId = snapshot.Child("hostId").Value?.ToString();
//                string guestId = snapshot.Child("guestId").Value?.ToString();
//                string currentUserId = FirebaseManager.Instance.CurrentUser.UserId;

//                isHost = currentUserId == hostId;

//                UnityEngine.Debug.Log($"[GameSetup] Server role verification:");
//                UnityEngine.Debug.Log($"[GameSetup] - Your User ID: {currentUserId}");
//                UnityEngine.Debug.Log($"[GameSetup] - Host ID: {hostId}");
//                UnityEngine.Debug.Log($"[GameSetup] - Guest ID: {guestId}");
//                UnityEngine.Debug.Log($"[GameSetup] - You are the: {(isHost ? "HOST" : "GUEST")}");

//                roomRef.ValueChanged += HandleRoomValueChanged;
//                isListening = true;
//                UnityEngine.Debug.Log("[GameSetup] Started listening for host's selections");
//            }
//        }
//        catch (Exception ex)
//        {
//            UnityEngine.Debug.LogError($"[GameSetup] Failed to verify role: {ex.Message}");
//        }
//    }

//    private void HandleRoomValueChanged(object sender, ValueChangedEventArgs args)
//    {
//        if (args.DatabaseError != null)
//        {
//            UnityEngine.Debug.LogError($"[GameSetup] Database error: {args.DatabaseError}");
//            return;
//        }

//        DataSnapshot snapshot = args.Snapshot;
//        if (!snapshot.Exists) return;

//        string hostFlagPos = snapshot.Child("hostFlagPosition").Value?.ToString();
//        string hostTrapPos = snapshot.Child("hostTrapPosition").Value?.ToString();
//        string guestFlagPos = snapshot.Child("guestFlagPosition").Value?.ToString();
//        string guestTrapPos = snapshot.Child("guestTrapPosition").Value?.ToString();
//        string currentPhase = snapshot.Child("currentSetupPhase").Value?.ToString();

//        UnityEngine.Debug.Log($"[GameSetup] Received server update:");
//        UnityEngine.Debug.Log($"[GameSetup] - Host Flag: {hostFlagPos}");
//        UnityEngine.Debug.Log($"[GameSetup] - Host Trap: {hostTrapPos}");
//        UnityEngine.Debug.Log($"[GameSetup] - Guest Flag: {guestFlagPos}");
//        UnityEngine.Debug.Log($"[GameSetup] - Guest Trap: {guestTrapPos}");
//        UnityEngine.Debug.Log($"[GameSetup] - Current Phase: {currentPhase}");

//        UnityMainThreadDispatcher.Instance().Enqueue(() =>
//        {
//            if (!string.IsNullOrEmpty(hostFlagPos))
//            {
//                UpdateUnitFromPosition(hostFlagPos, RPSUnit.UnitRole.Flag);
//            }
//            if (!string.IsNullOrEmpty(hostTrapPos))
//            {
//                UpdateUnitFromPosition(hostTrapPos, RPSUnit.UnitRole.Trap);
//            }
//            if (!string.IsNullOrEmpty(guestFlagPos))
//            {
//                UpdateUnitFromPosition(guestFlagPos, RPSUnit.UnitRole.Flag);
//            }
//            if (!string.IsNullOrEmpty(guestTrapPos))
//            {
//                UpdateUnitFromPosition(guestTrapPos, RPSUnit.UnitRole.Trap);
//            }

//            if (currentPhase == "guest" && !isHost)
//            {
//                isHostTurn = false;
//                selectionStep = 2;
//                foreach (var unit in player2Units)
//                {
//                    if (unit.Position.y < 2)
//                    {
//                        unit.EnableSetupSelection();
//                    }
//                }
//                UnityEngine.Debug.Log($"[GameSetup] Host completed setup. Your turn to select FLAG. Selection step: {selectionStep}");
//            }
//            else if (currentPhase == "complete")
//            {
//                var boardState = snapshot.Child("boardState");
//                if (boardState.Exists)
//                {
//                    foreach (var unitState in boardState.Children)
//                    {
//                        string position = unitState.Key;
//                        string player = unitState.Child("player").Value?.ToString();
//                        string role = unitState.Child("role").Value?.ToString();
//                        string kind = unitState.Child("kind").Value?.ToString();

//                        if (!string.IsNullOrEmpty(position) && !string.IsNullOrEmpty(player) && !string.IsNullOrEmpty(role) && !string.IsNullOrEmpty(kind))
//                        {
//                            string[] coords = position.Split('_');
//                            if (coords.Length == 2)
//                            {
//                                int x = int.Parse(coords[0]);
//                                int y = int.Parse(coords[1]);
//                                RPSUnit unit = FindUnitAtPosition(x, y);
//                                if (unit != null)
//                                {
//                                    unit.role = (RPSUnit.UnitRole)Enum.Parse(typeof(RPSUnit.UnitRole), role);
//                                    unit.Kind = (RPSUnit.RPSKind)Enum.Parse(typeof(RPSUnit.RPSKind), kind);
//                                    unit.UpdateVisual();
//                                }
//                            }
//                        }
//                    }
//                }
//                GameSetupManager.Instance.FinalizeSetup();
//            }
//        });
//    }

//    private void UpdateUnitFromPosition(string position, RPSUnit.UnitRole role)
//    {
//        string[] coords = position.Split('_');
//        if (coords.Length != 2) return;

//        int x = int.Parse(coords[0]);
//        int y = int.Parse(coords[1]);

//        RPSUnit unit = FindUnitAtPosition(x, y);
//        if (unit != null)
//        {
//            unit.role = role;
//            unit.UpdateVisual();
//            UnityEngine.Debug.Log($"[GameSetup] Updated unit at {position} to {role}");
//        }
//    }

//    private RPSUnit FindUnitAtPosition(int x, int y)
//    {
//        foreach (var unit in player1Units)
//        {
//            if (unit.Position.x == x && unit.Position.y == y)
//                return unit;
//        }
//        foreach (var unit in player2Units)
//        {
//            if (unit.Position.x == x && unit.Position.y == y)
//                return unit;
//        }
//        return null;
//    }

//    public async void OnUnitClicked(RPSUnit unit)
//    {
//        if (unit == null) return;

//        Vector2Int unitPosition = unit.Position;
//        bool isHostUnit = unitPosition.y >= 4;

//        if (isHostTurn != isHostUnit)
//        {
//            UnityEngine.Debug.Log($"[GameSetup] Please wait for the {(isHostTurn ? "host" : "guest")} to finish their selection.");
//            return;
//        }

//        if ((isHost && unitPosition.y < 4) || (!isHost && unitPosition.y >= 2))
//        {
//            UnityEngine.Debug.Log($"[GameSetup] You can only select pieces in your designated rows ({(isHost ? "bottom" : "top")} rows).");
//            return;
//        }

//        unit.DisableSetupSelection();

//        string position = $"{unitPosition.x}_{unitPosition.y}";
//        Dictionary<string, object> updates = new Dictionary<string, object>();

//        try
//        {
//            if (isHost)
//            {
//                await HandleHostSelection(unit, position, updates);
//            }
//            else
//            {
//                await HandleGuestSelection(unit, position, updates);
//            }
//        }
//        catch (Exception ex)
//        {
//            UnityEngine.Debug.LogError($"[GameSetup] Failed to update selection: {ex.Message}");
//            if (unit != null)
//            {
//                unit.EnableSetupSelection();
//            }
//        }
//    }

//    private async Task HandleHostSelection(RPSUnit unit, string position, Dictionary<string, object> updates)
//    {
//        switch (selectionStep)
//        {
//            case 0:
//                if (unit.Position.y < 4) return;
//                updates["hostFlagPosition"] = position;
//                updates["currentSetupPhase"] = "host_flag_selected";

//                unit.role = RPSUnit.UnitRole.Flag;
//                unit.UpdateVisual();

//                await UpdateRoomSetup(updates);

//                selectionStep = 1;
//                UnityEngine.Debug.Log($"[GameSetup] Host Flag confirmed at {position}");

//                foreach (var p1Unit in player1Units)
//                {
//                    if (p1Unit != null && p1Unit.Position.y >= 4 && p1Unit.role == RPSUnit.UnitRole.None)
//                    {
//                        p1Unit.EnableSetupSelection();
//                    }
//                }
//                break;

//            case 1:
//                if (unit.Position.y < 4) return;
//                if (unit.role == RPSUnit.UnitRole.Flag)
//                {
//                    UnityEngine.Debug.Log($"[GameSetup] This unit is already a flag.");
//                    unit.EnableSetupSelection();
//                    return;
//                }

//                updates["hostTrapPosition"] = position;
//                updates["hostSetupComplete"] = true;
//                updates["currentSetupPhase"] = "guest";

//                unit.role = RPSUnit.UnitRole.Trap;
//                unit.UpdateVisual();

//                await UpdateRoomSetup(updates);

//                selectionStep = 2;
//                isHostTurn = false;

//                DisableAllSelections();
//                foreach (var p2Unit in player2Units)
//                {
//                    if (p2Unit != null && p2Unit.Position.y < 2)
//                    {
//                        p2Unit.EnableSetupSelection();
//                    }
//                }

//                if (roomRef == null)
//                {
//                    roomRef = FirebaseManager.Instance.DatabaseReference.Child("rooms").Child(currentRoomId);
//                }
//                if (!isListening)
//                {
//                    roomRef.ValueChanged += HandleRoomValueChanged;
//                    isListening = true;
//                    UnityEngine.Debug.Log("[GameSetup] Host completed setup. Listening for guest's selections.");
//                }
//                break;
//        }
//    }

//    private async Task HandleGuestSelection(RPSUnit unit, string position, Dictionary<string, object> updates)
//    {
//        switch (selectionStep)
//        {
//            case 2:
//                if (unit.Position.y >= 2) return;
//                updates["guestFlagPosition"] = position;

//                unit.role = RPSUnit.UnitRole.Flag;
//                unit.UpdateVisual();

//                await UpdateRoomSetup(updates);

//                selectionStep = 3;
//                UnityEngine.Debug.Log($"[GameSetup] Guest Flag confirmed at {position}");

//                foreach (var p2Unit in player2Units)
//                {
//                    if (p2Unit != null && p2Unit.Position.y < 2 && p2Unit.role == RPSUnit.UnitRole.None)
//                    {
//                        p2Unit.EnableSetupSelection();
//                    }
//                }
//                break;

//            case 3:
//                if (unit.Position.y >= 2) return;
//                if (unit.role == RPSUnit.UnitRole.Flag)
//                {
//                    UnityEngine.Debug.Log($"[GameSetup] This unit is already a flag.");
//                    unit.EnableSetupSelection();
//                    return;
//                }

//                updates["guestFlagPosition"] = position;
//                updates["guestSetupComplete"] = true;
//                updates["currentSetupPhase"] = "complete";
//                updates["status"] = "playing";

//                unit.role = RPSUnit.UnitRole.Trap;
//                unit.UpdateVisual();

//                await UpdateRoomSetup(updates);

//                await SaveBoardStateToServer();

//                UnityEngine.Debug.Log($"[GameSetup] Guest Trap confirmed. Setup complete!");
//                GameSetupManager.Instance.FinalizeSetup();
//                break;
//        }
//    }

//    private async Task UpdateRoomSetup(Dictionary<string, object> updates)
//    {
//        if (string.IsNullOrEmpty(currentRoomId))
//        {
//            UnityEngine.Debug.LogError("[GameSetup] Cannot update server: currentRoomId is empty");
//            return;
//        }

//        try
//        {
//            UnityEngine.Debug.Log($"[GameSetup] Attempting to update server for room {currentRoomId}");
//            UnityEngine.Debug.Log($"[GameSetup] Updates to send: {string.Join(", ", updates.Select(kv => $"{kv.Key}={kv.Value}"))}");

//            var roomRef = FirebaseManager.Instance.DatabaseReference
//                .Child("rooms")
//                .Child(currentRoomId);

//            var snapshot = await roomRef.GetValueAsync();
//            if (!snapshot.Exists)
//            {
//                UnityEngine.Debug.LogError($"[GameSetup] Room {currentRoomId} does not exist in database");
//                return;
//            }

//            await roomRef.UpdateChildrenAsync(updates);
//            UnityEngine.Debug.Log("[GameSetup] Server update successful");

//            var verifySnapshot = await roomRef.GetValueAsync();
//            foreach (var update in updates)
//            {
//                var value = verifySnapshot.Child(update.Key).Value?.ToString();
//                UnityEngine.Debug.Log($"[GameSetup] Verifying update - {update.Key}: Expected={update.Value}, Actual={value}");
//            }
//        }
//        catch (Exception ex)
//        {
//            UnityEngine.Debug.LogError($"[GameSetup] Failed to update room setup: {ex.Message}");
//            UnityEngine.Debug.LogError($"[GameSetup] Stack trace: {ex.StackTrace}");
//            throw;
//        }
//    }

//    private async Task SaveBoardStateToServer()
//    {
//        try
//        {
//            GameSetupManager.Instance.AssignRandomRPS(player1Units);
//            GameSetupManager.Instance.AssignRandomRPS(player2Units);

//            Dictionary<string, object> boardState = new Dictionary<string, object>();

//            foreach (var unit in player1Units)
//            {
//                string position = $"{unit.Position.x}_{unit.Position.y}";
//                Dictionary<string, object> unitState = new Dictionary<string, object>
//                {
//                    { "player", "host" },
//                    { "role", unit.role.ToString() },
//                    { "kind", unit.Kind.ToString() }
//                };
//                boardState[position] = unitState;
//            }

//            foreach (var unit in player2Units)
//            {
//                string position = $"{unit.Position.x}_{unit.Position.y}";
//                Dictionary<string, object> unitState = new Dictionary<string, object>
//                {
//                    { "player", "guest" },
//                    { "role", unit.role.ToString() },
//                    { "kind", unit.Kind.ToString() }
//                };
//                boardState[position] = unitState;
//            }

//            for (int x = 0; x < 7; x++)
//            {
//                for (int y = 2; y < 4; y++)
//                {
//                    string position = $"{x}_{y}";
//                    Dictionary<string, object> emptyState = new Dictionary<string, object>
//                    {
//                        { "player", "none" },
//                        { "role", "None" },
//                        { "kind", "None" }
//                    };
//                    boardState[position] = emptyState;
//                }
//            }

//            var updates = new Dictionary<string, object>
//            {
//                { "boardState", boardState }
//            };

//            await UpdateRoomSetup(updates);
//            UnityEngine.Debug.Log("[GameSetup] Board state saved to server");

//            foreach (var unit in player1Units) unit.UpdateVisual();
//            foreach (var unit in player2Units) unit.UpdateVisual();
//        }
//        catch (Exception ex)
//        {
//            UnityEngine.Debug.LogError($"[GameSetup] Failed to save board state: {ex.Message}");
//        }
//    }

//    private void DisableAllSelections()
//    {
//        foreach (var unit in player1Units) unit.DisableSetupSelection();
//        foreach (var unit in player2Units) unit.DisableSetupSelection();
//    }

//    private void OnDestroy()
//    {
//        if (isListening && roomRef != null)
//        {
//            roomRef.ValueChanged -= HandleRoomValueChanged;
//            isListening = false;
//        }
//    }
//}