using UnityEngine;
using TMPro;
using System.Collections;
using System.Diagnostics;

public class RPSUnit : Unit
{
    public enum RPSKind { Rock, Paper, Scissors }
    public enum UnitRole { None, Flag, Trap }

    public RPSKind Kind;
    public UnitRole role = UnitRole.None;

    private bool isRevealed = false;
    public bool IsRevealed => isRevealed;

    public override string UnitType => Kind.ToString();

    private UnitVisualController visualController;

    private void Awake()
    {
        visualController = GetComponent<UnitVisualController>();
    }

    private void Start()
    {
        // Don't show tools during setup - wait until setup is complete
        if (GameSetupManager.Instance != null && !GameSetupManager.Instance.IsSetupComplete())
        {
            // During setup, only show if it's a Flag or Trap
            if (role == UnitRole.Flag || role == UnitRole.Trap)
            {
                UpdateVisual();
            }
            return;
        }

        // After setup, show tools for player 1 units
        if (playerId == 1)
        {
            UpdateVisual();
        }

        // Make sure all units are selectable during gameplay
        EnableSetupSelection();
    }

    public override bool Beats(Unit other)
    {
        if (other is not RPSUnit enemy) return false;

        return (Kind == RPSKind.Rock && enemy.Kind == RPSKind.Scissors) ||
               (Kind == RPSKind.Paper && enemy.Kind == RPSKind.Rock) ||
               (Kind == RPSKind.Scissors && enemy.Kind == RPSKind.Paper);
    }

    public string GetLetter()
    {
        return role switch
        {
            UnitRole.Flag => "F",
            UnitRole.Trap => "T",
            _ => Kind switch
            {
                RPSKind.Rock => "R",
                RPSKind.Paper => "P",
                RPSKind.Scissors => "S",
                _ => ""
            }
        };
    }

    public void UpdateVisual()
    {
        var text = GetComponentInChildren<TextMeshProUGUI>();

        if (text != null)
        {
            // During setup phase
            if (GameSetupManager.Instance != null && !GameSetupManager.Instance.IsSetupComplete())
            {
                // Only show Flag and Trap during setup
                text.text = (role == UnitRole.Flag || role == UnitRole.Trap) ? GetLetter() : "";
            }
            else
            {
                // After setup: Show tools for own units (player 1) always, others only when revealed
                // In PvP mode, show tools for both players
                bool shouldShowTool = (playerId == 1) || isRevealed ||
                                    (GameModeManager.Instance.SelectedMode == GameMode.PvP);
                text.text = shouldShowTool ? GetLetter() : "";
            }
            text.color = Color.white;
        }

        if (visualController != null)
        {
            // During setup phase
            if (GameSetupManager.Instance != null && !GameSetupManager.Instance.IsSetupComplete())
            {
                // Only show Flag and Trap during setup
                visualController.UpdateWeaponVisual((role == UnitRole.Flag || role == UnitRole.Trap) ? GetLetter() : "");
            }
            else
            {
                // After setup: Show tools for own units (player 1) always, others only when revealed
                // In PvP mode, show tools for both players
                bool shouldShowTool = (playerId == 1) || isRevealed ||
                                    (GameModeManager.Instance.SelectedMode == GameMode.PvP);
                visualController.UpdateWeaponVisual(shouldShowTool ? GetLetter() : "");
            }
        }
    }

    public void Reveal()
    {
        isRevealed = true;
        UpdateVisual();
        UnityEngine.Debug.Log($"📣 {name} Revealed → {GetLetter()}");
        
        // עדכון ה-AI הקשה על דמות שנחשפה
        var hardAI = FindObjectOfType<AIPlayerHardController>();
        if (hardAI != null)
        {
            hardAI.OnUnitRevealed(this);
        }
    }

    public void ResetVisual()
    {
        var text = GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            // During setup phase
            if (GameSetupManager.Instance != null && !GameSetupManager.Instance.IsSetupComplete())
            {
                // Only show Flag and Trap during setup
                text.text = (role == UnitRole.Flag || role == UnitRole.Trap) ? GetLetter() : "";
            }
            else
            {
                // After setup: Keep own units visible, hide others
                text.text = (playerId == 1) ? GetLetter() : "";
            }
        }

        if (visualController != null)
        {
            // During setup phase
            if (GameSetupManager.Instance != null && !GameSetupManager.Instance.IsSetupComplete())
            {
                // Only show Flag and Trap during setup
                visualController.UpdateWeaponVisual((role == UnitRole.Flag || role == UnitRole.Trap) ? GetLetter() : "");
            }
            else
            {
                // After setup: Keep own units visible, hide others
                visualController.UpdateWeaponVisual((playerId == 1) ? GetLetter() : "");
            }
        }
    }

    public void EnableSetupSelection()
    {
        SelectableUnit selectable = GetComponent<SelectableUnit>();
        if (selectable == null)
            selectable = gameObject.AddComponent<SelectableUnit>();

        selectable.onSetupClick = () =>
        {
            // During setup phase
            if (GameSetupManager.Instance != null && !GameSetupManager.Instance.IsSetupComplete())
            {
                GameSetupManager.Instance.OnUnitClicked(this);
                return;
            }

            // During gameplay
            PlayerController playerController = FindObjectOfType<PlayerController>();
            if (playerController == null) return;

            // If it's not player's turn, ignore
            if (!TurnManager.Instance.IsPlayerTurn(1))
            {
                UnityEngine.Debug.Log("⏳ Wait for your turn.");
                return;
            }

            // If there's a battle active, ignore
            if (BattleManager.Instance != null && BattleManager.Instance.IsBattleActive())
            {
                UnityEngine.Debug.Log("⚔️ Battle in progress – cannot move now.");
                return;
            }

            // If this is a player unit
            if (IsPlayerControlled)
            {
                playerController.SelectUnit(this);
                return;
            }

            // If this is an AI unit
            RPSUnit selectedUnit = playerController.SelectedUnit;
            if (selectedUnit != null)
            {
                // Calculate if this unit is adjacent to the selected unit
                Vector2Int direction = Position - selectedUnit.Position;
                bool isAdjacent = Mathf.Abs(direction.x) + Mathf.Abs(direction.y) == 1;

                if (isAdjacent)
                {
                    // Try to attack this unit
                    selectedUnit.TryMove(direction);
                }
                else
                {
                    UnityEngine.Debug.Log("🚫 Can't attack - unit is too far away.");
                    playerController.ClearSelection();
                }
            }
        };
    }

    public void DisableSetupSelection()
    {
        SelectableUnit selectable = GetComponent<SelectableUnit>();
        if (selectable != null)
        {
            selectable.onSetupClick = null;
        }
    }

    public bool IsMovable()
    {
        return role != UnitRole.Flag && role != UnitRole.Trap;
    }

    public bool TryMove(Vector2Int direction)
    {
        // Add check for movable units
        if (!IsMovable())
        {
            UnityEngine.Debug.Log($"🚫 {role} units cannot move!");
            return false;
        }

        Vector2Int targetPos = Position + direction;

        if (!BoardManager.Instance.IsInsideBoard(targetPos))
        {
            UnityEngine.Debug.Log($"⛔ Move is out of board bounds");
            return false;
        }

        // Store original position for logging
        Vector2Int originalPosition = Position;

        Unit target = BoardManager.Instance.GetUnitAt(targetPos);

        if (target != null)
        {
            if (target.playerId == playerId)
            {
                UnityEngine.Debug.Log("🚫 Cell is occupied by your own unit");
                return false;
            }

            RPSUnit enemy = target as RPSUnit;
            if (enemy == null)
            {
                UnityEngine.Debug.Log("❌ Target is not a valid RPS unit");
                return false;
            }

            this.Reveal();
            enemy.Reveal();

            if (enemy.role == UnitRole.Trap)
            {
                UnityEngine.Debug.Log("💥 Trap triggered! Unit destroyed.");

                // Log move for PvP before destruction
                if (GameModeManager.Instance.SelectedMode == GameMode.PvP && PvPMoveLogger.Instance != null)
                {
                    PvPMoveLogger.Instance.LogPlayerMove(originalPosition, targetPos);
                }

                // עדכון ה-AI הקשה על דמות שהושמדה
                var hardAI = FindObjectOfType<AIPlayerHardController>();
                if (hardAI != null)
                {
                    hardAI.OnUnitDestroyed(this);
                }

                BoardManager.Instance.RemoveUnit(this);
                Destroy(this.gameObject);
                return false;
            }

            if (enemy.role == UnitRole.Flag)
            {
                UnityEngine.Debug.Log("🎯 Flag captured!");
                
                // עדכון ה-AI הקשה על דגל שהושמד
                var hardAI = FindObjectOfType<AIPlayerHardController>();
                if (hardAI != null)
                {
                    hardAI.OnUnitDestroyed(enemy);
                }
                
                BoardManager.Instance.RemoveUnit(enemy);
                Destroy(enemy.gameObject);
                MoveTo(targetPos);
                BoardManager.Instance.PlaceUnit(this, targetPos);

                // Log move for PvP
                if (GameModeManager.Instance.SelectedMode == GameMode.PvP && PvPMoveLogger.Instance != null)
                {
                    PvPMoveLogger.Instance.LogPlayerMove(originalPosition, targetPos);
                }

                return true;
            }

            // Battle logic
            if (this.Kind == enemy.Kind)
            {
                UnityEngine.Debug.Log("⚔️ Same type battle!");

                // Log move for PvP before battle
                if (GameModeManager.Instance.SelectedMode == GameMode.PvP && PvPMoveLogger.Instance != null)
                {
                    PvPMoveLogger.Instance.LogPlayerMove(originalPosition, targetPos);
                }

                BattleManager.Instance?.StartBattle(this, enemy, targetPos);
                return true;
            }

            if (this.Beats(enemy))
            {
                UnityEngine.Debug.Log($"✅ {this.Kind} beats {enemy.Kind}!");
                
                // עדכון ה-AI הקשה על דמות שהושמדה
                var hardAI = FindObjectOfType<AIPlayerHardController>();
                if (hardAI != null)
                {
                    hardAI.OnUnitDestroyed(enemy);
                }
                
                BoardManager.Instance.RemoveUnit(enemy);
                Destroy(enemy.gameObject);
                MoveTo(targetPos);
                BoardManager.Instance.PlaceUnit(this, targetPos);
            }
            else
            {
                UnityEngine.Debug.Log($"❌ {enemy.Kind} beats {this.Kind}!");
                
                // עדכון ה-AI הקשה על דמות שהושמדה
                var hardAI = FindObjectOfType<AIPlayerHardController>();
                if (hardAI != null)
                {
                    hardAI.OnUnitDestroyed(this);
                }
                
                BoardManager.Instance.RemoveUnit(this);
                Destroy(this.gameObject);
                return false;
            }

            // Log move for PvP
            if (GameModeManager.Instance.SelectedMode == GameMode.PvP && PvPMoveLogger.Instance != null)
            {
                PvPMoveLogger.Instance.LogPlayerMove(originalPosition, targetPos);
            }

            return true;
        }

        // Empty space movement
        MoveTo(targetPos);
        BoardManager.Instance.PlaceUnit(this, targetPos);

        // Log move for PvP
        if (GameModeManager.Instance.SelectedMode == GameMode.PvP && PvPMoveLogger.Instance != null)
        {
            PvPMoveLogger.Instance.LogPlayerMove(originalPosition, targetPos);
        }

        return true;
    }

    public void MoveTo(Vector2Int newPos)
    {
        Vector2Int oldPos = Position;
        
        // Handle board management logic
        BoardManager.Instance.MoveUnit(this, newPos);
        SetPosition(newPos);

        // עדכון ה-AI הקשה על תזוזה
        var hardAI = FindObjectOfType<AIPlayerHardController>();
        if (hardAI != null)
        {
            hardAI.OnUnitMoved(this, oldPos, newPos);
        }

        // Get the target tile transform
        Transform targetTile = BoardManager.Instance.GetTileTransform(newPos);
        if (targetTile != null)
        {
            // Start the animation coroutine
            StartCoroutine(SmoothMove(targetTile, newPos));
        }

        UnityEngine.Debug.Log($"✅ Unit move initiated to → Column: {newPos.x}, Row: {newPos.y}");
    }

    private IEnumerator SmoothMove(Transform targetTile, Vector2Int targetGridPos)
    {
        // Trigger jump animation
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetInteger("playerId", playerId);
            anim.ResetTrigger("jump");
            anim.SetTrigger("jump");
        }

        // Wait for animation to start
        yield return new WaitForSeconds(0.2f);

        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector3 start = rt.position;
        Vector3 end = targetTile.position;

        float elapsed = 0f;
        float duration = 0.25f; // smooth time

        while (elapsed < duration)
        {
            rt.position = Vector3.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap to final position
        rt.position = end;

        // Update hierarchy and grid data
        transform.SetParent(targetTile, false);
        rt.anchoredPosition = Vector2.zero;

        // Make sure the board state is updated
        BoardManager.Instance.PlaceUnit(this, targetGridPos);

        UnityEngine.Debug.Log($"✅ Unit smoothly moved to [col {targetGridPos.x}, row {targetGridPos.y}]");

        // Only end turn if this is a player-controlled unit and not in battle
        if (IsPlayerControlled && !BattleManager.Instance.IsBattleActive())
        {
            TurnManager.Instance?.EndTurn();
        }
    }

    public bool IsEnemy(RPSUnit other)
    {
        return other != null && other.playerId != this.playerId;
    }
}