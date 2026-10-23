
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Collider))]
public class SnareHazard : NetworkBehaviour
{

    private Collider triggerCollider;

    [Header("Snare")]
    [SerializeField] private float rootDuration = 2f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmo = true;
    [SerializeField] private float triggerRadius = 1.5f;

    private MatiraMatibayHazardManager ownerManager;
    private bool hasTriggered;

    public void SetOwnerManager(
        MatiraMatibayHazardManager manager)
    {
        ownerManager = manager;
    }

private void Awake()
{
    triggerCollider = GetComponent<Collider>();

    if (triggerCollider == null)
        return;

    triggerCollider.isTrigger = true;

    if (triggerCollider is SphereCollider sphere)
        sphere.radius = triggerRadius;
}

public override void Spawned()
{
    hasTriggered = false;
}

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority || hasTriggered)
            return;

        PlayerMovement movement =
            other.GetComponentInParent<PlayerMovement>();

        if (movement == null)
            return;

        // Prevent a player from triggering the same snare twice.
        hasTriggered = true;

        movement.ApplySnareRoot(rootDuration);

        // One-use trap: remove it after a successful trigger.
        Runner.Despawn(Object);
    }

 public override void Despawned(
    NetworkRunner runner,
    bool hasState)
{
    if (ownerManager != null)
        ownerManager.NotifySnareDespawned(Object);

    ownerManager = null;
}
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            transform.position,
            triggerRadius
        );
    }
#endif
}
