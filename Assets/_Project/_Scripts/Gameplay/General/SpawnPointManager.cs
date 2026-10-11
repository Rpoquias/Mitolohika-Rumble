using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class SpawnPointManager : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    public int SpawnPointCount =>
        spawnPoints != null ? spawnPoints.Length : 0;

    public Transform GetSpawnPoint(PlayerRef player, NetworkRunner runner)
    {
        if (runner == null || spawnPoints == null || spawnPoints.Length == 0)
            return null;

        List<PlayerRef> players = new List<PlayerRef>();

        foreach (PlayerRef activePlayer in runner.ActivePlayers)
        {
            players.Add(activePlayer);
        }

        players.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));

        int index = players.IndexOf(player);

        if (index < 0 || index >= spawnPoints.Length)
            return null;

        return spawnPoints[index];
    }

    public bool TryGetSpawnIndex(
        PlayerRef player,
        NetworkRunner runner,
        out int spawnIndex)
    {
        spawnIndex = -1;

        if (runner == null)
            return false;

        List<PlayerRef> players = new List<PlayerRef>();

        foreach (PlayerRef activePlayer in runner.ActivePlayers)
        {
            players.Add(activePlayer);
        }

        players.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));

        spawnIndex = players.IndexOf(player);

        return spawnIndex >= 0 &&
               spawnPoints != null &&
               spawnIndex < spawnPoints.Length &&
               spawnPoints[spawnIndex] != null;
    }

    public Transform GetSpawnPointByIndex(int index)
    {
        if (spawnPoints == null ||
            index < 0 ||
            index >= spawnPoints.Length)
        {
            return null;
        }

        return spawnPoints[index];
    }
}