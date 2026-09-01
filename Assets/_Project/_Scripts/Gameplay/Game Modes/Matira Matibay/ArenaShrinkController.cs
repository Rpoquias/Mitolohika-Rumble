using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArenaShrinkController : MonoBehaviour
{
    [Header("Arena")]
    [SerializeField] private Transform arenaLevel;

    [Header("Shrinking")]
    [SerializeField] private float startDelay = 10f;
    [SerializeField] private float shrinkInterval = 5f;
private bool isShrinking = false;


    private List<GameObject> arenaTiles = new List<GameObject>();

private void Start()
{
    CacheArenaTiles();
}
public void StartShrinking()
{
    if (isShrinking)
        return;

    isShrinking = true;
    StartCoroutine(ShrinkRoutine());
}
public void StopShrinking()
{
    isShrinking = false;
}
    private void CacheArenaTiles()
    {
        arenaTiles.Clear();

        foreach (Transform tile in arenaLevel)
        {
            arenaTiles.Add(tile.gameObject);
        }

        Debug.Log("Arena tiles found: " + arenaTiles.Count);
    }

   private IEnumerator ShrinkRoutine()
{
    yield return new WaitForSeconds(startDelay);

    while (isShrinking)
    {
        ShrinkRandomSide();

        yield return new WaitForSeconds(shrinkInterval);
    }
}

    public void ShrinkRandomSide()
    {
        if (arenaTiles.Count == 0)
            return;

        // Find current arena boundaries
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        foreach (GameObject tile in arenaTiles)
        {
            if (!tile.activeSelf)
                continue;

            Vector3 position = tile.transform.position;

            minX = Mathf.Min(minX, position.x);
            maxX = Mathf.Max(maxX, position.x);
            minZ = Mathf.Min(minZ, position.z);
            maxZ = Mathf.Max(maxZ, position.z);
        }

        // Pick a random side
        int side = Random.Range(0, 4);

        switch (side)
        {
            case 0:
                RemoveSide(ArenaSide.North, maxZ);
                break;

            case 1:
                RemoveSide(ArenaSide.South, minZ);
                break;

            case 2:
                RemoveSide(ArenaSide.East, maxX);
                break;

            case 3:
                RemoveSide(ArenaSide.West, minX);
                break;
        }
    }

    private enum ArenaSide
    {
        North,
        South,
        East,
        West
    }

    private void RemoveSide(ArenaSide side, float boundary)
    {
        int removedCount = 0;

        foreach (GameObject tile in arenaTiles)
        {
            if (!tile.activeSelf)
                continue;

            Vector3 position = tile.transform.position;

            bool shouldRemove = false;

            switch (side)
            {
                case ArenaSide.North:
                case ArenaSide.South:
                    shouldRemove = Mathf.Approximately(position.z, boundary);
                    break;

                case ArenaSide.East:
                case ArenaSide.West:
                    shouldRemove = Mathf.Approximately(position.x, boundary);
                    break;
            }

            if (shouldRemove)
            {
                tile.SetActive(false);
                removedCount++;
            }
        }

        Debug.Log($"Shrinking {side}: Removed {removedCount} tiles.");
    }

    public void ResetArena()
{
    foreach (GameObject tile in arenaTiles)
    {
        if (tile != null)
            tile.SetActive(true);
    }

    isShrinking = false;

    Debug.Log("Arena reset. Restored " + arenaTiles.Count + " tiles.");
}
}