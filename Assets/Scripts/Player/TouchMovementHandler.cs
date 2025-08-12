using UnityEngine;
using UnityEngine.EventSystems;

public class TouchMovementHandler : MonoBehaviour
{
    public PlayerController playerController;
    
    void Start()
    {
        Debug.Log("🔄 TouchMovementHandler started!");
        if (playerController == null)
            Debug.LogError("❌ PlayerController is not assigned!");
        if (Camera.main == null)
            Debug.LogError("❌ No main camera found!");
    }
    
    void Update()
    {
        // Check for any input
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.touches.Length > 0)
        {
            Debug.Log("📱 Input detected!");
        }
        
        // Check mouse click
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("🖱️ Mouse button down detected!");
            
            // Skip if touching UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("🚫 Touch blocked by UI");
                return;
            }
                
            Vector2 screenPos = Input.mousePosition;
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
            Debug.Log($"📱 Touch at screen: {screenPos}, world: {worldPos}");
            
            HandleTouch(worldPos);
        }
        
        // Also check for touch input (mobile)
        if (Input.touches.Length > 0)
        {
            Touch touch = Input.touches[0];
            if (touch.phase == TouchPhase.Began)
            {
                Debug.Log("👆 Touch began!");
                
                Vector2 screenPos = touch.position;
                Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
                Debug.Log($"📱 Touch at screen: {screenPos}, world: {worldPos}");
                
                HandleTouch(worldPos);
            }
        }
    }
    
    private void HandleTouch(Vector2 worldPos)
    {
        // Use RaycastAll to get all colliders at this position
        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero);
        Debug.Log($"🎯 Found {hits.Length} colliders");
        
        Tile targetTile = null;
        RPSUnit clickedUnit = null;
        
        // First pass: Look for tiles and units
        foreach (RaycastHit2D hit in hits)
        {
            Debug.Log($"🔍 Hit: {hit.collider.name} on object {hit.collider.gameObject.name}");
            
            // Check for tile
            Tile tile = hit.collider.GetComponent<Tile>();
            if (tile != null)
            {
                targetTile = tile;
                Debug.Log($"✅ Found tile at position {tile.Position}");
            }
            
            // Check for unit
            RPSUnit unit = hit.collider.GetComponent<RPSUnit>();
            if (unit != null)
            {
                clickedUnit = unit;
                Debug.Log($"🎮 Found unit at position {unit.Position}");
            }
        }
        
        // Determine which tile to use
        Vector2Int targetPosition;
        
        if (clickedUnit != null)
        {
            // If we clicked on a unit, use the unit's position
            targetPosition = clickedUnit.Position;
            Debug.Log($"🎯 Using unit position: {targetPosition}");
        }
        else if (targetTile != null)
        {
            // If we only found a tile, use the tile's position
            targetPosition = targetTile.Position;
            Debug.Log($"🎯 Using tile position: {targetPosition}");
        }
        else
        {
            Debug.Log("❌ No tile or unit found in raycast hits");
            return;
        }
        
        // Send the position to the player controller
        if (playerController != null)
        {
            playerController.OnTileTapped(targetPosition);
        }
    }
}