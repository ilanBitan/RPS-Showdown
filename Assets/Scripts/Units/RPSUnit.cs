using UnityEngine;
using TMPro;

public class RPSUnit : Unit
{
    public enum RPSKind { Rock, Paper, Scissors }
    public enum UnitRole { None, Flag, Trap }

    public RPSKind Kind;
    public UnitRole role = UnitRole.None;

    private bool isRevealed = false;
    public bool IsRevealed => isRevealed;

    public override string UnitType => Kind.ToString();

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
        Debug.Log($"🟡 UpdateVisual called for {name}. Found text: {text != null}");

        if (text != null)
        {
            text.text = isRevealed ? GetLetter() : "";
            text.color = Color.white;
        }
    }


    public void Reveal()
    {
        isRevealed = true;
        UpdateVisual();
        Debug.Log($"📣 {name} Revealed → {GetLetter()}");
    }

    public void ResetVisual()
    {
        var text = GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
            text.text = "";
    }

    public void EnableSetupSelection()
    {
        var clickable = GetComponent<SelectableUnit>();
        if (clickable != null)
            clickable.onSetupClick = () => GameSetupManager.Instance.OnUnitClicked(this);
    }

    public void DisableSetupSelection()
    {
        var clickable = GetComponent<SelectableUnit>();
        if (clickable != null)
            clickable.onSetupClick = null;
    }

    public bool IsMovable()
    {
        return role != UnitRole.Flag && role != UnitRole.Trap;
    }

    public bool TryMove(Vector2Int direction)
    {
        Vector2Int targetPos = Position + direction;

        if (!BoardManager.Instance.IsInsideBoard(targetPos))
        {
            Debug.Log($"⛔ Move is out of board bounds");
            return false;
        }

        Unit target = BoardManager.Instance.GetUnitAt(targetPos);

        if (target != null)
        {
            if (target.playerId == playerId)
            {
                Debug.Log("🚫 Cell is occupied by your own unit");
                return false;
            }

            RPSUnit enemy = target as RPSUnit;
            if (enemy == null)
            {
                Debug.Log("❌ Target is not a valid RPS unit");
                return false;
            }

            this.Reveal();
            enemy.Reveal();

            if (enemy.role == UnitRole.Trap)
            {
                Debug.Log("💥 Trap triggered! Unit destroyed.");
                BoardManager.Instance.RemoveUnit(this);
                Destroy(this.gameObject);
                return false;
            }

            if (enemy.role == UnitRole.Flag)
            {
                Debug.Log("🎯 Flag captured!");
                BoardManager.Instance.RemoveUnit(enemy);
                Destroy(enemy.gameObject);
                MoveTo(targetPos);
                BoardManager.Instance.PlaceUnit(this, targetPos);

           /*     // ✨ הצג את מסך הניצחון
                GameEndHandler handler = FindObjectOfType<GameEndHandler>();
                if (handler != null)
                    handler.ShowVictory(playerId == 1 ? "Player 1" : "Player 2");
*/
                return true;
            }


            if (Kind == enemy.Kind)
            {
                Debug.Log("⚔️ Equal units – triggering RPS battle");
                if (BattleManager.Instance != null)
                    BattleManager.Instance.StartBattle(this, enemy, targetPos);
                return false;
            }

            if (Beats(enemy))
            {
                Debug.Log($"✅ {name} wins – replacing {enemy.name}");
                BoardManager.Instance.RemoveUnit(enemy);
                Destroy(enemy.gameObject);
                MoveTo(targetPos);
                BoardManager.Instance.PlaceUnit(this, targetPos);
                return true;
            }

            if (enemy.Beats(this))
            {
                Debug.Log($"💀 {name} loses to {enemy.name} and is destroyed");
                BoardManager.Instance.RemoveUnit(this);
                Destroy(this.gameObject);
                return false;
            }

            Debug.Log("❓ Unhandled combat case");
            return false;
        }

        MoveTo(targetPos);
        BoardManager.Instance.PlaceUnit(this, targetPos);
        return true;
    }

    public void MoveTo(Vector2Int newPos)
    {
        BoardManager.Instance.MoveUnit(this, newPos);
        SetPosition(newPos);
        transform.SetParent(BoardManager.Instance.GetTileTransform(newPos));
        transform.localPosition = Vector3.zero;
        Debug.Log($"✅ Unit moved to → Column: {newPos.x}, Row: {newPos.y}");
    }

    public bool IsEnemy(RPSUnit other)
    {
        return other != null && other.playerId != this.playerId;
    }
}
