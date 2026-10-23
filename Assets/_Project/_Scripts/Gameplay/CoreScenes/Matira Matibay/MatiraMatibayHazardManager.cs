
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class MatiraMatibayHazardManager : NetworkBehaviour
{
    [Header("Bomb Spawning")]
    [SerializeField] private NetworkObject bombPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float firstSpawnDelay = 8f;
    [SerializeField] private float spawnIntervalMin = 8f;
    [SerializeField] private float spawnIntervalMax = 14f;
    [SerializeField] private int maxActiveBombs = 2;

    [Header("Snare Spawning")]
    [SerializeField] private NetworkObject snarePrefab;
    [SerializeField] private Transform[] snareSpawnPoints;
    [SerializeField] private float firstSnareDelay = 12f;
    [SerializeField] private float snareIntervalMin = 10f;
    [SerializeField] private float snareIntervalMax = 16f;
    [SerializeField] private int maxActiveSnares = 2;

    [Networked]
    private NetworkBool IsSpawning { get; set; }

    [Networked]
    private TickTimer SpawnTimer { get; set; }

    [Networked]
    private NetworkBool IsSnareSpawning { get; set; }

    [Networked]
    private TickTimer SnareSpawnTimer { get; set; }

    // State Authority tracks currently spawned hazards.
    private readonly HashSet<NetworkObject> activeBombs = new();
    private readonly HashSet<NetworkObject> activeSnares = new();

    // Reusable buffers prevent collection modification during despawning
    // and avoid allocating new lists whenever a round ends.
    private readonly List<NetworkObject> bombsToDespawn = new(2);
    private readonly List<NetworkObject> snaresToDespawn = new(2);

    public void StartHazards()
    {
        if (!HasStateAuthority)
            return;

        if (bombPrefab != null && !IsSpawning)
        {
            IsSpawning = true;
            SpawnTimer = TickTimer.CreateFromSeconds(
                Runner,
                Mathf.Max(0f, firstSpawnDelay));
        }

        if (snarePrefab != null && !IsSnareSpawning)
        {
            IsSnareSpawning = true;
            SnareSpawnTimer = TickTimer.CreateFromSeconds(
                Runner,
                Mathf.Max(0f, firstSnareDelay));
        }
    }

    public void StopHazards()
    {
        if (!HasStateAuthority)
            return;

        // Stop future spawning.
        IsSpawning = false;
        SpawnTimer = TickTimer.None;

        IsSnareSpawning = false;
        SnareSpawnTimer = TickTimer.None;

        // Copy first because Despawned callbacks remove entries
        // from the active collections.
        bombsToDespawn.Clear();
        bombsToDespawn.AddRange(activeBombs);

        foreach (NetworkObject bomb in bombsToDespawn)
        {
            if (bomb != null && bomb.IsValid)
                Runner.Despawn(bomb);
        }

        bombsToDespawn.Clear();
        activeBombs.Clear();

        snaresToDespawn.Clear();
        snaresToDespawn.AddRange(activeSnares);

        foreach (NetworkObject snare in snaresToDespawn)
        {
            if (snare != null && snare.IsValid)
                Runner.Despawn(snare);
        }

        snaresToDespawn.Clear();
        activeSnares.Clear();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (IsSpawning && SpawnTimer.Expired(Runner))
        {
            if (activeBombs.Count < Mathf.Max(1, maxActiveBombs))
                TrySpawnBomb();

            float min = Mathf.Max(0.1f, spawnIntervalMin);
            float max = Mathf.Max(min, spawnIntervalMax);

            SpawnTimer = TickTimer.CreateFromSeconds(
                Runner,
                Random.Range(min, max));
        }

        if (IsSnareSpawning && SnareSpawnTimer.Expired(Runner))
        {
            if (activeSnares.Count < Mathf.Max(1, maxActiveSnares))
                TrySpawnSnare();

            float min = Mathf.Max(0.1f, snareIntervalMin);
            float max = Mathf.Max(min, snareIntervalMax);

            SnareSpawnTimer = TickTimer.CreateFromSeconds(
                Runner,
                Random.Range(min, max));
        }
    }

    private void TrySpawnBomb()
    {
        if (bombPrefab == null ||
            spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            Debug.LogWarning(
                "[Hazards] Bomb prefab or spawn points are missing.",
                this);
            return;
        }

        int startIndex = Random.Range(0, spawnPoints.Length);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform point =
                spawnPoints[(startIndex + i) % spawnPoints.Length];

            if (point == null)
                continue;

            NetworkObject bomb = Runner.Spawn(
                bombPrefab,
                point.position,
                point.rotation,
                onBeforeSpawned: (runner, obj) =>
                {
                    BombHazard hazard = obj.GetComponent<BombHazard>();

                    if (hazard != null)
                        hazard.SetOwnerManager(this);
                });

            if (bomb == null)
                continue;

            activeBombs.Add(bomb);
            return;
        }

        Debug.LogWarning(
            "[Hazards] No valid bomb spawn point found.",
            this);
    }

    private void TrySpawnSnare()
    {
        if (snarePrefab == null ||
            snareSpawnPoints == null ||
            snareSpawnPoints.Length == 0)
        {
            Debug.LogWarning(
                "[Hazards] Snare prefab or spawn points are missing.",
                this);
            return;
        }

        int startIndex = Random.Range(0, snareSpawnPoints.Length);

        for (int i = 0; i < snareSpawnPoints.Length; i++)
        {
            Transform point =
                snareSpawnPoints[
                    (startIndex + i) % snareSpawnPoints.Length];

            if (point == null)
                continue;

            NetworkObject snare = Runner.Spawn(
                snarePrefab,
                point.position,
                point.rotation,
                onBeforeSpawned: (runner, obj) =>
                {
                    SnareHazard hazard = obj.GetComponent<SnareHazard>();

                    if (hazard != null)
                        hazard.SetOwnerManager(this);
                });

            if (snare == null)
                continue;

            activeSnares.Add(snare);
            return;
        }

        Debug.LogWarning(
            "[Hazards] No valid snare spawn point found.",
            this);
    }

    public void NotifyBombDespawned(NetworkObject bomb)
    {
        if (!HasStateAuthority)
            return;

        activeBombs.Remove(bomb);
    }

    public void NotifySnareDespawned(NetworkObject snare)
    {
        if (!HasStateAuthority)
            return;

        activeSnares.Remove(snare);
    }
}
