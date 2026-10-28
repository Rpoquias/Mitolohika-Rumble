using UnityEngine;

public class KapiPlayerInputHandler : MonoBehaviour
{
    public Vector2 MoveDirection { get; private set; }
    private bool _jumpPressed;
    private bool _bumpPressed;
    private bool _abilityPressed;

    public bool JumpPressed => _jumpPressed;
    public bool BumpPressed => _bumpPressed;
    public bool AbilityPressed => _abilityPressed;

    public void SetMoveDirection(Vector2 direction)
    {
        MoveDirection = Vector2.ClampMagnitude(direction, 1f);
    }

    public void SetJumpPressed() => _jumpPressed = true;
    public void SetBumpPressed() => _bumpPressed = true;
    public void SetAbilityPressed() => _abilityPressed = true;

    public bool ConsumeJumpPressed()
    {
        bool pressed = _jumpPressed;
        _jumpPressed = false;
        return pressed;
    }

    public bool ConsumeBumpPressed()
    {
        bool pressed = _bumpPressed;
        _bumpPressed = false;
        return pressed;
    }

    public bool ConsumeAbilityPressed()
    {
        bool pressed = _abilityPressed;
        _abilityPressed = false;
        return pressed;
    }

    public void Clear()
    {
        MoveDirection = Vector2.zero;
        _jumpPressed = false;
        _bumpPressed = false;
        _abilityPressed = false;
    }
}