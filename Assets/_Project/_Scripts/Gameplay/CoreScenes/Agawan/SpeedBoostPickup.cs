
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

    private readonly Collider[] _hits = new Collider[16];

    private AgawanRelicBoostManager _manager;

    public void Initialize(AgawanRelicBoostManager manager)
    {
        _manager = manager;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || Runner == null)
            return;

        int hitCount = Runner.GetPhysicsScene().OverlapSphere(
            transform.position,
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

            PlayerMovement movement =
                hit.GetComponentInParent<PlayerMovement>();

            if (movement == null ||
                !movement.HasStateAuthority ||
                !movement.CanMove ||
                movement.IsStunned)
            {
                continue;
            }

            NetworkObject playerObject =
                movement.GetComponent<NetworkObject>();

            if (playerObject == null)
                continue;

            if (_manager == null ||
                !_manager.TryCollect(movement))
            {
                continue;
            }

            _manager.HandlePickupCollected(this);
            return;
        }
    }

    public override void Render()
    {
        transform.Rotate(
            0f,
            rotationSpeed * Time.deltaTime,
            0f,
            Space.Self
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
