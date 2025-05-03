using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int Position;

    public void SetPosition(Vector2Int pos)
    {
        Position = pos;
    }

    private void OnMouseDown()
    {
        Debug.Log($"🖱️ Tile clicked at {Position}");

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null)
        {
            Debug.Log("❌ PlayerController not found.");
            return;
        }

        RPSUnit selectedUnit = player.SelectedUnit;
        if (selectedUnit == null)
        {
            Debug.Log("ℹ️ No unit selected.");
            return;
        }

        Vector2Int currentPos = selectedUnit.Position;
        Vector2Int direction = Position - currentPos;

        if (Mathf.Abs(direction.x) + Mathf.Abs(direction.y) == 1)
        {
            Debug.Log($"✅ Valid move. Moving from {currentPos} to {Position}");
            player.TryMoveSelectedUnit(direction);
        }
        else
        {
            Debug.Log("🚫 Invalid move – must move one tile only.");
        }
    }
}
