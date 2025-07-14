using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Managers")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private TurnTimerManager timerManager;
    [SerializeField] private BoardManager boardManager;

    [Header("Prefabs")]
    [SerializeField] private GameObject tilePrefab;

    private List<RPSUnit> player1Units;
    private List<RPSUnit> player2Units;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log("🧱 GameManager is alive in scene: " + SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene")
        {
            Debug.Log("🎬 GameScene loaded, assigning scene objects...");
            StartCoroutine(AssignSceneObjects());
        }
    }

    private IEnumerator AssignSceneObjects()
    {
        yield return null; // Wait 1 frame to make sure everything is loaded

        AssignBattleManagerFields();
        AssignTimerManagerFields();
        AssignBoardManagerFields();
    }

    private void AssignBattleManagerFields()
    {
        if (battleManager == null) battleManager = GetComponent<BattleManager>();

        battleManager.battlePanel = GameObject.Find("BattlePanel");
        battleManager.rockButton = GameObject.Find("RockButton")?.GetComponent<Button>();
        battleManager.paperButton = GameObject.Find("PaperButton")?.GetComponent<Button>();
        battleManager.scissorsButton = GameObject.Find("ScissorsButton")?.GetComponent<Button>();

        if (battleManager.battlePanel == null)
            Debug.LogError("❌ BattlePanel not found in GameScene");
    }

    private void AssignTimerManagerFields()
    {
        if (timerManager == null) timerManager = GetComponent<TurnTimerManager>();

        timerManager.timerText = GameObject.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
        timerManager.playAgainContainer = GameObject.Find("PlayAgainContainer");
        timerManager.playAgainText = GameObject.Find("PlayAgainText")?.GetComponent<TMPro.TextMeshProUGUI>();

        if (timerManager.timerText == null)
            Debug.LogError("❌ TimerText not found in GameScene");
        
        if (timerManager.playAgainContainer == null)
            Debug.LogError("❌ PlayAgainContainer not found in GameScene");
        if (timerManager.playAgainText == null)
            Debug.LogError("❌ PlayAgainText not found in GameScene");
    }

    private void AssignBoardManagerFields()
    {
        if (boardManager == null) boardManager = GetComponent<BoardManager>();

        boardManager.tilePrefab = tilePrefab;
        boardManager.boardParent = GameObject.Find("Board")?.GetComponent<RectTransform>();

        if (boardManager.boardParent == null)
            Debug.LogError("❌ Board not found in GameScene");
    }

    public void SetPlayersUnits(List<RPSUnit> p1, List<RPSUnit> p2)
    {
        player1Units = p1;
        player2Units = p2;
    }

    public List<RPSUnit> GetPlayer1Units() => player1Units;
    public List<RPSUnit> GetPlayer2Units() => player2Units;
}
