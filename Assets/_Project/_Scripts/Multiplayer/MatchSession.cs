using Fusion;
using UnityEngine;

public class MatchSession : NetworkBehaviour
{

    
    public static MatchSession Instance { get; private set; }

    [Networked]
    public int TotalRounds { get; set; } = 3;

    [Networked, OnChangedRender(nameof(OnCurrentRoundChanged))]
    public int CurrentRound { get; set; } = 1;

    [Networked, OnChangedRender(nameof(OnCurrentGameModeChanged))]
    public GameModeType CurrentGameMode { get; set; } =
        GameModeType.MatiraMatibay;

        [Networked, Capacity(4)]
public NetworkDictionary<PlayerRef, MatchPlayerPlacementRecord>
    PlacementRecords => default;

    [Networked, Capacity(4)]
public NetworkDictionary<PlayerRef, int>
    OverallPlacements => default;

    [Networked, Capacity(4)]
public NetworkDictionary<PlayerRef, NetworkBool>
    CelebrationReady => default;
    [Networked]
public int OverallWinnerCount { get; set; }

[Networked, Capacity(4)]
public NetworkArray<PlayerRef> OverallWinners => default;

    public bool IsFinalRound =>
        CurrentRound >= TotalRounds;
        

    public override void Spawned()
    {
        Instance = this;

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.RegisterMatchSession(this);
        }

        // OnChangedRender does not fire on initial spawn,
        // so log the initial state manually.
        LogCurrentMatchState();
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        Debug.Log(
            $"[MATCH] MatchSession despawned on {runner.LocalPlayer}"
        );

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnCurrentRoundChanged()
    {
        Debug.Log(
            $"[MATCH] Current Round changed → " +
            $"{CurrentRound}/{TotalRounds} " +
            $"| Local Player: {Runner.LocalPlayer}"
        );
    }

    private void OnCurrentGameModeChanged()
    {
        Debug.Log(
            $"[MATCH] Current Game Mode changed → " +
            $"{CurrentGameMode} " +
            $"| Local Player: {Runner.LocalPlayer}"
        );
    }



    private void LogCurrentMatchState()
    {
        Debug.Log(
            $"[MATCH] MatchSession state → " +
            $"Round {CurrentRound}/{TotalRounds} " +
            $"| Mode: {CurrentGameMode} " +
            $"| Local Player: {Runner.LocalPlayer} " +
            $"| Authority: {Object.StateAuthority}"
        );
    }

    public bool SetTotalRounds(int roundCount)
    {
        if (!HasStateAuthority)
        {
            Debug.LogWarning(
                "[MATCH] Only the Host can change the number of rounds."
            );

            return false;
        }

        if (roundCount != 3 &&
            roundCount != 5 &&
            roundCount != 7)
        {
            Debug.LogWarning(
                $"[MATCH] Invalid round count: {roundCount}"
            );

            return false;
        }

        TotalRounds = roundCount;

        Debug.Log(
            $"[MATCH] Total rounds changed to {TotalRounds}"
        );

        return true;
    }

public void BeginMatch()
{
    if (!HasStateAuthority)
    {
        Debug.LogWarning(
            "[MATCH] Only the Host can begin the match."
        );

        return;
    }

    ResetMatchResults();

    CurrentRound = 1;

    CurrentGameMode =
        GameModeType.MatiraMatibay;

    Debug.Log(
        $"[MATCH] Match beginning. " +
        $"Round {CurrentRound}/{TotalRounds}"
    );
}
    public bool AdvanceRound()
    {
        if (!HasStateAuthority)
        {
            Debug.LogWarning(
                "[MATCH] Only the Host can advance the round."
            );

            return false;
        }

        if (IsFinalRound)
        {
            Debug.Log(
                $"[MATCH] Final round reached: " +
                $"{CurrentRound}/{TotalRounds}"
            );

            return false;
        }

        CurrentRound++;

        CurrentGameMode =
    GameModeType.MatiraMatibay;

        Debug.Log(
            $"[MATCH] Advancing to round " +
            $"{CurrentRound}/{TotalRounds} | " +
            $"Mode: {CurrentGameMode}"
        );

        return true;
    }

    public void SelectRandomGameMode()
    {
        if (!HasStateAuthority)
        {
            Debug.LogWarning(
                "[MATCH] Only the Host can select the game mode."
            );

            return;
        }

        CurrentGameMode =
            (GameModeType)Random.Range(0, 3);

        Debug.Log(
            $"[MATCH] Selected Game Mode: " +
            $"{CurrentGameMode}"
        );
    }

    public void RecordPlacement(
    PlayerRef player,
    int placement)
{
    if (!HasStateAuthority)
    {
        Debug.LogWarning(
            "[MATCH] Only the Host can record placements."
        );

        return;
    }

    if (placement < 1 || placement > 4)
    {
        Debug.LogWarning(
            $"[MATCH] Invalid placement: {placement}"
        );

        return;
    }

    MatchPlayerPlacementRecord record;

    if (!PlacementRecords.TryGet(
            player,
            out record))
    {
        record = new MatchPlayerPlacementRecord();
    }

    switch (placement)
    {
        case 1:
            record.FirstPlaces++;
            break;

        case 2:
            record.SecondPlaces++;
            break;

        case 3:
            record.ThirdPlaces++;
            break;

        case 4:
            record.FourthPlaces++;
            break;
    }

    PlacementRecords.Set(
        player,
        record
    );

    Debug.Log(
        $"[MATCH] Recorded {player} as " +
        $"{placement} Place. " +
        $"Record = " +
        $"{record.FirstPlaces}-" +
        $"{record.SecondPlaces}-" +
        $"{record.ThirdPlaces}-" +
        $"{record.FourthPlaces}"
    );
}
public bool TryGetPlacementRecord(
    PlayerRef player,
    out MatchPlayerPlacementRecord record)
{
    return PlacementRecords.TryGet(
        player,
        out record
    );
}
public void ResetMatchResults()
{
    if (!HasStateAuthority)
    {
        Debug.LogWarning(
            "[MATCH] Only the Host can reset match results."
        );

        return;
    }

    PlacementRecords.Clear();
OverallPlacements.Clear();
    OverallWinnerCount = 0;

    for (int i = 0; i < 4; i++)
    {
        OverallWinners.Set(
            i,
            PlayerRef.None
        );
    }

    Debug.Log(
        "[MATCH] Match placement records and " +
        "Overall Winner data cleared."
    );
}

public void CalculateOverallWinners()
{
    if (!HasStateAuthority)
    {
        Debug.LogWarning(
            "[MATCH] Only the Host can calculate " +
            "the Overall Winner."
        );

        return;
    }

    OverallPlacements.Clear();

    OverallWinnerCount = 0;

    for (int i = 0; i < 4; i++)
    {
        OverallWinners.Set(
            i,
            PlayerRef.None
        );
    }

    foreach (var entry in PlacementRecords)
    {
        PlayerRef player = entry.Key;
        MatchPlayerPlacementRecord playerRecord =
            entry.Value;

        int overallPlacement = 1;

        foreach (var otherEntry in PlacementRecords)
        {
            PlayerRef otherPlayer = otherEntry.Key;

            if (otherPlayer == player)
                continue;

            MatchPlayerPlacementRecord otherRecord =
                otherEntry.Value;

            int comparison =
                ComparePlacementRecords(
                    otherRecord,
                    playerRecord
                );

            // Another player has a better record.
            if (comparison > 0)
            {
                overallPlacement++;
            }
        }

        OverallPlacements.Set(
            player,
            overallPlacement
        );

        // Anyone with overall placement 1
        // is an Overall Winner.
        if (overallPlacement == 1 &&
            OverallWinnerCount < 4)
        {
            OverallWinners.Set(
                OverallWinnerCount,
                player
            );

            OverallWinnerCount++;
        }
    }

    LogOverallPlacements();
}
private void LogOverallPlacements()
{
    if (OverallPlacements.Count == 0)
    {
        Debug.LogWarning(
            "[MATCH] No overall placements were calculated."
        );

        return;
    }

    Debug.Log(
        "[MATCH] ===== OVERALL RESULTS ====="
    );

    foreach (var entry in OverallPlacements)
    {
        PlayerRef player =
            entry.Key;

        int placement =
            entry.Value;

        MatchPlayerPlacementRecord record =
            PlacementRecords[player];

        Debug.Log(
            $"[MATCH] {player} → " +
            $"{placement} Place | " +
            $"1st={record.FirstPlaces}, " +
            $"2nd={record.SecondPlaces}, " +
            $"3rd={record.ThirdPlaces}, " +
            $"4th={record.FourthPlaces}"
        );
    }

    if (OverallWinnerCount == 1)
    {
        Debug.Log(
            $"[MATCH] OVERALL WINNER: " +
            $"{OverallWinners.Get(0)}"
        );
    }
    else
    {
        Debug.Log(
            $"[MATCH] JOINT WINNERS: " +
            $"{OverallWinnerCount}"
        );

        for (int i = 0; i < OverallWinnerCount; i++)
        {
            Debug.Log(
                $"[MATCH] Joint Winner {i + 1}: " +
                $"{OverallWinners.Get(i)}"
            );
        }
    }
}
public bool TryGetOverallPlacement(
    PlayerRef player,
    out int placement)
{
    return OverallPlacements.TryGet(
        player,
        out placement
    );
}
private int ComparePlacementRecords(
    MatchPlayerPlacementRecord a,
    MatchPlayerPlacementRecord b)
{
    if (a.FirstPlaces != b.FirstPlaces)
    {
        return a.FirstPlaces > b.FirstPlaces
            ? 1
            : -1;
    }

    if (a.SecondPlaces != b.SecondPlaces)
    {
        return a.SecondPlaces > b.SecondPlaces
            ? 1
            : -1;
    }

    if (a.ThirdPlaces != b.ThirdPlaces)
    {
        return a.ThirdPlaces > b.ThirdPlaces
            ? 1
            : -1;
    }

    if (a.FourthPlaces != b.FourthPlaces)
    {
        return a.FourthPlaces > b.FourthPlaces
            ? 1
            : -1;
    }

    return 0;
}


public void ResetCelebrationReady()
{
    if (!HasStateAuthority)
    {
        Debug.LogWarning(
            "[MATCH] Only the Host can reset Celebration Ready."
        );

        return;
    }

    CelebrationReady.Clear();

    foreach (PlayerRef player in Runner.ActivePlayers)
    {
        CelebrationReady.Set(
            player,
            false
        );
    }

    Debug.Log(
        "[MATCH] Celebration Ready states reset."
    );
}

public void RequestCelebrationReady(bool ready)
{
    if (HasStateAuthority)
    {
        SetCelebrationReady(
            Runner.LocalPlayer,
            ready
        );

        return;
    }

    RPC_SetCelebrationReady(ready);
}

[Rpc(
    RpcSources.All,
    RpcTargets.StateAuthority
)]
private void RPC_SetCelebrationReady(
    bool ready,
    RpcInfo info = default)
{
    SetCelebrationReady(
        info.Source,
        ready
    );
}

private void SetCelebrationReady(
    PlayerRef player,
    bool ready)
{
    if (!HasStateAuthority)
        return;

    CelebrationReady.Set(
        player,
        ready
    );

    Debug.Log(
        $"[MATCH] {player} Celebration Ready = {ready}"
    );

    if (MatchFlowManager.Instance != null)
    {
        MatchFlowManager.Instance.CheckCelebrationReady();
    }
}

public bool IsCelebrationPlayerReady(
    PlayerRef player)
{
    if (CelebrationReady.TryGet(
            player,
            out NetworkBool ready))
    {
        return ready;
    }

    return false;
}

public int GetCelebrationReadyCount()
{
    int readyCount = 0;

    foreach (PlayerRef player in Runner.ActivePlayers)
    {
        if (IsCelebrationPlayerReady(player))
        {
            readyCount++;
        }
    }

    return readyCount;
}

public bool AreAllPlayersCelebrationReady()
{
    int playerCount = 0;

    foreach (PlayerRef player in Runner.ActivePlayers)
    {
        playerCount++;

        if (!IsCelebrationPlayerReady(player))
        {
            return false;
        }
    }

    return playerCount > 0;
}
}