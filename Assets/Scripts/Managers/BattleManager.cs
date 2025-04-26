
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
     //   if (GameEndHandler.gameEnded)
      //      return;

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

        ShowPlayerPanel();
    }

    private void ShowPlayerPanel()
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
        aiChoice = (RPSUnit.RPSKind)Random.Range(0, 3);

        Debug.Log($"⚔️ Battle initiated! Player chose {playerChoice}, AI chose {aiChoice}");
        ResolveBattle();
    }

    private void ResolveBattle()
    {
        battlePanel?.SetActive(false);

        bool playerWins = Beats(playerChoice, aiChoice);
        bool aiWins = Beats(aiChoice, playerChoice);

        // חשיפת שתי היחידות תמיד
        playerUnit.Kind = playerChoice;
        aiUnit.Kind = aiChoice;

        playerUnit.Reveal();
        aiUnit.Reveal();

        playerUnit.UpdateVisual();
        aiUnit.UpdateVisual();

        // בדיקת ניצחון אם FLAG נחשף
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
            Debug.Log("✅ Player wins the battle!");

            Destroy(aiUnit.gameObject);
            MoveUnitTo(playerUnit, targetPos);
        }
        else if (aiWins)
        {
            Debug.Log("❌ AI wins the battle!");

            Destroy(playerUnit.gameObject);
        }
        else
        {
            Debug.Log("🤝 Tie – rematch round!");
            Invoke(nameof(ShowPlayerPanel), 0.5f);
            return;
        }

        foreach (var controller in FindObjectsOfType<PlayerController>())
            controller.ClearSelection();

        EndBattle();
    }


    private void MoveUnitTo(RPSUnit unit, Vector2Int target)
    {
        Transform targetTile = BoardManager.Instance.GetTileTransform(target);
        if (targetTile != null)
        {
            unit.transform.SetParent(targetTile, false);
            RectTransform rt = unit.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            unit.Position = target;
            Debug.Log($"✅ Unit moved to [col {target.x}, row {target.y}]");
        }
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
}
