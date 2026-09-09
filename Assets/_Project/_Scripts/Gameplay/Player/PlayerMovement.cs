using Fusion;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : NetworkBehaviour
{

    [Header("Control")]
    [Tooltip("Can this player currently move?")]
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
    [FormerlySerializedAs("jumpForce")]
    [SerializeField] private float _jumpForce = 7f;

    [FormerlySerializedAs("groundCheck")]
    [SerializeField] private Transform _groundCheck;

    [FormerlySerializedAs("groundCheckRadius")]
    [SerializeField] private float _groundCheckRadius = 0.2f;

    [FormerlySerializedAs("groundLayer")]
    [SerializeField] private LayerMask _groundLayer;

    [Header("Physics")]
    [FormerlySerializedAs("externalVelocityDamping")]
    [SerializeField] private float _externalVelocityDamping = 10f;

    [FormerlySerializedAs("fallMultiplier")]
    [SerializeField] private float _fallMultiplier = 2.5f;


[Networked]
private NetworkButtons PreviousButtons { get; set; }
    private Rigidbody _rb;

    // Camera-relative movement converted to world-space.
    private Vector3 _moveDirection;

    private bool _isGrounded;

    private Vector3 _externalVelocity;
    private Vector3 _currentMoveVelocity;
    private readonly Collider[] _groundHits = new Collider[8];

    public bool CanMove => _canMove;
    public bool IsGrounded => _isGrounded;
    public bool IsBusy { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }


   public override void FixedUpdateNetwork()
{
    if (!HasStateAuthority)
        return;

    // Ground state belongs to the simulation.
    
    CheckGround();

    if (GetInput(out NetworkInputData input))
    {
        _moveDirection = new Vector3(
            input.MoveDirection.x,
            0f,
            input.MoveDirection.y
        );

        // Detect a new jump press.
        if (input.Buttons.WasPressed(
                PreviousButtons,
                EInputButton.Jump) &&
            _isGrounded)
        {
            Jump();
        }

        PreviousButtons = input.Buttons;
    }
    else
    {
        _moveDirection = Vector3.zero;
    }

    Move();
    Rotate();
    ApplyBetterGravity();

    _externalVelocity = Vector3.MoveTowards(
        _externalVelocity,
        Vector3.zero,
        _externalVelocityDamping * Runner.DeltaTime
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
            Vector3 movement =
                Vector3.ClampMagnitude(_moveDirection, 1f);

            Vector3 targetVelocity =
                movement * _moveSpeed;

            float speedChange =
                movement.sqrMagnitude > 0.01f
                    ? _acceleration
                    : _deceleration;

            _currentMoveVelocity =
                Vector3.MoveTowards(
                    _currentMoveVelocity,
                    targetVelocity,
                    speedChange * Runner.DeltaTime
                );
        }

        Vector3 finalVelocity =
            _currentMoveVelocity + _externalVelocity;

        finalVelocity.y = _rb.linearVelocity.y;

        _rb.linearVelocity = finalVelocity;
    }

    private void ApplyBetterGravity()
    {
        if (_rb.linearVelocity.y < 0f)
        {
            _rb.AddForce(
                Physics.gravity *
                (_fallMultiplier - 1f),
                ForceMode.Acceleration
            );
        }
    }

private void CheckGround()
{
    if (_groundCheck == null)
    {
        _isGrounded = false;
        return;
    }

    if (Runner == null || !Runner.IsRunning)
    {
        _isGrounded = false;
        return;
    }

    var physicsScene = Runner.GetPhysicsScene();

    int hitCount = physicsScene.OverlapSphere(
        _groundCheck.position,
        _groundCheckRadius,
        _groundHits,
        _groundLayer,
        QueryTriggerInteraction.Ignore
    );

    _isGrounded = hitCount > 0;
}
    private void Jump()
    {
        Vector3 velocity = _rb.linearVelocity;

        velocity.y = 0f;

        _rb.linearVelocity = velocity;

        _rb.AddForce(
            Vector3.up * _jumpForce,
            ForceMode.Impulse
        );
    }

    public void SetCanMove(bool canMove)
    {
        _canMove = canMove;

        if (!canMove)
        {
            _moveDirection = Vector3.zero;
            _currentMoveVelocity = Vector3.zero;
        }
    }

    private void Rotate()
    {
        if (!_canMove || IsBusy)
            return;

        Vector3 movement = _currentMoveVelocity;

        movement.y = 0f;

        if (movement.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(movement);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                _rotationSpeed * Runner.DeltaTime
            );
    }
    private void OnDrawGizmosSelected()
{
    if (_groundCheck == null)
        return;

    Gizmos.color = Color.yellow;

    Gizmos.DrawWireSphere(
        _groundCheck.position,
        _groundCheckRadius
    );
}
}