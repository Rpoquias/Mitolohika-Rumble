
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

    private readonly HashSet<NetworkObject> _activePickups = new();
    private readonly List<TickTimer> _respawnTimers = new();
private bool _roundCollectiblesActive;
    public override void FixedUpdateNetwork()
    {
           if (!HasStateAuthority || Runner == null)
        return;

    if (!_roundCollectiblesActive)
        return;

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
    SpawnInitialPickups();

    Debug.Log("[Relic Boost] Started collectibles for the round.");
}
public void ClearCollectibles()
{
    if (!HasStateAuthority || Runner == null)
        return;

    _roundCollectiblesActive = false;
    // Reset collectible stacks for all active players.
    foreach (PlayerRef player in Runner.ActivePlayers)
    {
        if (!Runner.TryGetPlayerObject(
                player,
                out NetworkObject playerObject))
        {
            continue;
        }

        PlayerMovement movement =
            playerObject.GetComponent<PlayerMovement>();

        if (movement != null && movement.HasStateAuthority)
        {
            movement.ResetCollectibleSpeedBoost();
        }
    }

    // Despawn all active pickups.
    foreach (NetworkObject pickup in _activePickups)
    {
        if (pickup != null && pickup.IsValid)
        {
            Runner.Despawn(pickup);
        }
    }

    _activePickups.Clear();

    // Cancel pending pickup respawns.
    _respawnTimers.Clear();

    Debug.Log("[Relic Boost] Cleared pickups, stacks, and respawn timers.");
}

    public bool TryCollect(PlayerMovement movement)
    {
        if (!HasStateAuthority ||
            movement == null ||
            !movement.HasStateAuthority)
        {
            return false;
        }

        return movement.TryAddCollectibleSpeedBoostStack();
    }

  public void HandlePickupCollected(SpeedBoostPickup pickup)
{
    if (!HasStateAuthority ||
        Runner == null ||
        !_roundCollectiblesActive ||
        pickup == null)
    {
        return;
    }

    NetworkObject pickupObject = pickup.Object;

    if (pickupObject != null)
    {
        _activePickups.Remove(pickupObject);
        Runner.Despawn(pickupObject);
    }

    _respawnTimers.Add(
        TickTimer.CreateFromSeconds(Runner, respawnDelay)
    );
}
  
private void SpawnInitialPickups()
{
    if (spawnPoints == null || spawnPoints.Length == 0)
    {
        Debug.LogWarning(
            "[Relic Boost] No spawn points assigned."
        );
        return;
    }

    List<Transform> availableSpawnPoints =
        new List<Transform>();

    foreach (Transform point in spawnPoints)
    {
        if (point != null)
            availableSpawnPoints.Add(point);
    }

    int count = Mathf.Min(
        initialPickupCount,
        availableSpawnPoints.Count
    );

    for (int i = 0; i < count; i++)
    {
        int randomIndex = Random.Range(
            0,
            availableSpawnPoints.Count
        );

        Transform spawnPoint =
            availableSpawnPoints[randomIndex];

        availableSpawnPoints.RemoveAt(randomIndex);

        SpawnPickupAt(spawnPoint);
    }
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
            SpeedBoostPickup pickup =
                networkObject.GetComponent<SpeedBoostPickup>();

            if (pickup != null)
                pickup.Initialize(this);
        }
    );

    if (spawnedObject != null)
        _activePickups.Add(spawnedObject);
}


    private void SpawnPickup()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return;

        Transform spawnPoint = spawnPoints[
            Random.Range(0, spawnPoints.Length)
        ];

        if (spawnPoint == null)
            return;

        NetworkObject spawnedObject = Runner.Spawn(
            pickupPrefab,
            spawnPoint.position,
            spawnPoint.rotation,
            PlayerRef.None,
            (runner, networkObject) =>
            {
                SpeedBoostPickup pickup =
                    networkObject.GetComponent<SpeedBoostPickup>();

                if (pickup != null)
                    pickup.Initialize(this);
            }
        );

        if (spawnedObject != null)
            _activePickups.Add(spawnedObject);
    }

    private void UpdatePlayerBoosts()
    {
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(
                    player,
                    out NetworkObject playerObject))
            {
                continue;
            }

            PlayerMovement movement =
                playerObject.GetComponent<PlayerMovement>();

            if (movement == null ||
                !movement.HasStateAuthority)
            {
                continue;
            }

            bool isHolder =
                relicController != null &&
                relicController.IsHeld &&
                relicController.CurrentHolder == player;

            movement.UpdateCollectibleSpeedBoost(isHolder);
        }
    }


public override void Despawned(
    NetworkRunner runner,
    bool hasState)
{
    // Never spawn new pickups during teardown.
    _activePickups.Clear();
    _respawnTimers.Clear();
}
}
