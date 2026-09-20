using UnityEngine;

public class MobileInputProvider : MonoBehaviour
{
    public static MobileInputProvider Instance { get; private set; }

   [SerializeField] private FixedJoystick movementJoystick;
   

   [Header("Action Buttons")]
[SerializeField] private RectTransform jumpButton;
[SerializeField] private RectTransform bumpButton;

private Vector3 _defaultJumpButtonScale;
private Vector3 _defaultBumpButtonScale;

private RectTransform _joystickTransform;
private Vector3 _defaultJoystickScale;

    private bool _jumpPressed;
    private bool _bumpPressed;

    public Vector2 MoveInput =>
        new Vector2(
            movementJoystick.Horizontal,
            movementJoystick.Vertical
        );

private void Awake()
{
    Instance = this;

    _joystickTransform =
        movementJoystick.GetComponent<RectTransform>();

    _defaultJoystickScale =
        _joystickTransform.localScale;

    _defaultJumpButtonScale =
        jumpButton.localScale;

    _defaultBumpButtonScale =
        bumpButton.localScale;
}
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
}
public void ApplyJoystickScale(float scale)
{
    if (_joystickTransform == null)
        return;

    _joystickTransform.localScale =
        _defaultJoystickScale * scale;
}
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PressJump()
    {
        _jumpPressed = true;
    }

    public void PressBump()
    {
        _bumpPressed = true;
    }

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
}