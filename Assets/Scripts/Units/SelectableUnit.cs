using UnityEngine;
using UnityEngine.EventSystems;

public class SelectableUnit : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        RPSUnit unit = GetComponent<RPSUnit>();
        if (unit == null) return;

        foreach (var controller in FindObjectsOfType<PlayerController>())
        {
            if (controller.myPlayerId == unit.playerId &&
                TurnManager.Instance.IsPlayerTurn(unit.playerId))
            {
                controller.SelectUnit(unit);
                break;
            }
        }
    }
}
