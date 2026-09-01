using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerBumpAttack : MonoBehaviour
{
public event Action<PlayerElimination, PlayerElimination> OnKnockbackApplied;

    [Header("Input")]
    [SerializeField] private InputActionReference _bumpAction;

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

    private readonly List<PlayerMovement> _hitPlayers = new List<PlayerMovement>();
    private PlayerMovement _movement;
    private bool _isBumping;
    private bool _onCooldown;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (!_movement.CanMove)
            return;

        if (_bumpAction.action.WasPressedThisFrame())
            TryBump();
    }

    private void TryBump()
    {
        if (_isBumping || _onCooldown)
            return;

        if (!_movement.IsGrounded)
            return;

        StartCoroutine(BumpRoutine());
    }

    private IEnumerator BumpRoutine()
    {
        _isBumping = true;
        _movement.SetBusy(true);

        yield return new WaitForSeconds(_startup);

        PerformBump();

        yield return new WaitForSeconds(_activeTime);
        yield return new WaitForSeconds(_recovery);

        _isBumping = false;
        _movement.SetBusy(false);

        _onCooldown = true;
        yield return new WaitForSeconds(_cooldown);
        _onCooldown = false;
    }

    private void PerformBump()
    {
        _hitPlayers.Clear();

        Vector3 bumpPosition = transform.position + transform.forward * _range;
        Collider[] hits = Physics.OverlapSphere(bumpPosition, _radius, _playerLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            PlayerMovement otherPlayer = hits[i].GetComponentInParent<PlayerMovement>();
            if (otherPlayer == null || otherPlayer == _movement)
                continue;

            if (_hitPlayers.Contains(otherPlayer))
                continue;

            Vector3 toTarget = otherPlayer.transform.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= 0.0001f)
                continue;

            Vector3 knockbackDirection = toTarget.normalized;
            if (Vector3.Dot(transform.forward, knockbackDirection) < _minDot)
                continue;

            _hitPlayers.Add(otherPlayer);

            otherPlayer.AddExternalForce(knockbackDirection * _force);

            PlayerElimination attacker = GetComponent<PlayerElimination>();
            PlayerElimination victim = otherPlayer.GetComponent<PlayerElimination>();

if (attacker != null && victim != null)
{
    OnKnockbackApplied?.Invoke(attacker, victim);
}
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + transform.forward * _range, _radius);
    }
}
