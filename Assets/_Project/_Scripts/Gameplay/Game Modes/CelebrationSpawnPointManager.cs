using UnityEngine;

public class CelebrationSpawnPointManager : MonoBehaviour
{
    [Header("1st Place Spawn Points")]
    [SerializeField]
    private Transform[] firstPlaceSpawnPoints;

    [Header("2nd Place Spawn Points")]
    [SerializeField]
    private Transform[] secondPlaceSpawnPoints;

    [Header("3rd Place Spawn Points")]
    [SerializeField]
    private Transform[] thirdPlaceSpawnPoints;

    [Header("4th Place Spawn Points")]
    [SerializeField]
    private Transform[] fourthPlaceSpawnPoints;

    public Transform GetSpawnPoint(
        int placement,
        int placementIndex)
    {
        Transform[] spawnPoints =
            GetSpawnPoints(placement);

        if (spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            return null;
        }

        if (placementIndex < 0 ||
            placementIndex >= spawnPoints.Length)
        {
            return null;
        }

        return spawnPoints[placementIndex];
    }

    private Transform[] GetSpawnPoints(
        int placement)
    {
        switch (placement)
        {
            case 1:
                return firstPlaceSpawnPoints;

            case 2:
                return secondPlaceSpawnPoints;

            case 3:
                return thirdPlaceSpawnPoints;

            case 4:
                return fourthPlaceSpawnPoints;

            default:
                return null;
        }
    }
}