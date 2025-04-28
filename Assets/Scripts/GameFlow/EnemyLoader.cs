using UnityEngine;

public class EnemyLoader : MonoBehaviour
{
    public GameObject easyAI;
    public GameObject mediumAI;
    public GameObject hardAI;

    private void Start()
    {
        switch (GameModeManager.Instance.SelectedMode)
        {
            case GameMode.PvE_Easy:
                Instantiate(easyAI);
                break;
            case GameMode.PvE_Medium:
                Instantiate(mediumAI);
                break;
            case GameMode.PvE_Hard:
                Instantiate(hardAI);
                break;
            default:
                Instantiate(easyAI); // ברירת מחדל
                break;
        }
    }
}
