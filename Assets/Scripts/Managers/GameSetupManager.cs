using System.Collections.Generic;
using UnityEngine;

public class GameSetupManager : MonoBehaviour
{
    public static GameSetupManager Instance;

    private List<RPSUnit> player1Units;
    private List<RPSUnit> player2Units;

    private int selectionStep = 0;
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
            unit.DisableSetupSelection();
            unit.ResetVisual();
        }

        Debug.Log("🎯 Setup started. Select FLAG for Player 1.");

        // ✅ הוספת העברת היחידות ל־GameManager
        GameManager.Instance?.SetPlayersUnits(player1Units, player2Units);
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

                if (GameModeManager.Instance.SelectedMode == GameMode.PvP)
                {
                    Debug.Log("🎯 Now select FLAG for Player 2.");
                }
                else
                {
                    Debug.Log("🤖 AI is choosing FLAG and TRAP...");
                    SelectFTForAI();
                    FinalizeSetup();
                }
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

    private void SelectFTForAI()
    {
        List<RPSUnit> available = player2Units.FindAll(u => u.role == RPSUnit.UnitRole.None);
        if (available.Count < 2) return;

        int index1 = Random.Range(0, available.Count);
        RPSUnit flag = available[index1];
        flag.role = RPSUnit.UnitRole.Flag;
        flag.UpdateVisual();

        available.RemoveAt(index1);
        int index2 = Random.Range(0, available.Count);
        RPSUnit trap = available[index2];
        trap.role = RPSUnit.UnitRole.Trap;
        trap.UpdateVisual();

        Debug.Log($"🤖 AI selected FLAG: {flag.name}, TRAP: {trap.name}");
    }

    private void FinalizeSetup()
    {
        Debug.Log("🎲 Finalizing setup: assigning RPS roles randomly...");

        // 🪨📄✂️ מגדיר תפקידי RPS ליחידות שאין להן תפקיד
        AssignRandomRPS(player1Units);
        AssignRandomRPS(player2Units);

        // ❌ מבטל אפשרות בחירה ליחידות אחרי שהוגדרו
        foreach (var unit in player1Units) unit.DisableSetupSelection();
        foreach (var unit in player2Units) unit.DisableSetupSelection();

        // ✅ מסמן שה-Setup הסתיים
        setupComplete = true;
        Debug.Log("✅ Setup complete. Game begins!");

        // ⏳ מפעיל טיימר כללי למשחק
        TurnTimerManager.Instance?.ActivateGameTimer();

        // 🧠 במצב נגד AI, מוודא שיש AIPlayerController
        var mode = GameModeManager.Instance.SelectedMode;
        if (mode == GameMode.PvE_Easy || mode == GameMode.PvE_Medium || mode == GameMode.PvE_Hard)
        {
            if (FindObjectOfType<AIPlayerController>() == null)
            {
                GameObject aiObj = new GameObject("AIPlayerController");

                switch (mode)
                {
                    case GameMode.PvE_Easy:
                        aiObj.AddComponent<AIPlayerController>();
                        Debug.Log("🧠 Easy AI instantiated.");
                        break;

                    case GameMode.PvE_Medium:
                        aiObj.AddComponent<AIPlayerMediumController>();
                        Debug.Log("🧠 Medium AI instantiated.");
                        break;

                    case GameMode.PvE_Hard:
                        // בהמשך תוכל להוסיף גם רמה קשה
                        aiObj.AddComponent<AIPlayerHardController>();
                        Debug.Log("🧠 Hard AI (placeholder) instantiated.");
                        break;
                }
            }
        }


        // 🎯 רנדומיזציה: מי יתחיל את המשחק, שחקן או AI?
        bool playerStarts = Random.Range(0, 2) == 0;

        if (playerStarts)
        {
            Debug.Log("🎯 Player 1 starts the game!");
            TurnManager.Instance?.StartPlayerTurn(); // מפעיל תור שחקן כולל טיימר
        }
        else
        {
            Debug.Log("🤖 AI starts the game!");
            TurnManager.Instance?.StartAITurn(); // מפעיל תור AI כולל טיימר
        }
    }


    private void AssignRandomRPS(List<RPSUnit> units)
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

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }

    public bool IsSetupComplete() => setupComplete;
}
