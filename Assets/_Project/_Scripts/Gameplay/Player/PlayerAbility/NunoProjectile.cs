using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class NunoProjectile : NetworkBehaviour
{
    [Header("Projectile")]
    [SerializeField] private float _defaultSpeed = 12f;
    [SerializeField] private float _defaultLifetime = 3f;
    [SerializeField] private float _defaultReversedInputDuration = 2f;

    [Networked] private PlayerRef Owner { get; set; }
    [Networked] private Vector3 Direction { get; set; }
    [Networked] private float ProjectileSpeed { get; set; }
    [Networked] private float ReversedInputDuration { get; set; }
    [Networked] private TickTimer LifetimeTimer { get; set; }
    [Networked] private NetworkBool IsInitialized { get; set; }

    private Rigidbody _rigidbody;
    private bool _hasHit;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

 public override void Spawned()
{
    if (_rigidbody == null)
        _rigidbody = GetComponent<Rigidbody>();

    _rigidbody.useGravity = false;
}

    public void Initialize(
        Vector3 direction,
        float speed,
        float reversedInputDuration,
        float lifetime)
    {
        if (!HasStateAuthority)
            return;

        if (IsInitialized)
            return;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            direction = transform.forward;

        Direction = direction.normalized;
        ProjectileSpeed = Mathf.Max(0f, speed);
        ReversedInputDuration = Mathf.Max(0f, reversedInputDuration);

        LifetimeTimer = TickTimer.CreateFromSeconds(
            Runner,
            Mathf.Max(0.1f, lifetime)
        );

        IsInitialized = true;

        _rigidbody.linearVelocity =
            Direction * ProjectileSpeed;
    }

    public void SetOwner(PlayerRef owner)
    {
        if (HasStateAuthority)
            Owner = owner;
    }

    public override void FixedUpdateNetwork()
    {
   if (!HasStateAuthority || !IsInitialized)
    return;

        if (LifetimeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        // Keep the projectile moving at its configured speed.
        _rigidbody.linearVelocity =
            Direction * ProjectileSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!HasStateAuthority || _hasHit)
            return;

        if (collision == null || collision.collider == null)
            return;

        PlayerMovement victim =
            collision.collider.GetComponentInParent<PlayerMovement>();

        if (victim != null)
        {
            // Do not hit the owner.
            if (victim.Object != null &&
                victim.Object.InputAuthority == Owner)
            {
                return;
            }

            // Ignore eliminated players if your elimination
            // system exposes a reliable eliminated-state property.
            PlayerTargetingState targetingState =
                victim.GetComponent<PlayerTargetingState>();

            if (targetingState != null &&
                targetingState.IsUntargetable)
            {
                return;
            }

            PlayerControlEffects effects =
                victim.GetComponent<PlayerControlEffects>();

            if (effects != null)
            {
                _hasHit = true;

                effects.ApplyReversedInput(
                    ReversedInputDuration
                );

                Runner.Despawn(Object);
                return;
            }

            // A player without PlayerControlEffects should not
            // receive the effect, but should still stop the projectile.
            _hasHit = true;
            Runner.Despawn(Object);
            return;
        }

        // Hit the environment.
        _hasHit = true;
        Runner.Despawn(Object);
    }
}