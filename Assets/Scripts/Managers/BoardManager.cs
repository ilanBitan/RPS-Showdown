using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public GameObject tilePrefab;  // The prefab of the Tile
    public Transform boardContainer;  // The container where all the Tiles will be placed
    public int rows = 6;  // The number of rows on the board
    public int columns = 7;  // The number of columns on the board

    void Start()
    {
        GenerateBoard();  // Call the GenerateBoard method to create the board
    }

    // Function to generate the board
    void GenerateBoard()
    {
        // Loop through each row
        for (int row = 0; row < rows; row++)
        {
            // Loop through each column
            for (int col = 0; col < columns; col++)
            {
                // Create a new Tile at the specified location
                GameObject newTile = Instantiate(tilePrefab, boardContainer);
                newTile.name = "Tile " + row + "-" + col; // Assign a name to each tile to avoid confusion

                // Set the position of the Tile based on column and row
                RectTransform rectTransform = newTile.GetComponent<RectTransform>();
                rectTransform.localPosition = new Vector3(col * 105, -row * 105, 0); // Adjust this according to tile size
            }
        }
    }
}
