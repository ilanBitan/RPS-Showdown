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

            // Show trap animation
            StartCoroutine(HandleTrapEncounter(this, enemy, targetPos));
            return false;
        }

        if (enemy.role == UnitRole.Flag)
        {
            UnityEngine.Debug.Log("🎯 Flag captured!");

            // Log move for PvP
            if (GameModeManager.Instance.SelectedMode == GameMode.PvP && PvPMoveLogger.Instance != null)
            {
                PvPMoveLogger.Instance.LogPlayerMove(originalPosition, targetPos);
            }

            // Show flag capture animation
            StartCoroutine(HandleFlagCapture(this, enemy, targetPos));
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

        StartCoroutine(ExecuteCombatWithAnimation(this, enemy, targetPos, originalPosition));
        return false;
    }

    // Empty space movement
    MoveTo(targetPos);
    //BoardManager.Instance.PlaceUnit(this, targetPos);

    // Log move for PvP
    if (GameModeManager.Instance.SelectedMode == GameMode.PvP && PvPMoveLogger.Instance != null)
    {
        PvPMoveLogger.Instance.LogPlayerMove(originalPosition, targetPos);
    }

    return true;
}

// New method to handle trap encounters with animation
private IEnumerator HandleTrapEncounter(RPSUnit attacker, RPSUnit trap, Vector2Int targetPos)
{
    // Show trap animation
    if (FightAnimationManager.Instance != null)
    {
        FightAnimationManager.Instance.fightPanel?.SetActive(true);
        FightAnimationManager.Instance.fightPlayer?.SetActive(true);
        FightAnimationManager.Instance.fightEnemy?.SetActive(true);
        yield return null;

        // Update weapon display for trap encounter
        // Always show from player's perspective (player vs trap)
        bool isPlayerUnit = attacker.playerId == 1;
        if (isPlayerUnit)
        {
            FightAnimationManager.Instance.UpdatePreChoiceWeaponDisplay(attacker.Kind, trap.role);
            FightAnimationManager.Instance.UpdateFightDisplaySprites(attacker.Kind, trap.role);
        }
        else
        {
            // AI unit hitting trap - show AI unit vs trap
            FightAnimationManager.Instance.UpdatePreChoiceWeaponDisplay(trap.role, attacker.Kind);
            FightAnimationManager.Instance.UpdateFightDisplaySprites(trap.role, attacker.Kind);
        }
        
        // Show trap result (whoever steps on trap loses)
        yield return StartCoroutine(FightAnimationManager.Instance.ShowTrapResult(isPlayerUnit));
    }

    // עדכון ה-AI הקשה על דמות שהושמדה
    var hardAI = FindObjectOfType<AIPlayerHardController>();
    if (hardAI != null)
    {
        hardAI.OnUnitDestroyed(attacker);
    }

    BoardManager.Instance.RemoveUnit(attacker);
    StartCoroutine(PlayJumpAndRemove(attacker.gameObject));
}

// New method to handle flag capture with animation
private IEnumerator HandleFlagCapture(RPSUnit attacker, RPSUnit flag, Vector2Int targetPos)
{
    // Show flag capture animation
    if (FightAnimationManager.Instance != null)
    {
        FightAnimationManager.Instance.fightPanel?.SetActive(true);
        FightAnimationManager.Instance.fightPlayer?.SetActive(true);
        FightAnimationManager.Instance.fightEnemy?.SetActive(true);
        yield return null;

        // Update weapon display for flag capture
        bool isPlayerUnit = attacker.playerId == 1;
        if (isPlayerUnit)
        {
            FightAnimationManager.Instance.UpdatePreChoiceWeaponDisplay(attacker.Kind, flag.role);
            FightAnimationManager.Instance.UpdateFightDisplaySprites(attacker.Kind, flag.role);
        }
        else
        {
            // AI capturing flag
            FightAnimationManager.Instance.UpdatePreChoiceWeaponDisplay(flag.role, attacker.Kind);
            FightAnimationManager.Instance.UpdateFightDisplaySprites(flag.role, attacker.Kind);
        }
        
        // Show flag capture result
        yield return StartCoroutine(FightAnimationManager.Instance.ShowFlagCaptureResult(isPlayerUnit));
    }

    // עדכון ה-AI הקשה על דגל שהושמד
    var hardAI = FindObjectOfType<AIPlayerHardController>();
    if (hardAI != null)
    {
        hardAI.OnUnitDestroyed(flag);
    }

    BoardManager.Instance.RemoveUnit(flag);
    Destroy(flag.gameObject);
    MoveTo(targetPos);
    //BoardManager.Instance.PlaceUnit(this, targetPos);

    PlayerController.gameEnded = true;

    // Set winner based on who captured the flag
    bool playerWon = attacker.playerId == 1;
    TurnTimerManager.Instance?.SetPlayerWon(playerWon);

    // Stop all game systems
    TurnManager.Instance?.StopGame();
}


public IEnumerator HandleTrapEncounter(RPSUnit trapUnit)
{
    yield return StartCoroutine(HandleTrapEncounter(this, trapUnit, trapUnit.Position));
}

public IEnumerator HandleFlagCapture(RPSUnit flagUnit)
{
    yield return StartCoroutine(HandleFlagCapture(this, flagUnit, flagUnit.Position));
}

    
        public IEnumerator ExecuteCombatWithAnimation(RPSUnit attacker, RPSUnit defender, Vector2Int targetPos, Vector2Int originalPosition)
    {
        UnityEngine.Debug.Log($"🔥 im from rpsunit {attacker.name} is attacking {defender.name}");
        if (FightAnimationManager.Instance != null)
        {
            // עדכון תצוגת הנשקים - תמיד מהזווית של השחקן
            bool isPlayerAttacking = attacker.playerId == 1;
            if (isPlayerAttacking)
            {
                FightAnimationManager.Instance.UpdatePreChoiceWeaponDisplay(attacker.Kind, defender.Kind);
            }
            else
            {
                FightAnimationManager.Instance.UpdatePreChoiceWeaponDisplay(defender.Kind, attacker.Kind);
            }
            
            // הפעלת אנימציית הקרב
           // yield return StartCoroutine(FightAnimationManager.Instance.PlayFightIntroAnimation());
            
            // עדכון הספרייטים
            if (isPlayerAttacking)
            {
                FightAnimationManager.Instance.UpdateFightDisplaySprites(attacker.Kind, defender.Kind);
            }
            else
            {
                FightAnimationManager.Instance.UpdateFightDisplaySprites(defender.Kind, attacker.Kind);
            }
        }

        if (attacker.Beats(defender))
        {
            UnityEngine.Debug.Log($"✅ {attacker.name} wins – replacing {defender.name}");
            
            // הצגת תוצאת הקרב
            if (FightAnimationManager.Instance != null)
            {
                bool playerWon = attacker.playerId == 1;
                yield return StartCoroutine(FightAnimationManager.Instance.ShowFightResult(playerWon, !playerWon));
            }
                var hardAI = FindObjectOfType<AIPlayerHardController>();
                if (hardAI != null)
                {
                    hardAI.OnUnitDestroyed(defender);
                }
            if (GameModeManager.Instance.SelectedMode == GameMode.PvP && PvPMoveLogger.Instance != null)
            {
                PvPMoveLogger.Instance.LogPlayerMove(originalPosition, targetPos);
            }

                
            
            BoardManager.Instance.RemoveUnit(defender);
            StartCoroutine(PlayJumpAndRemove(defender.gameObject));
            MoveTo(targetPos);
        }
        else if (defender.Beats(attacker))
        {
            UnityEngine.Debug.Log($"💀 {attacker.name} loses to {defender.name} and is destroyed");
            
            // הצגת תוצאת הקרב
            if (FightAnimationManager.Instance != null)
            {
                bool playerWon = defender.playerId == 1;
                yield return StartCoroutine(FightAnimationManager.Instance.ShowFightResult(playerWon, !playerWon));
            }              
            var hardAI = FindObjectOfType<AIPlayerHardController>();
                if (hardAI != null)
                {
                    hardAI.OnUnitDestroyed(attacker);
                }

             if (GameModeManager.Instance.SelectedMode == GameMode.PvP && PvPMoveLogger.Instance != null)
            {
                PvPMoveLogger.Instance.LogPlayerMove(originalPosition, targetPos);
            }
            BoardManager.Instance.RemoveUnit(attacker);
            StartCoroutine(PlayJumpAndRemove(attacker.gameObject));
        }
        else
        {
            UnityEngine.Debug.Log("❓ Unhandled combat case");
        }
    }


    public void MoveTo(Vector2Int newPos)
    {

        
        // Handle board management logic
        Vector2Int oldPos = Position;
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

    	
    private IEnumerator PlayJumpAndRemove(GameObject unitObject, float delay = 0.5f)
    {
        Animator anim = unitObject.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetInteger("playerId", playerId);
            anim.ResetTrigger("jump");
            anim.SetTrigger("jump");
        }
        yield return new WaitForSeconds(delay);
        Destroy(unitObject);
    }
}