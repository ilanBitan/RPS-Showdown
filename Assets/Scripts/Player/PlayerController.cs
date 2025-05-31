using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Diagnostics;
using TMPro;

public class PlayerController : MonoBehaviour
{
    private RPSUnit selectedUnit;
    private Outline activeOutline;
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
            StartCoroutine(HandleJumpAndMove(selectedUnit, direction));
        }
    }

    void Start()
    {
        gameEnded = false;
    }

    void Update()
    {
        if (gameEnded) return;

        if (TurnManager.Instance == null) return;
        if (!TurnManager.Instance.IsPlayerTurn(myPlayerId)) return;
        if (BattleManager.Instance != null && BattleManager.Instance.IsBattleActive()) return;
        if (selectedUnit == null) return;
        if (!selectedUnit.IsMovable()) return;

        // Support for both keyboard and touch/click input
        Vector2Int direction = Vector2Int.zero;

        // Arrow key movement
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
        if (gameEnded)
            return;

        if (unit.role == RPSUnit.UnitRole.Flag || unit.role == RPSUnit.UnitRole.Trap)
        {
            UnityEngine.Debug.Log("⛔ You cannot select a Flag or Trap.");
            return;
        }

        if (unit.playerId != myPlayerId) return;
        if (!unit.IsMovable()) return;
        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn(myPlayerId)) return;
        if (BattleManager.Instance != null && BattleManager.Instance.IsBattleActive()) return;

        // Clear previous selection
        ClearSelection();

        selectedUnit = unit;
        UnityEngine.Debug.Log($"🎯 Selected unit at [col {unit.Position.x}, row {unit.Position.y}]");

        // Add outline to selected unit
        Outline outline = unit.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.cyan;
        outline.effectDistance = new Vector2(5f, 5f);
        activeOutline = outline;

        // Highlight valid move tiles
        HighlightValidMoveTiles();
    }

    private void HighlightValidMoveTiles()
    {
        if (selectedUnit == null) return;

        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int targetPos = selectedUnit.Position + dir;
            if (IsValidMovePosition(targetPos))
            {
                Transform tile = GetTileTransform(targetPos);
                if (tile != null)
                {
                    // Add a subtle highlight effect
                    var highlight = tile.gameObject.AddComponent<UnityEngine.UI.Outline>();
                    highlight.effectColor = new Color(0.5f, 1f, 0.5f, 0.5f);
                    highlight.effectDistance = new Vector2(2f, 2f);
                }
            }
        }
    }

    private bool IsValidMovePosition(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= columns || pos.y < 0 || pos.y >= rows)
            return false;

        return true;
    }

    public void ClearSelection()
    {
        // Clear unit outline
        if (activeOutline != null)
            Destroy(activeOutline);
        
        // Clear tile highlights
        foreach (Transform tile in GameObject.Find("Board").transform)
        {
            var highlight = tile.GetComponent<UnityEngine.UI.Outline>();
            if (highlight != null)
                Destroy(highlight);
        }

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
            UnityEngine.Debug.Log("⛔ Move is out of board bounds");
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
                    UnityEngine.Debug.Log("🚫 Cell is occupied by your own unit");
                    return;
                }

                if (other.role == RPSUnit.UnitRole.Flag)
                {
                    UnityEngine.Debug.Log("🎯 You captured the enemy FLAG! YOU WIN!");
                    other.Reveal();
                    MoveUnitTo(unit, target);
                    Destroy(other.gameObject);
                    ClearSelection();
                    gameEnded = true;
                    
                    // Set player as winner
                    TurnTimerManager.Instance?.SetPlayerWon(true);
                    
                    // Stop all game systems
                    TurnManager.Instance?.StopGame();
                    return;
                }

                if (other.role == RPSUnit.UnitRole.Trap)
                {
                    UnityEngine.Debug.Log("💥 Trap triggered! Attacker destroyed.");

                    unit.Reveal();
                    Destroy(unit.gameObject);
                    ClearSelection();
                    TurnManager.Instance?.EndTurn();
                    return;
                }

                // 🏁 FLAG
                //if (other.role == RPSUnit.UnitRole.Flag)
                //{
                //    Debug.Log("🎉 You captured the enemy FLAG! YOU WIN!");
                //    Destroy(other.gameObject);
                //    MoveUnitTo(unit, target);
                //    ClearSelection();
                //    Debug.Log($"🏆 Player {myPlayerId} wins the game!");
                //    return;
                //}

                // 🔁 RPS Battle
                if (unit.Kind == other.Kind)
                {
                    UnityEngine.Debug.Log("⚔️ Equal kinds – entering RPS battle mode!");
                    BattleManager.Instance?.StartBattle(unit, other, target);
                    return;
                }

                unit.Reveal();
                other.Reveal();

                if (unit.Beats(other))
                {
                    UnityEngine.Debug.Log("✅ Attacker wins – replacing enemy");
                    BoardManager.Instance.RemoveUnit(other);
                    Destroy(other.gameObject);
                    MoveUnitTo(unit, target);
                }
                else
                {
                    UnityEngine.Debug.Log("❌ Attacker loses – removed");
                    BoardManager.Instance.RemoveUnit(unit);
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
        BoardManager.Instance.PlaceUnit(unit, targetGridPos);//add


        UnityEngine.Debug.Log($"✅ Smoothly moved to [col {targetGridPos.x}, row {targetGridPos.y}]");
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

        // Destroy all managers to start fresh
        TurnTimerManager timer = FindObjectOfType<TurnTimerManager>();
        if (timer != null) Destroy(timer.gameObject);

        AIPlayerController ai = FindObjectOfType<AIPlayerController>();
        if (ai != null) Destroy(ai.gameObject);

        TurnManager tm = FindObjectOfType<TurnManager>();
        if (tm != null) Destroy(tm.gameObject);

        BoardManager bm = FindObjectOfType<BoardManager>();
        if (bm != null) Destroy(bm.gameObject);

        // Reload the scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnTileTapped(Vector2Int tilePos)
    {
        if (selectedUnit == null)
        {
            UnityEngine.Debug.Log("❌ No unit selected.");
            return;
        }

        if (!selectedUnit.IsMovable())
        {
            UnityEngine.Debug.Log("⛔ Selected unit cannot move.");
            return;
        }

        if (BattleManager.Instance != null && BattleManager.Instance.IsBattleActive())
        {
            UnityEngine.Debug.Log("⚔️ Battle in progress – cannot move now.");
            return;
        }

        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn(myPlayerId))
        {
            UnityEngine.Debug.Log("⏳ Not your turn.");
            return;
        }

        Vector2Int direction = tilePos - selectedUnit.Position;
        UnityEngine.Debug.Log($"📍 Tile tapped at {tilePos}, selected unit at {selectedUnit.Position}, direction {direction}");

        if (Mathf.Abs(direction.x) + Mathf.Abs(direction.y) == 1)
        {
            UnityEngine.Debug.Log("✅ Valid move. Trying to move unit...");
            TryMoveUnit(selectedUnit, direction);
        }
        else
        {
            UnityEngine.Debug.Log("🚫 Invalid move – must move one tile only.");
        }
    }
}
