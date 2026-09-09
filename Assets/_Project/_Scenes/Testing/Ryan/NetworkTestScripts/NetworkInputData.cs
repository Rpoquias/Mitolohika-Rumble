using Fusion;
using UnityEngine;

public enum EInputButton
{
    Jump,
    Bump
}

public struct NetworkInputData : INetworkInput
{
    public Vector2 MoveDirection;
    public NetworkButtons Buttons;
}