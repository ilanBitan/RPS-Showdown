using UnityEngine;
using TMPro;

public class RPSUnit : Unit
{
    public enum RPSKind { Rock, Paper, Scissors }
    public RPSKind Kind;

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
        return Kind switch
        {
            RPSKind.Rock => "R",
            RPSKind.Paper => "P",
            RPSKind.Scissors => "S",
            _ => "?"
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
}
