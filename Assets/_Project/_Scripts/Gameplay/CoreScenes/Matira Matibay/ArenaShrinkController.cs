
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

    [Header("Audio")]
    [SerializeField] private SFXID tileDestroyedSFX = SFXID.ArenaTileDestroyed;

    [Header("Crumbling Chunks")]
    [SerializeField] private bool enableCrumbling = true;
    [SerializeField] private float crumbleStartDelay = 12f;
    [SerializeField] private float crumbleIntervalMin = 5f;
    [SerializeField] private float crumbleIntervalMax = 8f;
    [SerializeField] private float crumbleWarningDuration = 3f;
    [SerializeField] private int chunkWidth = 3;
    [SerializeField] private int chunkDepth = 3;
    [SerializeField] private WorldSpaceCrumbleWarning crumbleWarningPrefab;
    [SerializeField] private float warningHeight = 2f;

    private const int MaxCrumbledChunks = 256;

    // ---------------------------------------------------------------------
    // Networked crumbling state
    // ---------------------------------------------------------------------

    [Networked]
    private NetworkBool IsCrumbling { get; set; }

    [Networked]
    private TickTimer CrumbleTimer { get; set; }

    [Networked]
    private TickTimer CrumbleWarningTimer { get; set; }

    [Networked]
    private int WarningChunkIndex { get; set; }

    [Networked, Capacity(MaxCrumbledChunks)]
    private NetworkArray<int> CrumbledChunkIndices => default;

    [Networked, OnChangedRender(nameof(OnCrumbledChunkCountChanged))]
    private int CrumbledChunkCount { get; set; }

    [Networked, OnChangedRender(nameof(OnCrumbleCommandChanged))]
    private int CrumbleCommand { get; set; }

    // ---------------------------------------------------------------------
    // Networked shrinking state
    // ---------------------------------------------------------------------

    [Networked]
    private NetworkBool IsShrinking { get; set; }

    [Networked]
    private TickTimer ShrinkTimer { get; set; }

    [Networked, OnChangedRender(nameof(OnShrinkCommandChanged))]
    private int ShrinkCommand { get; set; }

    [Networked]
    private int ShrinkSide { get; set; }

    [Networked]
    private float ShrinkLayer { get; set; }

    [Networked]
    private int NorthShrinkCount { get; set; }

    [Networked]
    private int SouthShrinkCount { get; set; }

    [Networked]
    private int EastShrinkCount { get; set; }

    [Networked]
    private int WestShrinkCount { get; set; }

    // ---------------------------------------------------------------------
    // Local cached data
    // ---------------------------------------------------------------------

    private readonly List<GameObject> arenaTiles = new();
    private readonly Dictionary<GameObject, Vector3> originalTilePositions = new();

    private readonly List<List<int>> crumbleChunks = new();
    private readonly List<Vector3> crumbleChunkCenters = new();

    private WorldSpaceCrumbleWarning activeCrumbleWarning;

    private int lastAppliedShrinkCommand;
    private int lastAppliedCrumbleCommand = -1;
    private int lastAppliedCrumbledChunkCount = -1;

    private float minX;
    private float maxX;
    private float minZ;
    private float maxZ;

    private float tileSpacingX;
    private float tileSpacingZ;

    // ---------------------------------------------------------------------
    // Public warning information for the existing MatiraMatibayUI
    // ---------------------------------------------------------------------

    public bool IsShrinkWarningActive
    {
        get
        {
            return IsShrinking && ShrinkTimer.IsRunning;
        }
    }

    public float RemainingShrinkTime
    {
        get
        {
            if (Runner == null || !ShrinkTimer.IsRunning)
                return 0f;

            return ShrinkTimer.RemainingTime(Runner) ?? 0f;
        }
    }

    public bool IsCrumbleWarningActive
    {
        get
        {
            return IsCrumbling && CrumbleWarningTimer.IsRunning;
        }
    }

    public float RemainingCrumbleWarningTime
    {
        get
        {
            if (Runner == null || !CrumbleWarningTimer.IsRunning)
                return 0f;

            return CrumbleWarningTimer.RemainingTime(Runner) ?? 0f;
        }
    }

    // ---------------------------------------------------------------------
    // Audio
    // ---------------------------------------------------------------------

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayTileDestroyedSFX(Vector3 position)
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySFXAtPosition(
            tileDestroyedSFX,
            position
        );
    }

    // ---------------------------------------------------------------------
    // Initialization
    // ---------------------------------------------------------------------

    public override void Spawned()
    {
        CacheArenaTiles();
        BuildCrumbleChunks();

        ApplyCurrentShrinkState();
        ApplyCurrentCrumbleState();

        lastAppliedShrinkCommand = ShrinkCommand;
        lastAppliedCrumbleCommand = CrumbleCommand;
        lastAppliedCrumbledChunkCount = CrumbledChunkCount;

        Debug.Log(
            $"[ARENA] Initialized: {arenaTiles.Count} tiles, " +
            $"{crumbleChunks.Count} crumbling chunks."
        );
    }

    private void CacheArenaTiles()
    {
        if (arenaLevel == null)
        {
            Debug.LogError("[ARENA] Arena Level is not assigned.", this);
            return;
        }

        arenaTiles.Clear();
        originalTilePositions.Clear();

        MeshFilter[] meshTiles =
            arenaLevel.GetComponentsInChildren<MeshFilter>(true);

        minX = float.MaxValue;
        maxX = float.MinValue;
        minZ = float.MaxValue;
        maxZ = float.MinValue;

        foreach (MeshFilter meshTile in meshTiles)
        {
            GameObject tile = meshTile.gameObject;

            if (tile.GetComponent<MeshRenderer>() == null)
                continue;

            // Avoid caching the same GameObject more than once.
            if (originalTilePositions.ContainsKey(tile))
                continue;

            arenaTiles.Add(tile);
            originalTilePositions.Add(tile, tile.transform.position);

            Vector3 position = tile.transform.position;

            minX = Mathf.Min(minX, position.x);
            maxX = Mathf.Max(maxX, position.x);
            minZ = Mathf.Min(minZ, position.z);
            maxZ = Mathf.Max(maxZ, position.z);
        }

        // These must match the distance between adjacent tile centers.
        tileSpacingX = 2f;
        tileSpacingZ = 2f;
    }

    // ---------------------------------------------------------------------
    // Chunk construction
    // ---------------------------------------------------------------------

    private void BuildCrumbleChunks()
    {
        crumbleChunks.Clear();
        crumbleChunkCenters.Clear();

        if (arenaTiles.Count == 0)
            return;

        if (chunkWidth < 1 || chunkDepth < 1)
        {
            Debug.LogError("[ARENA] Chunk dimensions must be at least 1.", this);
            return;
        }

        Dictionary<Vector2Int, List<int>> groupedTiles = new();

        for (int i = 0; i < arenaTiles.Count; i++)
        {
            GameObject tile = arenaTiles[i];

            if (tile == null)
                continue;

            Vector3 position = originalTilePositions[tile];

            int gridX = Mathf.RoundToInt(
                (position.x - minX) / tileSpacingX
            );

            int gridZ = Mathf.RoundToInt(
                (position.z - minZ) / tileSpacingZ
            );

            Vector2Int chunkKey = new(
                Mathf.FloorToInt((float)gridX / chunkWidth),
                Mathf.FloorToInt((float)gridZ / chunkDepth)
            );

            if (!groupedTiles.TryGetValue(
                    chunkKey, out List<int> indices))
            {
                indices = new List<int>();
                groupedTiles.Add(chunkKey, indices);
            }

            indices.Add(i);
        }

        // All peers must produce the same chunk-index ordering.
        List<Vector2Int> keys = new(groupedTiles.Keys);

        keys.Sort((a, b) =>
        {
            int xComparison = a.x.CompareTo(b.x);

            return xComparison != 0
                ? xComparison
                : a.y.CompareTo(b.y);
        });

        foreach (Vector2Int key in keys)
        {
            List<int> indices = groupedTiles[key];

            Vector3 center = Vector3.zero;
            int validTiles = 0;

            foreach (int index in indices)
            {
                GameObject tile = arenaTiles[index];

                if (tile == null)
                    continue;

                center += originalTilePositions[tile];
                validTiles++;
            }

            if (validTiles == 0)
                continue;

            center /= validTiles;

            crumbleChunks.Add(indices);
            crumbleChunkCenters.Add(center);
        }

        Debug.Log($"[ARENA] Built {crumbleChunks.Count} crumbling chunks.");
    }

    // ---------------------------------------------------------------------
    // Network simulation
    // ---------------------------------------------------------------------

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        // Arena shrinking remains independent of crumbling.
        if (IsShrinking &&
            ShrinkTimer.IsRunning &&
            ShrinkTimer.Expired(Runner))
        {
            ShrinkRandomSide();

            ShrinkTimer = TickTimer.CreateFromSeconds(
                Runner,
                shrinkInterval
            );
        }

        if (!IsCrumbling)
            return;

        if (CrumbleWarningTimer.IsRunning)
        {
            if (CrumbleWarningTimer.Expired(Runner))
            {
                CrumbleWarningTimer = TickTimer.None;
                CrumbleSelectedChunk();
            }

            return;
        }

        if (CrumbleTimer.IsRunning &&
            CrumbleTimer.Expired(Runner))
        {
            CrumbleTimer = TickTimer.None;
            BeginRandomChunkWarning();
        }
    }

    // ---------------------------------------------------------------------
    // Crumbling lifecycle
    // ---------------------------------------------------------------------

    public void StartCrumbling()
    {
        if (!HasStateAuthority || !enableCrumbling)
            return;

        if (crumbleChunks.Count == 0)
        {
            Debug.LogWarning("[ARENA] No crumbling chunks were built.", this);
            return;
        }

        IsCrumbling = true;
        WarningChunkIndex = -1;
        CrumbleWarningTimer = TickTimer.None;

        CrumbleTimer = TickTimer.CreateFromSeconds(
            Runner,
            crumbleStartDelay
        );

        RefreshCrumblePresentation();
    }

    public void StopCrumbling()
    {
        if (!HasStateAuthority)
            return;

        IsCrumbling = false;
        CrumbleTimer = TickTimer.None;
        CrumbleWarningTimer = TickTimer.None;
        WarningChunkIndex = -1;
        CrumbleCommand++;

        RefreshCrumblePresentation();
    }

    private void BeginRandomChunkWarning()
    {
        List<int> availableChunks = new();

        for (int i = 0; i < crumbleChunks.Count; i++)
        {
            if (!IsChunkCrumbled(i) && IsChunkActive(i))
                availableChunks.Add(i);
        }

        if (availableChunks.Count == 0)
        {
            IsCrumbling = false;
            CrumbleTimer = TickTimer.None;
            WarningChunkIndex = -1;
            CrumbleCommand++;

            RefreshCrumblePresentation();
            return;
        }

        WarningChunkIndex = availableChunks[
            Random.Range(0, availableChunks.Count)
        ];

        CrumbleWarningTimer = TickTimer.CreateFromSeconds(
            Runner,
            crumbleWarningDuration
        );

        CrumbleCommand++;

        // State Authority must also update its own local presentation.
        RefreshCrumblePresentation();
    }

    private void CrumbleSelectedChunk()
    {
        int chunkIndex = WarningChunkIndex;

        if (chunkIndex >= 0 &&
            chunkIndex < crumbleChunks.Count &&
            !IsChunkCrumbled(chunkIndex) &&
            CrumbledChunkCount < MaxCrumbledChunks)
        {
            Vector3 soundPosition = crumbleChunkCenters[chunkIndex];

            CrumbledChunkIndices.Set(CrumbledChunkCount, chunkIndex);
            CrumbledChunkCount++;

            SetChunkActive(chunkIndex, false);

            RPC_PlayTileDestroyedSFX(soundPosition);
        }

        WarningChunkIndex = -1;
        CrumbleCommand++;

        RefreshCrumblePresentation();
        ScheduleNextCrumble();
    }

    private void ScheduleNextCrumble()
    {
        if (!IsCrumbling)
            return;

        CrumbleWarningTimer = TickTimer.None;

        float minimum = Mathf.Max(0f, crumbleIntervalMin);
        float maximum = Mathf.Max(minimum, crumbleIntervalMax);

        float delay = Random.Range(minimum, maximum);

        CrumbleTimer = TickTimer.CreateFromSeconds(Runner, delay);
    }

    private bool IsChunkCrumbled(int chunkIndex)
    {
        for (int i = 0; i < CrumbledChunkCount; i++)
        {
            if (CrumbledChunkIndices.Get(i) == chunkIndex)
                return true;
        }

        return false;
    }

    private bool IsChunkActive(int chunkIndex)
    {
        if (chunkIndex < 0 || chunkIndex >= crumbleChunks.Count)
            return false;

        foreach (int tileIndex in crumbleChunks[chunkIndex])
        {
            if (tileIndex >= 0 &&
                tileIndex < arenaTiles.Count &&
                arenaTiles[tileIndex] != null &&
                arenaTiles[tileIndex].activeSelf)
            {
                return true;
            }
        }

        return false;
    }

    // ---------------------------------------------------------------------
    // Applying crumbling state on every peer
    // ---------------------------------------------------------------------

    private void OnCrumbleCommandChanged()
    {
        if (lastAppliedCrumbleCommand == CrumbleCommand)
            return;

        lastAppliedCrumbleCommand = CrumbleCommand;
        RefreshCrumblePresentation();
    }

    private void OnCrumbledChunkCountChanged()
    {
        ApplyCrumbledChunks();
    }

    private void ApplyCurrentCrumbleState()
    {
        ApplyCrumbledChunks();
        RefreshCrumblePresentation();
    }

    private void ApplyCrumbledChunks()
    {
        for (int i = 0; i < CrumbledChunkCount; i++)
        {
            int chunkIndex = CrumbledChunkIndices.Get(i);
            SetChunkActive(chunkIndex, false);
        }

        lastAppliedCrumbledChunkCount = CrumbledChunkCount;
    }

    private void RefreshCrumblePresentation()
    {
        HideCrumbleWarning();
        ClearAllChunkHighlights();

        ApplyCrumbledChunks();

        if (!IsCrumbling ||
            !CrumbleWarningTimer.IsRunning ||
            WarningChunkIndex < 0 ||
            WarningChunkIndex >= crumbleChunks.Count)
        {
            return;
        }

        HighlightChunk(WarningChunkIndex);
        ShowCrumbleWarning(WarningChunkIndex);
    }

    private void SetChunkActive(int chunkIndex, bool isActive)
    {
        if (chunkIndex < 0 || chunkIndex >= crumbleChunks.Count)
            return;

        foreach (int tileIndex in crumbleChunks[chunkIndex])
        {
            if (tileIndex < 0 || tileIndex >= arenaTiles.Count)
                continue;

            GameObject tile = arenaTiles[tileIndex];

            if (tile != null)
                tile.SetActive(isActive);
        }
    }

    // ---------------------------------------------------------------------
    // Chunk warning visual
    // ---------------------------------------------------------------------

    private void HighlightChunk(int chunkIndex)
    {
        if (chunkIndex < 0 || chunkIndex >= crumbleChunks.Count)
            return;

        foreach (int tileIndex in crumbleChunks[chunkIndex])
        {
            if (tileIndex < 0 || tileIndex >= arenaTiles.Count)
                continue;

            GameObject tile = arenaTiles[tileIndex];

            if (tile == null)
                continue;

            Renderer tileRenderer = tile.GetComponent<Renderer>();

            if (tileRenderer == null)
                continue;

            MaterialPropertyBlock block = new();
            tileRenderer.GetPropertyBlock(block);

            block.SetColor("_BaseColor", Color.red);
            block.SetColor("_Color", Color.red);

            tileRenderer.SetPropertyBlock(block);
        }
    }

    private void ClearAllChunkHighlights()
    {
        foreach (List<int> chunk in crumbleChunks)
        {
            foreach (int tileIndex in chunk)
            {
                if (tileIndex < 0 || tileIndex >= arenaTiles.Count)
                    continue;

                GameObject tile = arenaTiles[tileIndex];

                if (tile == null)
                    continue;

                Renderer tileRenderer = tile.GetComponent<Renderer>();

                if (tileRenderer != null)
                    tileRenderer.SetPropertyBlock(null);
            }
        }
    }

    private void ShowCrumbleWarning(int chunkIndex)
    {
        if (crumbleWarningPrefab == null ||
            chunkIndex < 0 ||
            chunkIndex >= crumbleChunkCenters.Count)
        {
            return;
        }

        Vector3 position = crumbleChunkCenters[chunkIndex];
        position.y += warningHeight;

        activeCrumbleWarning = Instantiate(
            crumbleWarningPrefab,
            position,
            Quaternion.identity
        );

        activeCrumbleWarning.SetCountdown(
            RemainingCrumbleWarningTime
        );
    }

    private void HideCrumbleWarning()
    {
        if (activeCrumbleWarning == null)
            return;

        Destroy(activeCrumbleWarning.gameObject);
        activeCrumbleWarning = null;
    }

    private void Update()
    {
        if (activeCrumbleWarning == null ||
            Runner == null ||
            !CrumbleWarningTimer.IsRunning)
        {
            return;
        }

        activeCrumbleWarning.SetCountdown(
            RemainingCrumbleWarningTime
        );
    }

    // ---------------------------------------------------------------------
    // Existing shrinking lifecycle
    // ---------------------------------------------------------------------

    public void StartShrinking()
    {
        if (!HasStateAuthority || IsShrinking)
            return;

        IsShrinking = true;

        ShrinkTimer = TickTimer.CreateFromSeconds(
            Runner,
            startDelay
        );
    }

    public void StopShrinking()
    {
        if (!HasStateAuthority)
            return;

        IsShrinking = false;
        ShrinkTimer = TickTimer.None;
    }

    private void ApplyCurrentShrinkState()
    {
        if (arenaTiles.Count == 0 || useRandomSlash)
            return;

        ApplyShrinkCount(0, NorthShrinkCount);
        ApplyShrinkCount(1, SouthShrinkCount);
        ApplyShrinkCount(2, EastShrinkCount);
        ApplyShrinkCount(3, WestShrinkCount);
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
            ApplyRandomSlash(side);
        else
            ApplyShrink(side);

        lastAppliedShrinkCommand = ShrinkCommand;
    }

    private void ApplyRandomSlash(int side)
    {
        List<float> availableLayers = new();
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

        float targetLayer = availableLayers[
            Random.Range(0, availableLayers.Count)
        ];

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
            RemoveSide(ShrinkSide, ShrinkLayer);
        else
            ApplyShrink(ShrinkSide);

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

    private void ApplyShrinkCount(int side, int count)
    {
        for (int i = 1; i <= count; i++)
        {
            float targetLayer = GetShrinkLayer(side, i);
            RemoveSide(side, targetLayer);
        }
    }

    private float GetShrinkLayer(int side, int shrinkCount)
    {
        switch (side)
        {
            case 0: return maxZ - ((shrinkCount - 1) * tileSpacingZ);
            case 1: return minZ + ((shrinkCount - 1) * tileSpacingZ);
            case 2: return maxX - ((shrinkCount - 1) * tileSpacingX);
            case 3: return minX + ((shrinkCount - 1) * tileSpacingX);
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

        foreach (GameObject tile in arenaTiles)
        {
            if (tile == null || !tile.activeSelf)
                continue;

            Vector3 position = tile.transform.position;
            bool shouldRemove = false;

            switch (side)
            {
                case 0:
                case 1:
                    shouldRemove =
                        Mathf.Abs(position.z - boundary) <= tolerance;
                    break;

                case 2:
                case 3:
                    shouldRemove =
                        Mathf.Abs(position.x - boundary) <= tolerance;
                    break;
            }

            if (!shouldRemove)
                continue;

            Vector3 tilePosition = tile.transform.position;
            tile.SetActive(false);

            if (HasStateAuthority)
                RPC_PlayTileDestroyedSFX(tilePosition);
        }
    }

    // ---------------------------------------------------------------------
    // Reset
    // ---------------------------------------------------------------------

    public void ResetArena()
    {
        HideCrumbleWarning();
        ClearAllChunkHighlights();

        // Restore local visuals and colliders on every peer.
        foreach (GameObject tile in arenaTiles)
        {
            if (tile == null)
                continue;

            tile.SetActive(true);

            if (originalTilePositions.TryGetValue(
                    tile, out Vector3 originalPosition))
            {
                tile.transform.position = originalPosition;
            }

            Renderer tileRenderer = tile.GetComponent<Renderer>();

            if (tileRenderer != null)
                tileRenderer.SetPropertyBlock(null);
        }

        if (!HasStateAuthority)
            return;

        IsShrinking = false;
        ShrinkTimer = TickTimer.None;

        IsCrumbling = false;
        CrumbleTimer = TickTimer.None;
        CrumbleWarningTimer = TickTimer.None;
        WarningChunkIndex = -1;

        for (int i = 0; i < CrumbledChunkCount; i++)
            CrumbledChunkIndices.Set(i, -1);

        CrumbledChunkCount = 0;
        CrumbleCommand++;

        NorthShrinkCount = 0;
        SouthShrinkCount = 0;
        EastShrinkCount = 0;
        WestShrinkCount = 0;

        lastAppliedShrinkCommand = ShrinkCommand;
        lastAppliedCrumbleCommand = CrumbleCommand;
        lastAppliedCrumbledChunkCount = 0;
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
