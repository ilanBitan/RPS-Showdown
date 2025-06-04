using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static RPSUnit;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("Battle UI")]
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

        StartCoroutine(StartBattleSequence());
    }

    private IEnumerator StartBattleSequence()
    {
        // Play intro animation
        yield return StartCoroutine(FightAnimationManager.Instance.PlayFightIntroAnimation());
        
        // Show battle UI
        ShowBattleUI();
    }

    private void ShowBattleUI()
    {
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

        // Update unit data
        UpdateUnits();

        // Handle special case for flags
        if (playerUnit.role == RPSUnit.UnitRole.Flag || aiUnit.role == RPSUnit.UnitRole.Flag)
            return;

        // Handle battle result
        if (playerWins)
        {
            StartCoroutine(HandleBattleResult(true, false));
        }
        else if (aiWins)
        {
            StartCoroutine(HandleBattleResult(false, true));
        }
        else
        {
            // Tie - restart battle
        StartCoroutine(HandleTieReplay());
        }
    }
private IEnumerator HandleTieReplay()
{
    yield return StartCoroutine(FightAnimationManager.Instance.PlayFightIntroAnimation());
    ShowBattleUI(); // shows rock/paper/scissors buttons again
}
    private void UpdateUnits()
    {
        playerUnit.Kind = playerChoice;
        aiUnit.Kind = aiChoice;

        playerUnit.Reveal();
        aiUnit.Reveal();

        playerUnit.UpdateVisual();
        aiUnit.UpdateVisual();
    }

    private IEnumerator HandleBattleResult(bool playerWon, bool aiWon)
    {
        // Update visual displays right before showing result animation
        FightAnimationManager.Instance.UpdateWeaponDisplays(playerChoice, aiChoice);
        
        // Play result animation
        yield return StartCoroutine(FightAnimationManager.Instance.PlayFightResultAnimation(playerWon, aiWon));

        // Handle unit removal and movement
        if (playerWon)
        {
            BoardManager.Instance.RemoveUnit(aiUnit);
            Destroy(aiUnit.gameObject);
            playerUnit.MoveTo(targetPos);
        }
        else if (aiWon)
        {
            BoardManager.Instance.RemoveUnit(playerUnit);
            Destroy(playerUnit.gameObject);
            aiUnit.MoveTo(targetPos);
        }

        EndBattle();
    }

    private IEnumerator RestartBattle()
    {
        yield return new WaitForSeconds(0.5f);
        ShowBattleUI();
    }

    private void EndBattle()
    {
        isBattleActive = false;
        playerUnit = null;
        aiUnit = null;
        battlePanel?.SetActive(false);

        TurnManager.Instance?.EndTurn();

        foreach (var controller in FindObjectsOfType<PlayerController>())
            controller.ClearSelection();
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
}