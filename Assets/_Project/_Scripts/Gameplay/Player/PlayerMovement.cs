using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerMovement : MonoBehaviour
{
    [Header("Control")]
    [Tooltip("Uncheck on dummy players so they stay still but can still be knocked back.")]
    [SerializeField] private bool _canMove = true;

    [Header("Movement")]
    [FormerlySerializedAs("moveSpeed")]
    [SerializeField] private float _moveSpeed = 5f;
    [FormerlySerializedAs("acceleration")]
    [SerializeField] private float _acceleration = 20f;
    [FormerlySerializedAs("deceleration")]
    [SerializeField] private float _deceleration = 25f;
    [FormerlySerializedAs("rotationSpeed")]
    [SerializeField] private float _rotationSpeed = 10f;

    [Header("Jump")]
    [FormerlySerializedAs("jumpAction")]
    [SerializeField] private InputActionReference _jumpAction;
    [FormerlySerializedAs("jumpForce")]
    [SerializeField] private float _jumpForce = 7f;
    [FormerlySerializedAs("groundCheck")]
    [SerializeField] private Transform _groundCheck;
    [FormerlySerializedAs("groundCheckRadius")]
    [SerializeField] private float _groundCheckRadius = 0.2f;
    [FormerlySerializedAs("groundLayer")]
    [SerializeField] private LayerMask _groundLayer;

    [FormerlySerializedAs("externalVelocityDamping")]
    [SerializeField] private float _externalVelocityDamping = 10f;
    [FormerlySerializedAs("fallMultiplier")]
    [SerializeField] private float _fallMultiplier = 2.5f;

    [Header("References")]
    [FormerlySerializedAs("cameraTransform")]
    [SerializeField] private Transform _cameraTransform;

    private Rigidbody _rb;
    private Vector2 _moveInput;
    private bool _isGrounded;
    private Vector3 _externalVelocity;
    private Vector3 _currentMoveVelocity;

    public bool CanMove => _canMove;
    public bool IsGrounded => _isGrounded;
    public bool IsBusy { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        CheckGround();

        if (!_canMove || IsBusy)
            return;

        _moveInput = Keyboard.current != null
            ? new Vector2(
                Keyboard.current.aKey.isPressed ? -1 : Keyboard.current.dKey.isPressed ? 1 : 0,
                Keyboard.current.sKey.isPressed ? -1 : Keyboard.current.wKey.isPressed ? 1 : 0
            )
            : Vector2.zero;

        if (_jumpAction.action.WasPressedThisFrame() && _isGrounded)
            Jump();
    }

    private void FixedUpdate()
    {
        Move();
        Rotate();
        ApplyBetterGravity();

        _externalVelocity = Vector3.MoveTowards(
            _externalVelocity,
            Vector3.zero,
            _externalVelocityDamping * Time.fixedDeltaTime
        );
    }

    public void SetBusy(bool busy)
    {
        IsBusy = busy;

        if (busy)
            _currentMoveVelocity = Vector3.zero;
    }

    public void AddExternalForce(Vector3 force)
    {
        _externalVelocity += force;
    }

    private void Move()
    {
        if (IsBusy || !_canMove)
        {
            _currentMoveVelocity = Vector3.zero;
        }
        else
        {
            Vector3 cameraForward = _cameraTransform.forward;
            Vector3 cameraRight = _cameraTransform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;

            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 movement = cameraForward * _moveInput.y + cameraRight * _moveInput.x;
            movement = Vector3.ClampMagnitude(movement, 1f);

            Vector3 targetVelocity = movement * _moveSpeed;
            float speedChange = movement.sqrMagnitude > 0.01f
                ? _acceleration
                : _deceleration;

            _currentMoveVelocity = Vector3.MoveTowards(
                _currentMoveVelocity,
                targetVelocity,
                speedChange * Time.fixedDeltaTime
            );
        }

        Vector3 finalVelocity = _currentMoveVelocity + _externalVelocity;
        finalVelocity.y = _rb.linearVelocity.y;
        _rb.linearVelocity = finalVelocity;
    }

    private void ApplyBetterGravity()
    {
        if (_rb.linearVelocity.y < 0f)
        {
            _rb.AddForce(
                Physics.gravity * (_fallMultiplier - 1f),
                ForceMode.Acceleration
            );
        }
    }

    private void CheckGround()
    {
        _isGrounded = Physics.CheckSphere(
            _groundCheck.position,
            _groundCheckRadius,
            _groundLayer
        );
    }

    private void Jump()
    {
        Vector3 velocity = _rb.linearVelocity;
        velocity.y = 0f;
        _rb.linearVelocity = velocity;

        _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
    }

    private void Rotate()
    {
        if (!_canMove || IsBusy)
            return;

        Vector3 movement = _currentMoveVelocity;
        movement.y = 0f;

        if (movement.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(movement);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            _rotationSpeed * Time.fixedDeltaTime
        );
    }
}
