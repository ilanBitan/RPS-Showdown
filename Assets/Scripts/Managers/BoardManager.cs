using System.Diagnostics;
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
    private float tileSize;

    public static BoardManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        tiles = new RectTransform[rows, columns];
        unitGrid = new Unit[columns, rows]; // Note: [x, y] → [col, row]

        // Force the board to stretch
        RectTransform boardRect = boardParent.GetComponent<RectTransform>();
        boardRect.anchorMin = new Vector2(0, 0);
        boardRect.anchorMax = new Vector2(1, 1);
        boardRect.offsetMin = Vector2.zero;
        boardRect.offsetMax = Vector2.zero;

        // Calculate tile size based on container width
        float containerWidth = boardRect.rect.width;
        tileSize = containerWidth / columns;

        // Calculate total height needed
        float totalHeight = tileSize * rows;

        // Calculate vertical offset to center the board
        float verticalOffset = (boardRect.rect.height - totalHeight) / 2;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                GameObject tile = Instantiate(tilePrefab, boardParent);
                tile.name = $"Tile_{row}_{col}";
                RectTransform rt = tile.GetComponent<RectTransform>();
                tiles[row, col] = rt;

                // Position tiles to fill width and center vertically
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.sizeDelta = new Vector2(tileSize, tileSize);
                rt.anchoredPosition = new Vector2(
                    col * tileSize + (tileSize / 2),
                    -verticalOffset - (row * tileSize) - (tileSize / 2)
                );

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
        Unit unit = unitGrid[pos.x, pos.y];
        UnityEngine.Debug.Log($"[BoardManager] GetUnitAt({pos}): {(unit != null ? $"{unit.name} (Player {unit.playerId}) at {unit.Position}" : "null")}");
        return unit;
    }

    public void PlaceUnit(Unit unit, Vector2Int pos)
    {
        if (!IsInsideBoard(pos)) return;

        Unit existingUnit = unitGrid[pos.x, pos.y];
        if (existingUnit != null && existingUnit != unit)
        {
            UnityEngine.Debug.LogWarning($"[BoardManager] PlaceUnit WARNING: Overwriting unit {existingUnit.name} at {pos} with {unit.name}");
        }

        unitGrid[pos.x, pos.y] = unit;
        unit.SetPosition(pos);

        RectTransform tile = tiles[pos.y, pos.x];
        unit.transform.SetParent(tile, false);
        unit.transform.localPosition = Vector3.zero;

        UnityEngine.Debug.Log($"📌 [BoardManager] PlaceUnit: {unit.name} at {pos} (old position NOT cleared!)");
    }

    public void MoveUnit(Unit unit, Vector2Int newPos)
    {
        Vector2Int oldPos = unit.Position;
        UnityEngine.Debug.Log($"[BoardManager] MoveUnit: {unit.name} from {oldPos} to {newPos}");

        if (IsInsideBoard(oldPos))
        {
            unitGrid[oldPos.x, oldPos.y] = null;
            UnityEngine.Debug.Log($"[BoardManager] Cleared old position {oldPos}");
        }

        if (IsInsideBoard(newPos))
        {
            Unit existingUnit = unitGrid[newPos.x, newPos.y];
            if (existingUnit != null && existingUnit != unit)
            {
                UnityEngine.Debug.LogWarning($"[BoardManager] WARNING: Overwriting unit {existingUnit.name} at {newPos} with {unit.name}");
            }
            unitGrid[newPos.x, newPos.y] = unit;
            UnityEngine.Debug.Log($"[BoardManager] Placed {unit.name} at new position {newPos}");
        }
    }

    public void RemoveUnit(Unit unit)
    {
        if (unit == null) return;

        Vector2Int pos = unit.Position;
        if (IsInsideBoard(pos))
        {
            if (unitGrid[pos.x, pos.y] == unit)
            {
                unitGrid[pos.x, pos.y] = null;
                UnityEngine.Debug.Log($"🗑️ Removed unit from [col {pos.x}, row {pos.y}]");
            }
            else if (unitGrid[pos.x, pos.y] != null)
            {
                UnityEngine.Debug.Log($"⚠️ Warning: Unit at [col {pos.x}, row {pos.y}] doesn't match the unit being removed");
                unitGrid[pos.x, pos.y] = null;
            }
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

        UnityEngine.Debug.Log($"🔁 Swapped units {unitA.name} and {unitB.name} between {posA} and {posB}");
    }

    /// <summary>
    /// Clear unit reference at specific position - used for PvP synchronization fixes
    /// </summary>
    public void ClearUnitAt(Vector2Int pos)
    {
        if (!IsInsideBoard(pos)) return;

        unitGrid[pos.x, pos.y] = null;
        UnityEngine.Debug.Log($"[BoardManager] Manually cleared unit reference at {pos}");
    }
}
