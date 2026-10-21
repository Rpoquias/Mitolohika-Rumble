using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class AgawanRelicBoostManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private RelicController relicController;
    [SerializeField] private NetworkPrefabRef pickupPrefab;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Spawn Settings")]
    [SerializeField, Min(1)]
    private int initialPickupCount = 5;

    [SerializeField, Min(0.1f)]
    private float respawnDelay = 5f;

    [Header("Optimization")]
    [Tooltip("How often (in ticks) the player cache is re-scanned for joins/leaves. " +
             "Invalid entries force an immediate re-scan regardless.")]
    [SerializeField, Min(1)]
    private int movementCacheRefreshTicks = 30;

    private readonly Dictionary<PlayerRef, PlayerMovement> _movementCache = new();
    private readonly List<PlayerRef> _playersToRemove = new();
    private readonly HashSet<NetworkObject> _activePickups = new();
    private readonly List<TickTimer> _respawnTimers = new();
    private readonly List<Transform> _spawnScratch = new();

    private bool _roundCollectiblesActive;
    private int _nextCacheRefreshTick;

    // Boost flags are only pushed to players when the holder changes or the
    // player cache changed; PlayerMovement recalculates on stack changes itself.
    private PlayerRef _lastAppliedHolder = PlayerRef.None;
    private bool _boostsDirty = true;

    public override void FixedUpdateNetwork()
    {
        if (!_roundCollectiblesActive || !HasStateAuthority || Runner == null)
            return;

        int tick = Runner.Tick.Raw;

        // Re-scanning players every tick was wasted work; joins/leaves are rare.
        if (tick >= _nextCacheRefreshTick)
        {
            RefreshMovementCache();
            _nextCacheRefreshTick = tick + movementCacheRefreshTicks;
            _boostsDirty = true;
        }

        for (int i = _respawnTimers.Count - 1; i >= 0; i--)
        {
            if (!_respawnTimers[i].Expired(Runner))
                continue;

            _respawnTimers.RemoveAt(i);
            SpawnPickup();
        }

        UpdatePlayerBoosts();
    }

    public void StartRoundCollectibles()
    {
        if (!HasStateAuthority || Runner == null)
            return;

        ClearCollectibles();

        _roundCollectiblesActive = true;
        _nextCacheRefreshTick = 0; // force a fresh scan on the first tick
        SpawnInitialPickups();
    }

    public void ClearCollectibles()
    {
        if (!HasStateAuthority || Runner == null)
            return;

        _roundCollectiblesActive = false;
        _nextCacheRefreshTick = 0;
        _lastAppliedHolder = PlayerRef.None;
        _boostsDirty = true;

        RefreshMovementCache();

        foreach (PlayerMovement movement in _movementCache.Values)
        {
            if (IsInvalid(movement) || !movement.HasStateAuthority)
                continue;

            movement.ResetCollectibleSpeedBoost();
        }

        foreach (NetworkObject pickup in _activePickups)
        {
            if (pickup != null && pickup.IsValid)
                Runner.Despawn(pickup);
        }

        _activePickups.Clear();
        _respawnTimers.Clear();
    }

    public bool TryCollect(PlayerMovement movement)
    {
        return HasStateAuthority &&
               movement != null &&
               movement.HasStateAuthority &&
               movement.TryAddCollectibleSpeedBoostStack();
    }

    public void HandlePickupCollected(SpeedBoostPickup pickup)
    {
        if (!_roundCollectiblesActive || !HasStateAuthority || Runner == null || pickup == null)
            return;

        NetworkObject pickupObject = pickup.Object;

        if (pickupObject != null && pickupObject.IsValid)
        {
            _activePickups.Remove(pickupObject);
            Runner.Despawn(pickupObject);
        }

        _respawnTimers.Add(TickTimer.CreateFromSeconds(Runner, respawnDelay));
    }

    private static bool IsInvalid(PlayerMovement movement)
    {
        return movement == null ||
               movement.Object == null ||
               !movement.Object.IsValid;
    }

    private void RefreshMovementCache()
    {
        _playersToRemove.Clear();

        foreach (var entry in _movementCache)
        {
            if (IsInvalid(entry.Value))
                _playersToRemove.Add(entry.Key);
        }

        for (int i = 0; i < _playersToRemove.Count; i++)
            _movementCache.Remove(_playersToRemove[i]);

        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (_movementCache.ContainsKey(player))
                continue;

            if (!Runner.TryGetPlayerObject(player, out NetworkObject obj) ||
                obj == null ||
                !obj.IsValid)
            {
                continue;
            }

            if (obj.TryGetComponent(out PlayerMovement movement))
                _movementCache[player] = movement;
        }
    }

    private void UpdatePlayerBoosts()
    {
        // CurrentHolder is already PlayerRef.None when nobody holds the relic.
        PlayerRef holder = relicController != null
            ? relicController.CurrentHolder
            : PlayerRef.None;

        // Nothing changed since the last push: skip the loop entirely.
        if (!_boostsDirty && holder == _lastAppliedHolder)
            return;

        _lastAppliedHolder = holder;
        _boostsDirty = false;

        bool foundInvalid = false;

        foreach (var entry in _movementCache)
        {
            PlayerMovement movement = entry.Value;

            if (IsInvalid(movement))
            {
                foundInvalid = true;
                continue;
            }

            movement.UpdateCollectibleSpeedBoost(
                holder != PlayerRef.None && entry.Key == holder
            );
        }

        // A stale entry means someone left/respawned: re-scan next tick.
        if (foundInvalid)
        {
            _nextCacheRefreshTick = 0;
            _boostsDirty = true;
        }
    }

    private void SpawnInitialPickups()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[Relic Boost] No spawn points assigned.");
            return;
        }

        _spawnScratch.Clear();

        foreach (Transform point in spawnPoints)
        {
            if (point != null)
                _spawnScratch.Add(point);
        }

        int count = Mathf.Min(initialPickupCount, _spawnScratch.Count);

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, _spawnScratch.Count);
            SpawnPickupAt(_spawnScratch[randomIndex]);
            _spawnScratch.RemoveAt(randomIndex);
        }

        _spawnScratch.Clear();
    }

    private void SpawnPickup()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return;

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

        if (point != null)
            SpawnPickupAt(point);
    }

    private void SpawnPickupAt(Transform spawnPoint)
    {
        if (spawnPoint == null || Runner == null)
            return;

        NetworkObject spawnedObject = Runner.Spawn(
            pickupPrefab,
            spawnPoint.position,
            spawnPoint.rotation,
            PlayerRef.None,
            (runner, networkObject) =>
            {
                if (networkObject.TryGetComponent(out SpeedBoostPickup pickup))
                    pickup.Initialize(this);
            }
        );

        if (spawnedObject != null)
            _activePickups.Add(spawnedObject);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _roundCollectiblesActive = false;
        _activePickups.Clear();
        _respawnTimers.Clear();
        _movementCache.Clear();
        _playersToRemove.Clear();
        _spawnScratch.Clear();
    }
}