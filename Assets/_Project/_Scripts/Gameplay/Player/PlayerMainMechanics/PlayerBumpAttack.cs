using System;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerBumpAttack : NetworkBehaviour
{
    public event Action<PlayerElimination, PlayerElimination>
        OnKnockbackApplied;

    [Header("Timing")]
    [SerializeField] private float _startup = 0.1f;
    [SerializeField] private float _activeTime = 0.15f;
    [SerializeField] private float _recovery = 0.25f;
    [SerializeField] private float _cooldown = 1f;

    [Header("Hit")]
    [SerializeField] private float _force = 10f;
    [SerializeField] private float _range = 1.5f;
    [SerializeField] private float _radius = 0.75f;
    [SerializeField] private float _minDot = 0.5f;
    [SerializeField] private LayerMask _playerLayer;

    private PlayerMovement _movement;
    private PlayerInputHandler _inputHandler;
    private PlayerElimination _elimination;
    private bool _canBump;
    private bool _relicBlocksBump;
    private readonly Collider[] _hitColliders =
        new Collider[16];

    private enum BumpState : byte
    {
        None,
        Startup,
        Active,
        Recovery
    }

    [Networked]
    private BumpState State { get; set; }

    [Networked]
    private TickTimer AttackTimer { get; set; }

    [Networked]
    private TickTimer CooldownTimer { get; set; }

 public override void Spawned()
{
    _movement = GetComponent<PlayerMovement>();
    _inputHandler = GetComponent<PlayerInputHandler>();
    _elimination = GetComponent<PlayerElimination>();
}

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (_inputHandler.BumpPressed)
        {
            TryBump();
        }

        UpdateBumpState();
    }

public void SetRelicBumpBlocked(bool blocked)
{
    _relicBlocksBump = blocked;

    if (blocked)
    {
        State = BumpState.None;
        AttackTimer = TickTimer.None;
        _movement.SetBusy(false);
    }

    Debug.Log(
        $"[BUMP] {name} RelicBumpBlocked = {_relicBlocksBump}"
    );
}
public void SetCanBump(bool canBump)
{
    _canBump = canBump;
Debug.Log(
        $"[BUMP] {name} CanBump = {_canBump}"
    );
    if (!canBump)
    {
        State = BumpState.None;
        AttackTimer = TickTimer.None;
        _movement.SetBusy(false);
    }
}
 private void TryBump()
{

if (!_canBump ||
    _relicBlocksBump ||
    !_movement.CanMove ||
    _movement.IsBusy ||
    _movement.IsStunned ||
    !_movement.IsGrounded ||
    State != BumpState.None ||
    !CooldownTimer.ExpiredOrNotRunning(Runner))
{
    return;
}

    State = BumpState.Startup;
    AttackTimer = TickTimer.CreateFromSeconds(Runner, _startup);
    _movement.SetBusy(true);
}    private void UpdateBumpState()
    {
        if (State == BumpState.None)
            return;

        if (!AttackTimer.Expired(Runner))
            return;

        switch (State)
        {
            case BumpState.Startup:

                State = BumpState.Active;

                AttackTimer =
                    TickTimer.CreateFromSeconds(
                        Runner,
                        _activeTime
                    );

                PerformBump();

                break;

            case BumpState.Active:

                State = BumpState.Recovery;

                AttackTimer =
                    TickTimer.CreateFromSeconds(
                        Runner,
                        _recovery
                    );

                break;

            case BumpState.Recovery:

                State = BumpState.None;

                AttackTimer = TickTimer.None;

                CooldownTimer =
                    TickTimer.CreateFromSeconds(
                        Runner,
                        _cooldown
                    );

                _movement.SetBusy(false);

                break;
        }
    }

    private void PerformBump()
    {
        int hitCount =
            Runner.GetPhysicsScene()
                .OverlapSphere(
                    transform.position +
                    transform.forward * _range,
                    _radius,
                    _hitColliders,
                    _playerLayer,
                    QueryTriggerInteraction.Ignore
                );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit =
                _hitColliders[i];

            if (hit == null)
                continue;

            PlayerMovement otherPlayer =
                hit.GetComponentInParent<PlayerMovement>();

            if (otherPlayer == null)
                continue;

            if (otherPlayer == _movement)
                continue;

            Vector3 toTarget =
                otherPlayer.transform.position -
                transform.position;

            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= 0.0001f)
                continue;

            Vector3 knockbackDirection =
                toTarget.normalized;

            float dot =
                Vector3.Dot(
                    transform.forward,
                    knockbackDirection
                );

            if (dot < _minDot)
                continue;

            otherPlayer.AddExternalForce(
                knockbackDirection * _force
            );

            PlayerElimination victim =
                otherPlayer.GetComponent<PlayerElimination>();

            if (_elimination != null &&
                victim != null)
            {
                OnKnockbackApplied?.Invoke(
                    _elimination,
                    victim
                );
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position +
            transform.forward * _range,
            _radius
        );
    }
}