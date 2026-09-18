using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class ArenaShrinkController : NetworkBehaviour
{
    [Header("Arena")]
    [SerializeField] private Transform arenaLevel;
    [Header("Shrink Mode")]
    [SerializeField] private bool useRandomSlash = false;
    [Header("Shrinking")]
    [SerializeField] private float startDelay = 10f;
    [SerializeField] private float shrinkInterval = 5f;

    [Header("UI")]
    [SerializeField] private MatiraMatibayUIState uiState;
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

    [Networked]
    private int ShrinkSide { get; set; }
    [Networked]
private float ShrinkLayer { get; set; }


    private int lastAppliedShrinkCommand;

    private float minX;
private float maxX;
private float minZ;
private float maxZ;

private float tileSpacingX;
private float tileSpacingZ;

[Networked]
private int NorthShrinkCount { get; set; }

[Networked]
private int SouthShrinkCount { get; set; }

[Networked]
private int EastShrinkCount { get; set; }

[Networked]
private int WestShrinkCount { get; set; }

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

    if (!ShrinkTimer.IsRunning)
        return;

    if (!ShrinkTimer.Expired(Runner))
        return;

    // Timer expired → actually shrink
    ShrinkRandomSide();

    // Start countdown for next shrink
    ShrinkTimer = TickTimer.CreateFromSeconds(
        Runner,
        shrinkInterval
    );

    uiState?.ShowShrinkWarning(shrinkInterval);
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

if (uiState == null)
{
    Debug.LogError("[ARENA] UI State reference is NULL!");
}
else
{
    Debug.Log("[ARENA] Sending shrink warning to UI State.");
    uiState.ShowShrinkWarning(startDelay);
}

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

    MeshFilter[] meshTiles =
        arenaLevel.GetComponentsInChildren<MeshFilter>(true);

    bool hasBounds = false;

    minX = float.MaxValue;
    maxX = float.MinValue;
    minZ = float.MaxValue;
    maxZ = float.MinValue;

    foreach (MeshFilter meshTile in meshTiles)
    {
        GameObject tile = meshTile.gameObject;

        MeshRenderer renderer =
            tile.GetComponent<MeshRenderer>();

        if (renderer == null)
            continue;

        arenaTiles.Add(tile);

        Vector3 position = tile.transform.position;

        originalTilePositions[tile] = position;

        minX = Mathf.Min(minX, position.x);
        maxX = Mathf.Max(maxX, position.x);
        minZ = Mathf.Min(minZ, position.z);
        maxZ = Mathf.Max(maxZ, position.z);

        hasBounds = true;
    }

    if (hasBounds)
    {
        tileSpacingX = 2f;
        tileSpacingZ = 2f;

        Debug.Log(
            $"[ARENA] Bounds: " +
            $"X {minX} to {maxX}, " +
            $"Z {minZ} to {maxZ}"
        );
    }

    Debug.Log(
        $"[ARENA] Mesh tiles found: {arenaTiles.Count}"
    );
}
private void ShrinkRandomSide()
{
    if (arenaTiles.Count == 0)
        return;

    int side = Random.Range(0, 4);

    ShrinkSide = side;
    ShrinkCommand++;

    IncrementShrinkCount(side);

    Debug.Log(
        $"[ARENA] State Authority selected side: " +
        $"{GetSideName(side)}"
    );

if (useRandomSlash)
{
    ApplyRandomSlash(side);
}
else
{
    ApplyShrink(side);
}

    lastAppliedShrinkCommand = ShrinkCommand;
}


private void ApplyRandomSlash(int side)
{
    List<float> availableLayers = new List<float>();

    bool useX = side == 2 || side == 3;

    foreach (GameObject tile in arenaTiles)
    {
        if (tile == null || !tile.activeSelf)
            continue;

        Vector3 position = tile.transform.position;

        float layer = useX
            ? position.x
            : position.z;

        bool alreadyAdded = false;

        foreach (float existingLayer in availableLayers)
        {
            if (Mathf.Abs(existingLayer - layer) <= 0.1f)
            {
                alreadyAdded = true;
                break;
            }
        }

        if (!alreadyAdded)
            availableLayers.Add(layer);
    }

    if (availableLayers.Count == 0)
    {
        Debug.LogWarning(
            $"[ARENA] No available layers for " +
            $"{GetSideName(side)}."
        );

        return;
    }

    float targetLayer =
        availableLayers[
            Random.Range(0, availableLayers.Count)
        ];
    ShrinkLayer = targetLayer; 
    RemoveSide(side, targetLayer);

    Debug.Log(
        $"[ARENA] Random Slash → " +
        $"{GetSideName(side)} at " +
        $"{(useX ? "X" : "Z")} = {targetLayer}"
    );
}
private void IncrementShrinkCount(int side)
{
    switch (side)
    {
        case 0:
            NorthShrinkCount++;
            break;

        case 1:
            SouthShrinkCount++;
            break;

        case 2:
            EastShrinkCount++;
            break;

        case 3:
            WestShrinkCount++;
            break;
    }
}

   private void OnShrinkCommandChanged()
{
    if (ShrinkCommand == lastAppliedShrinkCommand)
        return;

    if (useRandomSlash)
    {
        RemoveSide(ShrinkSide, ShrinkLayer);
    }
    else
    {
        ApplyShrink(ShrinkSide);
    }

    lastAppliedShrinkCommand = ShrinkCommand;

    Debug.Log(
        $"[ARENA] Received shrink command: " +
        $"{GetSideName(ShrinkSide)}"
    );
}
private void ApplyShrink(int side)
{
    int shrinkCount = GetShrinkCount(side);

    if (shrinkCount <= 0)
        return;

    float targetLayer = GetShrinkLayer(side, shrinkCount);

    RemoveSide(side, targetLayer);
}
private float GetShrinkLayer(int side, int shrinkCount)
{
    switch (side)
    {
        case 0: // North
            return maxZ - ((shrinkCount - 1) * tileSpacingZ);

        case 1: // South
            return minZ + ((shrinkCount - 1) * tileSpacingZ);

        case 2: // East
            return maxX - ((shrinkCount - 1) * tileSpacingX);

        case 3: // West
            return minX + ((shrinkCount - 1) * tileSpacingX);

        default:
            return 0f;
    }
}
private int GetShrinkCount(int side)
{
    switch (side)
    {
        case 0:
            return NorthShrinkCount;

        case 1:
            return SouthShrinkCount;

        case 2:
            return EastShrinkCount;

        case 3:
            return WestShrinkCount;

        default:
            return 0;
    }
}
   
 private void RemoveSide(int side, float boundary)
{
    int removedCount = 0;

    const float tileSpacing = 2f;
    const float tolerance = 0.1f;

    // Move one grid layer inward from the current boundary.
    float targetLayer = boundary;

    foreach (GameObject tile in arenaTiles)
    {
        if (tile == null || !tile.activeSelf)
            continue;

        Vector3 position = tile.transform.position;

        bool shouldRemove = false;

        switch (side)
        {
            case 0: // North
                shouldRemove =
                    Mathf.Abs(position.z - targetLayer) <= tolerance;
                break;

            case 1: // South
                shouldRemove =
                    Mathf.Abs(position.z - targetLayer) <= tolerance;
                break;

            case 2: // East
                shouldRemove =
                    Mathf.Abs(position.x - targetLayer) <= tolerance;
                break;

            case 3: // West
                shouldRemove =
                    Mathf.Abs(position.x - targetLayer) <= tolerance;
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

    lastAppliedShrinkCommand = ShrinkCommand;

    if (HasStateAuthority)
    {
        IsShrinking = false;
        ShrinkTimer = TickTimer.None;

        NorthShrinkCount = 0;
        SouthShrinkCount = 0;
        EastShrinkCount = 0;
        WestShrinkCount = 0;
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