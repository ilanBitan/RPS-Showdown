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
            
            // Use RaycastAll to get all colliders at this position
            RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero);
            Debug.Log($"🎯 Found {hits.Length} colliders");
            
            foreach (RaycastHit2D hit in hits)
            {
                Debug.Log($"🔍 Hit: {hit.collider.name} on object {hit.collider.gameObject.name}");
                
                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile != null && playerController != null)
                {
                    Debug.Log($"✅ Found tile at position {tile.Position}");
                    playerController.OnTileTapped(tile.Position);
                    return; // Found tile, stop searching
                }
            }
            
            Debug.Log("❌ No tile found in raycast hits");
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
                
                RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero);
                Debug.Log($"🎯 Found {hits.Length} colliders");
                
                foreach (RaycastHit2D hit in hits)
                {
                    Debug.Log($"🔍 Hit: {hit.collider.name} on object {hit.collider.gameObject.name}");
                    
                    Tile tile = hit.collider.GetComponent<Tile>();
                    if (tile != null && playerController != null)
                    {
                        Debug.Log($"✅ Found tile at position {tile.Position}");
                        playerController.OnTileTapped(tile.Position);
                        return;
                    }
                }
                
                Debug.Log("❌ No tile found in raycast hits");
            }
        }
    }
}