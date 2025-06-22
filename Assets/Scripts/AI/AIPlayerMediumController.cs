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

    // זיכרון של היחידה האחרונה ששיחקה
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
                
                // אם אין יחידה במיקום היעד - עדיפות 5 (תנועה לכיוון דמות לא חשופה)
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

                // אם זו יחידה של ה-AI עצמו - דלג
                if (enemy.playerId == unit.playerId)
                    continue;

                // וידוא שהיחידה שאנחנו רוצים לתקוף נמצאת בדיוק במיקום היעד
                if (enemy.Position != move)
                    continue;

                // עדיפות 1: X שצמוד ל-Y חשופה והוא מנצח אותה
                if (enemy.IsRevealed && unit.Beats(enemy))
                {
                    priorityMoves.Add((1, unit, move));
                    UnityEngine.Debug.Log($"🤖 {unit.name} at {unit.Position}: Priority 1 - Can beat {enemy.Kind} at {move}");
                    continue;
                }

                // עדיפות 2: X שצמוד ל-Y חשופה והוא עושה איתה דו קרב
                if (enemy.IsRevealed && unit.Kind == enemy.Kind)
                {
                    priorityMoves.Add((2, unit, move));
                    UnityEngine.Debug.Log($"🤖 {unit.name} at {unit.Position}: Priority 2 - Equal battle with {enemy.Kind} at {move}");
                    continue;
                }

                // עדיפות 3: X שצמוד ל-Y לא חשופה
                if (!enemy.IsRevealed)
                {
                    priorityMoves.Add((3, unit, move));
                    UnityEngine.Debug.Log($"🤖 {unit.name} at {unit.Position}: Priority 3 - Attacking unrevealed unit at {move}");
                    continue;
                }

                // עדיפות 4: X שצמוד ל-Y חשופה והוא מפסיד לה - בורח
                if (enemy.IsRevealed && enemy.Beats(unit))
                {
                    // מציאת מסלול בריחה (צעד אחד לכיוון אחר)
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

        // אם אין מהלכים - סיים תור
        if (priorityMoves.Count == 0)
        {
            UnityEngine.Debug.Log("🤖 No valid moves found.");
            return null;
        }

        // מציאת העדיפות הגבוהה ביותר
        int highestPriority = priorityMoves.Min(m => m.priority);
        var bestMoves = priorityMoves.Where(m => m.priority == highestPriority).ToList();

        // בחירת יחידה לפי הלוגיקה החדשה
        RPSUnit selectedUnit = null;
        Vector2Int selectedMove = Vector2Int.zero;

        // בדיקה אם היחידה האחרונה נמצאת בעדיפות הגבוהה
        var lastPlayedMove = bestMoves.FirstOrDefault(m => m.unit == lastPlayedUnit);
        if (lastPlayedMove.unit != null)
        {
            selectedUnit = lastPlayedMove.unit;
            selectedMove = lastPlayedMove.move;
            UnityEngine.Debug.Log($"🤖 Selected last played unit: {selectedUnit.name} with priority {highestPriority}");
        }
        else
        {
            // בחירה רנדומלית מהעדיפות הגבוהה
            var randomMove = bestMoves[UnityEngine.Random.Range(0, bestMoves.Count)];
            selectedUnit = randomMove.unit;
            selectedMove = randomMove.move;
            UnityEngine.Debug.Log($"🤖 Selected random unit: {selectedUnit.name} with priority {highestPriority}");
        }

        // עדכון היחידה האחרונה ששיחקה
        lastPlayedUnit = selectedUnit;

        return (selectedUnit, selectedMove);
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
