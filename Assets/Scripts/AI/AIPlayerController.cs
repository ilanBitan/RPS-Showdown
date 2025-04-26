using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AIPlayerController : MonoBehaviour
{
    public static AIPlayerController Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private void Start()
    {
        Debug.Log("🔥 AIPlayerController active and ready.");
    }

    public void PlayTurn()
    {
        Debug.Log("🧠 PlayTurn called!");
        StartCoroutine(PerformAIAction());
    }


    private IEnumerator PerformAIAction()
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
                    if (unit.TryMove(dir))
                    {
                        yield return new WaitForSeconds(0.3f);
                        TurnManager.Instance?.EndTurn();
                        yield break;
                    }
                    continue;
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
                    BoardManager.Instance.PlaceUnit(unit, target);
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
                    BoardManager.Instance.PlaceUnit(unit, target);
                    unit.MoveTo(target);

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


}
