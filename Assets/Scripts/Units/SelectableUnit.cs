using UnityEngine;
using UnityEngine.EventSystems;

public class SelectableUnit : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        PlayerController controller = FindObjectOfType<PlayerController>();
        RPSUnit unit = GetComponent<RPSUnit>();

        if (controller != null && unit != null)
        {
            controller.SelectUnit(unit);
        }
    }
}
