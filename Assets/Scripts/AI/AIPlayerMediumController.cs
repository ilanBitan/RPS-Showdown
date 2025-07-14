using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AIPlayerMediumController : AIPlayerController
{
    private Dictionary<Vector2Int, RPSUnit.RPSKind> revealedEnemies = new Dictionary<Vector2Int, RPSUnit.RPSKind>();
    private HashSet<Vector2Int> knownTraps = new HashSet<Vector2Int>();

    // Memory of the last unit that played
    private RPSUnit lastPlayedUnit = null;

    protected override IEnumerator PerformAIAction()
    {
        if (PlayerController.gameEnded || !TurnManager.Instance.IsPlayerTurn(2))
        {
            Debug.Log("🛑 Game ended or not AI's turn - AI stops.");
            yield break;
        }

        yield return new WaitForSeconds(0.5f);
        Debug.Log("🤖 [Medium AI] Thinking...");

        List<RPSUnit> allUnits = FindObjectsOfType<RPSUnit>().ToList();
        List<RPSUnit> aiUnits = allUnits
            .Where(u => u.playerId == 2 && u.IsMovable())
            .OrderBy(_ => Random.value)
            .ToList();

        if (aiUnits.Count == 0)
        {
            Debug.Log("🤖 No movable units. Ending turn.");
            TurnManager.Instance?.EndTurn();
            yield break;
        }

        List<RPSUnit> enemyUnits = allUnits.Where(u => u.playerId == 1).ToList();

        // Update information about revealed units
        foreach (var enemy in enemyUnits)
        {
            if (enemy.IsRevealed && !revealedEnemies.ContainsKey(enemy.Position))
                revealedEnemies[enemy.Position] = enemy.Kind;
        }

        // Find the best move
        var bestMove = FindBestMove(aiUnits, enemyUnits);
        
        if (bestMove.HasValue)
        {
            var (unit, target) = bestMove.Value;
            yield return StartCoroutine(ExecuteMoveSequence(unit, target));
        }
        else
        {
            Debug.Log("🤖 No valid moves found. Ending turn.");
            TurnManager.Instance?.EndTurn();
        }
    }

private IEnumerator ExecuteMoveSequence(RPSUnit unit, Vector2Int target)
{
    var enemyUnit = BoardManager.Instance.GetUnitAt(target) as RPSUnit;

    // Check that the move is legal - only one step
    Vector2Int delta = target - unit.Position;
    if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) != 1)
    {
        Debug.Log($"🚫 Invalid move: {unit.name} tried to move more than one step from {unit.Position} to {target}");
        TurnManager.Instance?.EndTurn();
        yield break;
    }

    if (enemyUnit == null)
    {
        // Move to empty cell
        Debug.Log($"🚶 {unit.name} moves to empty tile {target}");
        unit.MoveTo(target);
        yield return new WaitForSeconds(0.6f);
        TurnManager.Instance?.EndTurn();
        yield break;
    }

    // Ensure we're attacking only a unit that's exactly at the target position
    if (enemyUnit.Position != target)
    {
        Debug.Log($"🚫 Cannot attack: Target unit is at {enemyUnit.Position} but move is to {target}");
        TurnManager.Instance?.EndTurn();
        yield break;
    }

    // Reveal units in combat
    unit.Reveal();
    enemyUnit.Reveal();

    // Handle Trap encounters with animation
    if (enemyUnit.role == RPSUnit.UnitRole.Trap)
    {
        Debug.Log($"💥 {unit.name} stepped on trap!");
        knownTraps.Add(target);
        
        // Show trap animation
        if (FightAnimationManager.Instance != null)
        {
            FightAnimationManager.Instance.fightPanel?.SetActive(true);
            FightAnimationManager.Instance.fightPlayer?.SetActive(true);
            FightAnimationManager.Instance.fightEnemy?.SetActive(true);
            yield return null;

            // Update weapon display for trap encounter
            bool isPlayerAttacking = unit.playerId == 1;
            if (isPlayerAttacking)
            {
                FightAnimationManager.Instance.UpdatePreChoiceWeaponDisplay(unit.Kind, enemyUnit.role);
                FightAnimationManager.Instance.UpdateFightDisplaySprites(unit.Kind, enemyUnit.role);
            }
            else
            {
                // For AI attacking trap, we need to handle it differently
                // We'll show the AI unit's kind vs the trap
                FightAnimationManager.Instance.UpdatePreChoiceWeaponDisplay(enemyUnit.role, unit.Kind);
                FightAnimationManager.Instance.UpdateFightDisplaySprites(enemyUnit.role, unit.Kind);
            }
            
            // Show trap result (whoever steps on trap loses)
            yield return StartCoroutine(FightAnimationManager.Instance.ShowTrapResult(unit.playerId == 1));
        }
        
        BoardManager.Instance.RemoveUnit(unit);
        Destroy(unit.gameObject);
        TurnManager.Instance?.EndTurn();
        yield break;
    }

    // Handle Flag capture with animation
    if (enemyUnit.role == RPSUnit.UnitRole.Flag)
    {
        Debug.Log($"🎯 {unit.name} captured the FLAG!");
        
        // Show flag capture animation
        if (FightAnimationManager.Instance != null)
        {
            FightAnimationManager.Instance.fightPanel?.SetActive(true);
            FightAnimationManager.Instance.fightPlayer?.SetActive(true);
            FightAnimationManager.Instance.fightEnemy?.SetActive(true);
            yield return null;

            // Update weapon display for flag capture
            bool isPlayerAttacking = unit.playerId == 1;
            if (isPlayerAttacking)
            {
                FightAnimationManager.Instance.UpdatePreChoiceWeaponDisplay(unit.Kind, enemyUnit.role);
                FightAnimationManager.Instance.UpdateFightDisplaySprites(unit.Kind, enemyUnit.role);
            }
            else
            {
                // AI capturing flag
                FightAnimationManager.Instance.UpdatePreChoiceWeaponDisplay(enemyUnit.role, unit.Kind);
                FightAnimationManager.Instance.UpdateFightDisplaySprites(enemyUnit.role, unit.Kind);
            }
            
            // Show flag capture result
            yield return StartCoroutine(FightAnimationManager.Instance.ShowFlagCaptureResult(unit.playerId == 1));
        }
        
        BoardManager.Instance.RemoveUnit(enemyUnit);
        Destroy(enemyUnit.gameObject);
        unit.MoveTo(target);
        PlayerController.gameEnded = true;
        
        // Set winner/loser based on who captured the flag
        if (unit.playerId == 1)
        {
            Debug.Log("🎉 PLAYER WINS! Flag captured!");
            TurnTimerManager.Instance?.SetPlayerWon(true);
        }
        else
        {
            Debug.Log("💀 YOU LOSE! AI captured the flag!");
            TurnTimerManager.Instance?.SetPlayerWon(false);
        }
        
        // Stop all game systems
        TurnManager.Instance?.StopGame();
        yield break;
    }

    if (unit.Kind == enemyUnit.Kind)
    {
        Debug.Log($"🤝 Tie – starting battle panel between {unit.name} and {enemyUnit.name} at {target}");
        BattleManager.Instance?.StartBattle(unit, enemyUnit, target);
        yield break;
    }

    // ✨ Execute combat with animation for non-tie battles
    yield return StartCoroutine(ExecuteCombatWithAnimation(unit, enemyUnit, target));
}

    private (RPSUnit unit, Vector2Int target)? FindBestMove(List<RPSUnit> aiUnits, List<RPSUnit> enemyUnits)
    {
        var priorityMoves = new List<(int priority, RPSUnit unit, Vector2Int move)>();

        foreach (var unit in aiUnits)
        {
            var validMoves = GetAdjacentMoves(unit);
            foreach (var move in validMoves)
            {
                var enemy = BoardManager.Instance.GetUnitAt(move) as RPSUnit;
                
                // If there is no unit at the target position - priority 5 (movement towards unrevealed unit)
                if (enemy == null)
                {
                    var nearestUnrevealed = enemyUnits
                        .Where(e => !e.IsRevealed)
                        .OrderBy(e => Vector2Int.Distance(e.Position, unit.Position))
                        .FirstOrDefault();

                    if (nearestUnrevealed != null)
                    {
                        float currentDist = Vector2Int.Distance(unit.Position, nearestUnrevealed.Position);
                        float newDist = Vector2Int.Distance(move, nearestUnrevealed.Position);
                        if (newDist < currentDist)
                        {
                            priorityMoves.Add((5, unit, move));
                            UnityEngine.Debug.Log($"🤖 {unit.name} at {unit.Position}: Priority 5 - Moving towards unrevealed unit (distance: {currentDist:F1} -> {newDist:F1})");
                        }
                    }
                    continue;
                }

                // If this is the AI's own unit - skip
                if (enemy.playerId == unit.playerId)
                    continue;

                // Ensure the unit we want to attack is exactly at the target position
                if (enemy.Position != move)
                    continue;

                // Priority 1: X adjacent to revealed Y and wins against it
                if (enemy.IsRevealed && unit.Beats(enemy))
                {
                    priorityMoves.Add((1, unit, move));
                    UnityEngine.Debug.Log($"🤖 {unit.name} at {unit.Position}: Priority 1 - Can beat {enemy.Kind} at {move}");
                    continue;
                }

                // Priority 2: X adjacent to revealed Y and ties with it
                if (enemy.IsRevealed && unit.Kind == enemy.Kind)
                {
                    priorityMoves.Add((2, unit, move));
                    UnityEngine.Debug.Log($"🤖 {unit.name} at {unit.Position}: Priority 2 - Equal battle with {enemy.Kind} at {move}");
                    continue;
                }

                // Priority 3: X adjacent to unrevealed Y
                if (!enemy.IsRevealed)
                {
                    priorityMoves.Add((3, unit, move));
                    UnityEngine.Debug.Log($"🤖 {unit.name} at {unit.Position}: Priority 3 - Attacking unrevealed unit at {move}");
                    continue;
                }

                // Priority 4: X adjacent to revealed Y and loses to it - flee
                if (enemy.IsRevealed && enemy.Beats(unit))
                {
                    // Find escape route (one step in another direction)
                    var escapeMoves = validMoves
                        .Where(m => m != move && BoardManager.Instance.GetUnitAt(m) == null)
                        .ToList();

                    if (escapeMoves.Any())
                    {
                        var escapeMove = escapeMoves.First();
                        priorityMoves.Add((4, unit, escapeMove));
                        UnityEngine.Debug.Log($"🤖 {unit.name} at {unit.Position}: Priority 4 - Escaping from {enemy.Kind} to {escapeMove}");
                    }
                    continue;
                }
            }
        }

        // If there are no moves - end turn
        if (priorityMoves.Count == 0)
        {
            UnityEngine.Debug.Log("🤖 No valid moves found.");
            return null;
        }

        // Find the highest priority
        int highestPriority = priorityMoves.Min(m => m.priority);
        var bestMoves = priorityMoves.Where(m => m.priority == highestPriority).ToList();

        // Choose unit according to new logic
        RPSUnit selectedUnit = null;
        Vector2Int selectedMove = Vector2Int.zero;

        // Check if the last unit is in the highest priority
        var lastPlayedMove = bestMoves.FirstOrDefault(m => m.unit == lastPlayedUnit);
        if (lastPlayedMove.unit != null)
        {
            selectedUnit = lastPlayedMove.unit;
            selectedMove = lastPlayedMove.move;
            UnityEngine.Debug.Log($"🤖 Selected last played unit: {selectedUnit.name} with priority {highestPriority}");
        }
        else
        {
            // Random choice from highest priority
            var randomMove = bestMoves[UnityEngine.Random.Range(0, bestMoves.Count)];
            selectedUnit = randomMove.unit;
            selectedMove = randomMove.move;
            UnityEngine.Debug.Log($"🤖 Selected random unit: {selectedUnit.name} with priority {highestPriority}");
        }

        // Update the last unit that played
        lastPlayedUnit = selectedUnit;

        return (selectedUnit, selectedMove);
    }

    private bool IsGoodEmptyMove(RPSUnit unit, Vector2Int move, List<RPSUnit> enemies)
    {
        // Check if the move brings us closer to the enemy
        var nearestEnemy = enemies
            .OrderBy(e => Vector2Int.Distance(e.Position, unit.Position))
            .FirstOrDefault();

        if (nearestEnemy != null)
        {
            float currentDist = Vector2Int.Distance(unit.Position, nearestEnemy.Position);
            float newDist = Vector2Int.Distance(move, nearestEnemy.Position);
            return newDist < currentDist;
        }

        return true;
    }
}




