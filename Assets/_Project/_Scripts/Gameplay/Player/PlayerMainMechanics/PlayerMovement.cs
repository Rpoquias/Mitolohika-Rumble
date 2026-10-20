        using Fusion;
        using UnityEngine;
        using UnityEngine.Serialization;
        using System;

        [RequireComponent(typeof(Rigidbody))]
        [RequireComponent(typeof(PlayerInputHandler))]
        public class PlayerMovement : NetworkBehaviour
        {
        public event Action OnHitReceived;
        public event Action OnJumped;
        public event Action OnLanded;
        private bool _hasInitializedGroundState;


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

        [Tooltip("Coyote Time")]
        [SerializeField] private float _jumpGuardTime = 0.1f;

        [Networked]
        private int LastGroundedTick { get; set; }

        [Networked]
        private int LastJumpTick { get; set; }

        private float SecondsSince(int tick)
        {
            return (Runner.Tick.Raw - tick) * Runner.DeltaTime;
        }
            [Header("Physics")]
            [FormerlySerializedAs("externalVelocityDamping")]
            [SerializeField] private float _externalVelocityDamping = 10f;

            [FormerlySerializedAs("fallMultiplier")]
            [SerializeField] private float _fallMultiplier = 2.5f;
            [Header("Global Event Speed Transition")]
        [SerializeField, Min(0.1f)]
        private float _eventSpeedTransitionRate = 0.6f;

        [Networked]
        private float GlobalEventSpeedMultiplier { get; set; } = 1f;

        [Networked]
        private float TargetEventSpeedMultiplier { get; set; } = 1f;

            [Networked]
            private TickTimer StunTimer { get; set; }
            [Networked]
        public NetworkBool CanJump { get; private set; }
        [Networked]
        private TickTimer SnareRootTimer { get; set; }

        public bool IsSnareRooted =>
            Runner != null &&
            !SnareRootTimer.ExpiredOrNotRunning(Runner);
            [Networked]
            private Vector3 CurrentMoveVelocity { get; set; }

            [Networked]
            private Vector3 ExternalVelocity { get; set; }

            [Networked]
            public NetworkBool IsBusy { get; private set; }

            [Networked]
            public NetworkBool CanMove { get; private set; }


            [Networked]
        private int CollectibleSpeedBoostStacks { get; set; }
[Networked]
private Vector3 BlackHoleVelocity { get; set; }

[Networked]
private NetworkBool BlackHoleActive { get; set; }

[Networked]
private Vector3 BlackHoleCenter { get; set; }

[Networked]
private float BlackHoleAcceleration { get; set; }

[Networked]
private float BlackHoleMaxSpeed { get; set; }
        [Networked]
        private float CollectibleSpeedBoostMultiplier { get; set; } = 1f;
        [Networked]
        private float MovementSpeedMultiplier { get; set; } = 1f;
        [Networked]
        private float CarrierSpeedMultiplier { get; set; } = 1f;
        [Networked]
        public int HitSequence { get; private set; }

        [Networked]
        public HitType LastHitType { get; private set; }    private Rigidbody _rb;

        [Networked]
        public TickTimer HitstopTimer { get; private set; }

        [Networked]
        private float PendingLaunchY { get; set; }

        public bool IsInHitstop =>
            Runner != null &&
            !HitstopTimer.ExpiredOrNotRunning(Runner);
            private PlayerInputHandler _inputHandler;
            private PlayerControlEffects _controlEffects;
            private PlayerAudio _audio;

            private Vector3 _moveDirection;
            private bool _isGrounded;
            public bool IsMoving =>
            CurrentMoveVelocity.sqrMagnitude > 0.1f;
            private bool _wasGrounded;
        private float _airTime;
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
                    _controlEffects =
            GetComponent<PlayerControlEffects>();
            _audio = GetComponent<PlayerAudio>();

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
            CanJump = true;
            IsBusy = false;

            LastJumpTick = -10000;
        }
            }

        public override void FixedUpdateNetwork()
        {
            CheckGround();

            bool shouldSimulateMovement =
                HasStateAuthority || HasInputAuthority;

            if (!shouldSimulateMovement)
                return;

            if (IsInHitstop)
            {
                _rb.linearVelocity = Vector3.zero;
                return;
            }
            if (HasStateAuthority)
{
    SimulateBlackHolePull();
}
            if (HasStateAuthority)
        {
            GlobalEventSpeedMultiplier = Mathf.MoveTowards(
                GlobalEventSpeedMultiplier,
                TargetEventSpeedMultiplier,
                _eventSpeedTransitionRate * Runner.DeltaTime
            );
        }

            if (HasStateAuthority && PendingLaunchY > 0f)
            {
                Vector3 velocity = _rb.linearVelocity;
                velocity.y = PendingLaunchY;

                _rb.linearVelocity = velocity;
                PendingLaunchY = 0f;

                LastJumpTick = Runner.Tick.Raw;
            }

            bool gotInput = GetInput(out NetworkInputData input);

            if (gotInput)
            {
                Vector2 moveInput = input.MoveDirection;

                if (_controlEffects != null &&
                    _controlEffects.IsInputReversed)
                {
                    moveInput = -moveInput;
                }

                _moveDirection = new Vector3(
                    moveInput.x,
                    0f,
                    moveInput.y
                );

                bool inCoyoteWindow =
                    LastGroundedTick > LastJumpTick &&
                    SecondsSince(LastGroundedTick) <= _coyoteTime;

                bool canJumpNow =
                    (_isGrounded || inCoyoteWindow) &&
                    SecondsSince(LastJumpTick) > _jumpGuardTime;

                if (_inputHandler != null &&
                    _inputHandler.JumpPressed &&
                    CanJump &&
                    canJumpNow &&
                    !IsStunned &&
                    !IsSnareRooted)
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

            ExternalVelocity = Vector3.MoveTowards(
                ExternalVelocity,
                Vector3.zero,
                _externalVelocityDamping * Runner.DeltaTime
            );
            if (HasStateAuthority)
        {
        
        }
        }
   public void ClearBlackHolePull()
{
    if (!HasStateAuthority)
        return;

    BlackHoleActive = false;
    BlackHoleVelocity = Vector3.zero;
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
            if (!HasStateAuthority)
                return;

            ExternalVelocity += force;
            RegisterHit(HitType.Knockback);
        }
        public void ApplyHitstop(float seconds)
        {
            if (!HasStateAuthority || seconds <= 0f)
                return;

            HitstopTimer = TickTimer.CreateFromSeconds(
                Runner,
                seconds
            );
        }

        public void ApplyKnockback(
            Vector3 velocity,
            float hitstopSeconds = 0f)
        {
            ApplyKnockback(
                velocity,
                hitstopSeconds,
                HitType.PowerfulKick
            );
        }

        public void ApplyKnockback(
            Vector3 velocity,
            float hitstopSeconds,
            HitType hitType)
        {
            if (!HasStateAuthority)
                return;

            // Preserve your existing knockback behavior.
            ExternalVelocity += new Vector3(
                velocity.x,
                0f,
                velocity.z
            );

            if (velocity.y > 0f)
                PendingLaunchY = velocity.y;

            if (hitstopSeconds > 0f)
                HitstopTimer = TickTimer.CreateFromSeconds(
                    Runner,
                    hitstopSeconds
                );

            RegisterHit(hitType);
        }
        private void NotifyHitReceived()
        {
            OnHitReceived?.Invoke();
        }
        public void ApplyStun(float duration)
        {
            if (!HasStateAuthority)
                return;

            StunTimer = TickTimer.CreateFromSeconds(
                Runner,
                duration
            );

            _moveDirection = Vector3.zero;
            CurrentMoveVelocity = Vector3.zero;
            RegisterHit(HitType.Stun);
        }
        public void ApplySnareRoot(float duration)
        {
            if (!HasStateAuthority || duration <= 0f)
                return;

            SnareRootTimer = TickTimer.CreateFromSeconds(
                Runner,
                duration
            );

            _moveDirection = Vector3.zero;
            CurrentMoveVelocity = Vector3.zero;
        }
        private void RegisterHit(HitType type)
        {
            LastHitType = type;
            HitSequence++;
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
            public void SetMovementSpeedMultiplier(float multiplier)
        {
            if (!HasStateAuthority)
                return;

            MovementSpeedMultiplier = Mathf.Max(0f, multiplier);
        }

            private void Move()
            {
            if (IsBusy ||
            IsStunned ||
            IsSnareRooted ||
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
        float finalSpeedMultiplier =
            MovementSpeedMultiplier *
            CarrierSpeedMultiplier *
            CollectibleSpeedBoostMultiplier *
            GlobalEventSpeedMultiplier;

        Vector3 targetVelocity =
            movement * _moveSpeed * finalSpeedMultiplier;

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
            ExternalVelocity +
            BlackHoleVelocity;

                finalVelocity.y =
                    _rb.linearVelocity.y;

                _rb.linearVelocity =
                    finalVelocity;
            }

            private void Rotate()
            {
            if (!CanMove ||
            IsBusy ||
            IsStunned ||
            IsSnareRooted)
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

private void SimulateBlackHolePull()
{
    if (!HasStateAuthority || Runner == null)
        return;

    if (!BlackHoleActive)
    {
        BlackHoleVelocity = Vector3.zero;
        return;
    }

    Vector3 direction = BlackHoleCenter - transform.position;
    direction.y = 0f;

    if (direction.sqrMagnitude < 0.01f)
    {
        BlackHoleVelocity = Vector3.zero;
        return;
    }

    direction.Normalize();

    BlackHoleVelocity = Vector3.MoveTowards(
        BlackHoleVelocity,
        direction * BlackHoleMaxSpeed,
        BlackHoleAcceleration * Runner.DeltaTime
    );
}
        public void SetCanJump(bool canJump)
        {
            if (!HasStateAuthority)
                return;

            CanJump = canJump;
        }
        private void Jump()
        {
            LastJumpTick = Runner.Tick.Raw;

            Vector3 velocity = _rb.linearVelocity;
            velocity.y = 0f;

            _rb.linearVelocity = velocity;

            _rb.AddForce(
                Vector3.up * _jumpForce,
                ForceMode.Impulse
            );

            if (HasStateAuthority)
                _audio?.Play(SFXID.PlayerJump);

            OnJumped?.Invoke();
        }

        public void StartBlackHolePull(
    Vector3 center,
    float acceleration,
    float maxSpeed)
{
    if (!HasStateAuthority || Runner == null)
        return;

    BlackHoleCenter = center;
    BlackHoleAcceleration = Mathf.Max(0f, acceleration);
    BlackHoleMaxSpeed = Mathf.Max(0f, maxSpeed);
    BlackHoleActive = true;
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
        public void SetGlobalEventSpeedMultiplier(float multiplier)
        {
            if (!HasStateAuthority)
                return;

            TargetEventSpeedMultiplier = Mathf.Max(0f, multiplier);
        }public void SetCarrierSpeedMultiplier(float multiplier)
        {
            if (!HasStateAuthority)
                return;

            CarrierSpeedMultiplier = Mathf.Max(0f, multiplier);
        }

        public int GetCollectibleSpeedBoostStacks()
        {
            return CollectibleSpeedBoostStacks;
        }

        public bool TryAddCollectibleSpeedBoostStack()
        {
            if (!HasStateAuthority || CollectibleSpeedBoostStacks >= 5)
                return false;

            CollectibleSpeedBoostStacks++;
            return true;
        }

        public void LoseCollectibleSpeedBoostStack()
        {
            if (!HasStateAuthority || CollectibleSpeedBoostStacks <= 0)
                return;

            int previousStacks = CollectibleSpeedBoostStacks;

            CollectibleSpeedBoostStacks--;

            Debug.Log(
                $"[SpeedBoost] {name} lost 1 stack! " +
                $"{previousStacks} -> {CollectibleSpeedBoostStacks} stacks " +
                $"(Speed bonus: +{CollectibleSpeedBoostStacks * 10}%)",
                this
            );
        }
        public void ResetCollectibleSpeedBoost()
        {
            if (!HasStateAuthority)
                return;

            CollectibleSpeedBoostStacks = 0;
            CollectibleSpeedBoostMultiplier = 1f;
        }

        public void UpdateCollectibleSpeedBoost(bool isRelicHolder)
        {
            if (!HasStateAuthority)
                return;

            int stackCap = isRelicHolder ? 3 : 5;

            int effectiveStacks = Mathf.Min(
                CollectibleSpeedBoostStacks,
                stackCap
            );

            float newMultiplier = 1f + effectiveStacks * 0.1f;

            if (!Mathf.Approximately(
                    CollectibleSpeedBoostMultiplier,
                    newMultiplier))
            {
                CollectibleSpeedBoostMultiplier = newMultiplier;

                Debug.Log(
                    $"[SpeedBoost] {name} | " +
                    $"Stacks: {CollectibleSpeedBoostStacks}/{stackCap} | " +
                    $"Effective bonus: +{(newMultiplier - 1f) * 100f:0}% | " +
                    $"Multiplier: {newMultiplier:0.00}x | " +
                    $"Relic holder: {isRelicHolder}",
                    this
                );
            }
        }
        private void CheckGround()
        {
            if (_groundCheck == null ||
                Runner == null ||
                !Runner.IsRunning)
            {
                _isGrounded = false;
                _wasGrounded = false;
                _hasInitializedGroundState = false;
                _airTime = 0f;
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


        _isGrounded = hitCount > 0;

        if (_isGrounded &&
            LastGroundedTick <= LastJumpTick &&
            SecondsSince(LastJumpTick) > _jumpGuardTime)
        {
            LastGroundedTick = Runner.Tick.Raw;
        }

            if (!_hasInitializedGroundState)
            {
                _wasGrounded = _isGrounded;
                _hasInitializedGroundState = true;
                _airTime = 0f;
                return;
            }

            if (HasStateAuthority)
            {
                if (!_isGrounded)
                {
                    _airTime += Runner.DeltaTime;
                }
                else
                {
                if (!_wasGrounded && _airTime > 0.15f)
        {
            _audio?.Play(SFXID.PlayerLand);
            OnLanded?.Invoke();
        }

                    _airTime = 0f;
                }
            }

            _wasGrounded = _isGrounded;
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