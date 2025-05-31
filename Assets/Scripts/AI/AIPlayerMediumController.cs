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

        // עדכון מידע על יחידות שנחשפו
        foreach (var enemy in enemyUnits)
        {
            if (enemy.IsRevealed && !revealedEnemies.ContainsKey(enemy.Position))
                revealedEnemies[enemy.Position] = enemy.Kind;
        }

        // מציאת המהלך הטוב ביותר
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

        // בדיקה שהמהלך חוקי - רק צעד אחד
        Vector2Int delta = target - unit.Position;
        if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) != 1)
        {
            Debug.Log($"🚫 Invalid move: {unit.name} tried to move more than one step from {unit.Position} to {target}");
            TurnManager.Instance?.EndTurn();
            yield break;
        }

        if (enemyUnit == null)
        {
            // תזוזה למשבצת ריקה
            Debug.Log($"🚶 {unit.name} moves to empty tile {target}");
            unit.MoveTo(target);
            yield return new WaitForSeconds(0.6f);
            TurnManager.Instance?.EndTurn();
            yield break;
        }

        // וידוא שאנחנו תוקפים רק יחידה שנמצאת בדיוק במיקום היעד
        if (enemyUnit.Position != target)
        {
            Debug.Log($"🚫 Cannot attack: Target unit is at {enemyUnit.Position} but move is to {target}");
            TurnManager.Instance?.EndTurn();
            yield break;
        }

        // חשיפת יחידות בקרב
        unit.Reveal();
        enemyUnit.Reveal();

        if (enemyUnit.role == RPSUnit.UnitRole.Trap)
        {
            Debug.Log($"💥 {unit.name} stepped on trap!");
            knownTraps.Add(target);
            BoardManager.Instance.RemoveUnit(unit);
            Destroy(unit.gameObject);
            TurnManager.Instance?.EndTurn();
            yield break;
        }

        if (enemyUnit.role == RPSUnit.UnitRole.Flag)
        {
            Debug.Log($"🎯 {unit.name} captured the FLAG! YOU LOSE!");
            BoardManager.Instance.RemoveUnit(enemyUnit);
            Destroy(enemyUnit.gameObject);
            unit.MoveTo(target);
            PlayerController.gameEnded = true;
            
            // Set player as loser
            TurnTimerManager.Instance?.SetPlayerWon(false);
            
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

        if (unit.Beats(enemyUnit))
        {
            Debug.Log($"🏆 {unit.name} wins! {unit.Kind} beats {enemyUnit.Kind}");
            BoardManager.Instance.RemoveUnit(enemyUnit);
            Destroy(enemyUnit.gameObject);
            unit.MoveTo(target);
            yield return new WaitForSeconds(0.6f);
            TurnManager.Instance?.EndTurn();
            yield break;
        }

        if (enemyUnit.Beats(unit))
        {
            Debug.Log($"💀 {unit.name} loses the battle at {target}: {enemyUnit.Kind} beats {unit.Kind}");
            BoardManager.Instance.RemoveUnit(unit);
            Destroy(unit.gameObject);
            TurnManager.Instance?.EndTurn();
            yield break;
        }

        // במקרה שמשהו השתבש, נסיים את התור
        Debug.Log("🔄 Unexpected case - ending turn");
        TurnManager.Instance?.EndTurn();
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
                if (enemy == null)
                {
                    // בדיקה אם המהלך מקרב אותנו ליחידה לא חשופה
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
                            priorityMoves.Add((4, unit, move));
                        }
                        else
                        {
                            // גם תזוזה למשבצת ריקה היא אופציה, אבל בעדיפות נמוכה
                            priorityMoves.Add((5, unit, move));
                        }
                    }
                    else
                    {
                        // אם אין יחידות לא חשופות, כל תזוזה חוקית היא אופציה
                        priorityMoves.Add((5, unit, move));
                    }
                    continue;
                }

                if (enemy.playerId == unit.playerId)
                    continue;

                // וידוא שהיחידה שאנחנו רוצים לתקוף נמצאת בדיוק במיקום שאליו אנחנו זזים
                if (enemy.Position != move)
                {
                    // אם זה לא מהלך תקיפה חוקי, ננסה לזוז למשבצת הזו אם היא ריקה
                    if (BoardManager.Instance.GetUnitAt(move) == null)
                    {
                        priorityMoves.Add((5, unit, move));
                    }
                    continue;
                }

                // בדיקת מלכודת או דגל - עדיפות הכי גבוהה (1)
                if (enemy.role == RPSUnit.UnitRole.Trap || enemy.role == RPSUnit.UnitRole.Flag)
                {
                    if (!knownTraps.Contains(move))
                        priorityMoves.Add((1, unit, move));
                    continue;
                }

                // בדיקת קרב
                if (enemy.IsRevealed)
                {
                    if (unit.Beats(enemy))
                        // ניצחון בטוח - עדיפות הכי גבוהה (1)
                        priorityMoves.Add((1, unit, move));
                    else if (unit.Kind == enemy.Kind)
                        // תיקו - עדיפות שנייה (2)
                        priorityMoves.Add((2, unit, move));
                }
                else
                {
                    // תקיפת יחידה לא ידועה - עדיפות שלישית (3)
                    priorityMoves.Add((3, unit, move));
                }
            }
        }

        // אם מצאנו מהלכים עם עדיפות, נבחר את הטוב ביותר
        if (priorityMoves.Count > 0)
        {
            var bestMove = priorityMoves.OrderBy(m => m.priority).First();
            UnityEngine.Debug.Log($"🎯 Best move found - Priority {bestMove.priority}: {bestMove.unit.name} to {bestMove.move}");
            return (bestMove.unit, bestMove.move);
        }

        // אם לא מצאנו מהלכים עם עדיפות, נחפש את היחידה הכי קרובה ליחידה לא חשופה
        var unrevealedEnemies = enemyUnits.Where(e => !e.IsRevealed).ToList();
        if (unrevealedEnemies.Any())
        {
            var bestMove = (unit: (RPSUnit)null, move: Vector2Int.zero, distance: float.MaxValue);
            
            foreach (var enemy in unrevealedEnemies)
            {
                foreach (var unit in aiUnits)
                {
                    var moves = GetAdjacentMoves(unit)
                        .Where(m => BoardManager.Instance.GetUnitAt(m) == null) // רק משבצות ריקות
                        .ToList();

                    foreach (var move in moves)
                    {
                        float distAfterMove = Vector2Int.Distance(move, enemy.Position);
                        if (distAfterMove < bestMove.distance)
                        {
                            bestMove = (unit, move, distAfterMove);
                        }
                    }
                }
            }

            if (bestMove.unit != null)
            {
                UnityEngine.Debug.Log($"Moving {bestMove.unit.name} towards closest unrevealed enemy");
                return (bestMove.unit, bestMove.move);
            }
        }

        // זה לא אמור לקרות כי תמיד יש לפחות את הדגל שלא חשוף
        UnityEngine.Debug.Log("Warning: No possible moves found - this should not happen!");
        return null;
    }

    private bool IsGoodEmptyMove(RPSUnit unit, Vector2Int move, List<RPSUnit> enemies)
    {
        // בדיקה אם המהלך מקרב אותנו לאויב
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
