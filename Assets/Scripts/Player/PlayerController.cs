using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    private RPSUnit selectedUnit;
    private Outline activeOutline;

    public int columns = 7;
    public int rows = 6;

    void Update()
    {
        if (selectedUnit == null) return;

        Vector2Int direction = Vector2Int.zero;

        // תיקון כיוונים - כדי ש"למעלה" זה באמת למעלה בלוח
        if (Input.GetKeyDown(KeyCode.UpArrow)) direction = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.DownArrow)) direction = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) direction = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow)) direction = Vector2Int.right;

        if (direction != Vector2Int.zero)
        {
            TryMoveUnit(selectedUnit, direction);
        }
    }

    public void SelectUnit(RPSUnit unit)
    {
        if (!unit.IsPlayerControlled) return;

        selectedUnit = unit;
        Debug.Log($"🎯 Selected unit at [col {unit.Position.x}, row {unit.Position.y}]");

        // הסרת סימון קודם
        if (activeOutline != null)
            Destroy(activeOutline);

        // הוספת סימון חדש
        Outline outline = unit.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.cyan;
        outline.effectDistance = new Vector2(5f, 5f);
        activeOutline = outline;
    }

    void TryMoveUnit(RPSUnit unit, Vector2Int dir)
    {
        Vector2Int target = unit.Position + dir;

        // גבולות הלוח
        if (target.x < 0 || target.x >= columns || target.y < 0 || target.y >= rows)
        {
            Debug.Log("⛔ Move is out of board bounds");
            return;
        }

        // בדיקה אם יש מישהו ביעד
        foreach (var other in FindObjectsOfType<RPSUnit>())
        {
            if (other == unit) continue;
            if (other.Position == target)
            {
                if (other.IsPlayerControlled)
                {
                    Debug.Log("🚫 Cell is already occupied by another player unit");
                    return;
                }
                else
                {
                    Debug.Log("⚔️ Enemy encountered – initiating battle!");
                    Destroy(other.gameObject); // בהמשך תחליף לקרב אמיתי
                    break;
                }
            }
        }

        // העברה ויזואלית
        Transform targetTile = GetTileTransform(target);
        if (targetTile != null)
        {
            unit.transform.SetParent(targetTile, false);
            RectTransform rt = unit.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            unit.Position = target;

            Debug.Log($"✅ Unit moved to [col {target.x}, row {target.y}]");
        }
    }

    Transform GetTileTransform(Vector2Int pos)
    {
        int index = pos.y * columns + pos.x;
        Transform board = GameObject.Find("Board").transform;
        if (index >= board.childCount) return null;
        return board.GetChild(index);
    }
}
