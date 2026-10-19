using Fusion;
using UnityEngine;

public class RelicController : NetworkBehaviour
{

[Header("Idle Animation")]
[SerializeField] private float rotationSpeed = 60f;
    [Header("Pickup")]
    [SerializeField] private float pickupRadius = 1f;
    [SerializeField] private float pickupLockDuration = 0.6f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Carry")]
    [SerializeField] private Vector3 carryOffset =
        new Vector3(0f, 1.8f, 0f);
        

        [Header("Audio")]
[SerializeField] private SFXID pickupSFX = SFXID.RelicPickup;
[SerializeField] private SFXID dropSFX = SFXID.RelicDrop;

[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
private void RPC_PlayRelicSFX(SFXID sfxID, Vector3 position)
{
    if (AudioManager.Instance == null)
        return;

    AudioManager.Instance.PlaySFXAtPosition(sfxID, position);
}

    [Networked]
    private PlayerRef Holder { get; set; }

    [Networked]
    private TickTimer PickupLockTimer { get; set; }

    private PlayerMovement _holderMovement;
    private PlayerBumpAttack _holderBump;

    private Vector3 _startingPosition;
private Quaternion _startingRotation;
private Rigidbody _rb;
    private readonly Collider[] _pickupHits =
        new Collider[16];

    public bool IsHeld =>
        Holder != PlayerRef.None;

    public PlayerRef CurrentHolder =>
        Holder;

public override void Spawned()
{
    _rb = GetComponent<Rigidbody>();

    if (!HasStateAuthority)
        return;

   _startingPosition = transform.position;
_startingRotation = transform.rotation;


    Holder = PlayerRef.None;
    PickupLockTimer = TickTimer.None;

    SetCarriedPhysics(false);
}
private void UpdateIdleAnimation()
{
    if (_rb == null || _rb.isKinematic)
        return;

    Quaternion rotation = Quaternion.Euler(
        0f,
        rotationSpeed * Runner.DeltaTime,
        0f
    );

    _rb.MoveRotation(_rb.rotation * rotation);
}
private void SetCarriedPhysics(bool carried)
{
    if (_rb == null)
        return;

    _rb.isKinematic = carried;

    if (carried)
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }
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

    UpdateIdleAnimation();

    if (!PickupLockTimer.ExpiredOrNotRunning(Runner))
        return;

    TryFindPlayerToPickup();
}
public void ResetRelic()
{
    if (!HasStateAuthority)
        return;

    // Restore bump attack before clearing the holder reference.
    if (_holderBump != null)
        _holderBump.SetObjectiveBumpBlocked(false);

    UnsubscribeFromHolder(_holderMovement);

  Holder = PlayerRef.None;

SetCarriedPhysics(false);

    _holderMovement = null;
    _holderBump = null;
transform.SetPositionAndRotation(
    _startingPosition,
    _startingRotation
);

if (_rb != null)
{
    _rb.linearVelocity = Vector3.zero;
    _rb.angularVelocity = Vector3.zero;
}


    PickupLockTimer = TickTimer.CreateFromSeconds(
        Runner,
        pickupLockDuration
    );

  
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

SetCarriedPhysics(true);

_holderMovement =
    playerObject.GetComponent<PlayerMovement>();

_holderBump =
    playerObject.GetComponent<PlayerBumpAttack>();

SubscribeToHolder(_holderMovement);

if (_holderBump != null)
{
    _holderBump.SetObjectiveBumpBlocked(true);
}

UpdateHeldPosition();

RPC_PlayRelicSFX(pickupSFX, transform.position);

    
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

    if (_holderBump != null)
{
    _holderBump.SetObjectiveBumpBlocked(false);
}

   Holder = PlayerRef.None;

SetCarriedPhysics(false);

if (_rb != null)
{
    _rb.linearVelocity = Vector3.zero;
    _rb.angularVelocity = Vector3.zero;
}

RPC_PlayRelicSFX(dropSFX, transform.position);

UnsubscribeFromHolder(_holderMovement);
        _holderMovement = null;
        _holderBump = null;

        PickupLockTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                pickupLockDuration
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