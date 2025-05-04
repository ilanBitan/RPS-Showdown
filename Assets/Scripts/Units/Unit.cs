using UnityEngine;

public abstract class Unit : MonoBehaviour
{
    public Vector2Int Position;
    public bool IsPlayerControlled;
    public int playerId;

    public abstract string UnitType { get; }
    public abstract bool Beats(Unit other);

    public virtual void SetPosition(Vector2Int newPos)
    {
        Position = newPos;
    }
}