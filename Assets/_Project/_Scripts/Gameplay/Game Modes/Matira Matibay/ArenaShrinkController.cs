using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class ArenaShrinkController : NetworkBehaviour
{
    [Header("Arena")]
    [SerializeField] private Transform arenaLevel;

    [Header("Shrinking")]
    [SerializeField] private float startDelay = 10f;
    [SerializeField] private float shrinkInterval = 5f;

    private readonly List<GameObject> arenaTiles =
        new List<GameObject>();

    private readonly Dictionary<GameObject, Vector3> originalTilePositions =
        new Dictionary<GameObject, Vector3>();

    [Networked]
    private NetworkBool IsShrinking { get; set; }

    [Networked]
    private TickTimer ShrinkTimer { get; set; }

    // Incremented every time the State Authority decides to remove a side.
    [Networked, OnChangedRender(nameof(OnShrinkCommandChanged))]
    private int ShrinkCommand { get; set; }

    // Encoded as:
    // 0 = North
    // 1 = South
    // 2 = East
    // 3 = West
    [Networked]
    private int ShrinkSide { get; set; }

    private int lastAppliedShrinkCommand;

    private void Start()
    {
        CacheArenaTiles();
    }

    public override void Spawned()
    {
        CacheArenaTiles();

        lastAppliedShrinkCommand = ShrinkCommand;

        Debug.Log(
            $"[ARENA] Spawned for Runner {Runner.name}"
        );
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (!IsShrinking)
            return;

        if (!ShrinkTimer.Expired(Runner))
            return;

        ShrinkRandomSide();

        ShrinkTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                shrinkInterval
            );
    }

    public void StartShrinking()
    {
        if (!HasStateAuthority)
            return;

        if (IsShrinking)
            return;

        IsShrinking = true;

        ShrinkTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                startDelay
            );

        Debug.Log(
            $"[ARENA] Shrinking started. " +
            $"First shrink in {startDelay} seconds."
        );
    }

    public void StopShrinking()
    {
        if (!HasStateAuthority)
            return;

        IsShrinking = false;
        ShrinkTimer = TickTimer.None;

        Debug.Log("[ARENA] Shrinking stopped.");
    }

    private void CacheArenaTiles()
    {
        if (arenaLevel == null)
        {
            Debug.LogError(
                "[ARENA] Arena Level reference is missing."
            );

            return;
        }

        arenaTiles.Clear();
        originalTilePositions.Clear();

        foreach (Transform tile in arenaLevel)
        {
            GameObject tileObject = tile.gameObject;

            arenaTiles.Add(tileObject);
            originalTilePositions[tileObject] =
                tile.position;
        }

        Debug.Log(
            $"[ARENA] Tiles found: {arenaTiles.Count}"
        );
    }

    private void ShrinkRandomSide()
    {
        if (arenaTiles.Count == 0)
            return;

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        foreach (GameObject tile in arenaTiles)
        {
            if (tile == null || !tile.activeSelf)
                continue;

            Vector3 position = tile.transform.position;

            minX = Mathf.Min(minX, position.x);
            maxX = Mathf.Max(maxX, position.x);
            minZ = Mathf.Min(minZ, position.z);
            maxZ = Mathf.Max(maxZ, position.z);
        }

        if (minX == float.MaxValue)
            return;

        int side = Random.Range(0, 4);

        ShrinkSide = side;

        // Change the command number so every shrink is a new network event.
        ShrinkCommand++;

        Debug.Log(
            $"[ARENA] State Authority selected side: " +
            $"{GetSideName(side)}"
        );

        // Apply immediately on State Authority.
        ApplyShrink(side);
        lastAppliedShrinkCommand = ShrinkCommand;
    }

    private void OnShrinkCommandChanged()
    {
        // State Authority already applied this command.
        if (ShrinkCommand == lastAppliedShrinkCommand)
            return;

        ApplyShrink(ShrinkSide);

        lastAppliedShrinkCommand = ShrinkCommand;

        Debug.Log(
            $"[ARENA] Received shrink command: " +
            $"{GetSideName(ShrinkSide)}"
        );
    }

    private void ApplyShrink(int side)
    {
        float boundary;

        if (!TryGetBoundary(
                side,
                out boundary))
        {
            Debug.LogWarning(
                "[ARENA] Could not determine shrink boundary."
            );

            return;
        }

        RemoveSide(side, boundary);
    }

    private bool TryGetBoundary(
        int side,
        out float boundary)
    {
        boundary = 0f;

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        foreach (GameObject tile in arenaTiles)
        {
            if (tile == null || !tile.activeSelf)
                continue;

            Vector3 position = tile.transform.position;

            minX = Mathf.Min(minX, position.x);
            maxX = Mathf.Max(maxX, position.x);
            minZ = Mathf.Min(minZ, position.z);
            maxZ = Mathf.Max(maxZ, position.z);
        }

        if (minX == float.MaxValue)
            return false;

        switch (side)
        {
            case 0: // North
                boundary = maxZ;
                break;

            case 1: // South
                boundary = minZ;
                break;

            case 2: // East
                boundary = maxX;
                break;

            case 3: // West
                boundary = minX;
                break;

            default:
                return false;
        }

        return true;
    }

    private void RemoveSide(
        int side,
        float boundary)
    {
        int removedCount = 0;

        foreach (GameObject tile in arenaTiles)
        {
            if (tile == null || !tile.activeSelf)
                continue;

            Vector3 position = tile.transform.position;

            bool shouldRemove = false;

            switch (side)
            {
                case 0: // North
                case 1: // South
                    shouldRemove =
                        Mathf.Approximately(
                            position.z,
                            boundary
                        );
                    break;

                case 2: // East
                case 3: // West
                    shouldRemove =
                        Mathf.Approximately(
                            position.x,
                            boundary
                        );
                    break;
            }

            if (shouldRemove)
            {
                tile.SetActive(false);
                removedCount++;
            }
        }

        Debug.Log(
            $"[ARENA] Removed {removedCount} tiles from " +
            $"{GetSideName(side)}."
        );
    }

    private string GetSideName(int side)
    {
        switch (side)
        {
            case 0:
                return "North";

            case 1:
                return "South";

            case 2:
                return "East";

            case 3:
                return "West";

            default:
                return "Unknown";
        }
    }

    public void ResetArena()
    {
        foreach (GameObject tile in arenaTiles)
        {
            if (tile == null)
                continue;

            tile.SetActive(true);

            if (originalTilePositions.TryGetValue(
                    tile,
                    out Vector3 originalPosition))
            {
                tile.transform.position =
                    originalPosition;
            }
        }

        lastAppliedShrinkCommand =
            ShrinkCommand;

        if (HasStateAuthority)
        {
            IsShrinking = false;
            ShrinkTimer = TickTimer.None;
        }

        Debug.Log(
            $"[ARENA] Arena reset. Restored " +
            $"{arenaTiles.Count} tiles."
        );
    }
    public void DebugShrink()
{
    if (!HasStateAuthority)
        return;

    ShrinkRandomSide();

    ShrinkTimer = TickTimer.CreateFromSeconds(
        Runner,
        shrinkInterval
    );
}
}