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
    private readonly List<GameObject> arenaTiles = new List<GameObject>();

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

        // Reconstruct the current arena state (important for late-joining clients)
        ApplyCurrentShrinkState();

        lastAppliedShrinkCommand = ShrinkCommand;

        // Single, distinct red log for initialization feedback
        Debug.Log($"<color=red>[ARENA] ArenaShrinkController successfully spawned and initialized ({arenaTiles.Count} tiles cached).</color>");
    }

    private void ApplyCurrentShrinkState()
    {
        if (arenaTiles.Count == 0)
            return;

        if (!useRandomSlash)
        {
            ApplyShrinkCount(0, NorthShrinkCount);
            ApplyShrinkCount(1, SouthShrinkCount);
            ApplyShrinkCount(2, EastShrinkCount);
            ApplyShrinkCount(3, WestShrinkCount);
        }
    }

    private void ApplyShrinkCount(int side, int count)
    {
        for (int i = 1; i <= count; i++)
        {
            float targetLayer = GetShrinkLayer(side, i);
            RemoveSide(side, targetLayer);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (!ShrinkTimer.IsRunning)
            return;

        if (!ShrinkTimer.Expired(Runner))
            return;

        ShrinkRandomSide();

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

        ShrinkTimer = TickTimer.CreateFromSeconds(
            Runner,
            startDelay
        );

        if (uiState != null)
        {
            uiState.ShowShrinkWarning(startDelay);
        }
    }

    public void StopShrinking()
    {
        if (!HasStateAuthority)
            return;

        IsShrinking = false;
        ShrinkTimer = TickTimer.None;
    }

    private void CacheArenaTiles()
    {
        if (arenaLevel == null)
            return;

        arenaTiles.Clear();
        originalTilePositions.Clear();

        MeshFilter[] meshTiles = arenaLevel.GetComponentsInChildren<MeshFilter>(true);

        bool hasBounds = false;

        minX = float.MaxValue;
        maxX = float.MinValue;
        minZ = float.MaxValue;
        maxZ = float.MinValue;

        foreach (MeshFilter meshTile in meshTiles)
        {
            GameObject tile = meshTile.gameObject;

            MeshRenderer renderer = tile.GetComponent<MeshRenderer>();

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
        }
    }

    private void ShrinkRandomSide()
    {
        if (arenaTiles.Count == 0)
            return;

        int side = Random.Range(0, 4);

        ShrinkSide = side;
        ShrinkCommand++;

        IncrementShrinkCount(side);

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
            float layer = useX ? position.x : position.z;

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
            return;

        float targetLayer = availableLayers[Random.Range(0, availableLayers.Count)];
        ShrinkLayer = targetLayer;
        RemoveSide(side, targetLayer);
    }

    private void IncrementShrinkCount(int side)
    {
        switch (side)
        {
            case 0: NorthShrinkCount++; break;
            case 1: SouthShrinkCount++; break;
            case 2: EastShrinkCount++; break;
            case 3: WestShrinkCount++; break;
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
            case 0: return maxZ - ((shrinkCount - 1) * tileSpacingZ); // North
            case 1: return minZ + ((shrinkCount - 1) * tileSpacingZ); // South
            case 2: return maxX - ((shrinkCount - 1) * tileSpacingX); // East
            case 3: return minX + ((shrinkCount - 1) * tileSpacingX); // West
            default: return 0f;
        }
    }

    private int GetShrinkCount(int side)
    {
        switch (side)
        {
            case 0: return NorthShrinkCount;
            case 1: return SouthShrinkCount;
            case 2: return EastShrinkCount;
            case 3: return WestShrinkCount;
            default: return 0;
        }
    }

    private void RemoveSide(int side, float boundary)
    {
        const float tolerance = 0.1f;
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
                case 1: // South
                    shouldRemove = Mathf.Abs(position.z - targetLayer) <= tolerance;
                    break;

                case 2: // East
                case 3: // West
                    shouldRemove = Mathf.Abs(position.x - targetLayer) <= tolerance;
                    break;
            }

            if (shouldRemove)
            {
                tile.SetActive(false);
            }
        }
    }

    private string GetSideName(int side)
    {
        switch (side)
        {
            case 0: return "North";
            case 1: return "South";
            case 2: return "East";
            case 3: return "West";
            default: return "Unknown";
        }
    }

    public void ResetArena()
    {
        foreach (GameObject tile in arenaTiles)
        {
            if (tile == null)
                continue;

            tile.SetActive(true);

            if (originalTilePositions.TryGetValue(tile, out Vector3 originalPosition))
            {
                tile.transform.position = originalPosition;
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