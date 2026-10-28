using UnityEngine;
using UnityEngine.UI;

public class KapiMobileInputProvider : MonoBehaviour
{
    public static KapiMobileInputProvider Instance { get; private set; }

    [Header("Movement")]
    [SerializeField] private KapiFixedJoystick movementJoystick;
    [Tooltip("Assign the new Empty Parent Wrapper (Joystick Root) here!")]

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
    private bool inputEnabled = true;
    private Button _jumpUiButton;
    private Button _bumpUiButton;
    private Button _abilityUiButton;

    public Vector2 MoveInput
    {
        get
        {
            if (!inputEnabled)
                return Vector2.zero;

            if (movementJoystick == null)
                return Vector2.zero;

            return new Vector2(
                movementJoystick.Horizontal,
                movementJoystick.Vertical
            );
        }
    }

    private void Awake()
    {
        Instance = this;

        // The pp scene may not have these references assigned in the Inspector.
        // Resolve the copied joystick and buttons locally so the Kapi input path
        // can still receive touch input without changing shared scripts.
        if (movementJoystick == null)
        {
            movementJoystick = GetComponentInChildren<KapiFixedJoystick>(true);
            if (movementJoystick == null)
                movementJoystick = FindAnyObjectByType<KapiFixedJoystick>(FindObjectsInactive.Include);
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            string buttonName = buttons[i].name.ToLowerInvariant();
            if (jumpButton == null && buttonName.Contains("jump"))
            {
                jumpButton = buttons[i].GetComponent<RectTransform>();
                _jumpUiButton = buttons[i];
            }
            else if (bumpButton == null && buttonName.Contains("bump"))
            {
                bumpButton = buttons[i].GetComponent<RectTransform>();
                _bumpUiButton = buttons[i];
            }
            else if (abilityButton == null && buttonName.Contains("ability"))
            {
                abilityButton = buttons[i].GetComponent<RectTransform>();
                _abilityUiButton = buttons[i];
            }
        }

        if (_jumpUiButton == null && jumpButton != null)
            _jumpUiButton = jumpButton.GetComponent<Button>();
        if (_bumpUiButton == null && bumpButton != null)
            _bumpUiButton = bumpButton.GetComponent<Button>();
        if (_abilityUiButton == null && abilityButton != null)
            _abilityUiButton = abilityButton.GetComponent<Button>();

        if (_jumpUiButton != null && !HasPersistentCallback(_jumpUiButton, nameof(PressJump)))
            _jumpUiButton.onClick.AddListener(PressJump);
        if (_bumpUiButton != null && !HasPersistentCallback(_bumpUiButton, nameof(PressBump)))
            _bumpUiButton.onClick.AddListener(PressBump);
        if (_abilityUiButton != null && !HasPersistentCallback(_abilityUiButton, nameof(PressAbility)))
            _abilityUiButton.onClick.AddListener(PressAbility);

        if (jumpButton != null)
        {
            _defaultJumpButtonScale = jumpButton.localScale;
        }

        if (bumpButton != null)
        {
            _defaultBumpButtonScale = bumpButton.localScale;
        }

        if (abilityButton != null)
        {
            _defaultAbilityButtonScale = abilityButton.localScale;
        }
    }

    private void Update()
    {
        if (KapiMobileInputProvider.Instance == null)
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
        if (_jumpUiButton != null)
            _jumpUiButton.onClick.RemoveListener(PressJump);
        if (_bumpUiButton != null)
            _bumpUiButton.onClick.RemoveListener(PressBump);
        if (_abilityUiButton != null)
            _abilityUiButton.onClick.RemoveListener(PressAbility);

        if (Instance == this)
            Instance = null;
    }

    private bool HasPersistentCallback(Button button, string methodName)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == this &&
                button.onClick.GetPersistentMethodName(i) == methodName)
                return true;
        }

        return false;
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!enabled)
        {
            _jumpPressed = false;
            _bumpPressed = false;
            _abilityPressed = false;
        }
    }

    // --------------------------------------------------------------------
    // Button presses
    // --------------------------------------------------------------------

    public void PressJump()
    {
        if (!inputEnabled)
            return;

        _jumpPressed = true;
    }

    public void PressBump()
    {
        if (!inputEnabled)
            return;

        _bumpPressed = true;
    }

    public void PressAbility()
    {
        if (!inputEnabled)
            return;

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
            jumpButton.localScale = _defaultJumpButtonScale * scale;
        }

        if (bumpButton != null)
        {
            bumpButton.localScale = _defaultBumpButtonScale * scale;
        }

        if (abilityButton != null)
        {
            abilityButton.localScale = _defaultAbilityButtonScale * scale;
        }
    }

    public void ApplyJoystickScale(float scale)
    {
        if (_joystickTransform == null)
            return;

        // Automatically scales the wrapper parent if assigned, keeping internal touch math aligned
        _joystickTransform.localScale = _defaultJoystickScale * scale;
    }
}
