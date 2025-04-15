using UnityEngine;
using TMPro;

public class RPSUnit : Unit
{
    public enum RPSKind { Rock, Paper, Scissors }
    public enum UnitRole { None, Flag, Trap }

    public RPSKind Kind;
    public UnitRole role = UnitRole.None;

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
        if (text != null)
        {
            text.text = GetLetter();
        }
    }

    public void ResetVisual()
    {
        var text = GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = "";
        }
    }

    public void EnableSetupSelection()
    {
        var clickable = GetComponent<SelectableUnit>();
        if (clickable != null)
        {
            clickable.onSetupClick = () => GameSetupManager.Instance.OnUnitClicked(this);
        }
    }

    public void DisableSetupSelection()
    {
        var clickable = GetComponent<SelectableUnit>();
        if (clickable != null)
        {
            clickable.onSetupClick = null;
        }
    }

    public bool IsMovable()
    {
        return role != UnitRole.Flag && role != UnitRole.Trap;
    }
}
