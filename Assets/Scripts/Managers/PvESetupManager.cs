//using UnityEngine;
//using System.Collections.Generic;

//public class PvESetupManager : MonoBehaviour
//{
//    private List<RPSUnit> player1Units;
//    private List<RPSUnit> player2Units;
//    private int selectionStep = 0;

//    public void Initialize(List<RPSUnit> p1Units, List<RPSUnit> p2Units)
//    {
//        player1Units = p1Units;
//        player2Units = p2Units;
//        selectionStep = 0;

//        // Enable selection for player 1 units only
//        foreach (var unit in player1Units)
//        {
//            unit.EnableSetupSelection();
//            unit.ResetVisual();
//        }
//        foreach (var unit in player2Units)
//        {
//            unit.DisableSetupSelection();
//            unit.ResetVisual();
//        }
//        UnityEngine.Debug.Log("[GameSetup] Setup started. Select FLAG for Player 1.");
//    }

//    public void OnUnitClicked(RPSUnit unit)
//    {
//        if (unit == null) return;

//        switch (selectionStep)
//        {
//            case 0:
//                if (unit.playerId != 1) return;
//                unit.role = RPSUnit.UnitRole.Flag;
//                unit.UpdateVisual();
//                selectionStep++;
//                UnityEngine.Debug.Log("[GameSetup] Player 1 Flag selected. Select TRAP.");
//                break;

//            case 1:
//                if (unit.playerId != 1 || unit.role != RPSUnit.UnitRole.None) return;
//                unit.role = RPSUnit.UnitRole.Trap;
//                unit.UpdateVisual();
//                selectionStep++;

//                UnityEngine.Debug.Log("[GameSetup] AI is choosing FLAG and TRAP...");
//                SelectFTForAI();
//                GameSetupManager.Instance.FinalizeSetup();
//                break;
//        }
//    }

//    private void SelectFTForAI()
//    {
//        List<RPSUnit> available = player2Units.FindAll(u => u.role == RPSUnit.UnitRole.None);
//        if (available.Count < 2) return;

//        int index1 = UnityEngine.Random.Range(0, available.Count);
//        RPSUnit flag = available[index1];
//        flag.role = RPSUnit.UnitRole.Flag;
//        flag.UpdateVisual();

//        available.RemoveAt(index1);
//        int index2 = UnityEngine.Random.Range(0, available.Count);
//        RPSUnit trap = available[index2];
//        trap.role = RPSUnit.UnitRole.Trap;
//        trap.UpdateVisual();

//        UnityEngine.Debug.Log($"[GameSetup] AI selected FLAG: {flag.name}, TRAP: {trap.name}");
//    }
//}