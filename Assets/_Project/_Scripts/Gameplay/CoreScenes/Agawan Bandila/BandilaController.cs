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
[Header("Audio")]
[SerializeField] private SFXID pickupSFX = SFXID.BandilaPickup;
[SerializeField] private SFXID dropSFX = SFXID.BandilaDrop;
[SerializeField] private SFXID storedSFX = SFXID.BandilaStored;

[Header("Visuals & Invisibility")]
[SerializeField] private Renderer[] modelRenderers;
[SerializeField] private GameObject valuableVFX;
[SerializeField] private Target targetIndicator;
[Header("Idle Animation")]
[SerializeField] private float rotationSpeed = 60f;
[SerializeField] private Transform modelRoot;
private bool lastHiddenState;
private bool _hasSpawned;
private bool hasInitializedVisualState;
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
private void RPC_PlayFlagSFX(SFXID sfxID, Vector3 position)
{
    if (AudioManager.Instance == null)
        return;

    AudioManager.Instance.PlaySFXAtPosition(sfxID, position);
}
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
private PlayerBumpAttack _holderBump;
private Rigidbody _rb;

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
private void Update()
{
    if (!_hasSpawned)
        return;

    if (State != FlagState.Free || modelRoot == null)
        return;

    modelRoot.Rotate(
        0f,
        rotationSpeed * Time.deltaTime,
        0f,
        Space.Self
    );
}
public override void Spawned()
{
    _rb = GetComponent<Rigidbody>();
    _hasSpawned = true;

    if (!HasStateAuthority)
        return;

    State = FlagState.Free;
    Holder = PlayerRef.None;
    StoredOwner = PlayerRef.None;

    PickupLockTimer = TickTimer.None;
    PreviousHolderLockTimer = TickTimer.None;

    SetCarriedPhysics(false);
}


public override void Render()
{
    if (Object == null || Runner == null)
        return;

    bool shouldHide = false;

    if (State == FlagState.Carried &&
        Holder != PlayerRef.None &&
        Runner.TryGetPlayerObject(
            Holder,
            out NetworkObject playerObject))
    {
        InvisibilityAbility ability =
            playerObject.GetComponent<InvisibilityAbility>();

        bool holderIsInvisible =
            ability != null && ability.IsInvisible;

        bool isLocalHolder =
            playerObject.InputAuthority == Runner.LocalPlayer;

        // Hide the flag only from other players.
        shouldHide = holderIsInvisible && !isLocalHolder;
    }

    if (hasInitializedVisualState &&
        shouldHide == lastHiddenState)
    {
        return;
    }

    hasInitializedVisualState = true;
    lastHiddenState = shouldHide;

    SetModelVisible(!shouldHide);
}


private void SetModelVisible(bool visible)
{
    if (modelRenderers != null)
    {
        foreach (Renderer modelRenderer in modelRenderers)
        {
            if (modelRenderer != null)
                modelRenderer.enabled = visible;
        }
    }

    if (valuableVFX != null)
        valuableVFX.SetActive(visible);

    if (targetIndicator != null)
        targetIndicator.SetHidden(!visible);
}

private void SetCarriedPhysics(bool carried)
{
    if (_rb == null)
        return;

    if (carried)
    {
        // Clear velocity before making the body kinematic.
        if (!_rb.isKinematic)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        _rb.isKinematic = true;
    }
    else
    {
        _rb.isKinematic = false;
    }
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

if (IsCarryingAnotherFlag(player))
    continue;

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

    if (IsCarryingAnotherFlag(player))
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
    SetCarriedPhysics(true);
    
_holderMovement =
    playerObject.GetComponent<PlayerMovement>();

_holderBump =
    playerObject.GetComponent<PlayerBumpAttack>();

SubscribeToHolder(_holderMovement);

SetHolderBumpBlocked(true);

UpdateCarriedPosition();

RPC_PlayFlagSFX(pickupSFX, transform.position);

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

    PlayerRef previousHolder = Holder;

    // Restore the holder's bump attack.
    SetHolderBumpBlocked(false);

    // Stop listening for hits from the previous holder.
    UnsubscribeFromHolder();

    // Release the flag.
    Holder = PlayerRef.None;
    State = FlagState.Free;
SetCarriedPhysics(false);
if (_rb != null)
{
    _rb.linearVelocity = Vector3.zero;
    _rb.angularVelocity = Vector3.zero;
}

    _holderMovement = null;
    _holderBump = null;

    _previousHolder = previousHolder;

    PickupLockTimer = TickTimer.CreateFromSeconds(
        Runner,
        pickupLockDuration
    );

    PreviousHolderLockTimer = TickTimer.CreateFromSeconds(
        Runner,
        previousHolderLockDuration
    );

    RPC_PlayFlagSFX(dropSFX, transform.position);

   
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

    // Restore the player's bump ability.
    SetHolderBumpBlocked(false);

    // Stop listening for hits from the holder.
    UnsubscribeFromHolder();

    // Clear the holder.
    Holder = PlayerRef.None;
    _holderMovement = null;
    _holderBump = null;

    // Store the flag for its owner.
    State = FlagState.Stored;
    StoredOwner = player;
SetCarriedPhysics(true);
    PickupLockTimer = TickTimer.None;
    PreviousHolderLockTimer = TickTimer.None;

    RPC_PlayFlagSFX(storedSFX, transform.position);


}

private bool IsCarryingAnotherFlag(PlayerRef player)
{
    return flagManager != null &&
           flagManager.IsPlayerCarryingFlag(player);
}
 public void StealFromBase(PlayerRef player)
{
    if (!HasStateAuthority)
        return;

    if (State != FlagState.Stored)
        return;

    if (StoredOwner == player)
        return;

    if (IsCarryingAnotherFlag(player))
        return;

    if (!Runner.TryGetPlayerObject(
            player,
            out NetworkObject playerObject))
    {
        return;
    }

    StoredOwner = PlayerRef.None;
    State = FlagState.Carried;
    Holder = player;

SetCarriedPhysics(true);
   _holderMovement =
    playerObject.GetComponent<PlayerMovement>();

_holderBump =
    playerObject.GetComponent<PlayerBumpAttack>();

SubscribeToHolder(_holderMovement);

SetHolderBumpBlocked(true);

UpdateCarriedPosition();

RPC_PlayFlagSFX(pickupSFX, transform.position);
   
}

private void SetHolderBumpBlocked(bool blocked)
{
    if (_holderBump != null)
    {
        _holderBump.SetObjectiveBumpBlocked(blocked);
    }
}
  public void ResetFlag(Vector3 position, Quaternion rotation)
{
    if (!HasStateAuthority)
        return;

    // Restore bump ability before clearing the holder reference.
    SetHolderBumpBlocked(false);

    UnsubscribeFromHolder();

    State = FlagState.Free;
    Holder = PlayerRef.None;
    StoredOwner = PlayerRef.None;
SetCarriedPhysics(false);
    _holderMovement = null;
    _holderBump = null;

    _previousHolder = PlayerRef.None;

    PickupLockTimer = TickTimer.CreateFromSeconds(
        Runner,
        pickupLockDuration
    );

    PreviousHolderLockTimer = TickTimer.None;

    transform.SetPositionAndRotation(position, rotation);
    if (_rb != null)
{
    _rb.linearVelocity = Vector3.zero;
    _rb.angularVelocity = Vector3.zero;
}
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