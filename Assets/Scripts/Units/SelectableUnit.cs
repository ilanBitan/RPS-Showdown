using UnityEngine;
using UnityEngine.EventSystems;

public class SelectableUnit : MonoBehaviour, IPointerClickHandler
{
    public System.Action onSetupClick;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (PlayerController.gameEnded)
            return;

        // If there is setupClick – this is an override for regular click
        if (onSetupClick != null)
        {
            onSetupClick.Invoke();
            return;
        }

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
