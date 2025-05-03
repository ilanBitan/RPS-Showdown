using UnityEngine;
using UnityEngine.EventSystems;

public class TouchMovementHandler : MonoBehaviour
{
    public PlayerController playerController;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // למנוע לחיצה על UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

            if (hit.collider != null)
            {
                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile != null && playerController != null)
                {
                    playerController.OnTileTapped(tile.Position);
                }
            }
        }
    }
}
