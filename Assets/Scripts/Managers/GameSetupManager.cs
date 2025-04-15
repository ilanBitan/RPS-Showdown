using System.Collections.Generic;
using UnityEngine;

public class GameSetupManager : MonoBehaviour
{
    public static GameSetupManager Instance;

    private List<RPSUnit> player1Units;
    private List<RPSUnit> player2Units;

    private int selectionStep = 0; // 0 = F1, 1 = T1, 2 = F2, 3 = T2
    private bool setupComplete = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    public void StartSetup(List<RPSUnit> p1Units, List<RPSUnit> p2Units)
    {
        player1Units = p1Units;
        player2Units = p2Units;

        foreach (var unit in player1Units)
        {
            unit.EnableSetupSelection();
            unit.ResetVisual();
        }

        foreach (var unit in player2Units)
        {
            unit.EnableSetupSelection();
            unit.ResetVisual();
        }

        Debug.Log("🎯 Setup phase started. Select FLAG for Player 1.");
    }

    public void OnUnitClicked(RPSUnit unit)
    {
        if (setupComplete || unit == null) return;

        switch (selectionStep)
        {
            case 0:
                if (unit.playerId != 1) return;
                unit.role = RPSUnit.UnitRole.Flag;
                unit.UpdateVisual();
                selectionStep++;
                Debug.Log("🎯 Player 1 Flag selected. Select TRAP.");
                break;

            case 1:
                if (unit.playerId != 1 || unit.role != RPSUnit.UnitRole.None) return;
                unit.role = RPSUnit.UnitRole.Trap;
                unit.UpdateVisual();
                selectionStep++;
                Debug.Log("🎯 Player 1 Trap selected. Select FLAG for Player 2.");
                break;

            case 2:
                if (unit.playerId != 2) return;
                unit.role = RPSUnit.UnitRole.Flag;
                unit.UpdateVisual();
                selectionStep++;
                Debug.Log("🎯 Player 2 Flag selected. Select TRAP.");
                break;

            case 3:
                if (unit.playerId != 2 || unit.role != RPSUnit.UnitRole.None) return;
                unit.role = RPSUnit.UnitRole.Trap;
                unit.UpdateVisual();
                FinalizeSetup();
                break;
        }
    }

    void FinalizeSetup()
    {
        Debug.Log("🎲 Finalizing setup: assigning RPS roles randomly");

        AssignRandomRPS(player1Units);
        AssignRandomRPS(player2Units);

        foreach (var unit in player1Units) unit.DisableSetupSelection();
        foreach (var unit in player2Units) unit.DisableSetupSelection();

        setupComplete = true;
        Debug.Log("✅ Setup complete! Let the game begin.");

        // ⏳ מפעילים את הטיימר רק עכשיו
        TurnTimerManager.Instance?.ActivateGameTimer();
        TurnTimerManager.Instance?.StartTurn();
    }

    void AssignRandomRPS(List<RPSUnit> units)
    {
        List<RPSUnit> toAssign = units.FindAll(u => u.role == RPSUnit.UnitRole.None);

        int total = toAssign.Count;
        int countPerKind = total / 3;

        List<RPSUnit.RPSKind> kinds = new List<RPSUnit.RPSKind>();

        for (int i = 0; i < countPerKind; i++)
        {
            kinds.Add(RPSUnit.RPSKind.Rock);
            kinds.Add(RPSUnit.RPSKind.Paper);
            kinds.Add(RPSUnit.RPSKind.Scissors);
        }

        while (kinds.Count < total)
        {
            kinds.Add((RPSUnit.RPSKind)Random.Range(0, 3));
        }

        Shuffle(kinds);

        for (int i = 0; i < toAssign.Count; i++)
        {
            toAssign[i].Kind = kinds[i];
            toAssign[i].UpdateVisual();
        }
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = Random.Range(i, list.Count);
            (list[i], list[rnd]) = (list[rnd], list[i]);
        }
    }

    public bool IsSetupComplete() => setupComplete;
}
