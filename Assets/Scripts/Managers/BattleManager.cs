using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static RPSUnit;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    public GameObject battlePanel;
    public Button rockButton, paperButton, scissorsButton;

    private RPSUnit playerUnit;
    private RPSUnit aiUnit;
    private Vector2Int targetPos;
    private bool isBattleActive = false;

    private RPSUnit.RPSKind playerChoice;
    private RPSUnit.RPSKind aiChoice;

    private bool isPlayerInitiator;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        battlePanel?.SetActive(false);
    }

    public void StartBattle(RPSUnit initiator, RPSUnit opponent, Vector2Int target)
    {
        isPlayerInitiator = initiator.IsPlayerControlled;

        if (isPlayerInitiator)
        {
            playerUnit = initiator;
            aiUnit = opponent;
        }
        else
        {
            aiUnit = initiator;
            playerUnit = opponent;
        }

        targetPos = target;
        isBattleActive = true;

        // CRITICAL: Stop the timer during battles to prevent race conditions
        TurnTimerManager.Instance?.StopTimer();
        UnityEngine.Debug.Log("[BattleManager] Timer stopped for battle");

        // If in PvP mode, use PvPMoveLogger
        if (GameModeManager.Instance.SelectedMode == GameMode.PvP && PvPMoveLogger.Instance != null)
        {
            PvPMoveLogger.Instance.StartBattle(playerUnit, aiUnit, targetPos);
        }

        ShowPlayerPanel();
    }

    /// <summary>
    /// Prepares and shows the battle interface for the player to choose their move.
    /// It handles differences between PvP and single-player modes,
    /// and starts the battle intro animation.
    /// </summary>
    public void ShowPlayerPanel()
    {
            if (playerUnit == null || aiUnit == null)
    {
        UnityEngine.Debug.LogError("[BattleManager] Cannot show player panel - units not set!");
        return;
    }
        // For PvP mode, use simple logic without animation
        if (GameModeManager.Instance.SelectedMode == GameMode.PvP && PvPMoveLogger.Instance != null)
        {
            battlePanel?.SetActive(true);

          if (playerUnit.playerId != 1)
        {
            var temp = playerUnit;
            playerUnit = aiUnit;
            aiUnit = temp;
        }

        FightAnimationManager.Instance?.UpdatePreChoiceWeaponDisplay(playerUnit.Kind, aiUnit.Kind);

        StartCoroutine(AnimateFightPanelThenShowBattle());
            return;
        }

        // For single player mode, use animation
        if (playerUnit.playerId != 1)
        {
            var temp = playerUnit;
            playerUnit = aiUnit;
            aiUnit = temp;
        }
        // Update weapon display before animation starts
        FightAnimationManager.Instance?.UpdatePreChoiceWeaponDisplay(playerUnit.Kind, aiUnit.Kind);

        StartCoroutine(AnimateFightPanelThenShowBattle());
    }
public void SetUnits(RPSUnit player, RPSUnit ai)
{
    this.playerUnit = player;
    this.aiUnit = ai;
}
    /// <summary>
    /// Handles the sequence of animations when a battle starts:
    /// 1. Plays an intro animation through FightAnimationManager
    /// 2. Shows the battle panel with RPS choice buttons
    /// 3. Sets up button listeners for player choices
    /// </summary>
    private IEnumerator AnimateFightPanelThenShowBattle()
    {
        // Play the fight intro animation (shows the VS screen for 1.2 seconds)
        yield return StartCoroutine(FightAnimationManager.Instance.PlayFightIntroAnimation());

        // After intro animation, show the battle panel with RPS buttons
        battlePanel?.SetActive(true);

        // Set up fresh click listeners for each RPS choice
        rockButton.onClick.RemoveAllListeners();
        rockButton.onClick.AddListener(() => OnPlayerChoice(RPSUnit.RPSKind.Rock));

        paperButton.onClick.RemoveAllListeners();
        paperButton.onClick.AddListener(() => OnPlayerChoice(RPSUnit.RPSKind.Paper));

        scissorsButton.onClick.RemoveAllListeners();
        scissorsButton.onClick.AddListener(() => OnPlayerChoice(RPSUnit.RPSKind.Scissors));
    }

    private void OnPlayerChoice(RPSUnit.RPSKind choice)
    {
        playerChoice = choice;

        // If in PvP mode, log the choice to server
        if (GameModeManager.Instance.SelectedMode == GameMode.PvP && PvPMoveLogger.Instance != null)
        {
            PvPMoveLogger.Instance.LogBattleChoice(choice);
            return;
        }

        // Check if this is hard mode
        bool isHardLevel = FindObjectOfType<AIPlayerHardController>() != null;

        if (isHardLevel)
        {
            UnityEngine.Debug.Log("Hard level detected - AI will make smart choice based on player statistics");

            // Get statistics from server
            FirebaseManager.Instance?.DatabaseService?.GetUserStats((userData) =>
            {
                if (userData != null)
                {
                    UnityEngine.Debug.Log($"Player statistics - Rock: {userData.rockChoices}, Paper: {userData.paperChoices}, Scissors: {userData.scissorsChoices}");

                    // Find the player's most common piece
                    RPSUnit.RPSKind mostCommonChoice = RPSUnit.RPSKind.Rock;
                    int maxCount = userData.rockChoices;

                    if (userData.paperChoices > maxCount)
                    {
                        mostCommonChoice = RPSUnit.RPSKind.Paper;
                        maxCount = userData.paperChoices;
                    }
                    if (userData.scissorsChoices > maxCount)
                    {
                        mostCommonChoice = RPSUnit.RPSKind.Scissors;
                        maxCount = userData.scissorsChoices;
                    }

                    UnityEngine.Debug.Log($"Player's most common choice: {mostCommonChoice}");

                    // Choose the winning piece
                    switch (mostCommonChoice)
                    {
                        case RPSUnit.RPSKind.Rock:
                            aiChoice = RPSUnit.RPSKind.Paper;
                            break;
                        case RPSUnit.RPSKind.Paper:
                            aiChoice = RPSUnit.RPSKind.Scissors;
                            break;
                        case RPSUnit.RPSKind.Scissors:
                            aiChoice = RPSUnit.RPSKind.Rock;
                            break;
                    }

                    UnityEngine.Debug.Log($"AI chose {aiChoice} to counter player's {mostCommonChoice}");
                }
                else
                {
                    // If there is no data, choose randomly
                    aiChoice = (RPSUnit.RPSKind)UnityEngine.Random.Range(0, 3);
                    UnityEngine.Debug.Log("No player statistics available - AI chose randomly: " + aiChoice);
                }

                // Update statistics every time the player chooses a piece
                FirebaseManager.Instance?.DatabaseService?.UpdateRPSChoice(choice);

                ResolveBattle();
            });
        }
        else
        {
            // Normal mode - random choice
            aiChoice = (RPSUnit.RPSKind)UnityEngine.Random.Range(0, 3);
            UnityEngine.Debug.Log($"Battle initiated! Player chose {playerChoice}, AI chose {aiChoice} (random)");

            // Update statistics every time the player chooses a piece
            FirebaseManager.Instance?.DatabaseService?.UpdateRPSChoice(choice);

            ResolveBattle();
        }
    }

    /// <summary>
    /// Resolves the battle outcome and manages the animation sequence:
    /// 1. Hides the battle panel
    /// 2. Determines winner
    /// 3. Reveals both units' choices
    /// 4. Updates unit visuals
    /// 5. Triggers appropriate fight animations based on game mode
    /// 6. Handles special cases for Flag units
    /// </summary>
    private void ResolveBattle()
    {
        // Hide the RPS choice panel
        battlePanel?.SetActive(false);

        bool playerWins = Beats(playerChoice, aiChoice);
        bool aiWins = Beats(aiChoice, playerChoice);

        // Always reveal both units
        playerUnit.Kind = playerChoice;
        aiUnit.Kind = aiChoice;

        playerUnit.Reveal();
        aiUnit.Reveal();

        playerUnit.UpdateVisual();
        aiUnit.UpdateVisual();

        // For PvP mode, use simple logic without animation
        if (GameModeManager.Instance.SelectedMode == GameMode.PvP && PvPMoveLogger.Instance != null)
        {
            // Check for win if FLAG is revealed
            if (playerUnit.role == RPSUnit.UnitRole.Flag)
            {
                //FindObjectOfType<GameEndHandler>().ShowVictory("Player 2");
                return;
            }
            if (aiUnit.role == RPSUnit.UnitRole.Flag)
            {
                //   FindObjectOfType<GameEndHandler>().ShowVictory("Player 1");
                return;
            }

            if (playerWins)
            {
                UnityEngine.Debug.Log("✅ Player wins the battle!");
                  StartCoroutine(ShowFightResultAndFinish(true, false));
            }
            else if (aiWins)
            {
                UnityEngine.Debug.Log("❌ AI wins the battle!");
                StartCoroutine(ShowFightResultAndFinish(false, true)); // 👈 Use same animation!

            }
            else
            {
                UnityEngine.Debug.Log("🤝 Tie – rematch round!");
                battlePanel?.SetActive(true);
                Invoke(nameof(ShowPlayerPanel), 0.5f);
                return;
            }

            foreach (var controller in FindObjectsOfType<PlayerController>())
                controller.ClearSelection();
            
            return;
        }

        // For single player mode, update the battle animation display:
        // 1. Updates the weapon sprites for both units to show their choices
        // 2. These sprites will be visible during the fight animation sequence
        FightAnimationManager.Instance?.UpdateFightDisplaySprites(playerChoice, aiChoice);

        // Special case: If either unit is a Flag, skip the battle animation
        // Flag reveals trigger game end conditions handled elsewhere
        if (playerUnit.role == RPSUnit.UnitRole.Flag || aiUnit.role == RPSUnit.UnitRole.Flag)
            return;

        if (playerWins)
        {
            StartCoroutine(ShowFightResultAndFinish(true, false));
        }
        else if (aiWins)
        {
            StartCoroutine(ShowFightResultAndFinish(false, true));
        }
        else
        {
            battlePanel?.SetActive(true);
            Invoke(nameof(ShowPlayerPanel), 0.5f);
        }
    }

    /// <summary>
    /// Plays battle result animation, removes losing unit, moves winner to target position,
    /// updates AI knowledge, and cleans up battle state.
    /// </summary>
    /// <param name="playerWon">True if player won the battle</param>
    /// <param name="aiWon">True if AI won the battle</param>
    private IEnumerator ShowFightResultAndFinish(bool playerWon, bool aiWon)
    {
        // Play the fight result animation showing victory/defeat animations for both units
        yield return StartCoroutine(FightAnimationManager.Instance.ShowFightResult(playerWon, aiWon));

        if (playerWon)
        {
            UnityEngine.Debug.Log("✅ Player wins the battle!");
            
            // Update hard AI about destroyed unit
            var hardAI = FindObjectOfType<AIPlayerHardController>();
            if (hardAI != null)
            {
                hardAI.OnUnitDestroyed(aiUnit);
            }
            
            BoardManager.Instance.RemoveUnit(aiUnit);
            Destroy(aiUnit.gameObject);
            playerUnit.MoveTo(targetPos);
        }
        else if (aiWon)
        {
            UnityEngine.Debug.Log("❌ AI wins the battle!");
            
            // Update hard AI about destroyed unit
            var hardAI = FindObjectOfType<AIPlayerHardController>();
            if (hardAI != null)
            {
                hardAI.OnUnitDestroyed(playerUnit);
            }
            
            BoardManager.Instance.RemoveUnit(playerUnit);
            Destroy(playerUnit.gameObject);
            aiUnit.MoveTo(targetPos);
        }

        EndBattle();

        foreach (var controller in FindObjectsOfType<PlayerController>())
            controller.ClearSelection();
    }

    private void MoveUnitTo(RPSUnit unit, Vector2Int target)
    {
        unit.MoveTo(target);
    }

    private bool Beats(RPSUnit.RPSKind a, RPSUnit.RPSKind b)
    {
        return (a == RPSUnit.RPSKind.Rock && b == RPSUnit.RPSKind.Scissors) ||
               (a == RPSUnit.RPSKind.Paper && b == RPSUnit.RPSKind.Rock) ||
               (a == RPSUnit.RPSKind.Scissors && b == RPSUnit.RPSKind.Paper);
    }

    private void EndBattle()
    {
        isBattleActive = false;
        playerUnit = null;
        aiUnit = null;
        battlePanel?.SetActive(false);

        TurnManager.Instance?.EndTurn();
    }

    public bool IsBattleActive()
    {
        return isBattleActive;
    }

    public void SetBattleActive(bool active)
    {
        isBattleActive = active;
        if (!active)
        {
            battlePanel?.SetActive(false);
        }
    }
}