using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    private RPSUnit selectedUnit;
    private Outline activeOutline;
    public GameObject playAgainContainer;
    public int columns = 7;
    public int rows = 6;
    public int myPlayerId = 1;

    public static bool gameEnded = false; // 🛡️ משתנה שמנהל סיום משחק

    // ✅ מאפיין לקריאה חיצונית
    public RPSUnit SelectedUnit => selectedUnit;

    // ✅ מתודת עזר להזזת היחידה הנבחרת
    public void TryMoveSelectedUnit(Vector2Int direction)
    {
        if (selectedUnit != null)
        {
            TryMoveUnit(selectedUnit, direction);
        }
    }

    void Update()
    {
        if (gameEnded)
            return;

        if (TurnManager.Instance == null) return;
        if (!TurnManager.Instance.IsPlayerTurn(myPlayerId)) return;
        if (BattleManager.Instance != null && BattleManager.Instance.IsBattleActive()) return;
        if (selectedUnit == null) return;
        if (!selectedUnit.IsMovable()) return;

        Vector2Int direction = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.UpArrow)) direction = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.DownArrow)) direction = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) direction = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow)) direction = Vector2Int.right;

        if (direction != Vector2Int.zero)
        {
            TryMoveUnit(selectedUnit, direction);
        }
    }

    public void SelectUnit(RPSUnit unit)
    {
        if (gameEnded)
            return;

        if (unit.role == RPSUnit.UnitRole.Flag || unit.role == RPSUnit.UnitRole.Trap)
        {
            Debug.Log("⛔ You cannot select a Flag or Trap.");
            return;
        }

        if (unit.playerId != myPlayerId) return;
        if (!unit.IsMovable()) return;
        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn(myPlayerId)) return;
        if (BattleManager.Instance != null && BattleManager.Instance.IsBattleActive()) return;

        selectedUnit = unit;
        Debug.Log($"🎯 Selected unit at [col {unit.Position.x}, row {unit.Position.y}]");

        if (activeOutline != null)
            Destroy(activeOutline);

        Outline outline = unit.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.cyan;
        outline.effectDistance = new Vector2(5f, 5f);
        activeOutline = outline;
    }

    public void ClearSelection()
    {
        if (activeOutline != null)
            Destroy(activeOutline);
        selectedUnit = null;
    }

    void TryMoveUnit(RPSUnit unit, Vector2Int dir)
    {
        Vector2Int target = unit.Position + dir;

        if (target.x < 0 || target.x >= columns || target.y < 0 || target.y >= rows)
        {
            Debug.Log("⛔ Move is out of board bounds");
            return;
        }

        foreach (var other in FindObjectsOfType<RPSUnit>())
        {
            if (other == null) continue;
            if (other == unit) continue;
            if (other.Position == target)
            {
                if (other.playerId == myPlayerId)
                {
                    Debug.Log("🚫 Cell is occupied by your own unit");
                    return;
                }

                if (other.role == RPSUnit.UnitRole.Flag)
                {
                    Debug.Log("🎯 You captured the enemy FLAG!");

                    other.Reveal();
                    MoveUnitTo(unit, target);
                    Destroy(other.gameObject);
                    ClearSelection();
                    gameEnded = true;
                    if (playAgainContainer != null)
                    {
                        playAgainContainer.SetActive(true);
                    }

                    return;
                }

                if (other.role == RPSUnit.UnitRole.Trap)
                {
                    Debug.Log("💥 Trap triggered! Attacker destroyed.");

                    unit.Reveal();
                    Destroy(unit.gameObject);
                    ClearSelection();
                    TurnManager.Instance?.EndTurn();
                    return;
                }

                if (unit.Kind == other.Kind)
                {
                    Debug.Log("⚔️ Equal kinds – entering RPS battle mode!");
                    BattleManager.Instance?.StartBattle(unit, other, target);
                    return;
                }

                unit.Reveal();
                other.Reveal();

                if (unit.Beats(other))
                {
                    Debug.Log("✅ Attacker wins – replacing enemy");
                    Destroy(other.gameObject);
                    MoveUnitTo(unit, target);
                }
                else
                {
                    Debug.Log("❌ Attacker loses – removed");
                    Destroy(unit.gameObject);
                }

                ClearSelection();
                TurnManager.Instance?.EndTurn();
                return;
            }
        }

        MoveUnitTo(unit, target);
        ClearSelection();
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
            BoardManager.Instance.PlaceUnit(unit, target);
            Debug.Log($"✅ Unit moved to [col {target.x}, row {target.y}]");
        }
    }

    Transform GetTileTransform(Vector2Int pos)
    {
        int index = pos.y * columns + pos.x;
        Transform board = GameObject.Find("Board")?.transform;
        if (board == null || index >= board.childCount) return null;
        return board.GetChild(index);
    }

    public void OnPlayAgainButtonClicked()
    {
        PlayerController.gameEnded = false;

        TurnTimerManager timer = FindObjectOfType<TurnTimerManager>();
        if (timer != null) Destroy(timer.gameObject);

        AIPlayerController ai = FindObjectOfType<AIPlayerController>();
        if (ai != null) Destroy(ai.gameObject);

        TurnManager tm = FindObjectOfType<TurnManager>();
        if (tm != null) Destroy(tm.gameObject);

        BoardManager bm = FindObjectOfType<BoardManager>();
        if (bm != null) Destroy(bm.gameObject);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnTileTapped(Vector2Int tilePos)
    {
        if (selectedUnit == null)
        {
            Debug.Log("❌ No unit selected.");
            return;
        }

        if (!selectedUnit.IsMovable())
        {
            Debug.Log("⛔ Selected unit cannot move.");
            return;
        }

        if (BattleManager.Instance != null && BattleManager.Instance.IsBattleActive())
        {
            Debug.Log("⚔️ Battle in progress – cannot move now.");
            return;
        }

        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn(myPlayerId))
        {
            Debug.Log("⏳ Not your turn.");
            return;
        }

        Vector2Int direction = tilePos - selectedUnit.Position;
        Debug.Log($"📍 Tile tapped at {tilePos}, selected unit at {selectedUnit.Position}, direction {direction}");

        if (Mathf.Abs(direction.x) + Mathf.Abs(direction.y) == 1)
        {
            Debug.Log("✅ Valid move. Trying to move unit...");
            TryMoveUnit(selectedUnit, direction);
        }
        else
        {
            Debug.Log("🚫 Invalid move – must move one tile only.");
        }
    }
}
