using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class BandilaBaseController : NetworkBehaviour
{
    [Header("Flag Storage")]
    [SerializeField] private Transform[] flagStorageSlots;
    [SerializeField] private float storageRadius = 2f;

    [Header("Stealing")]
    [SerializeField] private float stealRadius = 1.5f;
    [SerializeField] private LayerMask playerLayer;

    [Networked]
    private PlayerRef Owner { get; set; }

    public PlayerRef CurrentOwner => Owner;
    public int StoredFlagCount => storedFlags.Count;

    // Raised (server only) whenever the stored flag list changes.
    public event Action<PlayerRef> StoredFlagsChanged;

    private AgawanBandilaManager _manager;

    // Only used for player queries now.
    private readonly Collider[] _playerHits = new Collider[16];

    // Last in, first out. List index == slot index, so slots stay compact.
    private readonly List<BandilaController> storedFlags =
        new List<BandilaController>();

    public void Bind(AgawanBandilaManager manager)
    {
        _manager = manager;
    }

    public override void Spawned()
    {
        if (!HasStateAuthority)
            return;

        Owner = PlayerRef.None;
        storedFlags.Clear();
    }

    public void SetOwner(PlayerRef player)
    {
        if (!HasStateAuthority)
            return;

        Owner = player;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || Owner == PlayerRef.None)
            return;

        TryStoreCarriedFlag();

        if (storedFlags.Count > 0)
            TryStealStoredFlag();
    }

    // No physics query: the owner can only carry one flag, so just
    // check how far that flag is from the base.
    private void TryStoreCarriedFlag()
    {
        if (_manager == null)
            return;

        if (!_manager.TryGetCarriedFlag(Owner, out BandilaController flag))
            return;

        float sqrDistance =
            (flag.transform.position - transform.position).sqrMagnitude;

        if (sqrDistance > storageRadius * storageRadius)
            return;

        if (storedFlags.Contains(flag))
            return;

        // Only add it if the flag really accepted the store.
        if (!flag.StoreAtBase(Owner))
            return;

        storedFlags.Add(flag);
        MoveFlagToStorage(flag, storedFlags.Count - 1);
        StoredFlagsChanged?.Invoke(Owner);
    }

    private void TryStealStoredFlag()
    {
        PlayerRef thief = FindThief();

        if (thief == PlayerRef.None)
            return;

        int last = storedFlags.Count - 1;
        BandilaController flag = storedFlags[last];

        // Flag object was destroyed: drop the dead entry.
        if (flag == null)
        {
            storedFlags.RemoveAt(last);
            StoredFlagsChanged?.Invoke(Owner);
            return;
        }

        // Only remove it from the list if the steal actually happened.
        if (!flag.StealFromBase(thief))
            return;

        storedFlags.RemoveAt(last);
        flag.transform.SetParent(null);
        StoredFlagsChanged?.Invoke(Owner);
    }

    // One player query, and it skips players who can't take a flag.
    private PlayerRef FindThief()
    {
        int hitCount = Runner.GetPhysicsScene().OverlapSphere(
            transform.position,
            stealRadius,
            _playerHits,
            playerLayer,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _playerHits[i];

            if (hit == null)
                continue;

            PlayerMovement movement =
                hit.GetComponentInParent<PlayerMovement>();

            if (movement == null)
                continue;

            // Stunned players can't steal.
            if (movement.IsStunned)
                continue;

            NetworkObject playerObject =
                movement.GetComponent<NetworkObject>();

            if (playerObject == null)
                continue;

            PlayerRef player = playerObject.InputAuthority;

            if (player == Owner || player == PlayerRef.None)
                continue;

            // Already carrying a flag: can't steal, try the next player.
            if (_manager != null && _manager.IsPlayerCarryingFlag(player))
                continue;

            return player;
        }

        return PlayerRef.None;
    }

    private void MoveFlagToStorage(BandilaController flag, int slotIndex)
    {
        if (flag == null ||
            flagStorageSlots == null ||
            flagStorageSlots.Length == 0)
        {
            return;
        }

        slotIndex = Mathf.Clamp(slotIndex, 0, flagStorageSlots.Length - 1);

        Transform slot = flagStorageSlots[slotIndex];

        if (slot == null)
            return;

        flag.TeleportTo(slot.position, slot.rotation);
    }

    public void ResetBase()
    {
        storedFlags.Clear();

        // Clear the old owner so a player who left doesn't leave a ghost base.
        if (HasStateAuthority)
            Owner = PlayerRef.None;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, storageRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stealRadius);
    }
}