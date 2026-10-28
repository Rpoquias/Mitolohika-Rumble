using UnityEngine;

[RequireComponent(typeof(KapiPlayerInputHandler))]
[RequireComponent(typeof(KapiPlayerMovement))]
public abstract class KapiCharacterAbility : MonoBehaviour, ICharacterAbility
{
    [Header("Cooldown")]
    [SerializeField] protected float cooldown = 5f;

    [Header("Targeting")]
    [SerializeField] protected float targetRange = 4f;
    [SerializeField] protected float maxVerticalDifference = 1.5f;
    [SerializeField, Range(0f, 90f)] protected float targetAngle = 50f;
    [SerializeField] protected LayerMask playerLayer;

    private readonly Collider[] _targetColliders = new Collider[16];
    private KapiPlayerAnimationController _animationController;
    private KapiPlayerBumpAttack _bumpAttack;
    protected KapiPlayerMovement _movement;
    protected KapiPlayerInputHandler _inputHandler;
    protected KapiPlayerMovement CurrentTarget { get; private set; }
    protected KapiPlayerAudio Audio { get; private set; }

    private float _cooldownEndsAt;
    private float _bumpLockEndsAt;
    private bool _canUseAbility = true;
    private float MinimumTargetDot => Mathf.Cos(targetAngle * Mathf.Deg2Rad);

    public int ActivationCount { get; private set; }
    public bool IsBlockingBump => Time.time < _bumpLockEndsAt;
    public float CooldownRemaining => Mathf.Max(0f, _cooldownEndsAt - Time.time);

    protected virtual void Awake()
    {
        _movement = GetComponent<KapiPlayerMovement>();
        _inputHandler = GetComponent<KapiPlayerInputHandler>();
        Audio = GetComponent<KapiPlayerAudio>();
        _bumpAttack = GetComponent<KapiPlayerBumpAttack>();
        _animationController = GetComponentInChildren<KapiPlayerAnimationController>();
    }

    protected virtual void Update()
    {
        if (_inputHandler != null &&
            _inputHandler.ConsumeAbilityPressed() &&
            CanActivate())
        {
            Activate();
        }
    }

    public virtual bool CanActivate()
    {
        return Time.time >= _cooldownEndsAt &&
               _canUseAbility &&
               _movement != null &&
               _movement.CanMove &&
               !_movement.IsBusy &&
               !_movement.IsStunned &&
               !_movement.IsSnareRooted &&
               (_bumpAttack == null || !_bumpAttack.IsBumping);
    }

    public void SetCanUseAbility(bool canUse) => _canUseAbility = canUse;
    public abstract void Activate();

    protected void NotifyActivated()
    {
        ActivationCount++;
        _bumpLockEndsAt = Time.time + 0.5f;
        _movement?.LoseCollectibleSpeedBoostStack();
    }

    protected void StartCooldown() => _cooldownEndsAt = Time.time + cooldown;

    protected KapiPlayerMovement FindBestTarget()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position, targetRange, _targetColliders, playerLayer,
            QueryTriggerInteraction.Ignore);
        KapiPlayerMovement bestTarget = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _targetColliders[i];
            if (hit == null)
                continue;
            KapiPlayerMovement candidate = hit.GetComponentInParent<KapiPlayerMovement>();
            if (candidate == null || candidate == _movement)
                continue;
            KapiPlayerTargetingState targeting = candidate.GetComponent<KapiPlayerTargetingState>();
            if (targeting != null && targeting.IsUntargetable)
                continue;

            Vector3 toTarget = candidate.transform.position - transform.position;
            if (Mathf.Abs(toTarget.y) > maxVerticalDifference)
                continue;
            Vector3 horizontal = toTarget;
            horizontal.y = 0f;
            if (horizontal.sqrMagnitude <= 0.001f)
                continue;
            if (horizontal.magnitude > targetRange)
                continue;

            float score = Vector3.Dot(transform.forward, horizontal.normalized);
            if (score < MinimumTargetDot || score <= bestScore)
                continue;
            bestScore = score;
            bestTarget = candidate;
        }

        CurrentTarget = bestTarget;
        return bestTarget;
    }

    protected bool HasAbilityAnimation(string triggerName) =>
        _animationController != null && _animationController.HasTrigger(triggerName);

    protected bool PlayAbilityAnimation(string triggerName) =>
        _animationController != null && _animationController.PlayAbilityAnimation(triggerName);

    public bool IsAbilityAvailable => CanActivate();

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, targetRange);
    }
#endif
}