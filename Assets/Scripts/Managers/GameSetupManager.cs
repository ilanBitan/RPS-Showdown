using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Firebase.Database;
using System.Linq;
using System.Threading.Tasks;

public class GameSetupManager : MonoBehaviour
{
    public static GameSetupManager Instance;

    private List<RPSUnit> player1Units;
    private List<RPSUnit> player2Units;
    private int selectionStep = 0;
    private bool setupComplete = false;
    private bool isHostTurn = true;
    private string currentRoomId;
    private bool isHost;
    private DatabaseReference roomRef;
    private bool isListening = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private async void StartListeningForHostSelections()
    {
        if (string.IsNullOrEmpty(currentRoomId)) return;

        roomRef = FirebaseManager.Instance.DatabaseReference.Child("rooms").Child(currentRoomId);

        // First, verify the player's role from the server
        try
        {
            DataSnapshot snapshot = await roomRef.GetValueAsync();
            if (snapshot.Exists)
            {
                string hostId = snapshot.Child("hostId").Value?.ToString();
                string guestId = snapshot.Child("guestId").Value?.ToString();
                string currentUserId = FirebaseManager.Instance.CurrentUser.UserId;

                // Update isHost based on server data
                isHost = currentUserId == hostId;

                UnityEngine.Debug.Log($"[GameSetup] 🔍 Server role verification:");
                UnityEngine.Debug.Log($"[GameSetup] - Your User ID: {currentUserId}");
                UnityEngine.Debug.Log($"[GameSetup] - Host ID: {hostId}");
                UnityEngine.Debug.Log($"[GameSetup] - Guest ID: {guestId}");
                UnityEngine.Debug.Log($"[GameSetup] - You are the: {(isHost ? "HOST" : "GUEST")}");

                // Now start listening for changes
                roomRef.ValueChanged += HandleRoomValueChanged;
                isListening = true;
                UnityEngine.Debug.Log("[GameSetup] 🎯 Started listening for host's selections");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[GameSetup] Failed to verify role: {ex.Message}");
        }
    }

    public async void StartSetup(List<RPSUnit> p1Units, List<RPSUnit> p2Units, string roomId = "", bool isHostPlayer = true)
    {
        // Prevent multiple setup calls
        if (setupComplete)
        {
            UnityEngine.Debug.Log("[GameSetup] Setup already complete, ignoring StartSetup call");
            return;
        }

        // If we're already in setup, only allow if it's a different room
        if (selectionStep > 0 && !string.IsNullOrEmpty(currentRoomId))
        {
            if (currentRoomId == roomId)
            {
                UnityEngine.Debug.Log($"[GameSetup] Already in setup for room {roomId}, ignoring duplicate call");
                return;
            }
            else
            {
                UnityEngine.Debug.Log($"[GameSetup] Switching from room {currentRoomId} to {roomId}");
            }
        }

        UnityEngine.Debug.Log($"[GameSetup] Starting setup with roomId: {roomId}, isHostPlayer: {isHostPlayer}");

        // Reset setup state only if this is the first call
        if (selectionStep == 0)
        {
            selectionStep = 0;
            setupComplete = false;
            isHostTurn = true;
        }

        player1Units = p1Units;
        player2Units = p2Units;
        currentRoomId = roomId;

        // Verify role from server if in PvP mode
        if (GameModeManager.Instance.SelectedMode == GameMode.PvP && !string.IsNullOrEmpty(roomId))
        {
            try
            {
                UnityEngine.Debug.Log($"[GameSetup] 🔍 Verifying role for room: {roomId}");
                DataSnapshot snapshot = await FirebaseManager.Instance.DatabaseReference
                    .Child("rooms")
                    .Child(roomId)
                    .GetValueAsync();

                if (snapshot.Exists)
                {
                    string hostId = snapshot.Child("hostId").Value?.ToString();
                    string currentUserId = FirebaseManager.Instance.CurrentUser.UserId;
                    isHost = currentUserId == hostId;

                    UnityEngine.Debug.Log($"[GameSetup] 🔍 Initial role verification:");
                    UnityEngine.Debug.Log($"[GameSetup] - Your User ID: {currentUserId}");
                    UnityEngine.Debug.Log($"[GameSetup] - Host ID: {hostId}");
                    UnityEngine.Debug.Log($"[GameSetup] - You are the: {(isHost ? "HOST" : "GUEST")}");
                    UnityEngine.Debug.Log($"[GameSetup] - Room ID: {roomId}");
                }
                else
                {
                    UnityEngine.Debug.LogError($"[GameSetup] Room {roomId} not found in database");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GameSetup] Failed to verify initial role: {ex.Message}");
            }
        }
        else
        {
            isHost = isHostPlayer;
        }

        // Enable/disable selections based on role
        if (GameModeManager.Instance.SelectedMode == GameMode.PvP)
        {
            if (isHost)
            {
                // Host can only select in bottom rows
                foreach (var unit in player1Units)
                {
                    if (unit.Position.y >= 4) // Bottom rows
                    {
                        unit.EnableSetupSelection();
                    }
                    else
                    {
                        unit.DisableSetupSelection();
                    }
                    unit.ResetVisual();
                }
                foreach (var unit in player2Units)
                {
                    unit.DisableSetupSelection();
                    unit.ResetVisual();
                }
                UnityEngine.Debug.Log($"[GameSetup] Setup started. You are the HOST. Select FLAG for your pieces (bottom rows). Room ID: {currentRoomId}");
            }
            else
            {
                // Guest can only select in top rows
                foreach (var unit in player1Units)
                {
                    unit.DisableSetupSelection();
                    unit.ResetVisual();
                }
                foreach (var unit in player2Units)
                {
                    if (unit.Position.y < 2) // Top rows
                    {
                        unit.EnableSetupSelection();
                    }
                    else
                    {
                        unit.DisableSetupSelection();
                    }
                    unit.ResetVisual();
                }
                // Start listening for host's selections
                StartListeningForHostSelections();
                UnityEngine.Debug.Log($"[GameSetup] Setup started. You are the GUEST. Waiting for host to complete their setup. Room ID: {currentRoomId}");
            }
        }
        else
        {
            // PvE mode - original behavior
            foreach (var unit in player1Units)
            {
                unit.EnableSetupSelection();
                unit.ResetVisual();
            }
            foreach (var unit in player2Units)
            {
                unit.DisableSetupSelection();
                unit.ResetVisual();
            }
            UnityEngine.Debug.Log("[GameSetup] Setup started. Select FLAG for Player 1.");
        }

        GameManager.Instance?.SetPlayersUnits(player1Units, player2Units);
    }

    private void HandleRoomValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            UnityEngine.Debug.LogError($"[GameSetup] Database error: {args.DatabaseError}");
            return;
        }

        DataSnapshot snapshot = args.Snapshot;
        if (!snapshot.Exists) return;

        // Get all selections
        string hostFlagPos = snapshot.Child("hostFlagPosition").Value?.ToString();
        string hostTrapPos = snapshot.Child("hostTrapPosition").Value?.ToString();
        string guestFlagPos = snapshot.Child("guestFlagPosition").Value?.ToString();
        string guestTrapPos = snapshot.Child("guestTrapPosition").Value?.ToString();
        string currentPhase = snapshot.Child("currentSetupPhase").Value?.ToString();

        UnityEngine.Debug.Log($"[GameSetup] 📡 Received server update:");
        UnityEngine.Debug.Log($"[GameSetup] - Host Flag: {hostFlagPos}");
        UnityEngine.Debug.Log($"[GameSetup] - Host Trap: {hostTrapPos}");
        UnityEngine.Debug.Log($"[GameSetup] - Guest Flag: {guestFlagPos}");
        UnityEngine.Debug.Log($"[GameSetup] - Guest Trap: {guestTrapPos}");
        UnityEngine.Debug.Log($"[GameSetup] - Current Phase: {currentPhase}");

        // Update visuals on main thread
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            // Update host's selections
            if (!string.IsNullOrEmpty(hostFlagPos))
            {
                UpdateUnitFromPosition(hostFlagPos, RPSUnit.UnitRole.Flag);
            }
            if (!string.IsNullOrEmpty(hostTrapPos))
            {
                UpdateUnitFromPosition(hostTrapPos, RPSUnit.UnitRole.Trap);
            }

            // Update guest's selections
            if (!string.IsNullOrEmpty(guestFlagPos))
            {
                UpdateUnitFromPosition(guestFlagPos, RPSUnit.UnitRole.Flag);
            }
            if (!string.IsNullOrEmpty(guestTrapPos))
            {
                UpdateUnitFromPosition(guestTrapPos, RPSUnit.UnitRole.Trap);
            }

            // If host completed setup, enable guest's selection
            if (currentPhase == "guest" && !isHost)
            {
                isHostTurn = false;
                selectionStep = 2; // Set the correct step for guest's turn
                foreach (var unit in player2Units)
                {
                    if (unit.Position.y < 2) // Only enable top rows
                    {
                        unit.EnableSetupSelection();
                    }
                }
                UnityEngine.Debug.Log($"[GameSetup] 🎯 Host completed setup. Your turn to select FLAG. Selection step: {selectionStep}");
            }
            // If setup is complete, update board state from server
            else if (currentPhase == "complete")
            {
                var boardState = snapshot.Child("boardState");
                if (boardState.Exists)
                {
                    foreach (var unitState in boardState.Children)
                    {
                        string position = unitState.Key;
                        string player = unitState.Child("player").Value?.ToString();
                        string role = unitState.Child("role").Value?.ToString();
                        string kind = unitState.Child("kind").Value?.ToString();

                        if (!string.IsNullOrEmpty(position) && !string.IsNullOrEmpty(player) && !string.IsNullOrEmpty(role) && !string.IsNullOrEmpty(kind))
                        {
                            string[] coords = position.Split('_');
                            if (coords.Length == 2)
                            {
                                int x = int.Parse(coords[0]);
                                int y = int.Parse(coords[1]);
                                RPSUnit unit = FindUnitAtPosition(x, y);
                                if (unit != null)
                                {
                                    unit.role = (RPSUnit.UnitRole)Enum.Parse(typeof(RPSUnit.UnitRole), role);
                                    unit.Kind = (RPSUnit.RPSKind)Enum.Parse(typeof(RPSUnit.RPSKind), kind);
                                    unit.UpdateVisual();
                                }
                            }
                        }
                    }
                }
                FinalizeSetup();
            }
        });
    }

    private void UpdateUnitFromPosition(string position, RPSUnit.UnitRole role)
    {
        string[] coords = position.Split('_');
        if (coords.Length != 2) return;

        int x = int.Parse(coords[0]);
        int y = int.Parse(coords[1]);

        // Find the unit at this position
        RPSUnit unit = FindUnitAtPosition(x, y);
        if (unit != null)
        {
            unit.role = role;
            unit.UpdateVisual();
            UnityEngine.Debug.Log($"[GameSetup] 📝 Updated unit at {position} to {role}");
        }
    }

    private RPSUnit FindUnitAtPosition(int x, int y)
    {
        // Search in both player units
        foreach (var unit in player1Units)
        {
            if (unit.Position.x == x && unit.Position.y == y)
                return unit;
        }
        foreach (var unit in player2Units)
        {
            if (unit.Position.x == x && unit.Position.y == y)
                return unit;
        }
        return null;
    }

    private void OnDestroy()
    {
        if (isListening && roomRef != null)
        {
            roomRef.ValueChanged -= HandleRoomValueChanged;
            isListening = false;
        }
    }

    private void DisableAllSelections()
    {
        foreach (var unit in player1Units) unit.DisableSetupSelection();
        foreach (var unit in player2Units) unit.DisableSetupSelection();
    }

    public void OnUnitClicked(RPSUnit unit)
    {
        if (setupComplete || unit == null) return;

        if (GameModeManager.Instance.SelectedMode == GameMode.PvP)
        {
            HandlePvPSelection(unit);
        }
        else
        {
            HandlePvESelection(unit);
        }
    }

    private async Task UpdateRoomSetup(Dictionary<string, object> updates)
    {
        if (string.IsNullOrEmpty(currentRoomId))
        {
            UnityEngine.Debug.LogError("[GameSetup] Cannot update server: currentRoomId is empty");
            return;
        }

        try
        {
            UnityEngine.Debug.Log($"[GameSetup] Attempting to update server for room {currentRoomId}");
            UnityEngine.Debug.Log($"[GameSetup] Updates to send: {string.Join(", ", updates.Select(kv => $"{kv.Key}={kv.Value}"))}");

            var roomRef = FirebaseManager.Instance.DatabaseReference
                .Child("rooms")
                .Child(currentRoomId);

            // First verify the room exists
            var snapshot = await roomRef.GetValueAsync();
            if (!snapshot.Exists)
            {
                UnityEngine.Debug.LogError($"[GameSetup] Room {currentRoomId} does not exist in database");
                return;
            }

            // Perform the update
            await roomRef.UpdateChildrenAsync(updates);
            UnityEngine.Debug.Log("[GameSetup] Server update successful");

            // Verify the update
            var verifySnapshot = await roomRef.GetValueAsync();
            foreach (var update in updates)
            {
                var value = verifySnapshot.Child(update.Key).Value?.ToString();
                UnityEngine.Debug.Log($"[GameSetup] Verifying update - {update.Key}: Expected={update.Value}, Actual={value}");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[GameSetup] Failed to update room setup: {ex.Message}");
            UnityEngine.Debug.LogError($"[GameSetup] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private async void HandlePvPSelection(RPSUnit unit)
    {
        if (unit == null)
        {
            UnityEngine.Debug.LogError("[GameSetup] Unit is null!");
            return;
        }

        UnityEngine.Debug.Log($"[GameSetup] ===== SELECTION STEP TRACKING =====");
        UnityEngine.Debug.Log($"[GameSetup] Current selection step: {selectionStep}, IsHost: {isHost}");
        UnityEngine.Debug.Log($"[GameSetup] Unit position: {unit.Position}, IsHostUnit: {unit.Position.y >= 4}");

        // Store position before any async operations
        Vector2Int unitPosition = unit.Position;
        bool isHostUnit = unitPosition.y >= 4; // Bottom rows (4,5) are host's units

        // Check if it's the correct player's turn
        if (isHostTurn != isHostUnit)
        {
            UnityEngine.Debug.Log($"[GameSetup] Please wait for the {(isHostTurn ? "host" : "guest")} to finish their selection.");
            return;
        }

        // Verify player is selecting in their designated rows
        if ((isHost && unitPosition.y < 4) || (!isHost && unitPosition.y >= 2))
        {
            UnityEngine.Debug.Log($"[GameSetup] You can only select pieces in your designated rows ({(isHost ? "bottom" : "top")} rows).");
            return;
        }

        // Disable selection until server update is complete
        unit.DisableSetupSelection();

        string position = $"{unitPosition.x}_{unitPosition.y}";
        Dictionary<string, object> updates = new Dictionary<string, object>();

        try
        {
            if (isHost)
            {
                switch (selectionStep)
                {
                    case 0: // Host selecting flag
                        if (!isHostUnit) return;
                        updates["hostFlagPosition"] = position;
                        updates["currentSetupPhase"] = "host_flag_selected";

                        // Update the unit before server update
                        unit.role = RPSUnit.UnitRole.Flag;
                        unit.UpdateVisual();

                        await UpdateRoomSetup(updates);

                        // After server update, update the game state
                        selectionStep = 1;
                        UnityEngine.Debug.Log($"[GameSetup] Host Flag confirmed at {position}");

                        // Re-enable selection for remaining units
                        foreach (var p1Unit in player1Units)
                        {
                            if (p1Unit != null && p1Unit.Position.y >= 4 && p1Unit.role == RPSUnit.UnitRole.None)
                            {
                                p1Unit.EnableSetupSelection();
                            }
                        }
                        break;

                    case 1: // Host selecting trap
                        if (!isHostUnit) return;
                        if (unit.role == RPSUnit.UnitRole.Flag)
                        {
                            UnityEngine.Debug.Log($"[GameSetup] This unit is already a flag.");
                            unit.EnableSetupSelection();
                            return;
                        }

                        updates["hostTrapPosition"] = position;
                        updates["hostSetupComplete"] = true;
                        updates["currentSetupPhase"] = "guest";

                        // Update the unit before server update
                        unit.role = RPSUnit.UnitRole.Trap;
                        unit.UpdateVisual();

                        await UpdateRoomSetup(updates);

                        // After server update, update the game state
                        selectionStep = 2;
                        isHostTurn = false;

                        // Enable guest's selection
                        DisableAllSelections();
                        foreach (var p2Unit in player2Units)
                        {
                            if (p2Unit != null && p2Unit.Position.y < 2)
                            {
                                p2Unit.EnableSetupSelection();
                            }
                        }

                        // Keep listening for guest's selections
                        if (roomRef == null)
                        {
                            roomRef = FirebaseManager.Instance.DatabaseReference.Child("rooms").Child(currentRoomId);
                        }
                        if (!isListening)
                        {
                            roomRef.ValueChanged += HandleRoomValueChanged;
                            isListening = true;
                            UnityEngine.Debug.Log("[GameSetup] 🎯 Host completed setup. Listening for guest's selections.");
                        }
                        break;
                }
            }
            else // Guest's turn
            {
                switch (selectionStep)
                {
                    case 2: // Guest selecting flag
                        if (isHostUnit) return;
                        updates["guestFlagPosition"] = position;

                        // Update the unit before server update
                        unit.role = RPSUnit.UnitRole.Flag;
                        unit.UpdateVisual();

                        await UpdateRoomSetup(updates);

                        // After server update, update the game state
                        selectionStep = 3;
                        UnityEngine.Debug.Log($"[GameSetup] Guest Flag confirmed at {position}");

                        // Re-enable selection for remaining units
                        foreach (var p2Unit in player2Units)
                        {
                            if (p2Unit != null && p2Unit.Position.y < 2 && p2Unit.role == RPSUnit.UnitRole.None)
                            {
                                p2Unit.EnableSetupSelection();
                            }
                        }
                        break;

                    case 3: // Guest selecting trap
                        if (isHostUnit) return;
                        if (unit.role == RPSUnit.UnitRole.Flag)
                        {
                            UnityEngine.Debug.Log($"[GameSetup] This unit is already a flag.");
                            unit.EnableSetupSelection();
                            return;
                        }

                        updates["guestTrapPosition"] = position;
                        updates["guestSetupComplete"] = true;
                        updates["currentSetupPhase"] = "complete";
                        updates["status"] = "playing";

                        // Update the unit before server update
                        unit.role = RPSUnit.UnitRole.Trap;
                        unit.UpdateVisual();

                        await UpdateRoomSetup(updates);

                        // After server update, save the board state
                        await SaveBoardStateToServer();

                        // Finalize setup
                        UnityEngine.Debug.Log($"[GameSetup] Guest Trap confirmed. Setup complete!");
                        FinalizeSetup();
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[GameSetup] Failed to update selection: {ex.Message}");
            // Re-enable selection for the unit if update failed
            if (unit != null)
            {
                unit.EnableSetupSelection();
            }
        }
    }

    private async Task SaveBoardStateToServer()
    {
        try
        {
            // First, assign random RPS roles to all units except flags and traps
            AssignRandomRPS(player1Units);
            AssignRandomRPS(player2Units);

            // Create a dictionary to store the complete board state
            Dictionary<string, object> boardState = new Dictionary<string, object>();

            // Process all units and store their state
            foreach (var unit in player1Units)
            {
                string position = $"{unit.Position.x}_{unit.Position.y}";
                Dictionary<string, object> unitState = new Dictionary<string, object>
                {
                    { "player", "host" },
                    { "role", unit.role.ToString() },
                    { "kind", unit.Kind.ToString() }
                };
                boardState[position] = unitState;
            }

            foreach (var unit in player2Units)
            {
                string position = $"{unit.Position.x}_{unit.Position.y}";
                Dictionary<string, object> unitState = new Dictionary<string, object>
                {
                    { "player", "guest" },
                    { "role", unit.role.ToString() },
                    { "kind", unit.Kind.ToString() }
                };
                boardState[position] = unitState;
            }

            // Add empty spaces for middle rows (2-3)
            for (int x = 0; x < 7; x++)
            {
                for (int y = 2; y < 4; y++)
                {
                    string position = $"{x}_{y}";
                    Dictionary<string, object> emptyState = new Dictionary<string, object>
                    {
                        { "player", "none" },
                        { "role", "None" },
                        { "kind", "None" }
                    };
                    boardState[position] = emptyState;
                }
            }

            // Update the server with the complete board state
            var updates = new Dictionary<string, object>
            {
                { "boardState", boardState }
            };

            await UpdateRoomSetup(updates);
            UnityEngine.Debug.Log("[GameSetup] Board state saved to server");

            // Update visuals for all units
            foreach (var unit in player1Units) unit.UpdateVisual();
            foreach (var unit in player2Units) unit.UpdateVisual();
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[GameSetup] Failed to save board state: {ex.Message}");
        }
    }

    private void HandlePvESelection(RPSUnit unit)
    {
        switch (selectionStep)
        {
            case 0:
                if (unit.playerId != 1) return;
                unit.role = RPSUnit.UnitRole.Flag;
                unit.UpdateVisual();
                selectionStep++;
                UnityEngine.Debug.Log("[GameSetup] 🎯 Player 1 Flag selected. Select TRAP.");
                break;

            case 1:
                if (unit.playerId != 1 || unit.role != RPSUnit.UnitRole.None) return;
                unit.role = RPSUnit.UnitRole.Trap;
                unit.UpdateVisual();
                selectionStep++;

                UnityEngine.Debug.Log("[GameSetup] 🤖 AI is choosing FLAG and TRAP...");
                SelectFTForAI();
                FinalizeSetup();
                break;
        }
    }

    private void SelectFTForAI()
    {
        List<RPSUnit> available = player2Units.FindAll(u => u.role == RPSUnit.UnitRole.None);
        if (available.Count < 2) return;

        int index1 = UnityEngine.Random.Range(0, available.Count);
        RPSUnit flag = available[index1];
        flag.role = RPSUnit.UnitRole.Flag;
        flag.UpdateVisual();

        available.RemoveAt(index1);
        int index2 = UnityEngine.Random.Range(0, available.Count);
        RPSUnit trap = available[index2];
        trap.role = RPSUnit.UnitRole.Trap;
        trap.UpdateVisual();

        UnityEngine.Debug.Log($"[GameSetup] 🤖 AI selected FLAG: {flag.name}, TRAP: {trap.name}");
    }

    private void FinalizeSetup()
    {
        if (GameModeManager.Instance.SelectedMode != GameMode.PvP)
        {
            UnityEngine.Debug.Log("[GameSetup] 🎲 Finalizing setup: assigning RPS roles randomly...");
            AssignRandomRPS(player1Units);
            AssignRandomRPS(player2Units);
        }

        setupComplete = true;

        // Always update visuals regardless of game mode
        foreach (var unit in player1Units) unit.UpdateVisual();
        foreach (var unit in player2Units) unit.UpdateVisual();

        DisableAllSelections();

        UnityEngine.Debug.Log("[GameSetup] ✅ Setup complete. Game begins!");

        TurnTimerManager.Instance?.ActivateGameTimer();

        var mode = GameModeManager.Instance.SelectedMode;
        if (mode == GameMode.PvE_Easy || mode == GameMode.PvE_Medium || mode == GameMode.PvE_Hard)
        {
            if (FindObjectOfType<AIPlayerController>() == null)
            {
                GameObject aiObj = new GameObject("AIPlayerController");

                switch (mode)
                {
                    case GameMode.PvE_Easy:
                        aiObj.AddComponent<AIPlayerController>();
                        UnityEngine.Debug.Log("[GameSetup] 🧠 Easy AI instantiated.");
                        break;

                    case GameMode.PvE_Medium:
                        aiObj.AddComponent<AIPlayerMediumController>();
                        UnityEngine.Debug.Log("[GameSetup] 🧠 Medium AI instantiated.");
                        break;

                    case GameMode.PvE_Hard:
                        aiObj.AddComponent<AIPlayerHardController>();
                        UnityEngine.Debug.Log("[GameSetup] 🧠 Hard AI (placeholder) instantiated.");
                        break;
                }
            }
        }

        bool playerStarts = UnityEngine.Random.Range(0, 2) == 0;

        if (playerStarts)
        {
            UnityEngine.Debug.Log("[GameSetup] 🎯 Player 1 starts the game!");
            TurnManager.Instance?.StartPlayerTurn();
        }
        else
        {
            UnityEngine.Debug.Log("[GameSetup] 🤖 AI/Player 2 starts the game!");
            TurnManager.Instance?.StartAITurn();

            if (mode != GameMode.PvP)
            {
                AIPlayerController ai = FindObjectOfType<AIPlayerController>();
                if (ai != null)
                {
                    ai.PlayTurn();
                }
            }
        }
    }

    private void AssignRandomRPS(List<RPSUnit> units)
    {
        List<RPSUnit> toAssign = units.FindAll(u => u.role == RPSUnit.UnitRole.None);

        int total = toAssign.Count;
        int countPerKind = total / 3;

        List<RPSUnit.RPSKind> kinds = new List<RPSUnit.RPSKind>();
        for (int i = 0; i < countPerKind; i++)
        {
            kinds.Add(RPSUnit.RPSKind.Rock);
            kinds.Add(RPSUnit.RPSKind.Paper);
            kinds.Add(RPSUnit.RPSKind.Scissors);
        }

        while (kinds.Count < total)
        {
            kinds.Add((RPSUnit.RPSKind)UnityEngine.Random.Range(0, 3));
        }

        Shuffle(kinds);

        for (int i = 0; i < toAssign.Count; i++)
        {
            toAssign[i].Kind = kinds[i];
            toAssign[i].UpdateVisual();
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }

    public bool IsSetupComplete() => setupComplete;
}