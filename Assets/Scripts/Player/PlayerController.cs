using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    private RPSUnit selectedUnit;
    private Outline activeOutline;

    public int columns = 7;
    public int rows = 6;
    public int myPlayerId = 1;

    void Update()
    {
        if (TurnManager.Instance == null) return;
        if (!TurnManager.Instance.IsPlayerTurn(myPlayerId)) return;
        if (BattleManager.Instance != null && BattleManager.Instance.IsBattleActive()) return;
        if (selectedUnit == null) return;

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
        if (unit.playerId != myPlayerId) return;
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
            if (other == unit) continue;
            if (other.Position == target)
            {
                if (other.playerId == myPlayerId)
                {
                    Debug.Log("🚫 Cell is occupied by your own unit");
                    return;
                }

                if (unit.Kind == other.Kind)
                {
                    Debug.Log("⚔️ Equal kinds – entering RPS battle mode!");
                    BattleManager.Instance?.StartBattle(unit, other, target);
                    return;
                }

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

        // אין יריב – פשוט זז
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
}
