    using Fusion;
    using UnityEngine;

    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(PlayerMovement))]
    public abstract class CharacterAbility : NetworkBehaviour, ICharacterAbility
    {
        [Header("Cooldown")]
        [SerializeField] protected float cooldown = 5f;

        [Header("Targeting")]
        [SerializeField] protected float targetRange = 4f;
        [SerializeField] protected float maxVerticalDifference = 1.5f;
        [SerializeField, Range(0f, 90f)] protected float targetAngle = 50f;
        [SerializeField] protected LayerMask playerLayer;
private PlayerAnimationController _animationController;
        [Networked]
        protected TickTimer CooldownTimer { get; set; }
        [Networked]

        
public int ActivationCount { get; private set; }
[Networked]
private TickTimer BumpLockTimer { get; set; }

public bool IsBlockingBump =>
    Runner != null &&
    !BumpLockTimer.ExpiredOrNotRunning(Runner);
    private PlayerBumpAttack _bumpAttack;
protected void NotifyActivated()
{
    ActivationCount++;

    // Prevent bumping immediately after a skill activates.
    BumpLockTimer = TickTimer.CreateFromSeconds(
        Runner,
        0.5f
    );

    if (_movement != null)
    {
        _movement.LoseCollectibleSpeedBoostStack();
    }
}

        private readonly Collider[] _targetColliders = new Collider[16];

        private PlayerMovement _movement;
        private PlayerInputHandler _inputHandler;
        private bool _canUseAbility;
        // Used only for visualization/debugging.
        protected PlayerMovement CurrentTarget { get; private set; }
        protected PlayerAudio Audio { get; private set; }

        private float MinimumTargetDot =>
            Mathf.Cos(targetAngle * Mathf.Deg2Rad);

public virtual bool CanActivate()
{
    return CooldownTimer.ExpiredOrNotRunning(Runner) &&
           _movement != null &&
           _movement.CanMove &&
           !_movement.IsBusy &&
           !_movement.IsStunned &&
           !_movement.IsSnareRooted &&
           (_bumpAttack == null || !_bumpAttack.IsBumping);
}

public override void Spawned()
{
    _movement = GetComponent<PlayerMovement>();
    _inputHandler = GetComponent<PlayerInputHandler>();
    Audio = GetComponent<PlayerAudio>();
    _bumpAttack = GetComponent<PlayerBumpAttack>();

    _animationController =
        GetComponentInChildren<PlayerAnimationController>();
}
public override void FixedUpdateNetwork()
{
    if (!HasStateAuthority)
        return;

    if (!_canUseAbility)
        return;

 if (_inputHandler.AbilityPressed &&
    CanActivate())
{
    Activate();
}
}
protected bool HasAbilityAnimation(string triggerName)
{
    return _animationController != null &&
           _animationController.HasTrigger(triggerName);
}

protected bool PlayAbilityAnimation(string triggerName)
{
    return _animationController != null &&
           _animationController.PlayAbilityAnimation(triggerName);
}
public void SetCanUseAbility(bool canUse)
{
    _canUseAbility = canUse;
 
}
        public abstract void Activate();

        protected void StartCooldown()
        {
            CooldownTimer =
                TickTimer.CreateFromSeconds(
                    Runner,
                    cooldown
                );
        }

        protected PlayerMovement FindBestTarget()
        {
            int hitCount =
                Runner.GetPhysicsScene()
                    .OverlapSphere(
                        transform.position,
                        targetRange,
                        _targetColliders,
                        playerLayer,
                        QueryTriggerInteraction.Ignore
                    );

            PlayerMovement bestTarget = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _targetColliders[i];

                if (hit == null)
                    continue;

                PlayerMovement target =
                    hit.GetComponentInParent<PlayerMovement>();

                if (target == null)
                    continue;
                    PlayerTargetingState targetingState =
    target.GetComponent<PlayerTargetingState>();

if (targetingState != null &&
    targetingState.IsUntargetable)
{
    continue;
}

                // Don't target yourself.
                if (target == _movement)
                    continue;

                Vector3 toTarget =
                    target.transform.position -
                    transform.position;

                // -------------------------
                // Vertical check
                // -------------------------

                float verticalDifference =
                    Mathf.Abs(toTarget.y);

                if (verticalDifference > maxVerticalDifference)
                    continue;

                // -------------------------
                // Horizontal check
                // -------------------------

                Vector3 horizontalDirection = toTarget;
                horizontalDirection.y = 0f;

                if (horizontalDirection.sqrMagnitude <= 0.001f)
                    continue;

                float horizontalDistance =
                    horizontalDirection.magnitude;

                if (horizontalDistance > targetRange)
                    continue;

                horizontalDirection.Normalize();

                // -------------------------
                // Facing angle check
                // -------------------------

                float dot =
                    Vector3.Dot(
                        transform.forward,
                        horizontalDirection
                    );

                if (dot < MinimumTargetDot)
                    continue;

                // Higher dot = more directly in front.
                float score = dot;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = target;
                }
            }

            CurrentTarget = bestTarget;

            return bestTarget;
        }

public float CooldownRemaining
{
    get
    {
        if (Runner == null || !Runner.IsRunning)
            return 0f;

        return CooldownTimer.RemainingTime(Runner) ?? 0f;
    }
}

public bool IsAbilityAvailable
{
    get
    {
        return _movement != null &&
               _movement.CanMove &&
               !_movement.IsBusy &&
               !_movement.IsStunned &&
               !_movement.IsSnareRooted &&
               (_bumpAttack == null || !_bumpAttack.IsBumping) &&
               CooldownTimer.ExpiredOrNotRunning(Runner);
    }
}
    #if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            // --------------------------------
            // Target range
            // --------------------------------

            Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(
                transform.position,
                targetRange
            );

            // --------------------------------
            // Forward targeting cone
            // --------------------------------

            Vector3 origin = transform.position;

            Vector3 forward = transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;

            forward.Normalize();

            Quaternion leftRotation =
                Quaternion.AngleAxis(
                    -targetAngle,
                    Vector3.up
                );

            Quaternion rightRotation =
                Quaternion.AngleAxis(
                    targetAngle,
                    Vector3.up
                );

            Vector3 leftDirection =
                leftRotation * forward;

            Vector3 rightDirection =
                rightRotation * forward;

            Gizmos.color = Color.cyan;

            Gizmos.DrawLine(
                origin,
                origin + leftDirection * targetRange
            );

            Gizmos.DrawLine(
                origin,
                origin + rightDirection * targetRange
            );

            // --------------------------------
            // Vertical tolerance
            // --------------------------------

            Gizmos.color = Color.green;

            Vector3 top =
                origin +
                Vector3.up * maxVerticalDifference;

            Vector3 bottom =
                origin -
                Vector3.up * maxVerticalDifference;

            Gizmos.DrawLine(
                top,
                bottom
            );

            // --------------------------------
            // Current target
            // --------------------------------

            if (Application.isPlaying && CurrentTarget != null)
            {
                Gizmos.color = Color.red;

                Gizmos.DrawLine(
                    origin,
                    CurrentTarget.transform.position
                );

                Gizmos.DrawWireSphere(
                    CurrentTarget.transform.position,
                    0.3f
                );
            }
        }

    #endif
    }