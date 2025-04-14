using UnityEngine;

public abstract class Unit : MonoBehaviour
{
    public Vector2Int Position;
    public bool IsPlayerControlled;

    public abstract string UnitType { get; }
    public abstract bool Beats(Unit other);
}
