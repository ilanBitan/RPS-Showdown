using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public int rows = 6;
    public int columns = 7;
    public GameObject tilePrefab;
    public RectTransform boardParent;

    [HideInInspector]
    public RectTransform[,] tiles;

    private Unit[,] unitGrid;

    public static BoardManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        tiles = new RectTransform[rows, columns];
        unitGrid = new Unit[columns, rows]; // לוגי: [x, y] → [col, row]

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                GameObject tile = Instantiate(tilePrefab, boardParent);
                tile.name = $"Tile_{row}_{col}";
                RectTransform rt = tile.GetComponent<RectTransform>();
                tiles[row, col] = rt;

                rt.anchoredPosition = new Vector2(col * 100, -row * 100);
                rt.sizeDelta = new Vector2(100, 100);

                // Initialize the Tile component's Position
                Tile tileComponent = tile.GetComponent<Tile>();
                if (tileComponent != null)
                {
                    tileComponent.SetPosition(new Vector2Int(col, row));
                }
            }
        }
    }

    public bool IsInsideBoard(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < columns && pos.y >= 0 && pos.y < rows;
    }

    public Unit GetUnitAt(Vector2Int pos)
    {
        if (!IsInsideBoard(pos)) return null;
        return unitGrid[pos.x, pos.y];
    }

    public void PlaceUnit(Unit unit, Vector2Int pos)
    {
        if (!IsInsideBoard(pos)) return;

        unitGrid[pos.x, pos.y] = unit;
        unit.SetPosition(pos);

        RectTransform tile = tiles[pos.y, pos.x];
        unit.transform.SetParent(tile, false);
        unit.transform.localPosition = Vector3.zero;

        Debug.Log($"📌 [BoardManager] Placed unit {unit.name} at [col {pos.x}, row {pos.y}]");
    }



    public void MoveUnit(Unit unit, Vector2Int newPos)
    {
        Vector2Int oldPos = unit.Position;
        if (IsInsideBoard(oldPos))
            unitGrid[oldPos.x, oldPos.y] = null;

        if (IsInsideBoard(newPos))
            unitGrid[newPos.x, newPos.y] = unit;
    }

    public void RemoveUnit(Unit unit)
    {
        if (unit == null) return;

        Vector2Int pos = unit.Position;
        if (IsInsideBoard(pos) && unitGrid[pos.x, pos.y] == unit)
        {
            unitGrid[pos.x, pos.y] = null;
            Debug.Log($"🗑️ Removed unit from [col {pos.x}, row {pos.y}]");
        }
    }

    public Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        RectTransform tile = tiles[gridPos.y, gridPos.x];
        return tile.transform.position;
    }

    public Transform GetTileTransform(Vector2Int pos)
    {
        if (!IsInsideBoard(pos)) return null;
        return tiles[pos.y, pos.x];
    }
    public void SwapUnits(Vector2Int posA, Vector2Int posB)
    {
        if (!IsInsideBoard(posA) || !IsInsideBoard(posB)) return;

        Unit unitA = GetUnitAt(posA);
        Unit unitB = GetUnitAt(posB);

        if (unitA == null || unitB == null) return;

        unitGrid[posA.x, posA.y] = unitB;
        unitGrid[posB.x, posB.y] = unitA;

        unitA.SetPosition(posB);
        unitB.SetPosition(posA);

        Transform tileA = GetTileTransform(posA);
        Transform tileB = GetTileTransform(posB);

        if (tileA != null && tileB != null)
        {
            unitA.transform.SetParent(tileB, false);
            unitB.transform.SetParent(tileA, false);

            unitA.transform.localPosition = Vector3.zero;
            unitB.transform.localPosition = Vector3.zero;
        }

        Debug.Log($"🔁 Swapped units {unitA.name} and {unitB.name} between {posA} and {posB}");
    }

}
