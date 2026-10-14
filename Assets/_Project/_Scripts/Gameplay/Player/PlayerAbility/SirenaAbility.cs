using Fusion;
using UnityEngine;

public class SirenaAbility : CharacterAbility
{
  private enum SirenaState
{
    Normal,
    Diving,
    Emerging
}
    [Header("Dive Settings")]
    [SerializeField, Min(0.1f)]
    private float _diveDuration = 2f;

    [SerializeField, Range(0.1f, 1f)]
    private float _submergedSpeedMultiplier = 0.5f;

    [Header("Emergence Attack")]
    [SerializeField, Min(0.1f)]
    private float _knockbackRadius = 3f;

    [SerializeField, Min(0f)]
    private float _knockbackForce = 10f;

    [SerializeField, Min(0f)]
    private float _verticalKnockback = 3f;

    [SerializeField]
    private LayerMask _playerLayer;

    [Header("References")]
    [SerializeField]
    private Transform _effectOrigin;
[SerializeField, Min(0.1f)]
private float _emergeDuration = 0.8f;
[Networked]
private TickTimer EmergeTimer { get; set; }
    [Networked]
    private SirenaState CurrentState { get; set; }

    [Networked]
    private TickTimer DiveTimer { get; set; }

    private PlayerTargetingState _targetingState;
    private PlayerMovement _playerMovement;

    private readonly Collider[] _overlapResults =
        new Collider[16];

public bool IsDiving => CurrentState == SirenaState.Diving;
public bool IsEmerging => CurrentState == SirenaState.Emerging;



    public override void Spawned()
    {
        base.Spawned();

        _targetingState =
            GetComponent<PlayerTargetingState>();

        _playerMovement =
            GetComponent<PlayerMovement>();
    }

    public override void Activate()
    {
        if (!HasStateAuthority)
            return;

        if (!CanActivate())
            return;

        if (CurrentState != SirenaState.Normal)
            return;

        BeginDive();

        // Intentionally do not start the cooldown here.
        // It starts after the emergence attack.
    }

public override void FixedUpdateNetwork()
{
    base.FixedUpdateNetwork();

    if (!HasStateAuthority)
        return;

    if (CurrentState == SirenaState.Diving &&
        DiveTimer.ExpiredOrNotRunning(Runner))
    {
        Emerge();
    }
    else if (CurrentState == SirenaState.Emerging &&
             EmergeTimer.ExpiredOrNotRunning(Runner))
    {
        CurrentState = SirenaState.Normal;
    }
}
  private void BeginDive()
{
    CurrentState = SirenaState.Diving;

    DiveTimer = TickTimer.CreateFromSeconds(
        Runner,
        _diveDuration
    );

    if (_targetingState != null)
        _targetingState.SetUntargetable(true);

    if (_playerMovement != null)
    {
        _playerMovement.SetMovementSpeedMultiplier(
            _submergedSpeedMultiplier
        );

        _playerMovement.SetCanJump(false);
    }

    Audio?.Play(SFXID.SirenaDive);
}

private void Emerge()
{
    if (_playerMovement != null)
    {
        _playerMovement.SetMovementSpeedMultiplier(1f);
        _playerMovement.SetCanJump(true);
    }

    if (_targetingState != null)
        _targetingState.SetUntargetable(false);

    CurrentState = SirenaState.Emerging;

    EmergeTimer = TickTimer.CreateFromSeconds(
        Runner,
        _emergeDuration
    );

    ApplyEmergenceKnockback();

    Audio?.Play(SFXID.SirenaEmerge);

    StartCooldown();
}
    private void ApplyEmergenceKnockback()
    {
        Vector3 origin = _effectOrigin != null
            ? _effectOrigin.position
            : transform.position;

        int hitCount = Runner.GetPhysicsScene().OverlapSphere(
            origin,
            _knockbackRadius,
            _overlapResults,
            _playerLayer,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _overlapResults[i];

            if (hit == null)
                continue;

            PlayerMovement target =
                hit.GetComponentInParent<PlayerMovement>();

            if (target == null || target == _playerMovement)
                continue;

            if (target.Object == null ||
                !target.Object.HasStateAuthority)
            {
                continue;
            }

            PlayerTargetingState targetState =
                target.GetComponent<PlayerTargetingState>();

            if (targetState != null &&
                targetState.IsUntargetable)
            {
                continue;
            }

            Vector3 direction =
                target.transform.position - origin;

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
            {
                direction = transform.forward;
            }
            else
            {
                direction.Normalize();
            }

            Vector3 knockback =
                direction * _knockbackForce +
                Vector3.up * _verticalKnockback;

            target.AddExternalForce(knockback);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = _effectOrigin != null
            ? _effectOrigin.position
            : transform.position;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, _knockbackRadius);
    }
}