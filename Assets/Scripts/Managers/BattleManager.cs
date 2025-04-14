using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("Attacker Panel")]
    public GameObject battlePanel_Attacker;
    public TextMeshProUGUI titleText_Attacker;
    public Button rockButton_A;
    public Button paperButton_A;
    public Button scissorsButton_A;

    [Header("Defender Panel")]
    public GameObject battlePanel_Defender;
    public TextMeshProUGUI titleText_Defender;
    public Button rockButton_D;
    public Button paperButton_D;
    public Button scissorsButton_D;

    private RPSUnit attacker;
    private RPSUnit defender;
    private Vector2Int targetPos;

    private bool isBattleActive = false;

    private enum BattleTurn { Attacker, Defender }
    private BattleTurn currentTurn;

    private RPSUnit.RPSKind attackerChoice;
    private RPSUnit.RPSKind defenderChoice;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        battlePanel_Attacker?.SetActive(false);
        battlePanel_Defender?.SetActive(false);
    }

    public bool IsBattleActive()
    {
        return isBattleActive;
    }

    public void StartBattle(RPSUnit attackingUnit, RPSUnit defendingUnit, Vector2Int target)
    {
        attacker = attackingUnit;
        defender = defendingUnit;
        targetPos = target;
        isBattleActive = true;
        currentTurn = BattleTurn.Attacker;

        ShowPanelForCurrentTurn();
    }

    void ShowPanelForCurrentTurn()
    {
        battlePanel_Attacker?.SetActive(false);
        battlePanel_Defender?.SetActive(false);

        if (currentTurn == BattleTurn.Attacker)
        {
            battlePanel_Attacker?.SetActive(true);
            titleText_Attacker.text = "Attacker: Choose Rock / Paper / Scissors";

            rockButton_A.onClick.RemoveAllListeners();
            paperButton_A.onClick.RemoveAllListeners();
            scissorsButton_A.onClick.RemoveAllListeners();

            rockButton_A.onClick.AddListener(() => OnPlayerChoice(RPSUnit.RPSKind.Rock));
            paperButton_A.onClick.AddListener(() => OnPlayerChoice(RPSUnit.RPSKind.Paper));
            scissorsButton_A.onClick.AddListener(() => OnPlayerChoice(RPSUnit.RPSKind.Scissors));
        }
        else
        {
            battlePanel_Defender?.SetActive(true);
            titleText_Defender.text = "Defender: Choose Rock / Paper / Scissors";

            rockButton_D.onClick.RemoveAllListeners();
            paperButton_D.onClick.RemoveAllListeners();
            scissorsButton_D.onClick.RemoveAllListeners();

            rockButton_D.onClick.AddListener(() => OnPlayerChoice(RPSUnit.RPSKind.Rock));
            paperButton_D.onClick.AddListener(() => OnPlayerChoice(RPSUnit.RPSKind.Paper));
            scissorsButton_D.onClick.AddListener(() => OnPlayerChoice(RPSUnit.RPSKind.Scissors));
        }
    }

    void OnPlayerChoice(RPSUnit.RPSKind choice)
    {
        battlePanel_Attacker?.SetActive(false);
        battlePanel_Defender?.SetActive(false);

        if (currentTurn == BattleTurn.Attacker)
        {
            attackerChoice = choice;
            currentTurn = BattleTurn.Defender;
            Invoke(nameof(ShowPanelForCurrentTurn), 0.5f);
        }
        else
        {
            defenderChoice = choice;
            ResolveBattle();
        }
    }

    void ResolveBattle()
    {
        Debug.Log($"⚔️ Battle: Attacker chose {attackerChoice}, Defender chose {defenderChoice}");

        if (Beats(attackerChoice, defenderChoice))
        {
            Debug.Log("✅ Attacker wins the battle!");
            attacker.Kind = attackerChoice;
            attacker.UpdateVisual();

            // 🧼 מנקה בחירה מכל השחקנים
            foreach (var controller in FindObjectsOfType<PlayerController>())
            {
                controller.ClearSelection();
            }

            Destroy(defender.gameObject);
            MoveUnitTo(attacker, targetPos);
            EndBattle();
        }
        else if (Beats(defenderChoice, attackerChoice))
        {
            Debug.Log("❌ Attacker lost the battle!");
            defender.Kind = defenderChoice;
            defender.UpdateVisual();

            Destroy(attacker.gameObject);
            EndBattle();
        }
        else
        {
            Debug.Log("🤝 Tie – rematch round!");
            currentTurn = BattleTurn.Attacker;
            Invoke(nameof(ShowPanelForCurrentTurn), 0.5f);
        }
    }

    void EndBattle()
    {
        isBattleActive = false;
        attacker = null;
        defender = null;
        TurnManager.Instance?.EndTurn();
    }

    void MoveUnitTo(RPSUnit unit, Vector2Int target)
    {
        Transform targetTile = GetTileTransform(target);
        if (targetTile != null)
        {
            unit.transform.SetParent(targetTile, false);
            RectTransform rt = unit.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            unit.Position = target;
            Debug.Log($"✅ Unit moved to [col {target.x}, row {target.y}]");
        }
    }

    Transform GetTileTransform(Vector2Int pos)
    {
        int index = pos.y * 7 + pos.x;
        Transform board = GameObject.Find("Board")?.transform;
        if (board == null || index >= board.childCount) return null;
        return board.GetChild(index);
    }

    bool Beats(RPSUnit.RPSKind a, RPSUnit.RPSKind b)
    {
        return (a == RPSUnit.RPSKind.Rock && b == RPSUnit.RPSKind.Scissors) ||
               (a == RPSUnit.RPSKind.Paper && b == RPSUnit.RPSKind.Rock) ||
               (a == RPSUnit.RPSKind.Scissors && b == RPSUnit.RPSKind.Paper);
    }
}
