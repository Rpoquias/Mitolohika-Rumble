using Fusion;
using UnityEngine;

public class SpawnPointManager : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;

    public Transform GetSpawnPoint(PlayerRef player)
    {
        if (spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            return null;
        }

        int index =
            (player.PlayerId - 1) % spawnPoints.Length;

        return spawnPoints[index];
    }
}