using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIPlayerMediumController : AIPlayerController
{
    private Dictionary<Vector2Int, RPSUnit.RPSKind> revealedEnemies = new();
    private HashSet<Vector2Int> knownTraps = new HashSet<Vector2Int>();


    /*    protected override IEnumerator PerformAIAction()
        {
            if (PlayerController.gameEnded || !TurnManager.Instance.IsPlayerTurn(2))
                yield break;

            yield return new WaitForSeconds(0.5f);
            Debug.Log("🤖 [Medium AI] Thinking...");

            List<RPSUnit> allUnits = FindObjectsOfType<RPSUnit>().ToList();
            List<RPSUnit> aiUnits = allUnits
                .Where(u => u.playerId == 2 && u.IsMovable())
                .OrderBy(_ => Random.value) // ✅ רנדומליות
                .ToList();
            List<RPSUnit> enemyUnits = allUnits.Where(u => u.playerId == 1).ToList();

            foreach (var enemy in enemyUnits)
            {
                if (enemy.IsRevealed && !revealedEnemies.ContainsKey(enemy.Position))
                    revealedEnemies[enemy.Position] = enemy.Kind;
            }

            // ננסה עבור כל יחידה (בסדר רנדומלי)
            foreach (var unit in aiUnits)
            {
                var validMoves = GetValidMoves(unit);

                // 1. תקוף אויבים נחשפים שחלשים ממך
                foreach (var move in validMoves)
                {
                    var enemy = BoardManager.Instance.GetUnitAt(move) as RPSUnit;
                    if (enemy != null && enemy.playerId != unit.playerId && enemy.IsRevealed && unit.Beats(enemy))
                    {
                        Debug.Log($"⚔️ {unit.name} attacking revealed weaker enemy at {move}");
                        ExecuteMove(unit, move);
                        yield break;
                    }
                }

                // 2. הימנע מקרב מול אויבים נחשפים שיכולים להביס אותך
                // 3. אם אין ברירה – תקוף אויב מוסתר
                foreach (var move in validMoves)
                {
                    var enemy = BoardManager.Instance.GetUnitAt(move) as RPSUnit;
                    if (enemy != null && enemy.playerId != unit.playerId)
                    {
                        if (enemy.IsRevealed && enemy.Beats(unit))
                        {
                            // ננסה לברוח – חפש מהלך שמרחיק מהאויב
                            var escapeMoves = GetValidMoves(unit)
                                .Where(m => BoardManager.Instance.GetUnitAt(m) == null)
                                .OrderByDescending(m => Distance(m, enemy.Position))
                                .ToList();

                            if (escapeMoves.Any())
                            {
                                var escape = escapeMoves.First();
                                Debug.Log($"🏃 {unit.name} escaping to {escape} away from enemy at {move}");
                                ExecuteMove(unit, escape);
                                yield break;
                            }

                            continue; // אין לאן לברוח
                        }

                        if (!enemy.IsRevealed)
                        {
                            Debug.Log($"❔ {unit.name} attacking unknown enemy at {move}");
                            ExecuteMove(unit, move);
                            yield break;
                        }
                    }
                }
                // 3.5: אם האויב נחשף אבל לא חזק ממני – עדיף לנסות דו קרב
                foreach (var move in validMoves)
                {
                    var enemy = BoardManager.Instance.GetUnitAt(move) as RPSUnit;
                    if (enemy != null && enemy.playerId != unit.playerId && enemy.IsRevealed)
                    {
                        if (!enemy.Beats(unit) && !unit.Beats(enemy)) // תיקו R=R
                        {
                            if (Distance(unit.Position, move) == 1) // ⚠️ רק אם הם צמודים
                            {
                                Debug.Log($"🤝 {unit.name} starting tie battle with {enemy.name} at {move}");
                                BattleManager.Instance?.StartBattle(unit, enemy, move);
                                yield break;
                            }
                        }
                    }
                }



                // 4. תזוזה חכמה לכיוון ממוצע האויבים
                Vector2Int bestMove = unit.Position;
                int bestDistance = Distance(unit.Position, GetEnemyAveragePosition(enemyUnits));

                foreach (var move in validMoves.OrderBy(m => Random.value))
                {
                    if (BoardManager.Instance.GetUnitAt(move) == null)
                    {
                        int newDistance = Distance(move, GetEnemyAveragePosition(enemyUnits));
                        if (newDistance < bestDistance)
                        {
                            bestMove = move;
                            bestDistance = newDistance;
                        }
                    }
                }

                if (bestMove != unit.Position)
                {
                    Debug.Log($"🚶 {unit.name} moving toward enemy cluster: {bestMove}");
                    ExecuteMove(unit, bestMove);
                    yield break;
                }
            }

            // 5. רק F ו־T נותרו – ננסה לתקוף אחד
            if (enemyUnits.Count <= 2 && enemyUnits.All(e => !e.IsRevealed))
            {
                foreach (var unit in aiUnits)
                {
                    foreach (var move in GetValidMoves(unit))
                    {
                        var potential = BoardManager.Instance.GetUnitAt(move) as RPSUnit;
                        if (potential != null && potential.playerId != unit.playerId)
                        {
                            Debug.Log("🎯 Only unrevealed enemies remain – taking a risk.");
                            ExecuteMove(unit, move);
                            yield break;
                        }
                    }
                }
            }

            Debug.Log("🤷 No smart moves available, ending turn.");
            TurnManager.Instance?.EndTurn();
        }
    */


    protected override IEnumerator PerformAIAction()
    {
        if (PlayerController.gameEnded || !TurnManager.Instance.IsPlayerTurn(2))
            yield break;

        yield return new WaitForSeconds(0.5f);
        Debug.Log("🤖 [Medium AI] Thinking...");

        List<RPSUnit> allUnits = FindObjectsOfType<RPSUnit>().ToList();
        List<RPSUnit> aiUnits = allUnits
            .Where(u => u.playerId == 2 && u.IsMovable())
            .OrderBy(_ => Random.value)
            .ToList();
        List<RPSUnit> enemyUnits = allUnits.Where(u => u.playerId == 1).ToList();

        foreach (var enemy in enemyUnits)
        {
            if (enemy.IsRevealed && !revealedEnemies.ContainsKey(enemy.Position))
                revealedEnemies[enemy.Position] = enemy.Kind;
        }

        // 🧠 דירוג מהלכים: נמוך = חשוב יותר
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
                    if (unit.Beats(enemy))        // ⚔️ שלב 1 – נצחון בטוח
                        priorityMoves.Add((1, unit, target));

                    else if (enemy.Beats(unit))   // 🧠 שלב 2 – בריחה חכמה עם ראייה קדימה
                    {
                        var escapeOptions = GetValidMoves(unit)
                            .Where(m => BoardManager.Instance.GetUnitAt(m) == null)
                            .OrderByDescending(m => Distance(m, enemy.Position))
                            .ToList();

                        foreach (var escape in escapeOptions)
                        {
                            // סרוק סביבת היעד (המשבצת שנברח אליה)
                            foreach (var dir2 in new[] { Vector2Int.down, Vector2Int.up, Vector2Int.left, Vector2Int.right })
                            {
                                Vector2Int lookAhead = escape + dir2;
                                if (!BoardManager.Instance.IsInsideBoard(lookAhead)) continue;

                                var possibleEnemy = BoardManager.Instance.GetUnitAt(lookAhead) as RPSUnit;
                                if (possibleEnemy != null && possibleEnemy.playerId == 1)
                                {
                                    // אם זו מלכודת ידועה – אל תתקרב
                                    if (knownTraps.Contains(lookAhead)) continue;

                                    // יש סיכוי לתקוף – או אם היריב מוסתר או חלש
                                    if (!possibleEnemy.IsRevealed || unit.Beats(possibleEnemy) ||
                                        (!unit.Beats(possibleEnemy) && !possibleEnemy.Beats(unit)))
                                    {
                                        priorityMoves.Add((2, unit, escape)); // בריחה עם פוטנציאל תקיפה
                                        goto EndEscapeLoop;
                                    }
                                }
                            }
                        }

                        // fallback: אין יעד חכם? ברח הכי רחוק
                        if (escapeOptions.Any())
                            priorityMoves.Add((2, unit, escapeOptions.First()));

                        EndEscapeLoop:;
                    }


                    else                          // 🤝 שלב 3 – תיקו
                        priorityMoves.Add((3, unit, target));
                }
                else                              // ❔ שלב 4 – אויב מוסתר
                {
                    if (!knownTraps.Contains(target))
                        priorityMoves.Add((4, unit, target));
                }
            }
        }

        if (priorityMoves.Any())
        {
            var chosen = priorityMoves.OrderBy(p => p.priority).First();
            Debug.Log($"✅ Best priority {chosen.priority} – {chosen.unit.name} moves to {chosen.move}");
            ExecuteMove(chosen.unit, chosen.move);
            yield break;
        }

        // 🎯 שלב 5 – אם נשארו רק F ו־T
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
                Debug.Log("🎯 Trying unrevealed target – possible F or T.");
                ExecuteMove(target.unit, target.move);
                yield break;
            }
        }

        // 🧠 שלב 6 – תנועה חכמה עם חיזוי תוצאה בתור הבא
        var moveOptions = new List<(int rank, RPSUnit unit, Vector2Int move)>();

        foreach (var unit in aiUnits)
        {
            foreach (var move in GetValidMoves(unit).Where(m => BoardManager.Instance.GetUnitAt(m) == null))
            {
                // נניח שהיחידה עברה לשם – נבדוק מה קורה בתור הבא
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
                    moveOptions.Add((5, unit, move)); // צעד גרוע – מוות בטוח
            }
        }

        // אם מצאנו משהו טוב יותר ממוות
        if (moveOptions.Any(m => m.rank < 5))
        {
            var best = moveOptions.OrderBy(m => m.rank).First();
            Debug.Log($"🤖 Smart move rank {best.rank} → {best.unit.name} moves to {best.move}");
            ExecuteMove(best.unit, best.move);
            yield break;
        }


        // ❌ לא נמצאו מהלכים חכמים
        Debug.Log("🤷 No smart moves available, ending turn.");
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
                Debug.Log($"💥 {unit.name} stepped on a TRAP at {target} and was destroyed");

                knownTraps.Add(target); // נשמר למניעת חזרה בעתיד

                BoardManager.Instance.RemoveUnit(unit);
                Destroy(unit.gameObject);
                TurnManager.Instance?.EndTurn();
                return;
            }

            if (enemy.role == RPSUnit.UnitRole.Flag)
            {
                Debug.Log($"🎯 {unit.name} captured the FLAG at {target}");

                BoardManager.Instance.RemoveUnit(enemy);
                Destroy(enemy.gameObject);
                BoardManager.Instance.PlaceUnit(unit, target);
                unit.MoveTo(target);

                PlayerController.gameEnded = true;
                return;
            }

            if (unit.Kind == enemy.Kind)
            {
                Debug.Log($"🤝 Tie – starting battle panel between {unit.name} and {enemy.name} at {target}");
                BattleManager.Instance?.StartBattle(unit, enemy, target);
                return;
            }

            if (unit.Beats(enemy))
            {
                Debug.Log($"🏆 {unit.name} wins the battle at {target}: {unit.Kind} beats {enemy.Kind}");

                BoardManager.Instance.RemoveUnit(enemy);
                Destroy(enemy.gameObject);
                BoardManager.Instance.PlaceUnit(unit, target);
                unit.MoveTo(target);
            }
            else
            {
                Debug.Log($"💀 {unit.name} loses the battle at {target}: {enemy.Kind} beats {unit.Kind}");

                BoardManager.Instance.RemoveUnit(unit);
                Destroy(unit.gameObject);
            }
        }
        else
        {
            Debug.Log($"🚶 {unit.name} moves to empty tile {target}");
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
