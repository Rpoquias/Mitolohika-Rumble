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


[Header("Visuals & Invisibility")]
[SerializeField] private Renderer[] modelRenderers;
[SerializeField] private GameObject valuableVFX;
[SerializeField] private Target targetIndicator;
[SerializeField] private Transform modelRoot;


[Header("Carrier Speed Boost")]
[SerializeField, Min(1f)]
private float carrierSpeedMultiplier = 1.25f;

[SerializeField, Min(0.1f)]
private float carrierSpeedBoostDuration = 4f;

[Networked]
private TickTimer CarrierSpeedBoostTimer { get; set; }

[Networked]
private NetworkBool CarrierSpeedBoostActive { get; set; }

private bool lastHiddenState;
private bool hasInitializedVisualState;
private bool _hasSpawned;

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


private void Update()
{
    if (!_hasSpawned)
        return;

    if (IsHeld || modelRoot == null)
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

    _startingPosition = transform.position;
    _startingRotation = transform.rotation;

    Holder = PlayerRef.None;
    PickupLockTimer = TickTimer.None;

    SetCarriedPhysics(false);
}


private void StopCarrierSpeedBoost()
{
    CarrierSpeedBoostActive = false;
    CarrierSpeedBoostTimer = TickTimer.None;

    if (_holderMovement != null &&
        _holderMovement.HasStateAuthority)
    {
      _holderMovement.SetCarrierSpeedMultiplier(1f);
    }
}

public override void Render()
{
    if (Object == null || Runner == null)
        return;

    bool shouldHide = false;

    if (IsHeld &&
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

        // Hide the relic only from other players.
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

    if (CarrierSpeedBoostActive &&
        CarrierSpeedBoostTimer.Expired(Runner))
    {
        StopCarrierSpeedBoost();
    }

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

  // Restore bump attack before clearing the holder reference.
if (_holderBump != null)
    _holderBump.SetObjectiveBumpBlocked(false);

// Clear the old holder's collectible stacks.
if (_holderMovement != null &&
    _holderMovement.HasStateAuthority)
{
    _holderMovement.ResetCollectibleSpeedBoost();
}

UnsubscribeFromHolder(_holderMovement);
StopCarrierSpeedBoost();

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

if (_holderMovement != null)
{
    // A new holder starts without any chaser collectible stacks.
    _holderMovement.ResetCollectibleSpeedBoost();

    // Apply the existing temporary relic carrier boost.
    _holderMovement.SetCarrierSpeedMultiplier(
        carrierSpeedMultiplier
    );

    CarrierSpeedBoostActive = true;

    CarrierSpeedBoostTimer =
        TickTimer.CreateFromSeconds(
            Runner,
            carrierSpeedBoostDuration
        );
}

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

    if (_holderBump != null)
{
    _holderBump.SetObjectiveBumpBlocked(false);
}
// The former holder loses all collectible speed stacks.
if (_holderMovement != null &&
    _holderMovement.HasStateAuthority)
{
    _holderMovement.ResetCollectibleSpeedBoost();
}

// Remove the temporary relic carrier boost.
StopCarrierSpeedBoost();

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