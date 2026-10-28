using System;
using UnityEngine;

[RequireComponent(typeof(KapiPlayerMovement))]
[RequireComponent(typeof(KapiPlayerInputHandler))]
public class KapiPlayerBumpAttack : MonoBehaviour
{
    public int BumpAnimationCount { get; private set; }
    public event Action<KapiPlayerElimination, KapiPlayerElimination> OnKnockbackApplied;
    public event Action OnBumpStarted;

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

    private KapiPlayerMovement _movement;
    private KapiPlayerAudio _audio;
    private KapiPlayerInputHandler _inputHandler;
    private KapiPlayerElimination _elimination;
    private KapiCharacterAbility _ability;
    private bool _canBump = true;
    private bool _objectiveBlocksBump;
    private BumpState _state;
    private float _stateEndsAt;
    private float _cooldownEndsAt;
    private readonly Collider[] _hitColliders = new Collider[16];

    private enum BumpState { None, Startup, Active, Recovery }

    private void Awake()
    {
        _movement = GetComponent<KapiPlayerMovement>();
        _inputHandler = GetComponent<KapiPlayerInputHandler>();
        _elimination = GetComponent<KapiPlayerElimination>();
        _audio = GetComponent<KapiPlayerAudio>();
        _ability = GetComponent<KapiCharacterAbility>();
    }

    private void Update()
    {
        if (_inputHandler != null && _inputHandler.ConsumeBumpPressed())
            TryBump();

        if (_state == BumpState.None || Time.time < _stateEndsAt)
            return;

        switch (_state)
        {
            case BumpState.Startup:
                _state = BumpState.Active;
                _stateEndsAt = Time.time + _activeTime;
                PerformBump();
                break;
            case BumpState.Active:
                _state = BumpState.Recovery;
                _stateEndsAt = Time.time + _recovery;
                break;
            case BumpState.Recovery:
                _state = BumpState.None;
                _movement?.SetBusy(false);
                break;
        }
    }

    public void SetObjectiveBumpBlocked(bool blocked)
    {
        _objectiveBlocksBump = blocked;
        if (blocked)
        {
            _state = BumpState.None;
            _movement?.SetBusy(false);
        }
    }

    public void SetCanBump(bool canBump)
    {
        _canBump = canBump;
        if (!canBump)
        {
            _state = BumpState.None;
            _movement?.SetBusy(false);
        }
    }

    private void TryBump()
    {
        if (!_canBump || _objectiveBlocksBump || _movement == null ||
            !_movement.CanMove || _movement.IsBusy || _movement.IsStunned ||
            !_movement.IsGrounded || _state != BumpState.None ||
            (_ability != null && _ability.IsBlockingBump) ||
            Time.time < _cooldownEndsAt)
            return;

        _state = BumpState.Startup;
        _stateEndsAt = Time.time + _startup;
        BumpAnimationCount++;
        _movement.SetBusy(true);
        _audio?.Play(SFXID.PlayerBump);
        OnBumpStarted?.Invoke();
    }

    private void PerformBump()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position + transform.forward * _range,
            _radius,
            _hitColliders,
            _playerLayer,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < count; i++)
        {
            Collider hit = _hitColliders[i];
            if (hit == null)
                continue;

            KapiPlayerMovement other = hit.GetComponentInParent<KapiPlayerMovement>();
            if (other == null || other == _movement)
                continue;

            Vector3 direction = other.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
                continue;
            direction.Normalize();
            if (Vector3.Dot(transform.forward, direction) < _minDot)
                continue;

            other.AddExternalForce(direction * _force);
            KapiPlayerElimination victim = other.GetComponent<KapiPlayerElimination>();
            if (_elimination != null && victim != null)
                OnKnockbackApplied?.Invoke(_elimination, victim);
        }

        _cooldownEndsAt = Time.time + _cooldown;
    }

    public float CooldownRemaining => Mathf.Max(0f, _cooldownEndsAt - Time.time);
    public bool IsBumpAvailable =>
        _canBump && !_objectiveBlocksBump && _movement != null &&
        _movement.CanMove && !_movement.IsBusy && !_movement.IsStunned &&
        _movement.IsGrounded && _state == BumpState.None &&
        (_ability == null || !_ability.IsBlockingBump) &&
        Time.time >= _cooldownEndsAt;
    public bool IsBumping => _state != BumpState.None;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + transform.forward * _range, _radius);
    }
#endif
}