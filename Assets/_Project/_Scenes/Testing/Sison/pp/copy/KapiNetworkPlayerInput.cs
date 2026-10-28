using UnityEngine;
using UnityEngine.InputSystem;

public class KapiNetworkPlayerInput : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference bumpAction;
    [SerializeField] private InputActionReference abilityAction;

    private KapiPlayerInputHandler _inputHandler;
    private Transform _cameraTransform;

    private void Awake()
    {
        _inputHandler = GetComponent<KapiPlayerInputHandler>();
        FindCamera();
    }

    private void OnEnable()
    {
        Enable(moveAction);
        Enable(jumpAction);
        Enable(bumpAction);
        Enable(abilityAction);
    }

    private void OnDisable()
    {
        Disable(moveAction);
        Disable(jumpAction);
        Disable(bumpAction);
        Disable(abilityAction);
        if (_inputHandler != null)
            _inputHandler.Clear();
    }

    private void Update()
    {
        if (_inputHandler == null)
            _inputHandler = GetComponent<KapiPlayerInputHandler>();
        if (_inputHandler == null)
            return;

        if (_cameraTransform == null)
            FindCamera();

        Vector2 rawInput = Read(moveAction);
        KapiMobileInputProvider mobile = KapiMobileInputProvider.Instance;
        if (mobile != null && mobile.MoveInput.sqrMagnitude > 0.001f)
            rawInput = mobile.MoveInput;

        Vector2 worldInput = rawInput;
        if (_cameraTransform != null)
        {
            Vector3 forward = _cameraTransform.forward;
            Vector3 right = _cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            Vector3 direction = Vector3.ClampMagnitude(
                forward * rawInput.y + right * rawInput.x, 1f);
            worldInput = new Vector2(direction.x, direction.z);
        }
        _inputHandler.SetMoveDirection(worldInput);

        bool jump = WasPressed(jumpAction) || (mobile != null && mobile.ConsumeJumpPressed());
        bool bump = WasPressed(bumpAction) || (mobile != null && mobile.ConsumeBumpPressed());
        bool ability = WasPressed(abilityAction) || (mobile != null && mobile.ConsumeAbilityPressed());

        if (jump) _inputHandler.SetJumpPressed();
        if (bump) _inputHandler.SetBumpPressed();
        if (ability) _inputHandler.SetAbilityPressed();
    }

    private void FindCamera()
    {
        KapiThirdPersonCamera camera = FindAnyObjectByType<KapiThirdPersonCamera>();
        if (camera != null)
            _cameraTransform = camera.CameraTransform;
        else if (Camera.main != null)
            _cameraTransform = Camera.main.transform;
    }

    private static Vector2 Read(InputActionReference action)
    {
        return action != null && action.action != null && action.action.enabled
            ? action.action.ReadValue<Vector2>()
            : Vector2.zero;
    }

    private static bool WasPressed(InputActionReference action)
    {
        return action != null && action.action != null &&
               action.action.enabled && action.action.WasPressedThisFrame();
    }

    private static void Enable(InputActionReference action)
    {
        if (action != null && action.action != null)
            action.action.Enable();
    }

    private static void Disable(InputActionReference action)
    {
        if (action != null && action.action != null)
            action.action.Disable();
    }
}