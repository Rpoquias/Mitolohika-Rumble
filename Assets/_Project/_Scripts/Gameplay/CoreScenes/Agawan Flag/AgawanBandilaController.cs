using Fusion;
using UnityEngine;

public class BandilaController : NetworkBehaviour
{
    public enum FlagState
    {
        Free,
        Carried,
        Stored
    }

    [Header("Game Manager")]
[SerializeField] private AgawanBandilaManager flagManager;

        [Header("Pickup")]
    [SerializeField] private float pickupRadius = 1f;
    [SerializeField] private float pickupLockDuration = 0.6f;
    [SerializeField] private float previousHolderLockDuration = 1f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Carry")]
    [SerializeField] private Vector3 carryOffset =
        new Vector3(0f, 1.8f, 0f);

    [Networked]
    private FlagState State { get; set; }

    [Networked]
    private PlayerRef Holder { get; set; }

    [Networked]
    private PlayerRef StoredOwner { get; set; }

    [Networked]
    private TickTimer PickupLockTimer { get; set; }

    [Networked]
    private TickTimer PreviousHolderLockTimer { get; set; }

    private PlayerMovement _holderMovement;

    private readonly Collider[] _pickupHits =
        new Collider[16];

    public FlagState CurrentState => State;

    public PlayerRef CurrentHolder => Holder;

    public PlayerRef CurrentOwner => StoredOwner;

    public bool IsCarried =>
        State == FlagState.Carried;

    public bool IsStored =>
        State == FlagState.Stored;

    public bool IsFree =>
        State == FlagState.Free;

    public override void Spawned()
    {
        if (!HasStateAuthority)
            return;

        State = FlagState.Free;
        Holder = PlayerRef.None;
        StoredOwner = PlayerRef.None;

        PickupLockTimer = TickTimer.None;
        PreviousHolderLockTimer = TickTimer.None;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (State == FlagState.Carried)
        {
            UpdateCarriedPosition();
            return;
        }

        if (State != FlagState.Free)
            return;

        if (!PickupLockTimer.ExpiredOrNotRunning(Runner))
            return;

        TryFindPlayerToPickup();
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

         PlayerRef player =
    playerObject.InputAuthority;

if (IsPreviousHolderLocked(player))
    continue;

if (flagManager != null &&
    flagManager.IsPlayerCarryingFlag(player))
{
    continue;
}

Pickup(player);
return;
        }
    }

    private bool IsPreviousHolderLocked(PlayerRef player)
    {
        if (PreviousHolderLockTimer.ExpiredOrNotRunning(Runner))
            return false;

        return player == GetPreviousHolder();
    }

    private PlayerRef _previousHolder =
        PlayerRef.None;

    private PlayerRef GetPreviousHolder()
    {
        return _previousHolder;
    }

    private void Pickup(PlayerRef player)
    {
        if (State != FlagState.Free)
            return;

        if (!Runner.TryGetPlayerObject(
                player,
                out NetworkObject playerObject))
        {
            return;
        }

        State = FlagState.Carried;
        Holder = player;
        StoredOwner = PlayerRef.None;

_holderMovement =
    playerObject.GetComponent<PlayerMovement>();

SubscribeToHolder(_holderMovement);

        UpdateCarriedPosition();

        Debug.Log(
            $"[BANDILA FLAG] {player} picked up a flag."
        );
    }

    private void UpdateCarriedPosition()
    {
        if (Holder == PlayerRef.None)
        {
            Drop();
            return;
        }

        if (!Runner.TryGetPlayerObject(
                Holder,
                out NetworkObject playerObject))
        {
            Drop();
            return;
        }

        transform.SetPositionAndRotation(
            playerObject.transform.position +
            carryOffset,
            transform.rotation
        );
    }

    public void Drop()
    {
        if (!HasStateAuthority)
            return;

        if (State != FlagState.Carried)
            return;

        PlayerRef previousHolder =
            Holder;

        UnsubscribeFromHolder();

        Holder = PlayerRef.None;
        State = FlagState.Free;

        _holderMovement = null;

        _previousHolder = previousHolder;

        PickupLockTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                pickupLockDuration
            );

        PreviousHolderLockTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                previousHolderLockDuration
            );

        Debug.Log(
            $"[BANDILA FLAG] Flag dropped by " +
            $"{previousHolder}."
        );
    }

    private void HandleHolderHit()
    {
        if (!HasStateAuthority)
            return;

        Drop();
    }

    private void SubscribeToHolder(
        PlayerMovement movement)
    {
        if (movement == null)
            return;

        movement.OnHitReceived +=
            HandleHolderHit;
    }

    private void UnsubscribeFromHolder()
    {
        if (_holderMovement == null)
            return;

        _holderMovement.OnHitReceived -=
            HandleHolderHit;
    }

    public void StoreAtBase(PlayerRef player)
    {
        if (!HasStateAuthority)
            return;

        if (State != FlagState.Carried)
            return;

        if (Holder != player)
            return;

        UnsubscribeFromHolder();

        Holder = PlayerRef.None;
        _holderMovement = null;

        State = FlagState.Stored;
        StoredOwner = player;

        PickupLockTimer = TickTimer.None;
        PreviousHolderLockTimer = TickTimer.None;

        Debug.Log(
            $"[BANDILA FLAG] Flag stored by {player}."
        );
    }

   public void StealFromBase(PlayerRef player)
{
    if (!HasStateAuthority)
        return;

    if (State != FlagState.Stored)
        return;

    if (StoredOwner == player)
        return;
        
if (flagManager != null &&
    flagManager.IsPlayerCarryingFlag(player))
{
    return;
}

    StoredOwner = PlayerRef.None;
    State = FlagState.Carried;
    Holder = player;

    if (Runner.TryGetPlayerObject(
            player,
            out NetworkObject playerObject))
    {
        _holderMovement =
            playerObject.GetComponent<PlayerMovement>();

        SubscribeToHolder(_holderMovement);

        UpdateCarriedPosition();
    }

    Debug.Log(
        $"[BANDILA FLAG] {player} stole a flag."
    );
}
    public void ResetFlag(
        Vector3 position,
        Quaternion rotation)
    {
        if (!HasStateAuthority)
            return;

        UnsubscribeFromHolder();

        State = FlagState.Free;
        Holder = PlayerRef.None;
        StoredOwner = PlayerRef.None;

        _holderMovement = null;

        PickupLockTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                pickupLockDuration
            );

        PreviousHolderLockTimer =
            TickTimer.None;

        transform.SetPositionAndRotation(
            position,
            rotation
        );
    }

    private void OnDestroy()
    {
        UnsubscribeFromHolder();
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