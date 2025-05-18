using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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


    protected virtual void Start()
    {
        Debug.Log("🔥 AIPlayerController active and ready.");
    }

    public void PlayTurn()
    {
        if (!TurnManager.Instance.IsPlayerTurn(2))
        {
            Debug.Log("🛑 Not AI's turn – skipping.");
            return;
        }

        Debug.Log("🧠 PlayTurn called!");
        StartCoroutine(PerformAIAction());
    }



    protected virtual IEnumerator PerformAIAction()
    {
        if (PlayerController.gameEnded)
        {
            Debug.Log("🛑 Game ended - AI stops.");
            yield break;
        }

        yield return new WaitForSeconds(0.5f);
        Debug.Log("🤖 AI is thinking...");
        RPSUnit[] allUnits = FindObjectsOfType<RPSUnit>();
        List<RPSUnit> aiUnits = allUnits.Where(u => u.playerId == 2).ToList();
        List<RPSUnit> enemyUnits = allUnits.Where(u => u.playerId == 1).ToList();

        List<RPSUnit> movableUnits = aiUnits
            .Where(u => u.IsMovable())
            .OrderBy(_ => Random.value)
            .ToList();

        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.up
        };

        foreach (var unit in movableUnits)
        {
            Debug.Log($"🎯 Evaluating {unit.name} at {unit.Position}");

            directions = directions.OrderBy(_ => Random.value).ToArray();

            foreach (var dir in directions)
            {
                Vector2Int target = unit.Position + dir;

                if (!BoardManager.Instance.IsInsideBoard(target))
                {
                    continue;
                }

                Unit targetUnit = BoardManager.Instance.GetUnitAt(target);

                if (targetUnit == null)
                {
                    Debug.Log($"🤖 AI moving unit to empty tile {target}");
                    // Use the RPSUnit's MoveTo method which now has animation built in
                    unit.MoveTo(target);
                    yield return new WaitForSeconds(0.6f); // Wait for animation to complete
                    TurnManager.Instance?.EndTurn();
                    yield break;
                }

                if (targetUnit.playerId == unit.playerId)
                {
                    continue;
                }

                RPSUnit enemy = targetUnit as RPSUnit;
                if (enemy == null)
                {
                    continue;
                }

                // 💣 TRAP
                if (enemy.role == RPSUnit.UnitRole.Trap)
                {
                    Debug.Log("💥 AI stepped on trap and is destroyed.");

                    BoardManager.Instance.RemoveUnit(unit);
                    Destroy(unit.gameObject);

                    TurnManager.Instance?.EndTurn();
                    yield break;
                }

                // 🏁 FLAG
                if (enemy.role == RPSUnit.UnitRole.Flag)
                {
                    Debug.Log("🎯 AI captured the FLAG!");
                    BoardManager.Instance.RemoveUnit(enemy);
                    Destroy(enemy.gameObject);
                    // Use unit.MoveTo which now includes animation
                    unit.MoveTo(target);

                    PlayerController.gameEnded = true; // ❗ נעילת המשחק אחרי תפיסת דגל
                    yield break;
                }


                // 🔁 RPS Battle
                if (unit.Kind == enemy.Kind)
                {
                    Debug.Log("🤝 Same kind – triggering battle panel.");
                    BattleManager.Instance?.StartBattle(unit, enemy, target);
                    yield break;
                }

                // ✅ קרב רגיל
                if (unit.Beats(enemy))
                {
                    Debug.Log($"🏆 AI wins! {unit.Kind} beats {enemy.Kind}");

                    unit.Reveal();    // ה-AI חושף את עצמו
                    enemy.Reveal();   // האויב נחשף

                    BoardManager.Instance.RemoveUnit(enemy);
                    Destroy(enemy.gameObject);
                    // Use unit.MoveTo which now includes animation
                    unit.MoveTo(target);

                    yield return new WaitForSeconds(0.6f); // Wait for animation to complete
                    TurnManager.Instance?.EndTurn();
                    yield break;
                }

                if (enemy.Beats(unit))
                {
                    Debug.Log($"💀 AI loses. {enemy.Kind} beats {unit.Kind}");

                    unit.Reveal();    // ה-AI חושף את עצמו
                    enemy.Reveal();   // היריב נחשף

                    BoardManager.Instance.RemoveUnit(unit);
                    Destroy(unit.gameObject);

                    TurnManager.Instance?.EndTurn();
                    yield break;
                }
            }
        }

        Debug.Log("🤖 No valid actions. Ending turn.");
        TurnManager.Instance?.EndTurn();
    }
    // ✅ פונקציות עזר ל-AI Medium
    protected List<RPSUnit> GetAllAIUnitsThatCanMove()
    {
        return FindObjectsOfType<RPSUnit>()
            .Where(u => u.playerId == 2 && u.IsMovable())
            .ToList();
    }

    protected List<Vector2Int> GetValidMoves(RPSUnit unit)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in dirs)
        {
            Vector2Int target = unit.Position + dir;
            if (BoardManager.Instance.IsInsideBoard(target))
                moves.Add(target);
        }

        return moves;
    }

    protected virtual void ExecuteMove(RPSUnit unit, Vector2Int target)
    {
        var enemyUnit = BoardManager.Instance.GetUnitAt(target);
        var enemy = enemyUnit as RPSUnit;

        if (enemy != null)
        {
            unit.Reveal();
            enemy.Reveal();

            // 🏁 FLAG
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

            // 💣 TRAP
            if (enemy.role == RPSUnit.UnitRole.Trap)
            {
                Debug.Log($"💥 {unit.name} stepped on a TRAP at {target} and was destroyed");

                BoardManager.Instance.RemoveUnit(unit);
                Destroy(unit.gameObject);
                TurnManager.Instance?.EndTurn();
                return;
            }

            // 🤝 TIE – trigger battle panel instead of resolving directly
            if (unit.Kind == enemy.Kind)
            {
                Debug.Log($"🤝 Tie – starting battle panel between {unit.name} and {enemy.name} at {target}");
                BattleManager.Instance?.StartBattle(unit, enemy, target);
                return;
            }

            // 🤜 RPS Battle
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
            // ➡️ Move only
            Debug.Log($"🚶 {unit.name} moves to empty tile {target}");
            unit.TryMove(target - unit.Position);
        }

        TurnManager.Instance?.EndTurn();
    }



}

