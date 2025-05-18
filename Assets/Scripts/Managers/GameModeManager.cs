using UnityEngine;

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance;

    public GameMode SelectedMode = GameMode.PvE_Easy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void SelectEasy()
    {
        SelectedMode = GameMode.PvE_Easy;
    }

    public void SelectMedium()
    {
        SelectedMode = GameMode.PvE_Medium;
    }

    public void SelectHard()
    {
        SelectedMode = GameMode.PvE_Hard;
    }
}
