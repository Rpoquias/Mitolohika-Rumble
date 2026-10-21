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
    [SerializeField] private Vector3 carryOffset = new Vector3(0f, 1.8f, 0f);

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

    [Networked] private FlagState State { get; set; }
    [Networked] private PlayerRef Holder { get; set; }
    [Networked] private PlayerRef StoredOwner { get; set; }
    [Networked] private TickTimer PickupLockTimer { get; set; }
    [Networked] private TickTimer PreviousHolderLockTimer { get; set; }

    // Server-side holder references.
    private PlayerMovement _holderMovement;
    private PlayerBumpAttack _holderBump;
    private PlayerRef _previousHolder = PlayerRef.None;

    private Rigidbody _rb;
    private NetworkTransform _netTransform;

    // Render caches.
    private bool lastHiddenState;
    private bool hasInitializedVisualState;
    private PlayerRef _cachedHolder = PlayerRef.None;
    private InvisibilityAbility _cachedAbility;
    private bool _cachedHolderIsLocal;
    private bool _holderCacheValid;

    private readonly Collider[] _pickupHits = new Collider[16];

    public FlagState CurrentState => State;
    public PlayerRef CurrentHolder => Holder;
    public PlayerRef CurrentOwner => StoredOwner;

    public bool IsCarried => State == FlagState.Carried;
    public bool IsStored => State == FlagState.Stored;
    public bool IsFree => State == FlagState.Free;

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayFlagSFX(SFXID sfxID, Vector3 position)
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySFXAtPosition(sfxID, position);
    }

    public override void Spawned()
    {
        _rb = GetComponent<Rigidbody>();
        _netTransform = GetComponent<NetworkTransform>();

        hasInitializedVisualState = false;
        _holderCacheValid = false;

        if (!HasStateAuthority)
            return;

        State = FlagState.Free;
        Holder = PlayerRef.None;
        StoredOwner = PlayerRef.None;

        PickupLockTimer = TickTimer.None;
        PreviousHolderLockTimer = TickTimer.None;

        SetCarriedPhysics(false);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        UnsubscribeFromHolder();
        _holderCacheValid = false;
        _cachedAbility = null;
    }

    // Moves the flag without interpolation, so clients don't see it slide.
    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        if (_netTransform != null)
            _netTransform.Teleport(position, rotation);
        else
            transform.SetPositionAndRotation(position, rotation);
    }

    // ---------- Visuals (all peers) ----------

    public override void Render()
    {
        if (Object == null || Runner == null)
            return;

        // Idle spin (replaces the old Update()).
        if (State == FlagState.Free && modelRoot != null)
        {
            modelRoot.Rotate(
                0f,
                rotationSpeed * Time.deltaTime,
                0f,
                Space.Self
            );
        }

        bool shouldHide = false;

        if (State == FlagState.Carried && Holder != PlayerRef.None)
        {
            RefreshHolderCache();

            // Hide the flag only from other players.
            shouldHide =
                _cachedAbility != null &&
                _cachedAbility.IsInvisible &&
                !_cachedHolderIsLocal;
        }

        if (hasInitializedVisualState && shouldHide == lastHiddenState)
            return;

        hasInitializedVisualState = true;
        lastHiddenState = shouldHide;

        SetModelVisible(!shouldHide);
    }

    // Only looks the holder up again when the holder changes
    // (or the cached ability was destroyed).
    private void RefreshHolderCache()
    {
        if (_holderCacheValid &&
            _cachedHolder == Holder &&
            _cachedAbility != null)
        {
            return;
        }

        _cachedHolder = Holder;
        _cachedAbility = null;
        _cachedHolderIsLocal = false;
        _holderCacheValid = false;

        if (!Runner.TryGetPlayerObject(Holder, out NetworkObject playerObject) ||
            playerObject == null)
        {
            return;
        }

        _cachedAbility = playerObject.GetComponent<InvisibilityAbility>();
        _cachedHolderIsLocal = playerObject.InputAuthority == Runner.LocalPlayer;
        _holderCacheValid = true;
    }

    private void SetModelVisible(bool visible)
    {
        if (modelRenderers != null)
        {
            for (int i = 0; i < modelRenderers.Length; i++)
            {
                if (modelRenderers[i] != null)
                    modelRenderers[i].enabled = visible;
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

    // ---------- Server logic ----------

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
        int hitCount = Runner.GetPhysicsScene().OverlapSphere(
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

            PlayerRef player = playerObject.InputAuthority;

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

        return player == _previousHolder;
    }

    // Caller (TryFindPlayerToPickup) already checked IsCarryingAnotherFlag.
    private void Pickup(PlayerRef player)
    {
        if (State != FlagState.Free)
            return;

        if (!Runner.TryGetPlayerObject(player, out NetworkObject playerObject))
            return;

        StoredOwner = PlayerRef.None;
        AttachToHolder(player, playerObject);
    }

    private void AttachToHolder(PlayerRef player, NetworkObject playerObject)
    {
        State = FlagState.Carried;
        Holder = player;

        SetCarriedPhysics(true);

        _holderMovement = playerObject.GetComponent<PlayerMovement>();
        _holderBump = playerObject.GetComponent<PlayerBumpAttack>();

        SubscribeToHolder(_holderMovement);
        SetHolderBumpBlocked(true);

        UpdateCarriedPosition();

        RPC_PlayFlagSFX(pickupSFX, transform.position);
    }

    private void UpdateCarriedPosition()
    {
        if (Holder == PlayerRef.None ||
            !Runner.TryGetPlayerObject(Holder, out NetworkObject playerObject) ||
            playerObject == null)
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
        if (!HasStateAuthority || State != FlagState.Carried)
            return;

        PlayerRef previousHolder = Holder;

        SetHolderBumpBlocked(false);
        UnsubscribeFromHolder();

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

        PickupLockTimer =
            TickTimer.CreateFromSeconds(Runner, pickupLockDuration);

        PreviousHolderLockTimer =
            TickTimer.CreateFromSeconds(Runner, previousHolderLockDuration);

        RPC_PlayFlagSFX(dropSFX, transform.position);
    }

    private void HandleHolderHit()
    {
        if (!HasStateAuthority)
            return;

        Drop();
    }

    private void SubscribeToHolder(PlayerMovement movement)
    {
        if (movement != null)
            movement.OnHitReceived += HandleHolderHit;
    }

    private void UnsubscribeFromHolder()
    {
        if (_holderMovement != null)
            _holderMovement.OnHitReceived -= HandleHolderHit;
    }

    // Returns true only if the flag was actually stored.
    public bool StoreAtBase(PlayerRef player)
    {
        if (!HasStateAuthority ||
            State != FlagState.Carried ||
            Holder != player)
        {
            return false;
        }

        SetHolderBumpBlocked(false);
        UnsubscribeFromHolder();

        Holder = PlayerRef.None;
        _holderMovement = null;
        _holderBump = null;

        State = FlagState.Stored;
        StoredOwner = player;

        SetCarriedPhysics(true);

        PickupLockTimer = TickTimer.None;
        PreviousHolderLockTimer = TickTimer.None;

        RPC_PlayFlagSFX(storedSFX, transform.position);

        return true;
    }

    private bool IsCarryingAnotherFlag(PlayerRef player)
    {
        return flagManager != null &&
               flagManager.IsPlayerCarryingFlag(player);
    }

    // Returns true only if the thief really took the flag.
    // The base must not remove the flag from its list when this is false.
    public bool StealFromBase(PlayerRef player)
    {
        if (!HasStateAuthority ||
            State != FlagState.Stored ||
            StoredOwner == player)
        {
            return false;
        }

        if (IsCarryingAnotherFlag(player))
            return false;

        if (!Runner.TryGetPlayerObject(player, out NetworkObject playerObject))
            return false;

        StoredOwner = PlayerRef.None;
        AttachToHolder(player, playerObject);

        return true;
    }

    private void SetHolderBumpBlocked(bool blocked)
    {
        if (_holderBump != null)
            _holderBump.SetObjectiveBumpBlocked(blocked);
    }

    public void ResetFlag(Vector3 position, Quaternion rotation)
    {
        if (!HasStateAuthority)
            return;

        SetHolderBumpBlocked(false);
        UnsubscribeFromHolder();

        State = FlagState.Free;
        Holder = PlayerRef.None;
        StoredOwner = PlayerRef.None;

        SetCarriedPhysics(false);

        _holderMovement = null;
        _holderBump = null;
        _previousHolder = PlayerRef.None;

        PickupLockTimer =
            TickTimer.CreateFromSeconds(Runner, pickupLockDuration);

        PreviousHolderLockTimer = TickTimer.None;

        TeleportTo(position, rotation);

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
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}