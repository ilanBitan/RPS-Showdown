using UnityEngine;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour, IPointerClickHandler
{
    public Vector2Int Position;
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null && Application.isMobilePlatform)
        {
            // Make the touch area slightly larger on mobile
            Vector2 size = rectTransform.sizeDelta;
            rectTransform.sizeDelta = size * 1.1f;
        }
    }

    public void SetPosition(Vector2Int pos)
    {
        Position = pos;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (PlayerController.gameEnded)
            return;

        Debug.Log($"🖱️ Tile clicked at {Position}");

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null)
        {
            Debug.Log("❌ PlayerController not found.");
            return;
        }

        // If we're not in player's turn, ignore the click
        if (!TurnManager.Instance.IsPlayerTurn(player.myPlayerId))
        {
            Debug.Log("⏳ Not your turn.");
            return;
        }

        // If there's a battle active, ignore the click
        if (BattleManager.Instance != null && BattleManager.Instance.IsBattleActive())
        {
            Debug.Log("⚔️ Battle in progress – cannot move now.");
            return;
        }

        RPSUnit selectedUnit = player.SelectedUnit;
        RPSUnit unitOnTile = BoardManager.Instance.GetUnitAt(Position) as RPSUnit;

        // If no unit is selected and clicked on a friendly unit, select it
        if (selectedUnit == null)
        {
            if (unitOnTile != null && unitOnTile.playerId == player.myPlayerId)
            {
                player.SelectUnit(unitOnTile);
                Debug.Log($"🎯 Selected unit at {Position}");
            }
            return;
        }

        // Calculate the movement direction from selected unit to clicked tile
        Vector2Int direction = Position - selectedUnit.Position;

        // Check if the clicked position is adjacent (exactly one step in any direction)
        bool isAdjacent = Mathf.Abs(direction.x) + Mathf.Abs(direction.y) == 1;

        if (isAdjacent)
        {
            // Try to move/attack in that direction
            // This will handle both movement to empty space and attacking enemy units

            selectedUnit.TryMove(direction);
        }
        else
        {
            // If clicked on a different friendly unit, select it instead
            if (unitOnTile != null && unitOnTile.playerId == player.myPlayerId)
            {
                player.SelectUnit(unitOnTile);
                Debug.Log($"🎯 Selected new unit at {Position}");
            }
            else
            {
                Debug.Log("🚫 Invalid move – must move one tile only.");
                player.ClearSelection();
            }
        }
    }
}
