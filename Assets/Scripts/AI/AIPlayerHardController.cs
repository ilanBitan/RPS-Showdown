using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class AIPlayerHardController : AIPlayerController
{
    private Dictionary<Vector2Int, RPSUnit.RPSKind> revealedEnemies = new();
    private HashSet<Vector2Int> knownTraps = new HashSet<Vector2Int>();


    protected override IEnumerator PerformAIAction()
    {
        if (PlayerController.gameEnded || !TurnManager.Instance.IsPlayerTurn(2))
            yield break;

        yield return new WaitForSeconds(0.5f);
        UnityEngine.Debug.Log("[Medium AI] Thinking...");

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

        // Move priority ranking: lower = more important
        var priorityMoves = new List<(int priority, RPSUnit unit, Vector2Int move)>();

        foreach (var unit in aiUnits)
        {
            foreach (var dir in new[] { Vector2Int.down, Vector2Int.up, Vector2Int.left, Vector2Int.right })
            {
                Vector2Int target = unit.Position + dir;
                if (!BoardManager.Instance.IsInsideBoard(target)) continue;

                var enemy = BoardManager.Instance.GetUnitAt(target) as RPSUnit;
                if (enemy == null || enemy.playerId == unit.playerId) continue;

                if (enemy.IsRevealed)
                {
                    if (unit.Beats(enemy))        // Stage 1 - Guaranteed win
                        priorityMoves.Add((1, unit, target));

                    else if (enemy.Beats(unit))   // Stage 2 - Smart escape with look ahead
                    {
                        var escapeOptions = GetValidMoves(unit)
                            .Where(m => BoardManager.Instance.GetUnitAt(m) == null)
                            .OrderByDescending(m => Distance(m, enemy.Position))
                            .ToList();

                        foreach (var escape in escapeOptions)
                        {
                            // Scan the target environment (the tile we'll escape to)
                            foreach (var dir2 in new[] { Vector2Int.down, Vector2Int.up, Vector2Int.left, Vector2Int.right })
                            {
                                Vector2Int lookAhead = escape + dir2;
                                if (!BoardManager.Instance.IsInsideBoard(lookAhead)) continue;

                                var possibleEnemy = BoardManager.Instance.GetUnitAt(lookAhead) as RPSUnit;
                                if (possibleEnemy != null && possibleEnemy.playerId == 1)
                                {
                                    // If this is a known trap - don't approach
                                    if (knownTraps.Contains(lookAhead)) continue;

                                    // There's a chance to attack - either if the enemy is hidden or weak
                                    if (!possibleEnemy.IsRevealed || unit.Beats(possibleEnemy) ||
                                        (!unit.Beats(possibleEnemy) && !possibleEnemy.Beats(unit)))
                                    {
                                        priorityMoves.Add((2, unit, escape)); // Escape with attack potential
                                        goto EndEscapeLoop;
                                    }
                                }
                            }
                        }

                        // fallback: no smart destination? escape as far as possible
                        if (escapeOptions.Any())
                            priorityMoves.Add((2, unit, escapeOptions.First()));

                        EndEscapeLoop:;
                    }


                    else                          // Stage 3 - Tie
                        priorityMoves.Add((3, unit, target));
                }
                else                              // Stage 4 - Hidden enemy
                {
                    if (!knownTraps.Contains(target))
                        priorityMoves.Add((4, unit, target));
                }
            }
        }

        if (priorityMoves.Any())
        {
            var chosen = priorityMoves.OrderBy(p => p.priority).First();
            UnityEngine.Debug.Log($"Best priority {chosen.priority} - {chosen.unit.name} moves to {chosen.move}");
            ExecuteMove(chosen.unit, chosen.move);
            yield break;
        }

        // Stage 5 - If only F and T remain
        if (enemyUnits.Count <= 2 && enemyUnits.All(e => !e.IsRevealed))
        {
            var allMoves = new List<(RPSUnit unit, Vector2Int move, int dist)>();
            foreach (var unit in aiUnits)
            {
                foreach (var move in GetValidMoves(unit))
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

        // Stage 6 - Smart movement with next turn result prediction
        var moveOptions = new List<(int rank, RPSUnit unit, Vector2Int move)>();

        foreach (var unit in aiUnits)
        {
            foreach (var move in GetValidMoves(unit).Where(m => BoardManager.Instance.GetUnitAt(m) == null))
            {
                // Assume the unit moved there - check what happens next turn
                bool willWinNext = false;
                bool willDrawNext = false;
                bool willMeetUnknown = false;
                bool willDie = false;

                foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    Vector2Int nextPos = move + dir;
                    if (!BoardManager.Instance.IsInsideBoard(nextPos)) continue;

                    var neighbor = BoardManager.Instance.GetUnitAt(nextPos) as RPSUnit;
                    if (neighbor != null && neighbor.playerId != unit.playerId)
                    {
                        if (neighbor.IsRevealed)
                        {
                            if (unit.Beats(neighbor)) willWinNext = true;
                            else if (neighbor.Beats(unit)) willDie = true;
                            else if (unit.Kind == neighbor.Kind) willDrawNext = true;
                        }
                        else
                        {
                            willMeetUnknown = true;
                        }
                    }
                }

                if (willWinNext)
                    moveOptions.Add((1, unit, move));
                else if (willDrawNext)
                    moveOptions.Add((2, unit, move));
                else if (willMeetUnknown)
                    moveOptions.Add((3, unit, move));
                else if (!willDie)
                    moveOptions.Add((4, unit, move));
                else
                    moveOptions.Add((5, unit, move)); // Bad move - certain death
            }
        }

        // If we found something better than death
        if (moveOptions.Any(m => m.rank < 5))
        {
            var best = moveOptions.OrderBy(m => m.rank).First();
            UnityEngine.Debug.Log($"Smart move rank {best.rank} -> {best.unit.name} moves to {best.move}");
            ExecuteMove(best.unit, best.move);
            yield break;
        }


        // No smart moves found
        UnityEngine.Debug.Log("No smart moves available, ending turn.");
        TurnManager.Instance?.EndTurn();
    }

    protected override void ExecuteMove(RPSUnit unit, Vector2Int target)
    {
        var enemyUnit = BoardManager.Instance.GetUnitAt(target);
        var enemy = enemyUnit as RPSUnit;

        if (enemy != null)
        {
            unit.Reveal();
            enemy.Reveal();

            if (enemy.role == RPSUnit.UnitRole.Trap)
            {
                UnityEngine.Debug.Log($"{unit.name} stepped on a TRAP at {target} and was destroyed");

                knownTraps.Add(target); // Save to prevent future repeat

                BoardManager.Instance.RemoveUnit(unit);
                Destroy(unit.gameObject);
                TurnManager.Instance?.EndTurn();
                return;
            }

            if (enemy.role == RPSUnit.UnitRole.Flag)
            {
                UnityEngine.Debug.Log($"{unit.name} captured the FLAG at {target}");

                BoardManager.Instance.RemoveUnit(enemy);
                Destroy(enemy.gameObject);
                BoardManager.Instance.PlaceUnit(unit, target);
                unit.MoveTo(target);

                PlayerController.gameEnded = true;
                return;
            }

            if (unit.Kind == enemy.Kind)
            {
                UnityEngine.Debug.Log($"Tie - starting battle panel between {unit.name} and {enemy.name} at {target}");
                BattleManager.Instance?.StartBattle(unit, enemy, target);
                return;
            }

            if (unit.Beats(enemy))
            {
                UnityEngine.Debug.Log($"{unit.name} wins the battle at {target}: {unit.Kind} beats {enemy.Kind}");

                BoardManager.Instance.RemoveUnit(enemy);
                Destroy(enemy.gameObject);
                BoardManager.Instance.PlaceUnit(unit, target);
                unit.MoveTo(target);
            }
            else
            {
                UnityEngine.Debug.Log($"{unit.name} loses the battle at {target}: {enemy.Kind} beats {unit.Kind}");

                BoardManager.Instance.RemoveUnit(unit);
                Destroy(unit.gameObject);
            }
        }
        else
        {
            UnityEngine.Debug.Log($"{unit.name} moves to empty tile {target}");
            unit.TryMove(target - unit.Position);
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
}