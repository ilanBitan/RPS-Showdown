using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using System;

public class AIPlayerController : MonoBehaviour
{
    public static AIPlayerController Instance;

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    public virtual void PlayTurn()
    {
        StartCoroutine(PerformAIAction());
    }

    protected virtual IEnumerator PerformAIAction()
    {
        if (PlayerController.gameEnded || !TurnManager.Instance.IsPlayerTurn(2))
        {
            UnityEngine.Debug.Log("🛑 Game ended or not AI's turn - AI stops.");
            yield break;
        }

        yield return new WaitForSeconds(0.5f);
        UnityEngine.Debug.Log("🤖 AI is thinking...");

        // מוצא את כל היחידות שיכולות לזוז
        List<RPSUnit> movableUnits = FindObjectsOfType<RPSUnit>()
            .Where(u => u.playerId == 2 && u.IsMovable())
            .OrderBy(_ => UnityEngine.Random.value)
            .ToList();

        if (movableUnits.Count == 0)
        {
            UnityEngine.Debug.Log("🤖 No movable units. Ending turn.");
            TurnManager.Instance?.EndTurn();
            yield break;
        }

        // מנסה כל יחידה בסדר רנדומלי עד שמוצא אחת שיכולה לזוז
        foreach (var unit in movableUnits)
        {
            // מוצא את כל המהלכים האפשריים (רק צעד אחד)
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            var validMoves = new List<Vector2Int>();

            foreach (var dir in directions)
            {
                Vector2Int newPos = unit.Position + dir;

                // בדיקה שהמיקום בתוך הלוח
                if (!BoardManager.Instance.IsInsideBoard(newPos))
                    continue;

                var targetUnit = BoardManager.Instance.GetUnitAt(newPos) as RPSUnit;

                // אם המשבצת ריקה או שיש בה יחידת אויב (לא AI)
                if (targetUnit == null || targetUnit.playerId != unit.playerId)
                {
                    // אם יש יחידת אויב, בודקים שהיא בדיוק במיקום הזה
                    if (targetUnit != null && targetUnit.Position != newPos)
                        continue;

                    validMoves.Add(newPos);
                }
            }

            // מערבב את המהלכים האפשריים
            validMoves = validMoves.OrderBy(_ => UnityEngine.Random.value).ToList();

            if (validMoves.Count > 0)
            {
                Vector2Int targetPos = validMoves[0];
                yield return StartCoroutine(ExecuteMoveSequence(unit, targetPos));
                yield break;
            }
        }

        UnityEngine.Debug.Log("🤖 No valid moves found for any unit. Ending turn.");
        TurnManager.Instance?.EndTurn();
    }

    protected IEnumerator ExecuteMoveSequence(RPSUnit unit, Vector2Int targetPos)
    {
        var targetUnit = BoardManager.Instance.GetUnitAt(targetPos) as RPSUnit;

        // בדיקה שהמהלך חוקי - רק צעד אחד
        Vector2Int delta = targetPos - unit.Position;
        if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) != 1)
        {
            UnityEngine.Debug.Log($"🚫 Invalid move: Distance must be 1 step, tried to move from {unit.Position} to {targetPos}");
            yield break;
        }

        if (targetUnit == null)
        {
            UnityEngine.Debug.Log($"🤖 AI moving {unit.name} to empty tile {targetPos}");
            unit.MoveTo(targetPos);
            yield return new WaitForSeconds(0.6f);
            TurnManager.Instance?.EndTurn();
            yield break;
        }

        // וידוא שאנחנו תוקפים רק יחידה שנמצאת בדיוק במיקום היעד
        if (targetUnit.Position != targetPos)
        {
            UnityEngine.Debug.Log($"🚫 Cannot attack: Target unit is at {targetUnit.Position} but move is to {targetPos}");
            yield break;
        }

        // חשיפת יחידות בקרב
        unit.Reveal();
        targetUnit.Reveal();

        // טיפול במקרה של מלכודת
        if (targetUnit.role == RPSUnit.UnitRole.Trap)
        {
            UnityEngine.Debug.Log("💥 AI stepped on trap and is destroyed.");
            
            // עדכון ה-AI הקשה על דמות שהושמדה
            var hardAI = FindObjectOfType<AIPlayerHardController>();
            if (hardAI != null)
            {
                hardAI.OnUnitDestroyed(unit);
            }
            
            BoardManager.Instance.RemoveUnit(unit);
            Destroy(unit.gameObject);
            TurnManager.Instance?.EndTurn();
            yield break;
        }

        // טיפול במקרה של דגל
        if (targetUnit.role == RPSUnit.UnitRole.Flag)
        {
            UnityEngine.Debug.Log("🎯 AI captured the FLAG! YOU LOSE!");
            
            // עדכון ה-AI הקשה על דגל שהושמד
            var hardAI = FindObjectOfType<AIPlayerHardController>();
            if (hardAI != null)
            {
                hardAI.OnUnitDestroyed(targetUnit);
            }
            
            BoardManager.Instance.RemoveUnit(targetUnit);
            Destroy(targetUnit.gameObject);
            unit.MoveTo(targetPos);
            PlayerController.gameEnded = true;

            // Set player as loser
            TurnTimerManager.Instance?.SetPlayerWon(false);

            // Stop all game systems
            TurnManager.Instance?.StopGame();
            yield break;
        }

        // טיפול בקרב
        if (unit.Kind == targetUnit.Kind)
        {
            UnityEngine.Debug.Log("🤝 Same kind – triggering battle panel.");
            BattleManager.Instance?.StartBattle(unit, targetUnit, targetPos);
            yield break;
        }

        if (unit.Beats(targetUnit))
        {
            UnityEngine.Debug.Log($"🏆 AI wins! {unit.Kind} beats {targetUnit.Kind}");
            
            // עדכון ה-AI הקשה על דמות שהושמדה
            var hardAI = FindObjectOfType<AIPlayerHardController>();
            if (hardAI != null)
            {
                hardAI.OnUnitDestroyed(targetUnit);
            }
            
            BoardManager.Instance.RemoveUnit(targetUnit);
            Destroy(targetUnit.gameObject);
            unit.MoveTo(targetPos);
            yield return new WaitForSeconds(0.6f);
            TurnManager.Instance?.EndTurn();
            yield break;
        }

        if (targetUnit.Beats(unit))
        {
            UnityEngine.Debug.Log($"💀 AI loses. {targetUnit.Kind} beats {unit.Kind}");
            
            // עדכון ה-AI הקשה על דמות שהושמדה
            var hardAI = FindObjectOfType<AIPlayerHardController>();
            if (hardAI != null)
            {
                hardAI.OnUnitDestroyed(unit);
            }
            
            BoardManager.Instance.RemoveUnit(unit);
            Destroy(unit.gameObject);
            TurnManager.Instance?.EndTurn();
            yield break;
        }

        // במקרה שמשהו השתבש, נסיים את התור
        UnityEngine.Debug.Log("🔄 Unexpected case - ending turn");
        TurnManager.Instance?.EndTurn();
        yield break;
    }

    // Helper method for getting adjacent moves (used by derived classes)
    protected List<Vector2Int> GetAdjacentMoves(RPSUnit unit)
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        return directions
            .Select(dir => unit.Position + dir)
            .Where(pos => BoardManager.Instance.IsInsideBoard(pos))
            .Where(pos => {
                Vector2Int delta = pos - unit.Position;
                return Mathf.Abs(delta.x) + Mathf.Abs(delta.y) == 1;
            })
            .Where(pos => {
                // בדיקה שלא תוקפים את עצמנו
                var targetUnit = BoardManager.Instance.GetUnitAt(pos) as RPSUnit;
                if (targetUnit != null && targetUnit.playerId == unit.playerId)
                {
                    return false; // לא תוקפים את עצמנו
                }
                return true;
            })
            .ToList();
    }

    // ✅ פונקציות עזר ל-AI Medium
    protected List<RPSUnit> GetAllAIUnitsThatCanMove()
    {
        return FindObjectsOfType<RPSUnit>()
            .Where(u => u.playerId == 2 && u.IsMovable())
            .ToList();
    }

    protected virtual void ExecuteMove(RPSUnit unit, Vector2Int target)
    {
        // בדיקה שהתנועה היא חוקית (צעד אחד)
        Vector2Int delta = target - unit.Position;
        if (!(Mathf.Abs(delta.x) + Mathf.Abs(delta.y) == 1))
        {
            UnityEngine.Debug.Log($"🚫 Invalid move attempt from {unit.Position} to {target}");
            TurnManager.Instance?.EndTurn();
            return;
        }

        var enemyUnit = BoardManager.Instance.GetUnitAt(target);
        var enemy = enemyUnit as RPSUnit;

        if (enemy != null)
        {
            unit.Reveal();
            enemy.Reveal();

            if (enemy.role == RPSUnit.UnitRole.Trap)
            {
                UnityEngine.Debug.Log($"💥 {unit.name} stepped on a TRAP at {target} and was destroyed");
                BoardManager.Instance.RemoveUnit(unit);
                Destroy(unit.gameObject);
                TurnManager.Instance?.EndTurn();
                return;
            }

            if (enemy.role == RPSUnit.UnitRole.Flag)
            {
                UnityEngine.Debug.Log($"🎯 {unit.name} captured the FLAG at {target}! YOU LOSE!");
                BoardManager.Instance.RemoveUnit(enemy);
                Destroy(enemy.gameObject);
                BoardManager.Instance.PlaceUnit(unit, target);
                unit.MoveTo(target);
                PlayerController.gameEnded = true;

                // Set player as loser
                TurnTimerManager.Instance?.SetPlayerWon(false);

                // Stop all game systems
                TurnManager.Instance?.StopGame();
                return;
            }

            if (unit.Kind == enemy.Kind)
            {
                UnityEngine.Debug.Log($"🤝 Tie – starting battle panel between {unit.name} and {enemy.name} at {target}");
                BattleManager.Instance?.StartBattle(unit, enemy, target);
                return;
            }

            if (unit.Beats(enemy))
            {
                UnityEngine.Debug.Log($"🏆 {unit.name} wins the battle at {target}: {unit.Kind} beats {enemy.Kind}");
                BoardManager.Instance.RemoveUnit(enemy);
                Destroy(enemy.gameObject);
                BoardManager.Instance.PlaceUnit(unit, target);
                unit.MoveTo(target);
            }
            else
            {
                UnityEngine.Debug.Log($"💀 {unit.name} loses the battle at {target}: {enemy.Kind} beats {unit.Kind}");
                BoardManager.Instance.RemoveUnit(unit);
                Destroy(unit.gameObject);
            }
        }
        else
        {
            UnityEngine.Debug.Log($"🚶 {unit.name} moves to empty tile {target}");
            unit.TryMove(target - unit.Position);
        }

        TurnManager.Instance?.EndTurn();
    }
}

