using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class AgawanBandilaManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BandilaBaseController[] bases;
    [SerializeField] private BandilaController[] flags;
    [SerializeField] private AgawanBandilaScoreManager scoreManager;
    [SerializeField] private AgawanBandilaPlacementManager placementManager;

    [Header("Round")]
    [SerializeField] private RoundStateManager roundStateManager;
    [SerializeField] private float roundDuration = 90f;

    [Header("Events")]
    [SerializeField] private AgawanBandilaEventManager eventManager;

    private bool basesAssigned;
    private bool roundActive;
    private bool roundEnding;

    // Cached while a round is running so Update() does no lookups.
    private MatchSession _matchSession;

    private readonly List<Vector3> flagStartPositions = new List<Vector3>();
    private readonly List<Quaternion> flagStartRotations = new List<Quaternion>();

    // Don't disable this component in Awake: Start would never run.
    private void Start()
    {
        CacheFlagStartPositions();

        if (bases != null)
        {
            for (int i = 0; i < bases.Length; i++)
            {
                if (bases[i] == null)
                    continue;

                bases[i].Bind(this);
                bases[i].StoredFlagsChanged += HandleStoredFlagsChanged;
            }
        }

        if (roundStateManager != null)
        {
            roundStateManager.OnRoundStarted += HandleRoundStarted;
            roundStateManager.OnRoundReset += HandleRoundReset;
        }
    }

    private void Update()
    {
        // roundActive is only ever true on the server.
        if (!roundActive || _matchSession == null)
            return;

        if (_matchSession.IsRoundTimerExpired())
            EndRound();
    }

    private void HandleStoredFlagsChanged(PlayerRef owner)
    {
        if (scoreManager != null)
            scoreManager.NotifyStoredFlagsChanged(owner);
    }

    private void HandleRoundStarted()
    {
        if (!HasServer())
            return;

        ResetRoundObjects();
        AssignBases();

        if (!basesAssigned)
            return;

        MatchSession matchSession =
            NetworkManager.Instance.CurrentMatchSession;

        if (matchSession == null)
        {
            Debug.LogError("[BANDILA MANAGER] MatchSession is missing.");
            return;
        }

        _matchSession = matchSession;
        _matchSession.StartRoundTimer(roundDuration);

        roundActive = true;
        roundEnding = false;

        if (scoreManager != null)
            scoreManager.StartScoring();

        if (eventManager != null)
            eventManager.StartRoundEvents();
    }

    private void EndRound()
    {
        if (!HasServer() || !roundActive || roundEnding)
            return;

        roundEnding = true;
        roundActive = false;

        if (eventManager != null)
            eventManager.StopRoundEvents();

        if (_matchSession != null)
            _matchSession.StopRoundTimer();

        _matchSession = null;

        if (scoreManager != null)
            scoreManager.StopScoring();

        List<RoundPlacement> placements =
            placementManager.BuildFinalPlacements();

        Dictionary<PlayerRef, int> flagCounts =
            new Dictionary<PlayerRef, int>();

        foreach (PlayerRef player in NetworkManager.Instance.Runner.ActivePlayers)
            flagCounts[player] = scoreManager.GetScore(player);

        // End the round once.
        roundStateManager.EndRound();

        if (MatchFlowManager.Instance == null)
        {
            Debug.LogError("[BANDILA] MatchFlowManager is missing.");
            return;
        }

        MatchFlowManager.Instance.HandleRoundComplete(
            placements,
            RoundResultDetailType.Flags,
            flagCounts
        );
    }

    private void AssignBases()
    {
        NetworkRunner runner = NetworkManager.Instance.Runner;

        if (runner == null || !runner.IsRunning || !runner.IsServer)
            return;

        if (basesAssigned)
            return;

        if (bases == null || bases.Length == 0)
        {
            Debug.LogError("[BANDILA MANAGER] No bases assigned.");
            return;
        }

        List<PlayerRef> players = new List<PlayerRef>();

        foreach (PlayerRef player in runner.ActivePlayers)
            players.Add(player);

        if (players.Count == 0)
            return;

        players.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));

        int assignmentCount = Mathf.Min(players.Count, bases.Length);

        for (int i = 0; i < assignmentCount; i++)
        {
            if (bases[i] != null)
                bases[i].SetOwner(players[i]);
        }

        basesAssigned = true;
    }

    // ---------- Queries used by flags, bases and score ----------

    public bool TryGetCarriedFlag(PlayerRef player, out BandilaController carried)
    {
        if (flags != null)
        {
            for (int i = 0; i < flags.Length; i++)
            {
                BandilaController f = flags[i];

                if (f != null && f.IsCarried && f.CurrentHolder == player)
                {
                    carried = f;
                    return true;
                }
            }
        }

        carried = null;
        return false;
    }

    public bool IsPlayerCarryingFlag(PlayerRef player)
    {
        return TryGetCarriedFlag(player, out _);
    }

    public BandilaBaseController GetBaseForPlayer(PlayerRef player)
    {
        if (bases == null)
            return null;

        for (int i = 0; i < bases.Length; i++)
        {
            BandilaBaseController flagBase = bases[i];

            if (flagBase != null && flagBase.CurrentOwner == player)
                return flagBase;
        }

        return null;
    }

    public int GetStoredFlagCount(PlayerRef player)
    {
        BandilaBaseController flagBase = GetBaseForPlayer(player);

        return flagBase != null ? flagBase.StoredFlagCount : 0;
    }

    // ---------- Reset ----------

    private void CacheFlagStartPositions()
    {
        flagStartPositions.Clear();
        flagStartRotations.Clear();

        if (flags == null)
            return;

        for (int i = 0; i < flags.Length; i++)
        {
            if (flags[i] == null)
            {
                flagStartPositions.Add(Vector3.zero);
                flagStartRotations.Add(Quaternion.identity);
                continue;
            }

            flagStartPositions.Add(flags[i].transform.position);
            flagStartRotations.Add(flags[i].transform.rotation);
        }
    }

    private void ResetRoundObjects()
    {
        ResetFlags();
        ResetBases();

        if (scoreManager != null)
            scoreManager.ResetScores();
    }

    private void ResetFlags()
    {
        if (flags == null)
            return;

        for (int i = 0; i < flags.Length; i++)
        {
            if (flags[i] == null)
                continue;

            if (i >= flagStartPositions.Count ||
                i >= flagStartRotations.Count)
            {
                continue;
            }

            flags[i].ResetFlag(
                flagStartPositions[i],
                flagStartRotations[i]
            );
        }
    }

    public void ResetBases()
    {
        if (!HasServer())
            return;

        if (bases != null)
        {
            for (int i = 0; i < bases.Length; i++)
            {
                if (bases[i] != null)
                    bases[i].ResetBase();
            }
        }

        basesAssigned = false;
    }

    private void HandleRoundReset()
    {
        if (!HasServer())
            return;

        if (eventManager != null)
            eventManager.StopRoundEvents();

        if (_matchSession != null)
            _matchSession.StopRoundTimer();
        else if (NetworkManager.Instance.CurrentMatchSession != null)
            NetworkManager.Instance.CurrentMatchSession.StopRoundTimer();

        _matchSession = null;
        roundActive = false;
        roundEnding = false;
    }

    private bool HasServer()
    {
        NetworkManager networkManager = NetworkManager.Instance;

        if (networkManager == null)
            return false;

        NetworkRunner runner = networkManager.Runner;

        return runner != null && runner.IsRunning && runner.IsServer;
    }

    private void OnDestroy()
    {
        if (bases != null)
        {
            for (int i = 0; i < bases.Length; i++)
            {
                if (bases[i] != null)
                    bases[i].StoredFlagsChanged -= HandleStoredFlagsChanged;
            }
        }

        if (roundStateManager != null)
        {
            roundStateManager.OnRoundStarted -= HandleRoundStarted;
            roundStateManager.OnRoundReset -= HandleRoundReset;
        }
    }
}