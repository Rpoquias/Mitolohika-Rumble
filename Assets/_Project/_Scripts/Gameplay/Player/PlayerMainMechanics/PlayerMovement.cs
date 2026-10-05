using Fusion;
using UnityEngine;
using UnityEngine.Serialization;
using System;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerMovement : NetworkBehaviour
{
public event Action OnHitReceived;

    [Header("Control")]
    [SerializeField] private bool _initialCanMove = true;

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
    private TickTimer StunTimer { get; set; }

    [Networked]
    private Vector3 CurrentMoveVelocity { get; set; }

    [Networked]
    private Vector3 ExternalVelocity { get; set; }

    [Networked]
    public NetworkBool IsBusy { get; private set; }

    [Networked]
    public NetworkBool CanMove { get; private set; }

    private Rigidbody _rb;
    private PlayerInputHandler _inputHandler;

    private Vector3 _moveDirection;
    private bool _isGrounded;

    private readonly Collider[] _groundHits =
        new Collider[8];

    public bool IsGrounded => _isGrounded;

    public bool IsStunned =>
        !StunTimer.ExpiredOrNotRunning(Runner);

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public override void Spawned()
    {
        _inputHandler =
            GetComponent<PlayerInputHandler>();

        if (_inputHandler == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerMovement)}] " +
                $"{name} requires a {nameof(PlayerInputHandler)}."
            );
        }

        if (HasStateAuthority)
        {
            CanMove = _initialCanMove;
            IsBusy = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        CheckGround();

        bool gotInput =
            GetInput(out NetworkInputData input);

        if (gotInput)
        {
            _moveDirection =
                new Vector3(
                    input.MoveDirection.x,
                    0f,
                    input.MoveDirection.y
                );

            if (_inputHandler != null &&
                _inputHandler.JumpPressed &&
                _isGrounded &&
                !IsStunned)
            {
                Jump();
            }
        }
        else
        {
            _moveDirection = Vector3.zero;
        }

        Move();
        Rotate();
        ApplyBetterGravity();

        ExternalVelocity =
            Vector3.MoveTowards(
                ExternalVelocity,
                Vector3.zero,
                _externalVelocityDamping *
                Runner.DeltaTime
            );
    }

    public void SetBusy(bool busy)
    {
        IsBusy = busy;

        if (busy)
        {
            CurrentMoveVelocity = Vector3.zero;
        }
    }

 public void AddExternalForce(Vector3 force)
{
    ExternalVelocity += force;
    OnHitReceived?.Invoke();
}

public void ApplyStun(float duration)
{
    if (!HasStateAuthority)
        return;

    StunTimer =
        TickTimer.CreateFromSeconds(
            Runner,
            duration
        );

    _moveDirection = Vector3.zero;
    CurrentMoveVelocity = Vector3.zero;

    OnHitReceived?.Invoke();
}

    public void SetCanMove(bool canMove)
    {
        CanMove = canMove;

        if (!canMove)
        {
            _moveDirection = Vector3.zero;
            CurrentMoveVelocity = Vector3.zero;
        }
    }

    private void Move()
    {
        if (IsBusy ||
            IsStunned ||
            !CanMove)
        {
            CurrentMoveVelocity = Vector3.zero;
        }
        else
        {
            Vector3 movement =
                Vector3.ClampMagnitude(
                    _moveDirection,
                    1f
                );

            Vector3 targetVelocity =
                movement * _moveSpeed;

            float speedChange =
                movement.sqrMagnitude > 0.01f
                    ? _acceleration
                    : _deceleration;

            CurrentMoveVelocity =
                Vector3.MoveTowards(
                    CurrentMoveVelocity,
                    targetVelocity,
                    speedChange *
                    Runner.DeltaTime
                );
        }

        Vector3 finalVelocity =
            CurrentMoveVelocity +
            ExternalVelocity;

        finalVelocity.y =
            _rb.linearVelocity.y;

        _rb.linearVelocity =
            finalVelocity;
    }

    private void Rotate()
    {
        if (!CanMove ||
            IsBusy ||
            IsStunned)
        {
            return;
        }

        Vector3 movement =
            CurrentMoveVelocity;

        movement.y = 0f;

        if (movement.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(movement);

        Quaternion newRotation =
            Quaternion.Slerp(
                _rb.rotation,
                targetRotation,
                _rotationSpeed *
                Runner.DeltaTime
            );

        _rb.MoveRotation(newRotation);
    }

    private void Jump()
    {
        Vector3 velocity =
            _rb.linearVelocity;

        velocity.y = 0f;

        _rb.linearVelocity =
            velocity;

        _rb.AddForce(
            Vector3.up * _jumpForce,
            ForceMode.Impulse
        );
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

        if (Runner == null ||
            !Runner.IsRunning)
        {
            _isGrounded = false;
            return;
        }

        var physicsScene =
            Runner.GetPhysicsScene();

        int hitCount =
            physicsScene.OverlapSphere(
                _groundCheck.position,
                _groundCheckRadius,
                _groundHits,
                _groundLayer,
                QueryTriggerInteraction.Ignore
            );

        _isGrounded =
            hitCount > 0;
    }

#if UNITY_EDITOR

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

#endif
}