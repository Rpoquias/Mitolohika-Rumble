
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class MatiraMatibayHazardManager : NetworkBehaviour
{
    [Header("Spawning")]
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
private NetworkBool IsSnareSpawning { get; set; }

[Networked]
private TickTimer SnareSpawnTimer { get; set; }

private readonly HashSet<NetworkObject> activeSnares = new();
    [Networked] private NetworkBool IsSpawning { get; set; }
    [Networked] private TickTimer SpawnTimer { get; set; }

    // Used by State Authority only.
    private readonly HashSet<NetworkObject> activeBombs = new();

  public void StartHazards()
{
    if (!HasStateAuthority)
        return;

    if (bombPrefab != null && !IsSpawning)
    {
        IsSpawning = true;
        SpawnTimer = TickTimer.CreateFromSeconds(
            Runner, Mathf.Max(0f, firstSpawnDelay));
    }

    if (snarePrefab != null && !IsSnareSpawning)
    {
        IsSnareSpawning = true;
        SnareSpawnTimer = TickTimer.CreateFromSeconds(
            Runner, Mathf.Max(0f, firstSnareDelay));
    }
}

    public void StopHazards()
    {
        if (!HasStateAuthority)
            return;

        IsSpawning = false;
        SpawnTimer = TickTimer.None;

        // Copy first because each despawn can notify this manager.
        var bombsToDespawn = new List<NetworkObject>(activeBombs);

        

        foreach (NetworkObject bomb in bombsToDespawn)
        {
            if (bomb != null && bomb.IsValid)
                Runner.Despawn(bomb);
        }

        activeBombs.Clear();
        IsSnareSpawning = false;
SnareSpawnTimer = TickTimer.None;

var snaresToDespawn =
    new List<NetworkObject>(activeSnares);

foreach (NetworkObject snare in snaresToDespawn)
{
    if (snare != null && snare.IsValid)
        Runner.Despawn(snare);
}

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
            Runner, Random.Range(min, max));
    }

    if (IsSnareSpawning && SnareSpawnTimer.Expired(Runner))
    {
        if (activeSnares.Count < Mathf.Max(1, maxActiveSnares))
            TrySpawnSnare();

        float min = Mathf.Max(0.1f, snareIntervalMin);
        float max = Mathf.Max(min, snareIntervalMax);

        SnareSpawnTimer = TickTimer.CreateFromSeconds(
            Runner, Random.Range(min, max));
    }
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
            snareSpawnPoints[(startIndex + i) % snareSpawnPoints.Length];

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

        if (snare != null)
        {
            activeSnares.Add(snare);
            return;
        }
    }

    Debug.LogWarning(
        "[Hazards] No valid snare spawn point found.",
        this);
}

public void NotifySnareDespawned(NetworkObject snare)
{
    if (!HasStateAuthority)
        return;

    activeSnares.Remove(snare);
}
    private void TrySpawnBomb()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[Hazards] No bomb spawn points assigned.", this);
            return;
        }

        int startIndex = Random.Range(0, spawnPoints.Length);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform point = spawnPoints[(startIndex + i) % spawnPoints.Length];

            if (point == null)
                continue;

            NetworkObject bomb = Runner.Spawn(
                bombPrefab,
                point.position,
                Quaternion.identity,
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

        Debug.LogWarning("[Hazards] No valid bomb spawn point found.", this);
    }

    public void NotifyBombDespawned(NetworkObject bomb)
    {
        if (!HasStateAuthority)
            return;

        activeBombs.Remove(bomb);
    }
}
