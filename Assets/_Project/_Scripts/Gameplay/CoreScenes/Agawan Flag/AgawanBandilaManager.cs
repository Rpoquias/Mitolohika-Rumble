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
    private bool basesAssigned;
    private bool roundActive;
    private bool roundEnding;

    private readonly List<Vector3> flagStartPositions =
        new List<Vector3>();

    private readonly List<Quaternion> flagStartRotations =
        new List<Quaternion>();

    private void Start()
    {
        CacheFlagStartPositions();

        if (roundStateManager != null)
        {
            roundStateManager.OnRoundStarted +=
                HandleRoundStarted;

            roundStateManager.OnRoundReset +=
                HandleRoundReset;
        }
    }

 private void Update()
{
    if (!roundActive)
        return;

    NetworkRunner runner =
        NetworkManager.Instance != null
            ? NetworkManager.Instance.Runner
            : null;

    if (runner == null ||
        !runner.IsRunning ||
        !runner.IsServer)
    {
        return;
    }

    MatchSession matchSession =
        NetworkManager.Instance.CurrentMatchSession;

    if (matchSession == null)
        return;

    if (matchSession.IsRoundTimerExpired())
    {
        EndRound();
    }
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
        NetworkManager.Instance != null
            ? NetworkManager.Instance.CurrentMatchSession
            : null;

    if (matchSession == null)
    {
        Debug.LogError(
            "[BANDILA MANAGER] MatchSession is missing."
        );

        return;
    }

    matchSession.StartRoundTimer(
        roundDuration
    );

    roundActive = true;
    roundEnding = false;

    if (scoreManager != null)
    {
        scoreManager.StartScoring();
    }

    Debug.Log(
        $"[BANDILA] Round started. " +
        $"Duration: {roundDuration} seconds."
    );
}
    private void EndRound()
{
    if (!HasServer())
        return;

    if (!roundActive)
        return;

    if (roundEnding)
        return;

    roundEnding = true;
    roundActive = false;

    MatchSession matchSession =
        NetworkManager.Instance.CurrentMatchSession;

    if (matchSession != null)
    {
        matchSession.StopRoundTimer();
    }

    if (scoreManager != null)
    {
        scoreManager.StopScoring();
    }

    Debug.Log(
        "[BANDILA] Timer reached zero. " +
        "Ending round."
    );

   roundStateManager.EndRound();

if (MatchFlowManager.Instance == null)
{
    Debug.LogError(
        "[BANDILA] MatchFlowManager is missing."
    );

    return;
}
     List<RoundPlacement> placements =
        placementManager.BuildFinalPlacements();

    Dictionary<PlayerRef, int> flagCounts =
        new Dictionary<PlayerRef, int>();

    foreach (PlayerRef player in NetworkManager.Instance.Runner.ActivePlayers)
    {
        flagCounts[player] = scoreManager.GetScore(player);
    }

    roundStateManager.EndRound();

    MatchFlowManager.Instance.HandleRoundComplete(
        placements,
        RoundResultDetailType.Flags,
        flagCounts
    );
}

    private void AssignBases()
    {
        NetworkRunner runner =
            NetworkManager.Instance.Runner;

        if (runner == null || !runner.IsRunning)
            return;

        if (!runner.IsServer)
            return;

        if (basesAssigned)
            return;

        if (bases == null || bases.Length == 0)
        {
            Debug.LogError(
                "[BANDILA MANAGER] No bases assigned."
            );

            return;
        }

        List<PlayerRef> players =
            new List<PlayerRef>();

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            players.Add(player);
        }

        if (players.Count == 0)
        {
            Debug.LogWarning(
                "[BANDILA MANAGER] No active players found."
            );

            return;
        }

        players.Sort(
            (a, b) =>
                a.PlayerId.CompareTo(b.PlayerId)
        );

        int assignmentCount =
            Mathf.Min(
                players.Count,
                bases.Length
            );

        for (int i = 0; i < assignmentCount; i++)
        {
            if (bases[i] == null)
                continue;

            bases[i].SetOwner(players[i]);

            Debug.Log(
                $"[BANDILA MANAGER] " +
                $"Base {i} assigned to {players[i]}."
            );
        }

        basesAssigned = true;

        Debug.Log(
            $"[BANDILA MANAGER] Assigned " +
            $"{assignmentCount} bases."
        );
    }

    public bool IsPlayerCarryingFlag(
        PlayerRef player)
    {
        if (flags == null)
            return false;

        for (int i = 0; i < flags.Length; i++)
        {
            BandilaController flag =
                flags[i];

            if (flag == null)
                continue;

            if (!flag.IsCarried)
                continue;

            if (flag.CurrentHolder == player)
                return true;
        }

        return false;
    }

    public BandilaBaseController GetBaseForPlayer(
        PlayerRef player)
    {
        if (bases == null)
            return null;

        for (int i = 0; i < bases.Length; i++)
        {
            BandilaBaseController flagBase =
                bases[i];

            if (flagBase == null)
                continue;

            if (flagBase.CurrentOwner == player)
                return flagBase;
        }

        return null;
    }

    public int GetStoredFlagCount(
        PlayerRef player)
    {
        BandilaBaseController flagBase =
            GetBaseForPlayer(player);

        if (flagBase == null)
            return 0;

        return flagBase.StoredFlagCount;
    }

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

            flagStartPositions.Add(
                flags[i].transform.position
            );

            flagStartRotations.Add(
                flags[i].transform.rotation
            );
        }
    }

    private void ResetRoundObjects()
    {
        ResetFlags();
        ResetBases();

        if (scoreManager != null)
        {
            scoreManager.ResetScores();
        }
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
                continue;

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

    MatchSession matchSession =
        NetworkManager.Instance.CurrentMatchSession;

    if (matchSession != null)
    {
        matchSession.StopRoundTimer();
    }

    roundActive = false;
    roundEnding = false;

    Debug.Log(
        "[BANDILA] Round reset."
    );
}
    private bool HasServer()
    {
        NetworkManager networkManager =
            NetworkManager.Instance;

        if (networkManager == null)
            return false;

        NetworkRunner runner =
            networkManager.Runner;

        if (runner == null ||
            !runner.IsRunning)
            return false;

        return runner.IsServer;
    }

    private void OnDestroy()
    {
        if (roundStateManager != null)
        {
            roundStateManager.OnRoundStarted -=
                HandleRoundStarted;

            roundStateManager.OnRoundReset -=
                HandleRoundReset;
        }
    }
}