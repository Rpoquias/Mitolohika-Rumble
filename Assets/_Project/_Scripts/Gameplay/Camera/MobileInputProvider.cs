using UnityEngine;

public class MobileInputProvider : MonoBehaviour
{
    public static MobileInputProvider Instance { get; private set; }

    [Header("Movement")]
    [SerializeField] private FixedJoystick movementJoystick;

    [Header("Action Buttons")]
    [SerializeField] private RectTransform jumpButton;
    [SerializeField] private RectTransform bumpButton;
    [SerializeField] private RectTransform abilityButton;

    private Vector3 _defaultJumpButtonScale;
    private Vector3 _defaultBumpButtonScale;
    private Vector3 _defaultAbilityButtonScale;

    private RectTransform _joystickTransform;
    private Vector3 _defaultJoystickScale;

    private bool _jumpPressed;
    private bool _bumpPressed;
    private bool _abilityPressed;

    public Vector2 MoveInput =>
        new Vector2(
            movementJoystick.Horizontal,
            movementJoystick.Vertical
        );

    private void Awake()
    {
        Instance = this;

        if (movementJoystick != null)
        {
            _joystickTransform =
                movementJoystick.GetComponent<RectTransform>();

            _defaultJoystickScale =
                _joystickTransform.localScale;
        }

        if (jumpButton != null)
        {
            _defaultJumpButtonScale =
                jumpButton.localScale;
        }

        if (bumpButton != null)
        {
            _defaultBumpButtonScale =
                bumpButton.localScale;
        }

        if (abilityButton != null)
        {
            _defaultAbilityButtonScale =
                abilityButton.localScale;
        }
    }
private void Update()
{
    if (MobileInputProvider.Instance == null)
    {
        return;
    }

    if (movementJoystick == null)
    {
        return;
    }

 
}
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // --------------------------------------------------------------------
    // Button presses
    // --------------------------------------------------------------------

    public void PressJump()
    {
        _jumpPressed = true;
    }

    public void PressBump()
    {
        _bumpPressed = true;
    }

    public void PressAbility()
    {
        _abilityPressed = true;
    }

    // --------------------------------------------------------------------
    // Button consumption
    // --------------------------------------------------------------------

    public bool ConsumeJumpPressed()
    {
        if (!_jumpPressed)
            return false;

        _jumpPressed = false;
        return true;
    }

    public bool ConsumeBumpPressed()
    {
        if (!_bumpPressed)
            return false;

        _bumpPressed = false;
        return true;
    }

    public bool ConsumeAbilityPressed()
    {
        if (!_abilityPressed)
            return false;

        _abilityPressed = false;
        return true;
    }

    // --------------------------------------------------------------------
    // UI scaling
    // --------------------------------------------------------------------

    public void ApplyButtonScale(float scale)
    {
        if (jumpButton != null)
        {
            jumpButton.localScale =
                _defaultJumpButtonScale * scale;
        }

        if (bumpButton != null)
        {
            bumpButton.localScale =
                _defaultBumpButtonScale * scale;
        }

        if (abilityButton != null)
        {
            abilityButton.localScale =
                _defaultAbilityButtonScale * scale;
        }
    }

    public void ApplyJoystickScale(float scale)
    {
        if (_joystickTransform == null)
            return;

        _joystickTransform.localScale =
            _defaultJoystickScale * scale;
    }
}