using System;
using UnityEngine;
using Fusion;

[RequireComponent(typeof(PlayerMovement))]
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

    private readonly Collider[] _hitColliders =
        new Collider[16];

    [Networked]
    private NetworkButtons PreviousButtons { get; set; }

    private float _attackTimer;
    private float _cooldownTimer;

    private bool _isBumping;

    private enum BumpState
    {
        None,
        Startup,
        Active,
        Recovery
    }

    private BumpState _state = BumpState.None;

    public override void Spawned()
    {
        _movement = GetComponent<PlayerMovement>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        UpdateTimers();

        if (GetInput(out NetworkInputData input))
        {
            bool bumpPressed =
                input.Buttons.WasPressed(
                    PreviousButtons,
                    EInputButton.Bump
                );

            if (bumpPressed)
            {
                TryBump();
            }

            PreviousButtons = input.Buttons;
        }
        else
        {
            PreviousButtons = default;
        }

        UpdateBumpState();
    }

    private void UpdateTimers()
    {
        float deltaTime = Runner.DeltaTime;

        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= deltaTime;

            if (_cooldownTimer < 0f)
                _cooldownTimer = 0f;
        }

        if (_attackTimer > 0f)
        {
            _attackTimer -= deltaTime;

            if (_attackTimer < 0f)
                _attackTimer = 0f;
        }
    }

    private void TryBump()
    {
        if (_movement == null)
            return;

        if (!_movement.CanMove)
            return;

        if (_movement.IsBusy)
            return;

        if (!_movement.IsGrounded)
            return;

        if (_isBumping)
            return;

        if (_cooldownTimer > 0f)
            return;

        StartBump();
    }

    private void StartBump()
    {
        _isBumping = true;
        _state = BumpState.Startup;

        _attackTimer = _startup;

        _movement.SetBusy(true);
    }

    private void UpdateBumpState()
    {
        if (!_isBumping)
            return;

        switch (_state)
        {
            case BumpState.Startup:

                if (_attackTimer <= 0f)
                {
                    _state = BumpState.Active;
                    _attackTimer = _activeTime;

                    PerformBump();
                }

                break;

            case BumpState.Active:

                if (_attackTimer <= 0f)
                {
                    _state = BumpState.Recovery;
                    _attackTimer = _recovery;
                }

                break;

            case BumpState.Recovery:

                if (_attackTimer <= 0f)
                {
                    FinishBump();
                }

                break;
        }
    }

    private void FinishBump()
    {
        _isBumping = false;
        _state = BumpState.None;

        _movement.SetBusy(false);

        _cooldownTimer = _cooldown;
    }

    private void PerformBump()
    {
        int hitCount = Runner.GetPhysicsScene()
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
            Collider hit = _hitColliders[i];

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

            PlayerElimination attacker =
                GetComponent<PlayerElimination>();

            PlayerElimination victim =
                otherPlayer.GetComponent<PlayerElimination>();

            if (attacker != null && victim != null)
            {
                OnKnockbackApplied?.Invoke(
                    attacker,
                    victim
                );
            }

            Debug.Log(
                $"[BUMP] {name} hit {otherPlayer.name}"
            );
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