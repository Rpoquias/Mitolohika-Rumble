using Fusion;
using UnityEngine;

public enum EInputButton
{
    Jump,
    Bump,
    Ability
}
public struct NetworkInputData : INetworkInput
{
    public Vector2 MoveDirection;
    public NetworkButtons Buttons;
}