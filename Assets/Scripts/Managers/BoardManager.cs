using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public int rows = 6;
    public int columns = 7;
    public GameObject tilePrefab;
    public RectTransform boardParent;

    [HideInInspector]
    public RectTransform[,] tiles;

    void Awake()
    {
        tiles = new RectTransform[rows, columns];

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                GameObject tile = Instantiate(tilePrefab, boardParent);
                tile.name = $"Tile_{row}_{col}";
                RectTransform rt = tile.GetComponent<RectTransform>();
                tiles[row, col] = rt;

                // שמור מיקום לפי גודל האריח
                rt.anchoredPosition = new Vector2(col * 100, -row * 100);
                rt.sizeDelta = new Vector2(100, 100);
            }
        }
    }
}
