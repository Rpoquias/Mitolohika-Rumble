using System;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(KapiPlayerInputHandler))]
public class KapiPlayerMovement : MonoBehaviour
{
    public event Action OnHitReceived;
    public event Action OnJumped;
    public event Action OnLanded;

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
    [SerializeField] private float _coyoteTime = 0.12f;
    [SerializeField] private float _jumpGuardTime = 0.1f;

    [Header("Physics")]
    [FormerlySerializedAs("externalVelocityDamping")]
    [SerializeField] private float _externalVelocityDamping = 10f;
    [FormerlySerializedAs("fallMultiplier")]
    [SerializeField] private float _fallMultiplier = 2.5f;

    [Header("Global Event Speed Transition")]
    [SerializeField, Min(0.1f)] private float _eventSpeedTransitionRate = 0.6f;

    private Rigidbody _rb;
    private KapiPlayerInputHandler _inputHandler;
    private KapiPlayerControlEffects _controlEffects;
    private KapiPlayerAudio _audio;
    private KapiPlayerStatusEffects _statusEffects;

    private Vector3 _moveDirection;
    private Vector3 _moveVelocity;
    private Vector3 _externalVelocity;
    private Vector3 _blackHoleVelocity;
    private Vector3 _blackHoleCenter;
    private float _blackHoleAcceleration;
    private float _blackHoleMaxSpeed;
    private float _globalEventSpeedMultiplier = 1f;
    private float _targetEventSpeedMultiplier = 1f;
    private float _collectibleSpeedBoostMultiplier = 1f;
    private float _movementSpeedMultiplier = 1f;
    private float _carrierSpeedMultiplier = 1f;
    private float _stunUntil;
    private float _snareRootUntil;
    private float _hitstopUntil;
    private float _lastGroundedTime = -10000f;
    private float _lastJumpTime = -10000f;
    private float _pendingLaunchY;
    private float _airTime;
    private int _collectibleSpeedBoostStacks;
    private bool _blackHoleActive;
    private bool _isGrounded;
    private bool _wasGrounded;
    private bool _hasInitializedGroundState;

    public bool CanJump { get; private set; } = true;
    public bool IsBusy { get; private set; }
    public bool CanMove { get; private set; }
    public int HitSequence { get; private set; }
    public HitType LastHitType { get; private set; }
    public bool IsGrounded => _isGrounded;
    public bool IsMoving => _moveVelocity.sqrMagnitude > 0.1f;
    public bool IsInHitstop => Time.time < _hitstopUntil;
    public bool IsSnareRooted => Time.time < _snareRootUntil;
    public bool IsStunned => Time.time < _stunUntil;

    private readonly Collider[] _groundHits = new Collider[8];

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _inputHandler = GetComponent<KapiPlayerInputHandler>();
        _controlEffects = GetComponent<KapiPlayerControlEffects>();
        _audio = GetComponent<KapiPlayerAudio>();
        _statusEffects = GetComponent<KapiPlayerStatusEffects>();
        CanMove = _initialCanMove;
        CanJump = true;

        if (_groundCheck == null)
        {
            var check = transform.Find("GroundCheck");
            if (check != null)
                _groundCheck = check;
        }
    }

    private void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;
        CheckGround();

        _globalEventSpeedMultiplier = Mathf.MoveTowards(
            _globalEventSpeedMultiplier,
            _targetEventSpeedMultiplier,
            _eventSpeedTransitionRate * deltaTime
        );

        SimulateBlackHolePull(deltaTime);

        if (_pendingLaunchY > 0f)
        {
            Vector3 velocity = _rb.linearVelocity;
            velocity.y = _pendingLaunchY;
            _rb.linearVelocity = velocity;
            _pendingLaunchY = 0f;
            _lastJumpTime = Time.time;
        }

        if (IsInHitstop)
        {
            _rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector2 input = _inputHandler != null
            ? _inputHandler.MoveDirection
            : Vector2.zero;

        if (_controlEffects != null && _controlEffects.IsInputReversed)
            input = -input;

        _moveDirection = new Vector3(input.x, 0f, input.y);

        bool inCoyoteWindow =
            _lastGroundedTime > _lastJumpTime &&
            Time.time - _lastGroundedTime <= _coyoteTime;

        bool canJumpNow =
            (_isGrounded || inCoyoteWindow) &&
            Time.time - _lastJumpTime > _jumpGuardTime;

        if (_inputHandler != null &&
            _inputHandler.ConsumeJumpPressed() &&
            CanJump &&
            canJumpNow &&
            !IsStunned &&
            !IsSnareRooted)
        {
            Jump();
        }

        Move(deltaTime);
        Rotate(deltaTime);
        ApplyBetterGravity();
        _externalVelocity = Vector3.MoveTowards(
            _externalVelocity,
            Vector3.zero,
            _externalVelocityDamping * deltaTime
        );
    }

    private void SimulateBlackHolePull(float deltaTime)
    {
        if (!_blackHoleActive)
        {
            _blackHoleVelocity = Vector3.MoveTowards(
                _blackHoleVelocity, Vector3.zero,
                _externalVelocityDamping * deltaTime);
            return;
        }

        Vector3 direction = _blackHoleCenter - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            _blackHoleVelocity = Vector3.zero;
            return;
        }

        direction.Normalize();
        _blackHoleVelocity = Vector3.MoveTowards(
            _blackHoleVelocity,
            direction * _blackHoleMaxSpeed,
            _blackHoleAcceleration * deltaTime
        );
    }

    public void StartBlackHolePull(Vector3 center, float acceleration, float maxSpeed)
    {
        _blackHoleCenter = center;
        _blackHoleAcceleration = Mathf.Max(0f, acceleration);
        _blackHoleMaxSpeed = Mathf.Max(0f, maxSpeed);
        _blackHoleActive = true;
    }

    public void ClearBlackHolePull()
    {
        _blackHoleActive = false;
        _blackHoleVelocity = Vector3.zero;
    }

    public void SetBusy(bool busy)
    {
        IsBusy = busy;
        if (busy)
            _moveVelocity = Vector3.zero;
    }

    public void SetCanMove(bool canMove)
    {
        CanMove = canMove;
        if (!canMove)
        {
            _moveDirection = Vector3.zero;
            _moveVelocity = Vector3.zero;
        }
    }

    public void SetCanJump(bool canJump) => CanJump = canJump;
    public void SetMovementSpeedMultiplier(float multiplier) =>
        _movementSpeedMultiplier = Mathf.Max(0f, multiplier);
    public void SetGlobalEventSpeedMultiplier(float multiplier) =>
        _targetEventSpeedMultiplier = Mathf.Max(0f, multiplier);
    public void SetCarrierSpeedMultiplier(float multiplier) =>
        _carrierSpeedMultiplier = Mathf.Max(0f, multiplier);

    public void AddExternalForce(Vector3 force)
    {
        _externalVelocity += force;
        RegisterHit(HitType.Knockback);
    }

    public void ApplyHitstop(float seconds)
    {
        if (seconds > 0f)
            _hitstopUntil = Mathf.Max(_hitstopUntil, Time.time + seconds);
    }

    public void ApplyKnockback(Vector3 velocity, float hitstopSeconds = 0f) =>
        ApplyKnockback(velocity, hitstopSeconds, HitType.PowerfulKick);

    public void ApplyKnockback(Vector3 velocity, float hitstopSeconds, HitType hitType)
    {
        _externalVelocity += new Vector3(velocity.x, 0f, velocity.z);
        if (velocity.y > 0f)
            _pendingLaunchY = velocity.y;
        ApplyHitstop(hitstopSeconds);
        RegisterHit(hitType);
    }

    public void ApplyStun(float duration)
    {
        _stunUntil = Mathf.Max(_stunUntil, Time.time + Mathf.Max(0f, duration));
        _moveDirection = Vector3.zero;
        _moveVelocity = Vector3.zero;
        RegisterHit(HitType.Stun);
    }

    public void ApplySnareRoot(float duration)
    {
        if (duration <= 0f)
            return;
        _snareRootUntil = Mathf.Max(_snareRootUntil, Time.time + duration);
        _moveDirection = Vector3.zero;
        _moveVelocity = Vector3.zero;
    }

    private void RegisterHit(HitType type)
    {
        LastHitType = type;
        HitSequence++;
        OnHitReceived?.Invoke();
    }

    private void Move(float deltaTime)
    {
        if (IsBusy || IsStunned || IsSnareRooted || !CanMove)
        {
            _moveVelocity = Vector3.zero;
        }
        else
        {
            Vector3 movement = Vector3.ClampMagnitude(_moveDirection, 1f);
            float smokeMultiplier =
                _statusEffects != null && _statusEffects.IsInSmoke
                    ? _statusEffects.SmokeSpeedMultiplier
                    : 1f;
            float speedMultiplier =
                _movementSpeedMultiplier *
                _carrierSpeedMultiplier *
                _collectibleSpeedBoostMultiplier *
                _globalEventSpeedMultiplier *
                smokeMultiplier;
            Vector3 targetVelocity = movement * _moveSpeed * speedMultiplier;
            float speedChange = movement.sqrMagnitude > 0.01f
                ? _acceleration
                : _deceleration;
            _moveVelocity = Vector3.MoveTowards(
                _moveVelocity, targetVelocity, speedChange * deltaTime);
        }

        Vector3 finalVelocity = _moveVelocity + _externalVelocity + _blackHoleVelocity;
        finalVelocity.y = _rb.linearVelocity.y;
        _rb.linearVelocity = finalVelocity;
    }

    private void Rotate(float deltaTime)
    {
        if (!CanMove || IsBusy || IsStunned || IsSnareRooted)
            return;

        Vector3 movement = _moveVelocity;
        movement.y = 0f;
        if (movement.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(movement);
        _rb.MoveRotation(Quaternion.Slerp(
            _rb.rotation, targetRotation, _rotationSpeed * deltaTime));
    }

    private void Jump()
    {
        _lastJumpTime = Time.time;
        Vector3 velocity = _rb.linearVelocity;
        velocity.y = 0f;
        _rb.linearVelocity = velocity;
        _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        _audio?.Play(SFXID.PlayerJump);
        OnJumped?.Invoke();
    }

    private void ApplyBetterGravity()
    {
        if (_rb.linearVelocity.y < 0f)
            _rb.AddForce(
                Physics.gravity * (_fallMultiplier - 1f),
                ForceMode.Acceleration
            );
    }

    public int GetCollectibleSpeedBoostStacks() => _collectibleSpeedBoostStacks;

    public bool TryAddCollectibleSpeedBoostStack()
    {
        if (_collectibleSpeedBoostStacks >= 5)
            return false;
        _collectibleSpeedBoostStacks++;
        UpdateCollectibleSpeedBoost(true);
        return true;
    }

    public void LoseCollectibleSpeedBoostStack()
    {
        if (_collectibleSpeedBoostStacks <= 0)
            return;
        _collectibleSpeedBoostStacks--;
        UpdateCollectibleSpeedBoost(true);
    }

    public void ResetCollectibleSpeedBoost()
    {
        _collectibleSpeedBoostStacks = 0;
        _collectibleSpeedBoostMultiplier = 1f;
        OnCollectibleSpeedBoostMultiplierChanged();
    }

    public void UpdateCollectibleSpeedBoost(bool isRelicHolder)
    {
        int stackCap = isRelicHolder ? 3 : 5;
        int effectiveStacks = Mathf.Min(_collectibleSpeedBoostStacks, stackCap);
        float multiplier = 1f + effectiveStacks * 0.1f;
        if (Mathf.Approximately(_collectibleSpeedBoostMultiplier, multiplier))
            return;
        _collectibleSpeedBoostMultiplier = multiplier;
        OnCollectibleSpeedBoostMultiplierChanged();
    }

    private void OnCollectibleSpeedBoostMultiplierChanged()
    {
        if (RelicSpeedBoostUI.Instance == null)
            return;
        int bonusPercent = Mathf.RoundToInt(
            (_collectibleSpeedBoostMultiplier - 1f) * 100f);
        RelicSpeedBoostUI.Instance.ShowBonus(bonusPercent);
    }

    private void CheckGround()
    {
        Vector3 checkPosition = _groundCheck != null
            ? _groundCheck.position
            : transform.position + Vector3.down * 0.5f;
        int hitCount = Physics.OverlapSphereNonAlloc(
            checkPosition,
            _groundCheckRadius,
            _groundHits,
            _groundLayer,
            QueryTriggerInteraction.Ignore
        );
        _isGrounded = hitCount > 0;
        if (_isGrounded)
        {
            _lastGroundedTime = Time.time;
            if (!_hasInitializedGroundState)
            {
                _wasGrounded = true;
                _hasInitializedGroundState = true;
                _airTime = 0f;
                return;
            }

            if (!_wasGrounded && _airTime > 0.15f)
            {
                _audio?.Play(SFXID.PlayerLand);
                OnLanded?.Invoke();
            }
            _airTime = 0f;
        }
        else
        {
            _airTime += Time.fixedDeltaTime;
            _hasInitializedGroundState = true;
        }
        _wasGrounded = _isGrounded;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_groundCheck == null)
            return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_groundCheck.position, _groundCheckRadius);
    }
#endif
}