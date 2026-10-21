using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class SpeedBoostPickup : NetworkBehaviour
{
    [SerializeField, Min(0.1f)]
    private float pickupRadius = 1f;

    [SerializeField]
    private LayerMask playerLayer;

    [SerializeField]
    private float rotationSpeed = 90f;

    [Header("Optimization")]
    [Tooltip("Run the overlap check every N ticks. 2 = half the physics queries, " +
             "still ~30 checks/sec at 60Hz. Set to 1 for the old every-tick behavior.")]
    [SerializeField, Min(1)]
    private int checkIntervalTicks = 2;

    private readonly Collider[] _hits = new Collider[8];

    private Transform _transform;
    private AgawanRelicBoostManager _manager;
    private int _tickOffset;

    private void Awake()
    {
        _transform = transform;
    }

    public override void Spawned()
    {
        // Stagger pickups so they don't all query on the same tick.
        _tickOffset = (int)(Object.Id.Raw % (uint)checkIntervalTicks);
    }

    public void Initialize(AgawanRelicBoostManager manager)
    {
        _manager = manager;
    }

    public override void FixedUpdateNetwork()
    {
        if (_manager == null || !HasStateAuthority)
            return;

        if (checkIntervalTicks > 1 &&
            (Runner.Tick.Raw + _tickOffset) % checkIntervalTicks != 0)
        {
            return;
        }

        int hitCount = Runner.GetPhysicsScene().OverlapSphere(
            _transform.position,
            pickupRadius,
            _hits,
            playerLayer,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _hits[i];

            if (hit == null)
                continue;

            PlayerMovement movement = hit.GetComponentInParent<PlayerMovement>();

            if (movement == null ||
                movement.Object == null ||
                !movement.Object.IsValid ||
                !movement.HasStateAuthority ||
                !movement.CanMove ||
                movement.IsStunned)
            {
                continue;
            }

            // A repeat attempt for a second collider of the same player is
            // harmless and cheap, so no per-tick HashSet is needed anymore.
            if (!_manager.TryCollect(movement))
                continue;

            _manager.HandlePickupCollected(this);
            return;
        }
    }

    public override void Render()
    {
        _transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.Self);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}