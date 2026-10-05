using Fusion;
using UnityEngine;

public class RelicController : NetworkBehaviour
{
    [Header("Pickup")]
    [SerializeField] private float pickupRadius = 1f;
    [SerializeField] private float pickupLockDuration = 0.6f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Carry")]
    [SerializeField] private Vector3 carryOffset =
        new Vector3(0f, 1.8f, 0f);

    [Networked]
    private PlayerRef Holder { get; set; }

    [Networked]
    private TickTimer PickupLockTimer { get; set; }

    private PlayerMovement _holderMovement;
    private PlayerBumpAttack _holderBump;

    private Vector3 _startingPosition;
private Quaternion _startingRotation;

    private readonly Collider[] _pickupHits =
        new Collider[16];

    public bool IsHeld =>
        Holder != PlayerRef.None;

    public PlayerRef CurrentHolder =>
        Holder;

public override void Spawned()
{
    if (!HasStateAuthority)
        return;

    _startingPosition = transform.position;
    _startingRotation = transform.rotation;

    Holder = PlayerRef.None;
    PickupLockTimer = TickTimer.None;
}

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (Holder != PlayerRef.None)
        {
            UpdateHeldPosition();
            return;
        }

        if (!PickupLockTimer.ExpiredOrNotRunning(Runner))
            return;

        TryFindPlayerToPickup();
    }


public void ResetRelic()
{
    if (!HasStateAuthority)
        return;

    if (Holder != PlayerRef.None)
    {
        if (Runner.TryGetPlayerObject(
            Holder,
            out NetworkObject playerObject))
        {
            PlayerBumpAttack bump =
                playerObject.GetComponent<PlayerBumpAttack>();

            if (bump != null)
                bump.SetRelicBumpBlocked(false);
        }
    }

    UnsubscribeFromHolder(_holderMovement);

    Holder = PlayerRef.None;

    _holderMovement = null;
    _holderBump = null;

    transform.SetPositionAndRotation(
        _startingPosition,
        _startingRotation
    );

    PickupLockTimer =
        TickTimer.CreateFromSeconds(
            Runner,
            pickupLockDuration
        );

    Debug.Log("[RELIC] Relic reset for new round.");
}
    private void TryFindPlayerToPickup()
    {
        int hitCount =
            Runner.GetPhysicsScene().OverlapSphere(
                transform.position,
                pickupRadius,
                _pickupHits,
                playerLayer,
                QueryTriggerInteraction.Ignore
            );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _pickupHits[i];

            if (hit == null)
                continue;

            PlayerMovement movement =
                hit.GetComponentInParent<PlayerMovement>();

            if (movement == null)
                continue;

            if (!movement.HasStateAuthority)
                continue;

            if (!movement.CanMove)
                continue;

            if (movement.IsStunned)
                continue;

            NetworkObject playerObject =
                movement.GetComponent<NetworkObject>();

            if (playerObject == null)
                continue;

            Pickup(playerObject.InputAuthority);
            return;
        }
    }

    private void SubscribeToHolder(PlayerMovement movement)
{
    if (movement == null)
        return;

    movement.OnHitReceived += HandleHolderHit;
}

private void UnsubscribeFromHolder(PlayerMovement movement)
{
    if (movement == null)
        return;

    movement.OnHitReceived -= HandleHolderHit;
}

private void HandleHolderHit()
{
    if (!HasStateAuthority)
        return;

    Drop();
}

    private void Pickup(PlayerRef player)
    {
        if (Holder != PlayerRef.None)
            return;

        if (!Runner.TryGetPlayerObject(
                player,
                out NetworkObject playerObject))
        {
            return;
        }

        Holder = player;

        _holderMovement =
            playerObject.GetComponent<PlayerMovement>();
        
        _holderBump =
            playerObject.GetComponent<PlayerBumpAttack>();
            SubscribeToHolder(_holderMovement);

   if (_holderBump != null)
{
    _holderBump.SetRelicBumpBlocked(true);
}

        UpdateHeldPosition();

        Debug.Log(
            $"[RELIC] {player} picked up the Relic."
        );
    }

    private void UpdateHeldPosition()
    {
        if (Holder == PlayerRef.None)
            return;

        if (!Runner.TryGetPlayerObject(
                Holder,
                out NetworkObject playerObject))
        {
            Drop();
            return;
        }

        transform.SetPositionAndRotation(
            playerObject.transform.position + carryOffset,
            transform.rotation
        );
    }

    public void Drop()
    {
        if (!HasStateAuthority)
            return;

        if (Holder == PlayerRef.None)
            return;

        PlayerRef previousHolder = Holder;

        if (Runner.TryGetPlayerObject(
                previousHolder,
                out NetworkObject playerObject))
        {
            PlayerBumpAttack bump =
                playerObject.GetComponent<PlayerBumpAttack>();

        if (bump != null)
{
    bump.SetRelicBumpBlocked(false);
}
        }

        Holder = PlayerRef.None;


UnsubscribeFromHolder(_holderMovement);
        _holderMovement = null;
        _holderBump = null;

        PickupLockTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                pickupLockDuration
            );

        Debug.Log(
            $"[RELIC] Relic dropped by {previousHolder}."
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            pickupRadius
        );
    }
}