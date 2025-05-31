using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AIPlayerHardController : AIPlayerController
{
    private Dictionary<Vector2Int, RPSUnit.RPSKind> revealedEnemies = new();
    private HashSet<Vector2Int> knownTraps = new HashSet<Vector2Int>();

    private struct AIMove
    {
        public int priority;
        public RPSUnit unit;
        public Vector2Int moveTarget;
        public RPSUnit attackTarget;  // יחידה שאנחנו מתכננים לתקוף (אם יש)
        public Vector2Int attackPos;   // המיקום של היחידה שאנחנו מתכננים לתקוף
    }

    protected override IEnumerator PerformAIAction()
    {
        if (PlayerController.gameEnded || !TurnManager.Instance.IsPlayerTurn(2))
            yield break;

        yield return new WaitForSeconds(0.5f);
        UnityEngine.Debug.Log("[Hard AI] Thinking...");

        List<RPSUnit> allUnits = FindObjectsOfType<RPSUnit>().ToList();
        List<RPSUnit> aiUnits = allUnits
            .Where(u => u.playerId == 2 && u.IsMovable())
            .OrderBy(_ => UnityEngine.Random.value)
            .ToList();
        List<RPSUnit> enemyUnits = allUnits.Where(u => u.playerId == 1).ToList();

        foreach (var enemy in enemyUnits)
        {
            if (enemy.IsRevealed && !revealedEnemies.ContainsKey(enemy.Position))
                revealedEnemies[enemy.Position] = enemy.Kind;
        }

        var possibleMoves = new List<AIMove>();

        foreach (var unit in aiUnits)
        {
            var validMoves = GetAdjacentMoves(unit);
            foreach (var moveTarget in validMoves)
            {
                var enemyAtTarget = BoardManager.Instance.GetUnitAt(moveTarget) as RPSUnit;
                
                // אם המיקום ריק או שיש שם יחידה שלנו
                if (enemyAtTarget == null || enemyAtTarget.playerId == unit.playerId)
                {
                    // בדיקה אם יש פוטנציאל התקפה טוב מהמיקום החדש
                    bool hasGoodAttackPotential = false;
                    foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                    {
                        Vector2Int adjacentPos = moveTarget + dir;
                        if (!BoardManager.Instance.IsInsideBoard(adjacentPos)) continue;

                        var adjacentUnit = BoardManager.Instance.GetUnitAt(adjacentPos) as RPSUnit;
                        if (adjacentUnit != null && adjacentUnit.playerId == 1)
                        {
                            if (!adjacentUnit.IsRevealed || unit.Beats(adjacentUnit) ||
                                (!unit.Beats(adjacentUnit) && !adjacentUnit.Beats(unit)))
                            {
                                hasGoodAttackPotential = true;
                                break;
                            }
                        }
                    }
                    
                    if (hasGoodAttackPotential)
                    {
                        possibleMoves.Add(new AIMove 
                        { 
                            priority = 4, 
                            unit = unit, 
                            moveTarget = moveTarget,
                            attackTarget = null,
                            attackPos = Vector2Int.zero
                        });
                    }
                    continue;
                }

                // מכאן והלאה זה מהלכי תקיפה
                if (enemyAtTarget.IsRevealed)
                {
                    if (unit.Beats(enemyAtTarget))        // Stage 1 - Guaranteed win
                    {
                        possibleMoves.Add(new AIMove 
                        { 
                            priority = 1, 
                            unit = unit, 
                            moveTarget = moveTarget,
                            attackTarget = enemyAtTarget,
                            attackPos = enemyAtTarget.Position
                        });
                    }
                    else if (enemyAtTarget.Beats(unit))   // Stage 2 - Smart escape
                    {
                        var escapeOptions = GetAdjacentMoves(unit)
                            .Where(m => BoardManager.Instance.GetUnitAt(m) == null)
                            .OrderByDescending(m => Distance(m, enemyAtTarget.Position))
                            .ToList();

                        if (escapeOptions.Any())
                        {
                            possibleMoves.Add(new AIMove 
                            { 
                                priority = 2, 
                                unit = unit, 
                                moveTarget = escapeOptions.First(),
                                attackTarget = null,
                                attackPos = Vector2Int.zero
                            });
                        }
                    }
                    else                          // Stage 3 - Tie
                    {
                        possibleMoves.Add(new AIMove 
                        { 
                            priority = 3, 
                            unit = unit, 
                            moveTarget = moveTarget,
                            attackTarget = enemyAtTarget,
                            attackPos = enemyAtTarget.Position
                        });
                    }
                }
                else                              // Stage 4 - Hidden enemy
                {
                    if (!knownTraps.Contains(moveTarget))
                    {
                        possibleMoves.Add(new AIMove 
                        { 
                            priority = 4, 
                            unit = unit, 
                            moveTarget = moveTarget,
                            attackTarget = enemyAtTarget,
                            attackPos = enemyAtTarget.Position
                        });
                    }
                }
            }
        }

        if (possibleMoves.Any())
        {
            var chosen = possibleMoves.OrderBy(m => m.priority).First();
            
            // אם זה מהלך תקיפה, נוודא שהיחידה עדיין במיקום המתוכנן
            if (chosen.attackTarget != null)
            {
                var currentEnemy = BoardManager.Instance.GetUnitAt(chosen.attackPos) as RPSUnit;
                if (currentEnemy != chosen.attackTarget)
                {
                    // היחידה זזה! נחפש את המיקום החדש שלה
                    UnityEngine.Debug.Log($"Target enemy {chosen.attackTarget.name} moved from {chosen.attackPos}, trying to chase it");
                    
                    // נחפש את היחידה על הלוח
                    var newEnemyPos = chosen.attackTarget.Position;
                    
                    // נמצא את הצעד הכי טוב בכיוון היחידה
                    var bestMove = GetAdjacentMoves(chosen.unit)
                        .OrderBy(pos => Distance(pos, newEnemyPos))
                        .FirstOrDefault();

                    if (bestMove != Vector2Int.zero)
                    {
                        UnityEngine.Debug.Log($"Moving towards enemy at {newEnemyPos}");
                        ExecuteMove(chosen.unit, bestMove);
                        yield break;
                    }
                }

                // נוודא שהמרחק מהמיקום החדש ליחידת האויב הוא 1
                Vector2Int newDelta = chosen.attackPos - chosen.moveTarget;
                if (Mathf.Abs(newDelta.x) + Mathf.Abs(newDelta.y) != 1)
                {
                    UnityEngine.Debug.Log($"🚫 Cannot attack {chosen.attackTarget.name} from {chosen.moveTarget} - Distance must be 1");
                    
                    // במקום לבטל את המהלך, ננסה להתקרב ליחידה
                    var bestMove = GetAdjacentMoves(chosen.unit)
                        .OrderBy(pos => Distance(pos, chosen.attackTarget.Position))
                        .FirstOrDefault();

                    if (bestMove != Vector2Int.zero)
                    {
                        UnityEngine.Debug.Log($"Moving towards enemy at {chosen.attackTarget.Position}");
                        ExecuteMove(chosen.unit, bestMove);
                        yield break;
                    }
                    
                    TurnManager.Instance?.EndTurn();
                    yield break;
                }
            }

            UnityEngine.Debug.Log($"Best priority {chosen.priority} - {chosen.unit.name} moves to {chosen.moveTarget}" + 
                (chosen.attackTarget != null ? $" to attack {chosen.attackTarget.name}" : ""));
            ExecuteMove(chosen.unit, chosen.moveTarget);
            yield break;
        }

        // Stage 5 - If only F and T remain
        if (enemyUnits.Count <= 2 && enemyUnits.All(e => !e.IsRevealed))
        {
            var allMoves = new List<(RPSUnit unit, Vector2Int move, int dist)>();
            foreach (var unit in aiUnits)
            {
                foreach (var move in GetAdjacentMoves(unit))
                {
                    var potential = BoardManager.Instance.GetUnitAt(move) as RPSUnit;
                    if (potential != null && potential.playerId != unit.playerId)
                    {
                        int dist = Distance(unit.Position, move);
                        allMoves.Add((unit, move, dist));
                    }
                }
            }
            if (allMoves.Any())
            {
                var target = allMoves.OrderBy(t => t.dist).First();
                UnityEngine.Debug.Log("Trying unrevealed target - possible F or T.");
                ExecuteMove(target.unit, target.move);
                yield break;
            }
        }

        // Final Stage - Always move towards closest unrevealed enemy
        var unrevealedEnemies = enemyUnits.Where(e => !e.IsRevealed).ToList();
        
        // Find the closest AI unit to any unrevealed enemy
        var closestUnitMove = (unit: (RPSUnit)null, move: Vector2Int.zero, distance: int.MaxValue);
        
        foreach (var enemy in unrevealedEnemies)
        {
            foreach (var unit in aiUnits)
            {
                var moves = GetAdjacentMoves(unit)
                    .Where(m => BoardManager.Instance.GetUnitAt(m) == null) // Only empty spaces
                    .ToList();

                foreach (var move in moves)
                {
                    int distAfterMove = Distance(move, enemy.Position);
                    if (distAfterMove < closestUnitMove.distance)
                    {
                        closestUnitMove = (unit, move, distAfterMove);
                    }
                }
            }
        }

        if (closestUnitMove.unit != null)
        {
            UnityEngine.Debug.Log($"Moving {closestUnitMove.unit.name} towards closest unrevealed enemy");
            ExecuteMove(closestUnitMove.unit, closestUnitMove.move);
            yield break;
        }

        // This should never happen as there should always be unrevealed enemies (at least the flag)
        // But keep it as a safeguard
        UnityEngine.Debug.Log("Warning: No possible moves found - this should not happen!");
        TurnManager.Instance?.EndTurn();
    }

    protected override void ExecuteMove(RPSUnit unit, Vector2Int target)
    {
        // בדיקה שהתנועה היא חוקית (צעד אחד)
        Vector2Int delta = target - unit.Position;
        if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) != 1)
        {
            UnityEngine.Debug.Log($"🚫 Invalid move attempt from {unit.Position} to {target} - Distance must be 1");
            TurnManager.Instance?.EndTurn();
            return;
        }

        // בדיקה מחדש של המצב הנוכחי של הלוח
        var currentEnemyUnit = BoardManager.Instance.GetUnitAt(target) as RPSUnit;
        
        // אם אין יחידה במיקום היעד, נבדוק אם יש יחידות אויב סמוכות
        if (currentEnemyUnit == null)
        {
            // נזוז למיקום החדש
            BoardManager.Instance.PlaceUnit(unit, target);
            unit.MoveTo(target);
            TurnManager.Instance?.EndTurn();
            return;
        }

        // אם זו יחידה של ה-AI עצמו, נבטל את המהלך
        if (currentEnemyUnit.playerId == unit.playerId)
        {
            UnityEngine.Debug.Log($"🚫 Cannot attack own unit at {target}");
            TurnManager.Instance?.EndTurn();
            return;
        }

        // מכאן והלאה זו תקיפה של יחידת אויב
        unit.Reveal();
        currentEnemyUnit.Reveal();

        if (currentEnemyUnit.role == RPSUnit.UnitRole.Trap)
        {
            UnityEngine.Debug.Log($"{unit.name} stepped on a TRAP at {target} and was destroyed");
            knownTraps.Add(target);
            BoardManager.Instance.RemoveUnit(unit);
            Destroy(unit.gameObject);
            TurnManager.Instance?.EndTurn();
            return;
        }

        if (currentEnemyUnit.role == RPSUnit.UnitRole.Flag)
        {
            UnityEngine.Debug.Log($"{unit.name} captured the FLAG at {target}! YOU LOSE!");
            BoardManager.Instance.RemoveUnit(currentEnemyUnit);
            Destroy(currentEnemyUnit.gameObject);
            BoardManager.Instance.PlaceUnit(unit, target);
            unit.MoveTo(target);
            PlayerController.gameEnded = true;
            TurnTimerManager.Instance?.SetPlayerWon(false);
            TurnManager.Instance?.StopGame();
            return;
        }

        if (unit.Kind == currentEnemyUnit.Kind)
        {
            UnityEngine.Debug.Log($"Tie - starting battle panel between {unit.name} and {currentEnemyUnit.name} at {target}");
            BattleManager.Instance?.StartBattle(unit, currentEnemyUnit, target);
            return;
        }

        if (unit.Beats(currentEnemyUnit))
        {
            UnityEngine.Debug.Log($"{unit.name} wins the battle at {target}: {unit.Kind} beats {currentEnemyUnit.Kind}");
            BoardManager.Instance.RemoveUnit(currentEnemyUnit);
            Destroy(currentEnemyUnit.gameObject);
            BoardManager.Instance.PlaceUnit(unit, target);
            unit.MoveTo(target);
        }
        else
        {
            UnityEngine.Debug.Log($"{unit.name} loses the battle at {target}: {currentEnemyUnit.Kind} beats {unit.Kind}");
            BoardManager.Instance.RemoveUnit(unit);
            Destroy(unit.gameObject);
        }

        TurnManager.Instance?.EndTurn();
    }


    private int Distance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private Vector2Int GetEnemyAveragePosition(List<RPSUnit> enemies)
    {
        if (enemies.Count == 0) return new Vector2Int(3, 0);

        int xSum = 0, ySum = 0;
        foreach (var e in enemies)
        {
            xSum += e.Position.x;
            ySum += e.Position.y;
        }
        return new Vector2Int(xSum / enemies.Count, ySum / enemies.Count);
    }

    private List<Vector2Int> GetAdjacentMoves(RPSUnit unit)
    {
        var moves = new List<Vector2Int>();
        var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in directions)
        {
            Vector2Int newPos = unit.Position + dir;
            if (BoardManager.Instance.IsInsideBoard(newPos))
            {
                // Only add positions that are exactly 1 step away
                if (Mathf.Abs(newPos.x - unit.Position.x) + Mathf.Abs(newPos.y - unit.Position.y) == 1)
                {
                    moves.Add(newPos);
                }
            }
        }

        return moves;
    }
}