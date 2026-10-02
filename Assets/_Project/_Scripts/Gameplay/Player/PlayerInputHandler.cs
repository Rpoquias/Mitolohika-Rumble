using Fusion;
using UnityEngine;

public class PlayerInputHandler : NetworkBehaviour
{
    [Networked]
    private NetworkButtons ButtonsPrevious { get; set; }

    private bool _jumpPressed;
    private bool _bumpPressed;
    private bool _abilityPressed;

    public bool JumpPressed => _jumpPressed;
    public bool BumpPressed => _bumpPressed;
    public bool AbilityPressed => _abilityPressed;

    public override void FixedUpdateNetwork()
    {
        // These are only true for the current simulation tick.
        _jumpPressed = false;
        _bumpPressed = false;
        _abilityPressed = false;

        if (!GetInput(out NetworkInputData input))
            return;

        _jumpPressed =
            input.Buttons.WasPressed(
                ButtonsPrevious,
                EInputButton.Jump
            );

        _bumpPressed =
            input.Buttons.WasPressed(
                ButtonsPrevious,
                EInputButton.Bump
            );

        _abilityPressed =
            input.Buttons.WasPressed(
                ButtonsPrevious,
                EInputButton.Ability
            );

        // Save the current buttons for the next tick.
        ButtonsPrevious = input.Buttons;
    }
}