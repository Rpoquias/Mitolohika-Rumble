    using System.Collections.Generic;
    using Fusion;
    using UnityEngine;

    public class BandilaBaseController : NetworkBehaviour
    {
        [Header("Flag Storage")]
       [SerializeField] private Transform[] flagStorageSlots;
        [SerializeField] private float storageRadius = 2f;
        [SerializeField] private LayerMask flagLayer;
        [SerializeField] private LayerMask playerLayer;
        [Header("Stealing")]
        [SerializeField] private float stealRadius = 1.5f;

        

        [Networked]
        private PlayerRef Owner { get; set; }

        public PlayerRef CurrentOwner => Owner;

        public int StoredFlagCount =>
            storedFlags.Count;

        private readonly Collider[] _flagHits =
            new Collider[16];

        private readonly List<BandilaController> storedFlags =
            new List<BandilaController>();

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

            Debug.Log(
                $"[BANDILA BASE] Base assigned to {player}."
            );
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (Owner == PlayerRef.None)
                return;

            DetectCarriedFlags();
            DetectStoredFlagSteal();
        }

    private void DetectCarriedFlags()
    {
        int hitCount =
            Runner.GetPhysicsScene().OverlapSphere(
                transform.position,
                storageRadius,
                _flagHits,
                flagLayer,
                QueryTriggerInteraction.Ignore
            );

        if (hitCount > 0)
        {
            Debug.Log(
                $"[BANDILA BASE] {Owner} detected " +
                $"{hitCount} collider(s) near base."
            );
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _flagHits[i];

            if (hit == null)
                continue;

            Debug.Log(
                $"[BANDILA BASE] Detected collider: " +
                $"{hit.name} | Layer: {LayerMask.LayerToName(hit.gameObject.layer)}"
            );

            BandilaController flag =
                hit.GetComponentInParent<BandilaController>();

            if (flag == null)
            {
                Debug.Log(
                    "[BANDILA BASE] Collider does not belong " +
                    "to a BandilaController."
                );

                continue;
            }

            Debug.Log(
                $"[BANDILA BASE] Found flag. " +
                $"State: {flag.CurrentState} | " +
                $"Holder: {flag.CurrentHolder}"
            );

            if (!flag.IsCarried)
                continue;

            PlayerRef holder =
                flag.CurrentHolder;

            if (holder == PlayerRef.None)
                continue;

            if (holder != Owner)
                continue;

            Debug.Log(
                $"[BANDILA BASE] Valid carried flag detected " +
                $"for owner {Owner}. Storing..."
            );

            StoreFlag(flag);
        }
    }

        private void DetectStoredFlagSteal()
        {
            if (storedFlags.Count == 0)
                return;

            int hitCount =
                Runner.GetPhysicsScene().OverlapSphere(
                    transform.position,
                    stealRadius,
                    _flagHits,
                    flagLayer,
                    QueryTriggerInteraction.Ignore
                );

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _flagHits[i];

                if (hit == null)
                    continue;

                BandilaController flag =
                    hit.GetComponentInParent<BandilaController>();

                if (flag == null)
                    continue;

                if (!flag.IsStored)
                    continue;

                if (flag.CurrentOwner != Owner)
                    continue;

                PlayerRef thief =
                    FindPlayerInsideBase();

                if (thief == PlayerRef.None)
                    continue;

                if (thief == Owner)
                    continue;

                StealFlag(flag, thief);

                return;
            }
        }

        private PlayerRef FindPlayerInsideBase()
        {
            int hitCount =
                Runner.GetPhysicsScene().OverlapSphere(
                    transform.position,
                    stealRadius,
                    _flagHits,
                    playerLayer,
                    QueryTriggerInteraction.Ignore
                );

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _flagHits[i];

                if (hit == null)
                    continue;

                PlayerMovement movement =
                    hit.GetComponentInParent<PlayerMovement>();

                if (movement == null)
                    continue;

                NetworkObject playerObject =
                    movement.GetComponent<NetworkObject>();

                if (playerObject == null)
                    continue;

                PlayerRef player =
                    playerObject.InputAuthority;

                if (player != Owner)
                    return player;
            }

            return PlayerRef.None;
        }

        private void StoreFlag(
            BandilaController flag)
        {
            if (flag == null)
                return;

            if (storedFlags.Contains(flag))
                return;

            flag.StoreAtBase(Owner);

            storedFlags.Add(flag);

            MoveFlagToStorage(flag);

            Debug.Log(
                $"[BANDILA BASE] {Owner} stored a flag. " +
                $"Total: {storedFlags.Count}"
            );
        }
private void StealFlag(
    BandilaController flag,
    PlayerRef thief)
{
    if (flag == null)
        return;

    if (!storedFlags.Contains(flag))
        return;

    storedFlags.Remove(flag);

    flag.transform.SetParent(null);

    flag.StealFromBase(thief);

    Debug.Log(
        $"[BANDILA BASE] {thief} stole a flag " +
        $"from {Owner}. " +
        $"Remaining: {storedFlags.Count}"
    );
}

  private void MoveFlagToStorage(
    BandilaController flag)
{
    if (flag == null)
        return;

    int slotIndex =
        storedFlags.IndexOf(flag);

    if (slotIndex < 0)
        return;

    if (flagStorageSlots == null ||
        flagStorageSlots.Length == 0)
        return;

    slotIndex =
        Mathf.Min(
            slotIndex,
            flagStorageSlots.Length - 1
        );

    Transform slot =
        flagStorageSlots[slotIndex];

    if (slot == null)
        return;

    flag.transform.SetPositionAndRotation(
        slot.position,
        slot.rotation
    );
}

        public void ResetBase()
        {
            storedFlags.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(
                transform.position,
                storageRadius
            );

            Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(
                transform.position,
                stealRadius
            );
        }
    }