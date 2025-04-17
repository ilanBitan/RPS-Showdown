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
        if (!selectedUnit.IsMovable()) return;

        Vector2Int direction = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.UpArrow)) direction = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.DownArrow)) direction = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) direction = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow)) direction = Vector2Int.right;

        if (direction != Vector2Int.zero)
        {
            StartCoroutine(HandleJumpAndMove(selectedUnit, direction));
        }
    }

    public void SelectUnit(RPSUnit unit)
    {
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

    System.Collections.IEnumerator HandleJumpAndMove(RPSUnit unit, Vector2Int dir)
    {
        // Trigger jump animation
        Animator anim = unit.GetComponent<Animator>();
        if (anim != null)
        {
    anim.SetInteger("playerId", unit.playerId); // 1 for player, 2 for enemy
    anim.ResetTrigger("jump");
    anim.SetTrigger("jump");
        }

        // Wait a short time to allow jump animation to show
        yield return new WaitForSeconds(0.2f);

        TryMoveUnit(unit, dir);
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

                // 💣 TRAP
                if (other.role == RPSUnit.UnitRole.Trap)
                {
                    Debug.Log("💥 Trap triggered! Attacker destroyed.");
                    Destroy(unit.gameObject);
                    ClearSelection();
                    TurnManager.Instance?.EndTurn();
                    return;
                }

                // 🏁 FLAG
                if (other.role == RPSUnit.UnitRole.Flag)
                {
                    Debug.Log("🎉 You captured the enemy FLAG! YOU WIN!");
                    Destroy(other.gameObject);
                    MoveUnitTo(unit, target);
                    ClearSelection();
                    Debug.Log($"🏆 Player {myPlayerId} wins the game!");
                    return;
                }

                // 🔁 RPS Battle
                if (unit.Kind == other.Kind)
                {
                    Debug.Log("⚔️ Equal kinds – entering RPS battle mode!");
                    BattleManager.Instance?.StartBattle(unit, other, target);
                    return;
                }

                // ✅ רגיל – קרב מבוסס חוקים
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
        StartCoroutine(SmoothMove(unit, targetTile, target));
    }
}
System.Collections.IEnumerator SmoothMove(RPSUnit unit, Transform targetTile, Vector2Int targetGridPos)
{
    RectTransform rt = unit.GetComponent<RectTransform>();
    if (rt == null) yield break;

    Vector3 start = rt.position;
    Vector3 end = targetTile.position;

    float elapsed = 0f;
    float duration = 0.25f; // smooth time (adjust as needed)

    while (elapsed < duration)
    {
        rt.position = Vector3.Lerp(start, end, elapsed / duration);
        elapsed += Time.deltaTime;
        yield return null;
    }

    // Snap to final position
    rt.position = end;

    // Update hierarchy and grid data
    unit.transform.SetParent(targetTile, false);
    rt.anchoredPosition = Vector2.zero;
    unit.Position = targetGridPos;

    Debug.Log($"✅ Smoothly moved to [col {targetGridPos.x}, row {targetGridPos.y}]");
}


    Transform GetTileTransform(Vector2Int pos)
    {
        int index = pos.y * columns + pos.x;
        Transform board = GameObject.Find("Board")?.transform;
        if (board == null || index >= board.childCount) return null;
        return board.GetChild(index);
    }
}
