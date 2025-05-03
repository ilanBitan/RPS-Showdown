public static class RPSExtensions
{
    public static bool Beats(this RPSUnit.RPSKind self, RPSUnit.RPSKind other)
    {
        return (self == RPSUnit.RPSKind.Rock && other == RPSUnit.RPSKind.Scissors) ||
               (self == RPSUnit.RPSKind.Paper && other == RPSUnit.RPSKind.Rock) ||
               (self == RPSUnit.RPSKind.Scissors && other == RPSUnit.RPSKind.Paper);
    }
}
