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
    [SerializeField] private Vector3 carryOffset = new Vector3(0f, 1.8f, 0f);

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

    [Networked] private PlayerRef Holder { get; set; }
    [Networked] private TickTimer PickupLockTimer { get; set; }
    [Networked] private TickTimer CarrierSpeedBoostTimer { get; set; }
    [Networked] private NetworkBool CarrierSpeedBoostActive { get; set; }

    public bool IsHeld => Holder != PlayerRef.None;
    public PlayerRef CurrentHolder => Holder;

    // Authority-side holder cache (resolved once on pickup).
    private PlayerMovement _holderMovement;
    private PlayerBumpAttack _holderBump;
    private NetworkObject _holderObject;
    private Transform _holderTransform;

    // Render-side cache (resolved once per holder change, not every frame).
    private PlayerRef _renderHolder = PlayerRef.None;
    private NetworkObject _renderPlayerObject;
    private InvisibilityAbility _renderInvisibility;
    private bool _renderHolderIsLocal;

    private Vector3 _startingPosition;
    private Quaternion _startingRotation;
    private Rigidbody _rb;
    private Transform _transform;

    private bool _lastHiddenState;
    private bool _hasInitializedVisualState;
    private bool _hasSpawned;

    private readonly Collider[] _pickupHits = new Collider[8];

    private void Awake()
    {
        _transform = transform;
    }

    public override void Spawned()
    {
        _rb = GetComponent<Rigidbody>();
        _hasSpawned = true;

        if (!HasStateAuthority)
            return;

        _startingPosition = _transform.position;
        _startingRotation = _transform.rotation;

        Holder = PlayerRef.None;
        PickupLockTimer = TickTimer.None;

        SetCarriedPhysics(false);
    }

    private void Update()
    {
        if (!_hasSpawned || modelRoot == null || IsHeld)
            return;

        modelRoot.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.Self);
    }

    // ---------------------------------
    // Network tick (authority only)
    // ---------------------------------

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (CarrierSpeedBoostActive && CarrierSpeedBoostTimer.Expired(Runner))
            StopCarrierSpeedBoost();

        if (Holder != PlayerRef.None)
        {
            UpdateHeldPosition();
            return;
        }

        if (!PickupLockTimer.ExpiredOrNotRunning(Runner))
            return;

        TryFindPlayerToPickup();
    }

    private void TryFindPlayerToPickup()
    {
        int hitCount = Runner.GetPhysicsScene().OverlapSphere(
            _transform.position,
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

            Pickup(movement);
            return;
        }
    }

    private void Pickup(PlayerMovement movement)
    {
        if (Holder != PlayerRef.None)
            return;

        // Resolve everything once; reused every tick while held.
        _holderObject = movement.Object;
        _holderTransform = movement.transform;
        _holderMovement = movement;
        _holderBump = movement.GetComponent<PlayerBumpAttack>();

        Holder = _holderObject.InputAuthority;

        SetCarriedPhysics(true);

        // A new holder starts without any chaser collectible stacks.
        movement.ResetCollectibleSpeedBoost();
        movement.SetCarrierSpeedMultiplier(carrierSpeedMultiplier);

        CarrierSpeedBoostActive = true;
        CarrierSpeedBoostTimer = TickTimer.CreateFromSeconds(
            Runner,
            carrierSpeedBoostDuration
        );

        movement.OnHitReceived += HandleHolderHit;

        if (_holderBump != null)
            _holderBump.SetObjectiveBumpBlocked(true);

        UpdateHeldPosition();

        RPC_PlayRelicSFX(pickupSFX, _transform.position);
    }

    private void UpdateHeldPosition()
    {
        // Holder left or was despawned.
        if (_holderObject == null || !_holderObject.IsValid || _holderTransform == null)
        {
            Drop();
            return;
        }

        _transform.SetPositionAndRotation(
            _holderTransform.position + carryOffset,
            _transform.rotation
        );
    }

    private void HandleHolderHit()
    {
        if (HasStateAuthority)
            Drop();
    }

    public void Drop()
    {
        if (!HasStateAuthority || Holder == PlayerRef.None)
            return;

        ReleaseHolder(playDropSfx: true);
    }

    public void ResetRelic()
    {
        if (!HasStateAuthority)
            return;

        ReleaseHolder(playDropSfx: false);

        _transform.SetPositionAndRotation(_startingPosition, _startingRotation);
        ZeroVelocity();
    }

    /// <summary>Shared cleanup for Drop and ResetRelic.</summary>
    private void ReleaseHolder(bool playDropSfx)
    {
        if (_holderBump != null)
            _holderBump.SetObjectiveBumpBlocked(false);

        if (_holderMovement != null)
        {
            _holderMovement.OnHitReceived -= HandleHolderHit;

            // The former holder loses all collectible speed stacks.
            if (_holderMovement.HasStateAuthority)
                _holderMovement.ResetCollectibleSpeedBoost();
        }

        // Uses _holderMovement, so it must run before the refs are cleared.
        StopCarrierSpeedBoost();

        Holder = PlayerRef.None;

        SetCarriedPhysics(false);
        ZeroVelocity();

        if (playDropSfx)
            RPC_PlayRelicSFX(dropSFX, _transform.position);

        _holderMovement = null;
        _holderBump = null;
        _holderObject = null;
        _holderTransform = null;

        PickupLockTimer = TickTimer.CreateFromSeconds(Runner, pickupLockDuration);
    }

    private void StopCarrierSpeedBoost()
    {
        CarrierSpeedBoostActive = false;
        CarrierSpeedBoostTimer = TickTimer.None;

        if (_holderMovement != null && _holderMovement.HasStateAuthority)
            _holderMovement.SetCarrierSpeedMultiplier(1f);
    }

    private void SetCarriedPhysics(bool carried)
    {
        if (_rb == null)
            return;

        if (carried && !_rb.isKinematic)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        _rb.isKinematic = carried;
    }

    private void ZeroVelocity()
    {
        if (_rb == null || _rb.isKinematic)
            return;

        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    // ---------------------------------
    // Rendering (all clients)
    // ---------------------------------

    public override void Render()
    {
        if (Object == null || Runner == null)
            return;

        bool shouldHide = false;
        PlayerRef holder = Holder;

        if (holder == PlayerRef.None)
        {
            _renderHolder = PlayerRef.None;
            _renderPlayerObject = null;
            _renderInvisibility = null;
        }
        else
        {
            // Only do lookups when the holder changes or the object died.
            if (holder != _renderHolder || _renderPlayerObject == null)
                ResolveRenderHolder(holder);

            if (_renderInvisibility != null)
                shouldHide = _renderInvisibility.IsInvisible && !_renderHolderIsLocal;
        }

        if (_hasInitializedVisualState && shouldHide == _lastHiddenState)
            return;

        _hasInitializedVisualState = true;
        _lastHiddenState = shouldHide;

        SetModelVisible(!shouldHide);
    }

    private void ResolveRenderHolder(PlayerRef holder)
    {
        _renderHolder = holder;
        _renderPlayerObject = null;
        _renderInvisibility = null;
        _renderHolderIsLocal = false;

        if (!Runner.TryGetPlayerObject(holder, out NetworkObject playerObject) ||
            playerObject == null)
        {
            return;
        }

        _renderPlayerObject = playerObject;
        _renderInvisibility = playerObject.GetComponent<InvisibilityAbility>();
        _renderHolderIsLocal = playerObject.InputAuthority == Runner.LocalPlayer;
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayRelicSFX(SFXID sfxID, Vector3 position)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAtPosition(sfxID, position);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}