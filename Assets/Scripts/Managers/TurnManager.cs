using UnityEngine;
using System.Collections;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    private bool isPlayer1Turn = true;
    private bool gameActive = true;
    private static bool lastGameStartedWithPlayer1 = true;
    private static bool isFirstGame = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        if (isFirstGame)
        {
            // First game - random selection
            isPlayer1Turn = UnityEngine.Random.Range(0, 2) == 0;
            lastGameStartedWithPlayer1 = isPlayer1Turn;
            isFirstGame = false;
        }
        else
        {
            // Subsequent games - reverse from who started last
            isPlayer1Turn = !lastGameStartedWithPlayer1;
            lastGameStartedWithPlayer1 = isPlayer1Turn;
        }

        // Ensure PvPMoveLogger is active for PvP mode
        if (GameModeManager.Instance != null && GameModeManager.Instance.SelectedMode == GameMode.PvP)
        {
            StartCoroutine(EnsurePvPMoveLoggerActive());
        }
    }

    /// <summary>
    /// Ensure PvPMoveLogger is active in PvP mode
    /// </summary>
    private IEnumerator EnsurePvPMoveLoggerActive()
    {
        // Wait until game is ready
        yield return new WaitForSeconds(1f);

        if (PvPMoveLogger.Instance == null)
        {
            // Create PvPMoveLogger if it doesn't exist
            GameObject loggerObj = new GameObject("PvPMoveLogger");
            PvPMoveLogger logger = loggerObj.AddComponent<PvPMoveLogger>();

            // Get room details from UnitPlacer or GameSetupManager
            UnitPlacer unitPlacer = FindObjectOfType<UnitPlacer>();
            if (unitPlacer != null)
            {
                // Cannot get details directly, will need to wait for them to be activated
                UnityEngine.Debug.Log("[TurnManager] PvPMoveLogger created, waiting for initialization...");
            }
        }

        UnityEngine.Debug.Log("[TurnManager] PvP mode detected - move synchronization active");
    }

    public void StartPlayerTurn()
    {
        if (!gameActive) return;
        isPlayer1Turn = true;
        TurnTimerManager.Instance?.StartTurn();
    }

    public void StartAITurn()
    {
        if (!gameActive) return;
        isPlayer1Turn = false;
        TurnTimerManager.Instance?.StartTurn();
    }

    public void StartGuestTurn()
    {
        if (!gameActive) return;
        isPlayer1Turn = true;
        TurnTimerManager.Instance?.StartTurn();
    }

    public void EndTurn()
    {
        if (!gameActive) return;

        // In PvP mode, just switch turns without AI involvement
        if (GameModeManager.Instance != null && GameModeManager.Instance.SelectedMode == GameMode.PvP)
        {
            isPlayer1Turn = !isPlayer1Turn;
            TurnTimerManager.Instance?.StartTurn();
            UnityEngine.Debug.Log($"[TurnManager] PvP turn ended. Next turn: Player {(isPlayer1Turn ? 1 : 2)}");
            return;
        }

        // In PvE mode - original logic
        isPlayer1Turn = !isPlayer1Turn;
        TurnTimerManager.Instance?.StartTurn();

        if (!isPlayer1Turn)
        {
            AIPlayerController ai = FindObjectOfType<AIPlayerController>();
            if (ai != null)
            {
                StartCoroutine(StartAITurnWithDelay(ai));
            }
        }
    }

    private IEnumerator StartAITurnWithDelay(AIPlayerController ai)
    {
        yield return new WaitForSeconds(0.5f);
        ai.PlayTurn();
    }

    public bool IsPlayerTurn(int playerId)
    {
        return gameActive && ((playerId == 1 && isPlayer1Turn) || (playerId == 2 && !isPlayer1Turn));
    }

    public void StopGame()
    {
        gameActive = false;
        TurnTimerManager.Instance?.StopTimer();

        // Stop listening to moves in PvP mode
        if (PvPMoveLogger.Instance != null)
        {
            PvPMoveLogger.Instance.StopListening();
        }
    }

    public void StartDuel(RPSUnit unit1, RPSUnit unit2)
    {
        int winner = UnityEngine.Random.Range(0, 2);

        if (winner == 0)
        {
            BoardManager.Instance.RemoveUnit(unit2);
            Destroy(unit2.gameObject);
            unit1.MoveTo(unit2.Position);
        }
        else
        {
            BoardManager.Instance.RemoveUnit(unit1);
            Destroy(unit1.gameObject);
        }

        EndTurn();
    }
}