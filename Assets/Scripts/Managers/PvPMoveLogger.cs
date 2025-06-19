using UnityEngine;
using Firebase.Database;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;

/// <summary>
/// Manages logging and synchronization of moves in PVP mode
/// Logs each player move to server and synchronizes opponent moves
/// </summary>
public class PvPMoveLogger : MonoBehaviour
{
    public static PvPMoveLogger Instance;

    private string currentRoomId;
    private bool isHost;
    private DatabaseReference roomRef;
    private bool isListening = false;
    private string lastProcessedMove = "";
    
    // Battle state tracking
    private bool isInBattle = false;
    private RPSUnit.RPSKind? myBattleChoice;
    private RPSUnit.RPSKind? opponentBattleChoice;
    private RPSUnit myBattleUnit;
    private RPSUnit opponentBattleUnit;
    private Vector2Int battleTargetPos;
    private bool isBattleInitiator = false;

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
    /// Initialize the logger with room details and start listening for opponent moves
    /// </summary>
    public void Initialize(string roomId, bool isHostPlayer)
    {
        currentRoomId = roomId;
        isHost = isHostPlayer;

        if (!string.IsNullOrEmpty(currentRoomId))
        {
            if (FirebaseManager.Instance == null || FirebaseManager.Instance.DatabaseReference == null)
            {
                UnityEngine.Debug.LogWarning("[PvPMoveLogger] Firebase not ready, will retry in 1 second...");
                StartCoroutine(RetryInitialization(roomId, isHostPlayer));
                return;
            }

            roomRef = FirebaseManager.Instance.DatabaseReference.Child("rooms").Child(currentRoomId);
            StartListeningForOpponentMoves();
            UnityEngine.Debug.Log($"[PvPMoveLogger]  Initialized for room {roomId}, isHost: {isHost}");
        }
    }

    /// <summary>
    /// Retry initialization if Firebase is not ready
    /// </summary>
    private IEnumerator RetryInitialization(string roomId, bool isHostPlayer)
    {
        yield return new WaitForSeconds(1f);

        int retryCount = 0;
        while (retryCount < 5 && (FirebaseManager.Instance == null || FirebaseManager.Instance.DatabaseReference == null))
        {
            UnityEngine.Debug.Log($"[PvPMoveLogger] Retry {retryCount + 1}/5 - waiting for Firebase...");
            yield return new WaitForSeconds(1f);
            retryCount++;
        }

        if (FirebaseManager.Instance != null && FirebaseManager.Instance.DatabaseReference != null)
        {
            roomRef = FirebaseManager.Instance.DatabaseReference.Child("rooms").Child(roomId);
            StartListeningForOpponentMoves();
            UnityEngine.Debug.Log($"[PvPMoveLogger]  Late initialization successful for room {roomId}, isHost: {isHostPlayer}");
        }
        else
        {
            UnityEngine.Debug.LogError("[PvPMoveLogger]  Failed to initialize - Firebase not available after retries");
        }
    }

    /// <summary>
    /// Start listening for opponent moves on the server
    /// </summary>
    private void StartListeningForOpponentMoves()
    {
        if (roomRef == null || isListening) return;

        try
        {
            roomRef.Child("nextStep").ValueChanged += HandleOpponentMove;
            roomRef.Child("battleResult").ValueChanged += HandleBattleResult;
            isListening = true;
            UnityEngine.Debug.Log("[PvPMoveLogger]  Started listening for opponent moves and battle results");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[PvPMoveLogger] Failed to start listening: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle new opponent move from server
    /// </summary>
    private void HandleOpponentMove(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            UnityEngine.Debug.LogError($"[PvPMoveLogger] Database error: {args.DatabaseError}");
            return;
        }

        string moveDescription = args.Snapshot.Value?.ToString();

        if (string.IsNullOrEmpty(moveDescription) || moveDescription == lastProcessedMove)
            return;

        // Ensure the move is from opponent and not our own - double check for safety
        string expectedPlayerType = isHost ? "guest" : "host";
        string myPlayerType = isHost ? "host" : "guest";

        if (!moveDescription.Contains(expectedPlayerType))
        {
            UnityEngine.Debug.Log($"[PvPMoveLogger]  Move is not from opponent ({expectedPlayerType}): {moveDescription}");
            return;
        }

        if (moveDescription.Contains(myPlayerType))
        {
            UnityEngine.Debug.Log($"[PvPMoveLogger]  Ignoring our own move ({myPlayerType}): {moveDescription}");
            return;
        }

        lastProcessedMove = moveDescription;

        UnityEngine.Debug.Log($"[PvPMoveLogger]  Received opponent move: {moveDescription}");

        // Parse and execute the move on local board
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            // Small delay to ensure local move has finished
            StartCoroutine(DelayedParseAndExecuteMove(moveDescription));
        });
    }

    /// <summary>
    /// Handle battle result from server
    /// </summary>
    private void HandleBattleResult(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            UnityEngine.Debug.LogError($"[PvPMoveLogger] Database error in battle result: {args.DatabaseError}");
            return;
        }

        UnityEngine.Debug.Log($"[PvPMoveLogger] Received battle result. isInBattle: {isInBattle}");

        if (!isInBattle) 
        {
            UnityEngine.Debug.LogWarning("[PvPMoveLogger] Ignoring battle result - not in battle state");
            return; // Only process if we're in a battle
        }

        var battleResultData = args.Snapshot.Value as Dictionary<string, object>;
        if (battleResultData == null) 
        {
            UnityEngine.Debug.LogWarning("[PvPMoveLogger] Battle result data is null");
            return;
        }

        UnityEngine.Debug.Log($"[PvPMoveLogger] Processing battle result: {string.Join(", ", battleResultData.Select(kv => $"{kv.Key}={kv.Value}"))}");

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            StartCoroutine(ApplyBattleResult(battleResultData));
        });
    }

    /// <summary>
    /// Apply battle result received from server
    /// </summary>
    private IEnumerator ApplyBattleResult(Dictionary<string, object> battleResultData)
    {
        if (!battleResultData.ContainsKey("winner") || !battleResultData.ContainsKey("hostChoice") || !battleResultData.ContainsKey("guestChoice"))
        {
            UnityEngine.Debug.LogError("[PvPMoveLogger] Invalid battle result data");
            yield break;
        }

        string winner = battleResultData["winner"].ToString();
        string hostChoiceStr = battleResultData["hostChoice"].ToString();
        string guestChoiceStr = battleResultData["guestChoice"].ToString();

        RPSUnit.RPSKind hostChoice;
        RPSUnit.RPSKind guestChoice;

        try
        {
            hostChoice = (RPSUnit.RPSKind)Enum.Parse(typeof(RPSUnit.RPSKind), hostChoiceStr);
            guestChoice = (RPSUnit.RPSKind)Enum.Parse(typeof(RPSUnit.RPSKind), guestChoiceStr);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[PvPMoveLogger] Error parsing battle choices: {ex.Message}");
            yield break;
        }

        string myPlayerType = isHost ? "host" : "guest";
        bool iWon = winner == myPlayerType;

        UnityEngine.Debug.Log($"[PvPMoveLogger] Applying battle result: Winner={winner}, HostChoice={hostChoice}, GuestChoice={guestChoice}, IWon={iWon}");
        UnityEngine.Debug.Log($"[PvPMoveLogger] My player type: {myPlayerType}, Battle units: myUnit={myBattleUnit?.name}, opponentUnit={opponentBattleUnit?.name}");

        // FIXED LOGIC: Now we know exactly which player made which choice
        RPSUnit.RPSKind myActualChoice;
        RPSUnit.RPSKind opponentActualChoice;
        
        if (isHost)
        {
            // I am the host, so I made the host choice
            myActualChoice = hostChoice;
            opponentActualChoice = guestChoice;
        }
        else
        {
            // I am the guest, so I made the guest choice
            myActualChoice = guestChoice;
            opponentActualChoice = hostChoice;
        }

        UnityEngine.Debug.Log($"[PvPMoveLogger] Final choice assignment: My choice={myActualChoice}, Opponent choice={opponentActualChoice}");

        // Update unit kinds and reveal them
        myBattleUnit.Kind = myActualChoice;
        opponentBattleUnit.Kind = opponentActualChoice;

        myBattleUnit.Reveal();
        opponentBattleUnit.Reveal();
        myBattleUnit.UpdateVisual();
        opponentBattleUnit.UpdateVisual();

        yield return new WaitForSeconds(0.5f);

        // Apply the battle outcome - the winner keeps their unit and moves to target
        if (iWon)
        {
            UnityEngine.Debug.Log($"[PvPMoveLogger] You won the battle! {myActualChoice} beats {opponentActualChoice}. Your unit survives and moves.");
            BoardManager.Instance.RemoveUnit(opponentBattleUnit);
            Destroy(opponentBattleUnit.gameObject);
            myBattleUnit.MoveTo(battleTargetPos);
        }
        else
        {
            UnityEngine.Debug.Log($"[PvPMoveLogger] You lost the battle! {opponentActualChoice} beats {myActualChoice}. Your unit is destroyed.");
            BoardManager.Instance.RemoveUnit(myBattleUnit);
            Destroy(myBattleUnit.gameObject);
            opponentBattleUnit.MoveTo(battleTargetPos);
        }

        // Hide battle panel for both players
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.battlePanel?.SetActive(false);
        }

        // Clean up battle state
        isInBattle = false;
        isBattleInitiator = false;
        myBattleUnit = null;
        opponentBattleUnit = null;
        myBattleChoice = null;
        opponentBattleChoice = null;

        // Clear player selections
        foreach (var controller in FindObjectsOfType<PlayerController>())
        {
            controller.ClearSelection();
        }

        // End turn for both players
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.EndTurn();
        }

        // Ensure battle panel is hidden
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.battlePanel?.SetActive(false);
        }

        UnityEngine.Debug.Log("[PvPMoveLogger] Battle ended and turn ended successfully");
    }

    /// <summary>
    /// Execute a small delay before performing opponent move
    /// </summary>
    private IEnumerator DelayedParseAndExecuteMove(string moveDescription)
    {
        yield return new WaitForSeconds(0.1f); // Small delay
        ParseAndExecuteMove(moveDescription);
    }

    /// <summary>
    /// Parse and execute opponent move on local board
    /// </summary>
    private void ParseAndExecuteMove(string moveDescription)
    {
        try
        {
            // Ensure this is not our own move
            string myPlayerType = isHost ? "host" : "guest";
            if (moveDescription.Contains(myPlayerType))
            {
                UnityEngine.Debug.Log($"[PvPMoveLogger]  Ignoring our own move: {moveDescription}");
                return;
            }

            // Parse the string: "host moves from (2,3) to (2,4)"
            var parts = moveDescription.Split(new string[] { " moves from (", ") to (", ")" }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 3)
            {
                UnityEngine.Debug.LogError($"[PvPMoveLogger] Invalid move format: {moveDescription}");
                return;
            }

            // Extract positions
            string[] fromParts = parts[1].Split(',');
            string[] toParts = parts[2].Split(',');

            if (fromParts.Length != 2 || toParts.Length != 2)
            {
                UnityEngine.Debug.LogError($"[PvPMoveLogger] Invalid position format: {moveDescription}");
                return;
            }

            Vector2Int fromPos = new Vector2Int(int.Parse(fromParts[0]), int.Parse(fromParts[1]));
            Vector2Int toPos = new Vector2Int(int.Parse(toParts[0]), int.Parse(toParts[1]));

            UnityEngine.Debug.Log($"[PvPMoveLogger]  Parsed opponent move: {fromPos} -> {toPos}");

            // Find unit at original position
            RPSUnit movingUnit = BoardManager.Instance.GetUnitAt(fromPos) as RPSUnit;

            if (movingUnit == null)
            {
                UnityEngine.Debug.LogError($"[PvPMoveLogger] No unit found at {fromPos} for opponent move");
                return;
            }

            // Ensure it's an opponent unit
            int expectedPlayerId = isHost ? 2 : 1; // If I'm host, opponent is player 2
            if (movingUnit.playerId != expectedPlayerId)
            {
                UnityEngine.Debug.LogError($"[PvPMoveLogger] Unit at {fromPos} belongs to player {movingUnit.playerId}, expected {expectedPlayerId}");
                return;
            }

            // Ensure unit hasn't moved already
            if (movingUnit.Position != fromPos)
            {
                UnityEngine.Debug.LogWarning($"[PvPMoveLogger] Unit position mismatch: unit is at {movingUnit.Position} but expected at {fromPos}. Move already executed?");
                return;
            }

            // Additional check - no friendly unit at target position (except for battle)
            RPSUnit unitAtTarget = BoardManager.Instance.GetUnitAt(toPos) as RPSUnit;
            if (unitAtTarget != null && unitAtTarget.playerId == movingUnit.playerId)
            {
                UnityEngine.Debug.LogWarning($"[PvPMoveLogger] Target position {toPos} already occupied by friendly unit. Move already executed?");
                return;
            }

            // Execute move using ExecuteMoveSequence
            StartCoroutine(ExecuteOpponentMove(movingUnit, toPos));
        }
        
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[PvPMoveLogger] Error parsing move: {ex.Message}");
        }
    }

    /// <summary>
    /// Execute opponent move using the same logic as ExecuteMoveSequence
    /// </summary>
    private IEnumerator ExecuteOpponentMove(RPSUnit unit, Vector2Int targetPos)
    {
        UnityEngine.Debug.Log($"[PvPMoveLogger]  Executing opponent move: {unit.name} (Player {unit.playerId}) from {unit.Position} to {targetPos}");

        // Ensure unit still exists and is valid
        if (unit == null)
        {
            UnityEngine.Debug.LogWarning($"[PvPMoveLogger] Unit was destroyed during execution. Aborting.");
            yield break;
        }

        var targetUnit = BoardManager.Instance.GetUnitAt(targetPos) as RPSUnit;

        // Check that move is legal - only one step
        Vector2Int delta = targetPos - unit.Position;
        if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) != 1)
        {
            UnityEngine.Debug.Log($" Invalid move: Distance must be 1 step, tried to move from {unit.Position} to {targetPos}");
            yield break;
        }

        if (targetUnit == null)
        {
            UnityEngine.Debug.Log($" Opponent moving {unit.name} to empty tile {targetPos}");
            unit.MoveTo(targetPos);
            yield return new WaitForSeconds(0.6f);
            yield break;
        }

        // Ensure we're attacking only unit that is exactly at target position
        if (targetUnit.Position != targetPos)
        {
            UnityEngine.Debug.Log($" Cannot attack: Target unit is at {targetUnit.Position} but move is to {targetPos}");
            yield break;
        }

        // Ensure units belong to different players
        if (unit.playerId == targetUnit.playerId)
        {
            UnityEngine.Debug.LogWarning($" Cannot attack own unit: both units belong to player {unit.playerId}");
            yield break;
        }

        UnityEngine.Debug.Log($"[PvPMoveLogger]  Battle: {unit.name}({unit.Kind}) vs {targetUnit.name}({targetUnit.Kind})");

        // Reveal units in battle
        unit.Reveal();
        targetUnit.Reveal();

        // Handle trap case
        if (targetUnit.role == RPSUnit.UnitRole.Trap)
        {
            UnityEngine.Debug.Log(" Opponent stepped on trap and is destroyed.");
            BoardManager.Instance.RemoveUnit(unit);
            Destroy(unit.gameObject);
            yield break;
        }

        // Handle flag case
        if (targetUnit.role == RPSUnit.UnitRole.Flag)
        {
            UnityEngine.Debug.Log(" Opponent captured the FLAG! YOU LOSE!");
            BoardManager.Instance.RemoveUnit(targetUnit);
            Destroy(targetUnit.gameObject);
            unit.MoveTo(targetPos);
            PlayerController.gameEnded = true;

            // Set player as loser
            TurnTimerManager.Instance?.SetPlayerWon(false);

            // Stop all game systems
            TurnManager.Instance?.StopGame();
            yield break;
        }

        // CRITICAL FIX: For PvP mode, ALL battles should go through the battle system
        // This ensures both players see the same battle results
        UnityEngine.Debug.Log($"[PvPMoveLogger] Normal battle detected in PvP mode - forcing through battle system: {unit.Kind} vs {targetUnit.Kind}");
    
        SetupBattleState(targetUnit, unit, targetPos, false);
    
    // Only show battle panel for tie battles (same unit types)
    if (unit.Kind == targetUnit.Kind)
    {
        BattleManager.Instance?.ShowPlayerPanel();
    }
        yield break;
    }

    /// <summary>
    /// Log player move to server in "nextStep" field
    /// </summary>
    /// <param name="fromPosition">Position unit moved from</param>
    /// <param name="toPosition">Position unit moved to</param>
    public async void LogPlayerMove(Vector2Int fromPosition, Vector2Int toPosition)
    {
        if (string.IsNullOrEmpty(currentRoomId) || roomRef == null)
        {
            UnityEngine.Debug.LogWarning("[PvPMoveLogger] Cannot log move - room not initialized");
            return;
        }

        try
        {
            // Determine who made the move
            string playerType = isHost ? "host" : "guest";

            // Create string describing the move
            string moveDescription = $"{playerType} moves from ({fromPosition.x},{fromPosition.y}) to ({toPosition.x},{toPosition.y})";

            UnityEngine.Debug.Log($"[PvPMoveLogger]  Sending {playerType} move to server: {moveDescription}");

            // Save our last move so we don't process it again
            lastProcessedMove = moveDescription;

            // Update nextStep field in server
            var updates = new Dictionary<string, object>
            {
                { "nextStep", moveDescription }
            };

            await roomRef.UpdateChildrenAsync(updates);

            UnityEngine.Debug.Log($"[PvPMoveLogger]  Move logging completed: {moveDescription}");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[PvPMoveLogger]  Error logging move: {ex.Message}");
        }
    }

    /// <summary>
    /// Find unit at specific position
    /// </summary>
    private RPSUnit FindUnitAtPosition(Vector2Int position)
    {
        RPSUnit[] allUnits = FindObjectsOfType<RPSUnit>();
        foreach (var unit in allUnits)
        {
            if (unit.Position == position)
                return unit;
        }
        return null;
    }

    /// <summary>
    /// Log player's battle choice to server
    /// </summary>
    public async void LogBattleChoice(RPSUnit.RPSKind choice)
    {
        if (string.IsNullOrEmpty(currentRoomId) || roomRef == null || !isInBattle)
        {
            UnityEngine.Debug.LogWarning("[PvPMoveLogger] Cannot log battle choice - not in battle or room not initialized");
            return;
        }

        // Only handle tie battles (same unit types)
        if (myBattleUnit.Kind != opponentBattleUnit.Kind)
        {
            UnityEngine.Debug.LogWarning("[PvPMoveLogger] Cannot log battle choice - this is a normal battle, not a tie battle");
            return;
        }

        try
        {
            string playerType = isHost ? "host" : "guest";
            string choiceStr = choice.ToString();
            
            UnityEngine.Debug.Log($"[PvPMoveLogger] Sending {playerType} battle choice for tie battle: {choiceStr}");

            // Update battle choice in server
            var updates = new Dictionary<string, object>
            {
                { $"battleChoice/{playerType}", choiceStr }
            };

            await roomRef.UpdateChildrenAsync(updates);
            myBattleChoice = choice;

            // If this is the initiator, wait for opponent choice and then resolve
            if (isBattleInitiator)
            {
                StartCoroutine(WaitForOpponentChoiceAndResolve());
            }
            else
            {
                // If we're not the initiator, we just wait for the battle result
                UnityEngine.Debug.Log("[PvPMoveLogger] Waiting for initiator to resolve tie battle...");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[PvPMoveLogger] Error logging battle choice: {ex.Message}");
        }
    }

    /// <summary>
    /// Wait for opponent's choice and resolve battle (only for initiator)
    /// </summary>
    private IEnumerator WaitForOpponentChoiceAndResolve()
    {
        if (!isBattleInitiator) yield break;
        
        // Only handle tie battles (same unit types)
        if (myBattleUnit.Kind != opponentBattleUnit.Kind)
        {
            UnityEngine.Debug.LogWarning("[PvPMoveLogger] WaitForOpponentChoiceAndResolve called for normal battle - this should not happen");
            yield break;
        }

        // Wait for opponent choice
        float timeout = 30f;
        float elapsed = 0f;

        while (!opponentBattleChoice.HasValue && elapsed < timeout)
        {
            bool shouldBreak = false;
            var task = roomRef.Child("battleChoice").GetValueAsync();
            
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Exception != null)
            {
                UnityEngine.Debug.LogError($"[PvPMoveLogger] Error checking opponent choice: {task.Exception}");
                shouldBreak = true;
            }
            else
            {
                try
                {
                    var data = task.Result.Value as Dictionary<string, object>;
                    if (data != null)
                    {
                        string opponentType = isHost ? "guest" : "host";
                        if (data.ContainsKey(opponentType))
                        {
                            string choiceStr = data[opponentType].ToString();
                            opponentBattleChoice = (RPSUnit.RPSKind)Enum.Parse(typeof(RPSUnit.RPSKind), choiceStr);
                            shouldBreak = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[PvPMoveLogger] Error processing opponent choice: {ex.Message}");
                }
            }

            if (shouldBreak) break;

            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }

        if (opponentBattleChoice.HasValue)
        {
            ResolveBattleAsInitiator();
        }
        else
        {
            UnityEngine.Debug.LogError("[PvPMoveLogger] Timeout waiting for opponent battle choice");
            EndBattle();
        }
    }

    private void SetupBattleState(RPSUnit myUnit, RPSUnit opponentUnit, Vector2Int targetPos, bool initiator)
    {
        isInBattle = true;
        isBattleInitiator = initiator;
        myBattleUnit = myUnit;
        opponentBattleUnit = opponentUnit;
        battleTargetPos = targetPos;
        myBattleChoice = null;
        opponentBattleChoice = null;

        // CRITICAL: Stop the timer during battles to prevent race conditions
        TurnTimerManager.Instance?.StopTimer();
        UnityEngine.Debug.Log("[PvPMoveLogger] Timer stopped for battle");

        // CRITICAL SAFETY CHECKS
        int myPlayerId = isHost ? 1 : 2;
        
        UnityEngine.Debug.Log($"[PvPMoveLogger] Battle setup: Initiator={initiator}, MyUnit={myUnit.name} (Player {myUnit.playerId}), OpponentUnit={opponentUnit.name} (Player {opponentUnit.playerId})");
        UnityEngine.Debug.Log($"[PvPMoveLogger] My player ID: {myPlayerId}, IsHost: {isHost}");
        
        // Verify unit assignments are correct
        if (myBattleUnit.playerId != myPlayerId)
        {
            UnityEngine.Debug.LogError($"[PvPMoveLogger] CRITICAL ERROR: myBattleUnit player ID ({myBattleUnit.playerId}) doesn't match my player ID ({myPlayerId})");
            UnityEngine.Debug.LogError($"[PvPMoveLogger] This should NEVER happen! Fixing by swapping units...");
            
            // Swap the units
            var temp = myBattleUnit;
            myBattleUnit = opponentBattleUnit;
            opponentBattleUnit = temp;
            
            UnityEngine.Debug.Log($"[PvPMoveLogger] After swap: MyUnit={myBattleUnit.name} (Player {myBattleUnit.playerId}), OpponentUnit={opponentBattleUnit.name} (Player {opponentBattleUnit.playerId})");
        }
        
        if (opponentBattleUnit.playerId == myPlayerId)
        {
            UnityEngine.Debug.LogError($"[PvPMoveLogger] CRITICAL ERROR: opponentBattleUnit player ID ({opponentBattleUnit.playerId}) matches my player ID ({myPlayerId})");
            UnityEngine.Debug.LogError($"[PvPMoveLogger] This should NEVER happen! Fixing by swapping units...");
            
            // Swap the units
            var temp = myBattleUnit;
            myBattleUnit = opponentBattleUnit;
            opponentBattleUnit = temp;
            
            UnityEngine.Debug.Log($"[PvPMoveLogger] After swap: MyUnit={myBattleUnit.name} (Player {myBattleUnit.playerId}), OpponentUnit={opponentBattleUnit.name} (Player {opponentBattleUnit.playerId})");
        }
        
        // Verify units are different objects
        if (myBattleUnit == opponentBattleUnit)
        {
            UnityEngine.Debug.LogError($"[PvPMoveLogger] CRITICAL ERROR: myBattleUnit and opponentBattleUnit are the SAME object! {myBattleUnit.name}");
        }
        
        UnityEngine.Debug.Log($"[PvPMoveLogger] Final battle setup confirmed: MyUnit={myBattleUnit.name} (Player {myBattleUnit.playerId}), OpponentUnit={opponentBattleUnit.name} (Player {opponentBattleUnit.playerId})");
    }

    /// <summary>
    /// Start a new battle between two units (called by BattleManager)
    /// </summary>
    public void StartBattle(RPSUnit myUnit, RPSUnit opponentUnit, Vector2Int targetPos)
    {
        if (string.IsNullOrEmpty(currentRoomId) || roomRef == null)
        {
            UnityEngine.Debug.LogWarning("[PvPMoveLogger] Cannot start battle - room not initialized");
            return;
        }

        SetupBattleState(myUnit, opponentUnit, targetPos, true);
        UnityEngine.Debug.Log($"[PvPMoveLogger] Started battle as initiator: {myUnit.name} vs {opponentUnit.name}");
        
        // Check if this is a normal battle (different unit types) or a tie battle (same unit types)
        bool isNormalBattle = myUnit.Kind != opponentUnit.Kind;
        
        if (isNormalBattle)
        {
            // Normal battle - resolve immediately without player choices
            UnityEngine.Debug.Log($"[PvPMoveLogger] Normal battle detected - resolving immediately: {myUnit.Kind} vs {opponentUnit.Kind}");
            ResolveBattleAsInitiator();
        }
        else
        {
            // Tie battle - show battle panel for player choices
            UnityEngine.Debug.Log($"[PvPMoveLogger] Tie battle detected - showing battle panel for choices: {myUnit.Kind} vs {opponentUnit.Kind}");
        }
    }

    /// <summary>
    /// Resolve the battle and send result to server (only for initiator)
    /// </summary>
    private async void ResolveBattleAsInitiator()
    {
        if (!isInBattle || myBattleUnit == null || opponentBattleUnit == null || !isBattleInitiator) return;

        try
        {
            // Check if this is a normal battle (different unit types) or a tie battle (same unit types)
            bool isNormalBattle = myBattleUnit.Kind != opponentBattleUnit.Kind;
            
            if (isNormalBattle)
            {
                // Normal battle - resolve based on unit types
                UnityEngine.Debug.Log($"[PvPMoveLogger] Normal battle detected: {myBattleUnit.Kind} vs {opponentBattleUnit.Kind}");
                
                bool iWin = myBattleUnit.Beats(opponentBattleUnit);
                bool opponentWins = opponentBattleUnit.Beats(myBattleUnit);
                
                if (iWin || opponentWins)
                {
                    string myPlayerType = isHost ? "host" : "guest";
                    string opponentPlayerType = isHost ? "guest" : "host";
                    string winner = iWin ? myPlayerType : opponentPlayerType;

                    // Send battle result with unit types as choices
                    var battleResult = new Dictionary<string, object>
                    {
                        { "winner", winner },
                        { "hostChoice", isHost ? myBattleUnit.Kind.ToString() : opponentBattleUnit.Kind.ToString() },
                        { "guestChoice", isHost ? opponentBattleUnit.Kind.ToString() : myBattleUnit.Kind.ToString() }
                    };

                    await roomRef.Child("battleResult").SetValueAsync(battleResult);

                    // Apply result locally for initiator
                    ApplyBattleResultLocally(iWin, myBattleUnit.Kind, opponentBattleUnit.Kind);
                    
                    // Wait a moment for opponent to receive the battle result
                    await Task.Delay(1000);
                    
                    // Clear battle data from server
                    await roomRef.Child("battleResult").RemoveValueAsync();
                    
                    // End battle
                    EndBattle();
                }
                else
                {
                    UnityEngine.Debug.LogError($"[PvPMoveLogger] Unexpected case in normal battle: {myBattleUnit.Kind} vs {opponentBattleUnit.Kind}");
                    EndBattle();
                }
            }
            else
            {
                // Tie battle - need player choices
                if (!myBattleChoice.HasValue || !opponentBattleChoice.HasValue)
                {
                    UnityEngine.Debug.LogError("[PvPMoveLogger] Tie battle but missing choices");
                    return;
                }

            bool iWin = Beats(myBattleChoice.Value, opponentBattleChoice.Value);
            bool opponentWins = Beats(opponentBattleChoice.Value, myBattleChoice.Value);

                UnityEngine.Debug.Log($"[PvPMoveLogger] Resolving tie battle: My choice={myBattleChoice}, Opponent choice={opponentBattleChoice}");

            if (iWin || opponentWins)
            {
                string myPlayerType = isHost ? "host" : "guest";
                    string opponentPlayerType = isHost ? "guest" : "host";
                    string winner = iWin ? myPlayerType : opponentPlayerType;

                    // Send more explicit battle result with each player's actual choice
                var battleResult = new Dictionary<string, object>
                {
                    { "winner", winner },
                        { "hostChoice", isHost ? myBattleChoice.Value.ToString() : opponentBattleChoice.Value.ToString() },
                        { "guestChoice", isHost ? opponentBattleChoice.Value.ToString() : myBattleChoice.Value.ToString() }
                };

                await roomRef.Child("battleResult").SetValueAsync(battleResult);

                // Apply result locally for initiator
                ApplyBattleResultLocally(iWin, myBattleChoice.Value, opponentBattleChoice.Value);
                
                // Wait a moment for opponent to receive the battle result
                await Task.Delay(1000);
                
                // Clear battle data from server
                await roomRef.Child("battleResult").RemoveValueAsync();
                
                // End battle - THIS IS CRITICAL
                EndBattle();
            }
            else
            {
                // Tie - restart battle
                UnityEngine.Debug.Log($"[PvPMoveLogger] Battle is a tie! Both chose {myBattleChoice}");
                myBattleChoice = null;
                opponentBattleChoice = null;
                
                // Clear battle choices on server
                await roomRef.Child("battleChoice").RemoveValueAsync();
                
                BattleManager.Instance?.ShowPlayerPanel();
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[PvPMoveLogger] Error resolving battle: {ex.Message}");
            EndBattle();
        }
    }

    /// <summary>
    /// Apply battle result locally
    /// </summary>
    private void ApplyBattleResultLocally(bool iWon, RPSUnit.RPSKind myChoice, RPSUnit.RPSKind opponentChoice)
    {
        // Reveal both units
        myBattleUnit.Kind = myChoice;
        opponentBattleUnit.Kind = opponentChoice;
        myBattleUnit.Reveal();
        opponentBattleUnit.Reveal();
        myBattleUnit.UpdateVisual();
        opponentBattleUnit.UpdateVisual();

        if (iWon)
        {
            UnityEngine.Debug.Log($"[PvPMoveLogger] You win the battle! {myChoice} beats {opponentChoice}");
            BoardManager.Instance.RemoveUnit(opponentBattleUnit);
            Destroy(opponentBattleUnit.gameObject);
            myBattleUnit.MoveTo(battleTargetPos);
        }
        else
        {
            UnityEngine.Debug.Log($"[PvPMoveLogger] You lose the battle! {opponentChoice} beats {myChoice}");
            BoardManager.Instance.RemoveUnit(myBattleUnit);
            Destroy(myBattleUnit.gameObject);
            opponentBattleUnit.MoveTo(battleTargetPos);
        }
    }

    /// <summary>
    /// End the current battle and clean up
    /// </summary>
    private void EndBattle()
    {
        UnityEngine.Debug.Log("[PvPMoveLogger] EndBattle() called - cleaning up battle state");
        
        // Clean up battle state
        isInBattle = false;
        isBattleInitiator = false;
        myBattleUnit = null;
        opponentBattleUnit = null;
        myBattleChoice = null;
        opponentBattleChoice = null;

        // Set battle as inactive and hide panel
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.SetBattleActive(false);
        }

        // Clear player selections
        foreach (var controller in FindObjectsOfType<PlayerController>())
        {
            controller.ClearSelection();
        }

        // CRITICAL FIX: End turn properly and restart timer
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.EndTurn();
        }

        UnityEngine.Debug.Log("[PvPMoveLogger] Battle ended and turn ended successfully");
    }

    private bool Beats(RPSUnit.RPSKind a, RPSUnit.RPSKind b)
    {
        return (a == RPSUnit.RPSKind.Rock && b == RPSUnit.RPSKind.Scissors) ||
               (a == RPSUnit.RPSKind.Paper && b == RPSUnit.RPSKind.Rock) ||
               (a == RPSUnit.RPSKind.Scissors && b == RPSUnit.RPSKind.Paper);
    }

    /// <summary>
    /// Stop listening for opponent moves
    /// </summary>
    public void StopListening()
    {
        if (isListening && roomRef != null)
        {
            roomRef.Child("nextStep").ValueChanged -= HandleOpponentMove;
            roomRef.Child("battleResult").ValueChanged -= HandleBattleResult;
            isListening = false;
            UnityEngine.Debug.Log("[PvPMoveLogger] Stopped listening for opponent moves");
        }
    }

    private void OnDestroy()
    {
        StopListening();
    }
}