using UnityEngine;
using Firebase.Database;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Collections;

/// <summary>
/// Manages PvP battle synchronization and resolution
/// </summary>
public class PvPBattleManager : MonoBehaviour
{
    public static PvPBattleManager Instance;

    private string currentRoomId;
    private bool isHost;
    private DatabaseReference roomRef;
    private bool isListening = false;
    
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

    public void Initialize(string roomId, bool isHostPlayer, DatabaseReference roomReference)
    {
        currentRoomId = roomId;
        isHost = isHostPlayer;
        roomRef = roomReference;
        StartListeningForBattleResults();
    }

    private void StartListeningForBattleResults()
    {
        if (roomRef == null || isListening) return;

        try
        {
            roomRef.Child("battleResult").ValueChanged += HandleBattleResult;
            isListening = true;
            UnityEngine.Debug.Log("[PvPBattleManager] Started listening for battle results");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[PvPBattleManager] Failed to start listening: {ex.Message}");
        }
    }

    private void HandleBattleResult(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            UnityEngine.Debug.LogError($"[PvPBattleManager] Database error in battle result: {args.DatabaseError}");
            return;
        }

        if (!isInBattle) return;

        var battleResultData = args.Snapshot.Value as Dictionary<string, object>;
        if (battleResultData == null) return;

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            StartCoroutine(ApplyBattleResult(battleResultData));
        });
    }

    private IEnumerator ApplyBattleResult(Dictionary<string, object> battleResultData)
    {
        if (!battleResultData.ContainsKey("winner") || !battleResultData.ContainsKey("winnerChoice") || !battleResultData.ContainsKey("loserChoice"))
        {
            UnityEngine.Debug.LogError("[PvPBattleManager] Invalid battle result data");
            yield break;
        }

        string winner = battleResultData["winner"].ToString();
        string winnerChoiceStr = battleResultData["winnerChoice"].ToString();
        string loserChoiceStr = battleResultData["loserChoice"].ToString();

        RPSUnit.RPSKind winnerChoice;
        RPSUnit.RPSKind loserChoice;

        try
        {
            winnerChoice = (RPSUnit.RPSKind)Enum.Parse(typeof(RPSUnit.RPSKind), winnerChoiceStr);
            loserChoice = (RPSUnit.RPSKind)Enum.Parse(typeof(RPSUnit.RPSKind), loserChoiceStr);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[PvPBattleManager] Error parsing battle choices: {ex.Message}");
            yield break;
        }

        string myPlayerType = isHost ? "host" : "guest";
        bool iWon = winner == myPlayerType;

        UnityEngine.Debug.Log($"[PvPBattleManager] Applying battle result: Winner={winner}, WinnerChoice={winnerChoice}, LoserChoice={loserChoice}, IWon={iWon}");

        // Update unit kinds and reveal them
        if (iWon)
        {
            myBattleUnit.Kind = winnerChoice;
            opponentBattleUnit.Kind = loserChoice;
        }
        else
        {
            myBattleUnit.Kind = loserChoice;
            opponentBattleUnit.Kind = winnerChoice;
        }

        myBattleUnit.Reveal();
        opponentBattleUnit.Reveal();
        myBattleUnit.UpdateVisual();
        opponentBattleUnit.UpdateVisual();

        yield return new WaitForSeconds(0.5f);

        // Apply the battle outcome
        if (iWon)
        {
            UnityEngine.Debug.Log("[PvPBattleManager] You won the battle!");
            BoardManager.Instance.RemoveUnit(opponentBattleUnit);
            Destroy(opponentBattleUnit.gameObject);
            myBattleUnit.MoveTo(battleTargetPos);
        }
        else
        {
            UnityEngine.Debug.Log("[PvPBattleManager] You lost the battle!");
            BoardManager.Instance.RemoveUnit(myBattleUnit);
            Destroy(myBattleUnit.gameObject);
            opponentBattleUnit.MoveTo(battleTargetPos);
        }

        // Set battle as inactive and hide panel
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.SetBattleActive(false);
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

        // End turn
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.EndTurn();
        }

        UnityEngine.Debug.Log("[PvPBattleManager] Battle ended and turn ended successfully");
    }

    public async void LogBattleChoice(RPSUnit.RPSKind choice)
    {
        if (string.IsNullOrEmpty(currentRoomId) || roomRef == null || !isInBattle)
        {
            UnityEngine.Debug.LogWarning("[PvPBattleManager] Cannot log battle choice - not in battle or room not initialized");
            return;
        }

        try
        {
            string playerType = isHost ? "host" : "guest";
            string choiceStr = choice.ToString();
            
            UnityEngine.Debug.Log($"[PvPBattleManager] Sending {playerType} battle choice: {choiceStr}");

            var updates = new Dictionary<string, object>
            {
                { $"battleChoice/{playerType}", choiceStr }
            };

            await roomRef.UpdateChildrenAsync(updates);
            myBattleChoice = choice;

            if (isBattleInitiator)
            {
                StartCoroutine(WaitForOpponentChoiceAndResolve());
            }
            else
            {
                UnityEngine.Debug.Log("[PvPBattleManager] Waiting for initiator to resolve battle...");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[PvPBattleManager] Error logging battle choice: {ex.Message}");
        }
    }

    private IEnumerator WaitForOpponentChoiceAndResolve()
    {
        if (!isBattleInitiator) yield break;

        float timeout = 30f;
        float elapsed = 0f;

        while (!opponentBattleChoice.HasValue && elapsed < timeout)
        {
            bool shouldBreak = false;
            var task = roomRef.Child("battleChoice").GetValueAsync();
            
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Exception != null)
            {
                UnityEngine.Debug.LogError($"[PvPBattleManager] Error checking opponent choice: {task.Exception}");
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
                    UnityEngine.Debug.LogError($"[PvPBattleManager] Error processing opponent choice: {ex.Message}");
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
            UnityEngine.Debug.LogError("[PvPBattleManager] Timeout waiting for opponent battle choice");
            EndBattle();
        }
    }

    public void StartBattle(RPSUnit myUnit, RPSUnit opponentUnit, Vector2Int targetPos)
    {
        if (string.IsNullOrEmpty(currentRoomId) || roomRef == null)
        {
            UnityEngine.Debug.LogWarning("[PvPBattleManager] Cannot start battle - room not initialized");
            return;
        }

        isInBattle = true;
        isBattleInitiator = true;
        myBattleUnit = myUnit;
        opponentBattleUnit = opponentUnit;
        battleTargetPos = targetPos;
        myBattleChoice = null;
        opponentBattleChoice = null;

        UnityEngine.Debug.Log($"[PvPBattleManager] Started battle as initiator: {myUnit.name} vs {opponentUnit.name}");
    }

    private async void ResolveBattleAsInitiator()
    {
        if (!isInBattle || myBattleUnit == null || opponentBattleUnit == null || !myBattleChoice.HasValue || !opponentBattleChoice.HasValue || !isBattleInitiator) return;

        try
        {
            bool iWin = Beats(myBattleChoice.Value, opponentBattleChoice.Value);
            bool opponentWins = Beats(opponentBattleChoice.Value, myBattleChoice.Value);

            UnityEngine.Debug.Log($"[PvPBattleManager] Resolving battle: My choice={myBattleChoice}, Opponent choice={opponentBattleChoice}");

            if (iWin || opponentWins)
            {
                string myPlayerType = isHost ? "host" : "guest";
                string winner = iWin ? myPlayerType : (isHost ? "guest" : "host");
                string winnerChoice = iWin ? myBattleChoice.Value.ToString() : opponentBattleChoice.Value.ToString();
                string loserChoice = iWin ? opponentBattleChoice.Value.ToString() : myBattleChoice.Value.ToString();

                var battleResult = new Dictionary<string, object>
                {
                    { "winner", winner },
                    { "winnerChoice", winnerChoice },
                    { "loserChoice", loserChoice }
                };

                await roomRef.Child("battleResult").SetValueAsync(battleResult);

                // Apply result locally for initiator
                ApplyBattleResultLocally(iWin, myBattleChoice.Value, opponentBattleChoice.Value);
                
                // Clear battle data from server
                await roomRef.Child("battleChoice").RemoveValueAsync();
                await roomRef.Child("battleResult").RemoveValueAsync();
                
                EndBattle();
            }
            else
            {
                UnityEngine.Debug.Log($"[PvPBattleManager] Battle is a tie! Both chose {myBattleChoice}");
                myBattleChoice = null;
                opponentBattleChoice = null;
                
                await roomRef.Child("battleChoice").RemoveValueAsync();
                
                BattleManager.Instance?.ShowPlayerPanel();
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[PvPBattleManager] Error resolving battle: {ex.Message}");
            EndBattle();
        }
    }

    private void ApplyBattleResultLocally(bool iWon, RPSUnit.RPSKind myChoice, RPSUnit.RPSKind opponentChoice)
    {
        myBattleUnit.Kind = myChoice;
        opponentBattleUnit.Kind = opponentChoice;
        myBattleUnit.Reveal();
        opponentBattleUnit.Reveal();
        myBattleUnit.UpdateVisual();
        opponentBattleUnit.UpdateVisual();

        if (iWon)
        {
            UnityEngine.Debug.Log($"[PvPBattleManager] You win the battle! {myChoice} beats {opponentChoice}");
            BoardManager.Instance.RemoveUnit(opponentBattleUnit);
            Destroy(opponentBattleUnit.gameObject);
            myBattleUnit.MoveTo(battleTargetPos);
        }
        else
        {
            UnityEngine.Debug.Log($"[PvPBattleManager] You lose the battle! {opponentChoice} beats {myChoice}");
            BoardManager.Instance.RemoveUnit(myBattleUnit);
            Destroy(myBattleUnit.gameObject);
            opponentBattleUnit.MoveTo(battleTargetPos);
        }
    }

    private void EndBattle()
    {
        UnityEngine.Debug.Log("[PvPBattleManager] EndBattle() called - cleaning up battle state");
        
        isInBattle = false;
        isBattleInitiator = false;
        myBattleUnit = null;
        opponentBattleUnit = null;
        myBattleChoice = null;
        opponentBattleChoice = null;

        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.SetBattleActive(false);
        }

        foreach (var controller in FindObjectsOfType<PlayerController>())
        {
            controller.ClearSelection();
        }

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.EndTurn();
        }

        UnityEngine.Debug.Log("[PvPBattleManager] Battle ended and turn ended successfully");
    }

    private bool Beats(RPSUnit.RPSKind a, RPSUnit.RPSKind b)
    {
        return (a == RPSUnit.RPSKind.Rock && b == RPSUnit.RPSKind.Scissors) ||
               (a == RPSUnit.RPSKind.Paper && b == RPSUnit.RPSKind.Rock) ||
               (a == RPSUnit.RPSKind.Scissors && b == RPSUnit.RPSKind.Paper);
    }

    public void StopListening()
    {
        if (isListening && roomRef != null)
        {
            roomRef.Child("battleResult").ValueChanged -= HandleBattleResult;
            isListening = false;
            UnityEngine.Debug.Log("[PvPBattleManager] Stopped listening for battle results");
        }
    }

    private void OnDestroy()
    {
        StopListening();
    }
}
