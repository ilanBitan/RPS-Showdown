using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using static RPSUnit;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    public GameObject battlePanel;
    public Button rockButton, paperButton, scissorsButton;

    private RPSUnit playerUnit;
    private RPSUnit aiUnit;
    private Vector2Int targetPos;
    private bool isBattleActive = false;

    private RPSUnit.RPSKind playerChoice;
    private RPSUnit.RPSKind aiChoice;

    private bool isPlayerInitiator;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        battlePanel?.SetActive(false);
    }

    public void StartBattle(RPSUnit initiator, RPSUnit opponent, Vector2Int target)
    {
        //   if (GameEndHandler.gameEnded)
        //      return;

        isPlayerInitiator = initiator.IsPlayerControlled;

        if (isPlayerInitiator)
        {
            playerUnit = initiator;
            aiUnit = opponent;
        }
        else
        {
            aiUnit = initiator;
            playerUnit = opponent;
        }

        targetPos = target;
        isBattleActive = true;

        // CRITICAL: Stop the timer during battles to prevent race conditions
        TurnTimerManager.Instance?.StopTimer();
        UnityEngine.Debug.Log("[BattleManager] Timer stopped for battle");

        // If in PvP mode, use PvPMoveLogger
        if (GameModeManager.Instance.SelectedMode == GameMode.PvP && PvPMoveLogger.Instance != null)
        {
            PvPMoveLogger.Instance.StartBattle(playerUnit, aiUnit, targetPos);
        }

        ShowPlayerPanel();
    }

    public void ShowPlayerPanel()
    {
        battlePanel?.SetActive(true);

        rockButton.onClick.RemoveAllListeners();
        rockButton.onClick.AddListener(() => OnPlayerChoice(RPSUnit.RPSKind.Rock));

        paperButton.onClick.RemoveAllListeners();
        paperButton.onClick.AddListener(() => OnPlayerChoice(RPSUnit.RPSKind.Paper));

        scissorsButton.onClick.RemoveAllListeners();
        scissorsButton.onClick.AddListener(() => OnPlayerChoice(RPSUnit.RPSKind.Scissors));
    }

    private void OnPlayerChoice(RPSUnit.RPSKind choice)
    {
        playerChoice = choice;

        // If in PvP mode, log the choice to server
        if (GameModeManager.Instance.SelectedMode == GameMode.PvP && PvPMoveLogger.Instance != null)
        {
            PvPMoveLogger.Instance.LogBattleChoice(choice);
            return;
        }

        // בדיקה אם זו רמה קשה
        bool isHardLevel = FindObjectOfType<AIPlayerHardController>() != null;

        if (isHardLevel)
        {
            UnityEngine.Debug.Log("Hard level detected - AI will make smart choice based on player statistics");

            // קבלת סטטיסטיקות מהשרת
            FirebaseManager.Instance?.DatabaseService?.GetUserStats((userData) =>
            {
                if (userData != null)
                {
                    UnityEngine.Debug.Log($"Player statistics - Rock: {userData.rockChoices}, Paper: {userData.paperChoices}, Scissors: {userData.scissorsChoices}");

                    // מציאת הכלי הנפוץ ביותר של השחקן
                    RPSUnit.RPSKind mostCommonChoice = RPSUnit.RPSKind.Rock;
                    int maxCount = userData.rockChoices;

                    if (userData.paperChoices > maxCount)
                    {
                        mostCommonChoice = RPSUnit.RPSKind.Paper;
                        maxCount = userData.paperChoices;
                    }
                    if (userData.scissorsChoices > maxCount)
                    {
                        mostCommonChoice = RPSUnit.RPSKind.Scissors;
                        maxCount = userData.scissorsChoices;
                    }

                    UnityEngine.Debug.Log($"Player's most common choice: {mostCommonChoice}");

                    // בחירת הכלי המנצח
                    switch (mostCommonChoice)
                    {
                        case RPSUnit.RPSKind.Rock:
                            aiChoice = RPSUnit.RPSKind.Paper;
                            break;
                        case RPSUnit.RPSKind.Paper:
                            aiChoice = RPSUnit.RPSKind.Scissors;
                            break;
                        case RPSUnit.RPSKind.Scissors:
                            aiChoice = RPSUnit.RPSKind.Rock;
                            break;
                    }

                    UnityEngine.Debug.Log($"AI chose {aiChoice} to counter player's {mostCommonChoice}");
                }
                else
                {
                    // אם אין נתונים, נבחר באופן רנדומלי
                    aiChoice = (RPSUnit.RPSKind)UnityEngine.Random.Range(0, 3);
                    UnityEngine.Debug.Log("No player statistics available - AI chose randomly: " + aiChoice);
                }

                // נעדכן את הסטטיסטיקות בכל פעם שהשחקן בוחר כלי
                FirebaseManager.Instance?.DatabaseService?.UpdateRPSChoice(choice);

                ResolveBattle();
            });
        }
        else
        {
            // רמה רגילה - בחירה רנדומלית
            aiChoice = (RPSUnit.RPSKind)UnityEngine.Random.Range(0, 3);
            UnityEngine.Debug.Log($"Battle initiated! Player chose {playerChoice}, AI chose {aiChoice} (random)");

            // נעדכן את הסטטיסטיקות בכל פעם שהשחקן בוחר כלי
            FirebaseManager.Instance?.DatabaseService?.UpdateRPSChoice(choice);

            ResolveBattle();
        }
    }

    private void ResolveBattle()
    {
        battlePanel?.SetActive(false);

        bool playerWins = Beats(playerChoice, aiChoice);
        bool aiWins = Beats(aiChoice, playerChoice);

        // חשיפת שתי היחידות תמיד
        playerUnit.Kind = playerChoice;
        aiUnit.Kind = aiChoice;

        playerUnit.Reveal();
        aiUnit.Reveal();

        playerUnit.UpdateVisual();
        aiUnit.UpdateVisual();

        // בדיקת ניצחון אם FLAG נחשף
        if (playerUnit.role == RPSUnit.UnitRole.Flag)
        {
            //FindObjectOfType<GameEndHandler>().ShowVictory("Player 2");
            return;
        }
        if (aiUnit.role == RPSUnit.UnitRole.Flag)
        {
            //   FindObjectOfType<GameEndHandler>().ShowVictory("Player 1");
            return;
        }

        if (playerWins)
        {
            var hardAI = FindObjectOfType<AIPlayerHardController>();
            if (hardAI != null)
            {
                hardAI.OnUnitDestroyed(aiUnit);
            }
            UnityEngine.Debug.Log("✅ Player wins the battle!");
            
            // עדכון ה-AI הקשה על דמות שהושמדה
            var hardAI = FindObjectOfType<AIPlayerHardController>();
            if (hardAI != null)
            {
                hardAI.OnUnitDestroyed(aiUnit);
            }
            
            BoardManager.Instance.RemoveUnit(aiUnit);
            Destroy(aiUnit.gameObject);
            playerUnit.MoveTo(targetPos);
            EndBattle();
        }
        else if (aiWins)
        {
                        // עדכון ה-AI הקשה על דמות שהושמדה
            var hardAI = FindObjectOfType<AIPlayerHardController>();
            if (hardAI != null)
            {
                hardAI.OnUnitDestroyed(playerUnit);
            }
            UnityEngine.Debug.Log("❌ AI wins the battle!");
            
            // עדכון ה-AI הקשה על דמות שהושמדה
            var hardAI = FindObjectOfType<AIPlayerHardController>();
            if (hardAI != null)
            {
                hardAI.OnUnitDestroyed(playerUnit);
            }
            
            BoardManager.Instance.RemoveUnit(playerUnit);
            Destroy(playerUnit.gameObject);
            aiUnit.MoveTo(targetPos);
            EndBattle();
        }
        else
        {
            UnityEngine.Debug.Log("🤝 Tie – rematch round!");
            battlePanel?.SetActive(true);
            Invoke(nameof(ShowPlayerPanel), 0.5f);
            return;
        }

        foreach (var controller in FindObjectsOfType<PlayerController>())
            controller.ClearSelection();
    }

    private void MoveUnitTo(RPSUnit unit, Vector2Int target)
    {
        unit.MoveTo(target);
    }

    private bool Beats(RPSUnit.RPSKind a, RPSUnit.RPSKind b)
    {
        return (a == RPSUnit.RPSKind.Rock && b == RPSUnit.RPSKind.Scissors) ||
               (a == RPSUnit.RPSKind.Paper && b == RPSUnit.RPSKind.Rock) ||
               (a == RPSUnit.RPSKind.Scissors && b == RPSUnit.RPSKind.Paper);
    }

    private void EndBattle()
    {
        isBattleActive = false;
        playerUnit = null;
        aiUnit = null;
        battlePanel?.SetActive(false);

        TurnManager.Instance?.EndTurn();
    }

    public bool IsBattleActive()
    {
        return isBattleActive;
    }

    public void SetBattleActive(bool active)
    {
        isBattleActive = active;
        if (!active)
        {
            battlePanel?.SetActive(false);
        }
    }
}
