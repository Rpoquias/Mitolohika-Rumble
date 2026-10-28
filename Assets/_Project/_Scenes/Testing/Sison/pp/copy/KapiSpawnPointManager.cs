using UnityEngine;

public class KapiSpawnPointManager : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;
    private int _nextSpawnIndex;

    public int SpawnPointCount => spawnPoints != null ? spawnPoints.Length : 0;

    public Transform GetSpawnPoint(int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;
        index = Mathf.Clamp(index, 0, spawnPoints.Length - 1);
        return spawnPoints[index];
    }

    public Transform GetNextSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;
        Transform point = spawnPoints[_nextSpawnIndex % spawnPoints.Length];
        _nextSpawnIndex++;
        return point;
    }

    public bool TryGetSpawnIndex(int index, out int spawnIndex)
    {
        spawnIndex = index;
        return spawnPoints != null &&
               index >= 0 &&
               index < spawnPoints.Length &&
               spawnPoints[index] != null;
    }

    public Transform GetSpawnPointByIndex(int index)
    {
        if (spawnPoints == null || index < 0 || index >= spawnPoints.Length)
            return null;
        return spawnPoints[index];
    }
}