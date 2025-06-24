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

    public void ShowPlayerPanel()
    {
        // Update weapon display before animation starts
if (GameModeManager.Instance.SelectedMode == GameMode.PvP && PvPMoveLogger.Instance != null)
    {
    if (playerUnit.playerId != 1)
    {
        var temp = playerUnit;
        playerUnit = aiUnit;
        aiUnit = temp;
    }
    }

        FightAnimationManager.Instance?.UpdatePreChoiceWeaponDisplay(playerUnit.Kind, aiUnit.Kind);

        StartCoroutine(AnimateFightPanelThenShowBattle());
    }

    private IEnumerator AnimateFightPanelThenShowBattle()
    {
        // Play the fight intro animation
        yield return StartCoroutine(FightAnimationManager.Instance.PlayFightIntroAnimation());

        battlePanel?.SetActive(true);

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

        // בדיקה אם זו רמה קשה
        bool isHardLevel = FindObjectOfType<AIPlayerHardController>() != null;

        if (isHardLevel)
        {
            FirebaseManager.Instance?.DatabaseService?.GetUserStats((userData) =>
            {
                if (userData != null)
                {
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

                    switch (mostCommonChoice)
                    {
                        case RPSUnit.RPSKind.Rock: aiChoice = RPSUnit.RPSKind.Paper; break;
                        case RPSUnit.RPSKind.Paper: aiChoice = RPSUnit.RPSKind.Scissors; break;
                        case RPSUnit.RPSKind.Scissors: aiChoice = RPSUnit.RPSKind.Rock; break;
                    }
                }
                else
                {
                    aiChoice = (RPSUnit.RPSKind)Random.Range(0, 3);
                }

                FirebaseManager.Instance?.DatabaseService?.UpdateRPSChoice(choice);
                ResolveBattle();
            });
        }
        else
        {
            aiChoice = (RPSUnit.RPSKind)Random.Range(0, 3);
            FirebaseManager.Instance?.DatabaseService?.UpdateRPSChoice(choice);
            ResolveBattle();
        }
    }

    private void ResolveBattle()
    {
        battlePanel?.SetActive(false);

        bool playerWins = Beats(playerChoice, aiChoice);
        bool aiWins = Beats(aiChoice, playerChoice);

        playerUnit.Kind = playerChoice;
        aiUnit.Kind = aiChoice;

        playerUnit.Reveal();
        aiUnit.Reveal();

        playerUnit.UpdateVisual();
        aiUnit.UpdateVisual();

        // Update the fight display sprites through animation manager
        FightAnimationManager.Instance?.UpdateFightDisplaySprites(playerChoice, aiChoice);

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

    private IEnumerator ShowFightResultAndFinish(bool playerWon, bool aiWon)
    {
        // Play the fight result animation
        yield return StartCoroutine(FightAnimationManager.Instance.ShowFightResult(playerWon, aiWon));

        if (playerWon)
        {
    
            UnityEngine.Debug.Log("✅ Player wins the battle!");
            
            // עדכון ה-AI הקשה על דמות שהושמדה
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
                        // עדכון ה-AI הקשה על דמות שהושמדה

            UnityEngine.Debug.Log("❌ AI wins the battle!");
            
            // עדכון ה-AI הקשה על דמות שהושמדה
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

    private void EndBattle()
    {
        isBattleActive = false;
        playerUnit = null;
        aiUnit = null;
        battlePanel?.SetActive(false);

        TurnManager.Instance?.EndTurn();
    }

    private bool Beats(RPSUnit.RPSKind a, RPSUnit.RPSKind b)
    {
        return (a == RPSUnit.RPSKind.Rock && b == RPSUnit.RPSKind.Scissors) ||
               (a == RPSUnit.RPSKind.Paper && b == RPSUnit.RPSKind.Rock) ||
               (a == RPSUnit.RPSKind.Scissors && b == RPSUnit.RPSKind.Paper);
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
