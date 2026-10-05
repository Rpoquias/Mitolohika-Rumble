using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public enum RoundResultDetailType
{
    Status,
    HoldTime,
    Flags
}
public class MatchSession : NetworkBehaviour
{
    private const int GAME_MODE_COUNT = 3;

    #region Singleton & Events
    public static MatchSession Instance { get; private set; }

    public event Action OnRoundRevealRequested;
    public event Action OnRoundNumberChanged;
    public event Action OnLobbySettingsChanged;
    #endregion

    #region Networked Properties
    [Networked]
private TickTimer RoundTimer { get; set; }

public bool IsRoundTimerRunning =>
    RoundTimer.IsRunning;

public float RemainingRoundTime
{
    get
    {
        if (!RoundTimer.IsRunning)
            return 0f;

        return RoundTimer.RemainingTime(Runner) ?? 0f;
    }
}
    [Networked] public int UsedGameModeMask { get; set; }
    [Networked] public int OverallWinnerCount { get; set; }

[Networked, OnChangedRender(nameof(OnRoundResultSequenceChanged))]
public int RoundResultSequence { get; set; }

    [Networked, OnChangedRender(nameof(OnTotalRoundsChanged))]
    public int TotalRounds { get; set; } = 3;

    [Networked, OnChangedRender(nameof(OnCurrentRoundChanged))]
    public int CurrentRound { get; set; } = 1;

    [Networked, OnChangedRender(nameof(OnCurrentGameModeChanged))]
    public GameModeType CurrentGameMode { get; set; } = GameModeType.MatiraMatibay;

    [Networked, OnChangedRender(nameof(OnRoundRevealSequenceChanged))]

    
public int RoundRevealSequence { get; set; }

[System.Serializable]
public struct RoundResultRecord : INetworkStruct
{
    public PlayerRef player;
    public int placement;

    public int detailValue;
}
[Networked, Capacity(4)]
public NetworkArray<RoundResultRecord> CurrentRoundResults => default;

[Networked]
public int CurrentRoundResultCount { get; set; }

public System.Action OnRoundResultRequested;
[Networked]
public RoundResultDetailType CurrentRoundResultDetailType { get; set; }
    [Networked, Capacity(4)] public NetworkDictionary<PlayerRef, MatchPlayerPlacementRecord> PlacementRecords => default;
    [Networked, Capacity(4)] public NetworkDictionary<PlayerRef, int> OverallPlacements => default;
    [Networked, Capacity(4)] public NetworkDictionary<PlayerRef, NetworkBool> CelebrationReady => default;
    [Networked, Capacity(4)] public NetworkArray<PlayerRef> OverallWinners => default;

    public bool IsFinalRound => CurrentRound >= TotalRounds;
    #endregion

    #region Fusion Lifecycle
    public override void Spawned()
    {
        Instance = this;
        if (NetworkManager.Instance != null) NetworkManager.Instance.RegisterMatchSession(this);

        Debug.Log($"<color=#39FF14><b>[MATCH SESSION] OK</b> - Spawned & registered | Round {CurrentRound}/{TotalRounds} | Mode: {CurrentGameMode} | Authority: {Object.StateAuthority}</color>");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this) Instance = null;
    }
    #endregion

    #region Match Control & Flow
    public bool SetTotalRounds(int roundCount)
    {
        if (!HasStateAuthority) return false;
        if (roundCount != 3 && roundCount != 5 && roundCount != 7) return false;

        TotalRounds = roundCount;
        return true;
    }

   public void BeginMatch()
{
    if (!HasStateAuthority) return;

    ResetMatchResults();
    UsedGameModeMask = 0;
    CurrentRound = 1;
    RoundTimer = TickTimer.None;

    SelectRandomGameMode();
    RoundRevealSequence++;
}
   public bool AdvanceRound()
{
    if (!HasStateAuthority || IsFinalRound)
        return false;

    CurrentRound++;
    RoundTimer = TickTimer.None;

    SelectRandomGameMode();
    RoundRevealSequence++;

    return true;
}
    public void SelectRandomGameMode()
    {
        if (!HasStateAuthority) return;

        int allModesMask = (1 << GAME_MODE_COUNT) - 1;
        int availableModesMask = allModesMask & ~UsedGameModeMask;

        // All modes have already been played. Start a new mode cycle.
        if (availableModesMask == 0)
        {
            UsedGameModeMask = 0;
            availableModesMask = allModesMask;
        }

        List<GameModeType> availableModes = new List<GameModeType>();
        for (int i = 0; i < GAME_MODE_COUNT; i++)
        {
            if ((availableModesMask & (1 << i)) != 0) availableModes.Add((GameModeType)i);
        }

        if (availableModes.Count == 0) return;

        CurrentGameMode = availableModes[UnityEngine.Random.Range(0, availableModes.Count)];
        UsedGameModeMask |= GetGameModeBit(CurrentGameMode);
    }

public void StartRoundTimer(float duration)
{
    if (!HasStateAuthority)
        return;

    if (duration <= 0f)
    {
        RoundTimer = TickTimer.None;
        return;
    }

    RoundTimer =
        TickTimer.CreateFromSeconds(
            Runner,
            duration
        );
}

public void StopRoundTimer()
{
    if (!HasStateAuthority)
        return;

    RoundTimer = TickTimer.None;
}

public bool IsRoundTimerExpired()
{
    if (!RoundTimer.IsRunning)
        return false;

    return RoundTimer.Expired(Runner);
}
    private int GetGameModeBit(GameModeType mode) => 1 << (int)mode;
    #endregion

    #region Placements & Winner Calculations
    public void RecordPlacement(PlayerRef player, int placement)
    {
        if (!HasStateAuthority) return;
        if (placement < 1 || placement > 4) return;

        if (!PlacementRecords.TryGet(player, out MatchPlayerPlacementRecord record)) record = new MatchPlayerPlacementRecord();

        switch (placement)
        {
            case 1: record.FirstPlaces++; break;
            case 2: record.SecondPlaces++; break;
            case 3: record.ThirdPlaces++; break;
            case 4: record.FourthPlaces++; break;
        }

        PlacementRecords.Set(player, record);
    }

    public bool TryGetPlacementRecord(PlayerRef player, out MatchPlayerPlacementRecord record) => PlacementRecords.TryGet(player, out record);

    public void ResetMatchResults()
    {
        if (!HasStateAuthority) return;

        PlacementRecords.Clear();
        OverallPlacements.Clear();
        OverallWinnerCount = 0;

        for (int i = 0; i < 4; i++) OverallWinners.Set(i, PlayerRef.None);
    }

    public void CalculateOverallWinners()
    {
        if (!HasStateAuthority) return;

        OverallPlacements.Clear();
        OverallWinnerCount = 0;

        for (int i = 0; i < 4; i++) OverallWinners.Set(i, PlayerRef.None);

        foreach (var entry in PlacementRecords)
        {
            PlayerRef player = entry.Key;
            MatchPlayerPlacementRecord playerRecord = entry.Value;
            int overallPlacement = 1;

            foreach (var otherEntry in PlacementRecords)
            {
                if (otherEntry.Key == player) continue;

                // Another player has a better record
                if (ComparePlacementRecords(otherEntry.Value, playerRecord) > 0) overallPlacement++;
            }

            OverallPlacements.Set(player, overallPlacement);

            // Anyone with overall placement 1 is an Overall Winner.
            if (overallPlacement == 1 && OverallWinnerCount < 4)
            {
                OverallWinners.Set(OverallWinnerCount, player);
                OverallWinnerCount++;
            }
        }
    }


public void SetCurrentRoundResults(
    List<RoundPlacement> placements,
    RoundResultDetailType detailType,
    Dictionary<PlayerRef, int> detailValues)
{
    if (!HasStateAuthority)
    {
        Debug.LogWarning(
            "[MATCH] Only the Host can set round results."
        );

        return;
    }

    CurrentRoundResultCount = 0;

    CurrentRoundResultDetailType =
        detailType;

    for (int i = 0;
         i < CurrentRoundResults.Length;
         i++)
    {
        CurrentRoundResults.Set(
            i,
            default
        );
    }

    int count =
        Mathf.Min(
            placements.Count,
            CurrentRoundResults.Length
        );

    for (int i = 0;
         i < count;
         i++)
    {
        RoundPlacement placement =
            placements[i];

        int detailValue = 0;

        if (detailValues != null &&
            detailValues.TryGetValue(
                placement.Player,
                out int value))
        {
            detailValue = value;
        }

        RoundResultRecord result =
            new RoundResultRecord
            {
                player =
                    placement.Player,

                placement =
                    placement.Placement,

                detailValue =
                    detailValue
            };

        CurrentRoundResults.Set(
            i,
            result
        );
    }

    CurrentRoundResultCount = count;

    RoundResultSequence++;

    Debug.Log(
        $"[MATCH] Round result updated. " +
        $"Round {CurrentRound} | " +
        $"Players: {CurrentRoundResultCount} | " +
        $"Detail: {CurrentRoundResultDetailType}"
    );
}
private void OnRoundResultSequenceChanged()
{
    OnRoundResultRequested?.Invoke();
}
public bool TryGetCurrentRoundResult(
    int index,
    out RoundResultRecord result)
{
    if (index < 0 ||
        index >= CurrentRoundResults.Length)
    {
        result = default;
        return false;
    }

    result = CurrentRoundResults[index];

    return result.player != PlayerRef.None;
}
    public bool TryGetOverallPlacement(PlayerRef player, out int placement) => OverallPlacements.TryGet(player, out placement);

    private int ComparePlacementRecords(MatchPlayerPlacementRecord a, MatchPlayerPlacementRecord b)
    {
        if (a.FirstPlaces != b.FirstPlaces) return a.FirstPlaces > b.FirstPlaces ? 1 : -1;
        if (a.SecondPlaces != b.SecondPlaces) return a.SecondPlaces > b.SecondPlaces ? 1 : -1;
        if (a.ThirdPlaces != b.ThirdPlaces) return a.ThirdPlaces > b.ThirdPlaces ? 1 : -1;
        if (a.FourthPlaces != b.FourthPlaces) return a.FourthPlaces > b.FourthPlaces ? 1 : -1;
        return 0;
    }
    #endregion

    #region Celebration Ready System
    public void ResetCelebrationReady()
    {
        if (!HasStateAuthority) return;

        CelebrationReady.Clear();
        foreach (PlayerRef player in Runner.ActivePlayers) CelebrationReady.Set(player, false);
    }

    public void RequestCelebrationReady(bool ready)
    {
        if (HasStateAuthority) { SetCelebrationReady(Runner.LocalPlayer, ready); return; }
        RPC_SetCelebrationReady(ready);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetCelebrationReady(bool ready, RpcInfo info = default) => SetCelebrationReady(info.Source, ready);

    private void SetCelebrationReady(PlayerRef player, bool ready)
    {
        if (!HasStateAuthority) return;

        CelebrationReady.Set(player, ready);
        if (MatchFlowManager.Instance != null) MatchFlowManager.Instance.CheckCelebrationReady();
    }

    public bool IsCelebrationPlayerReady(PlayerRef player) => CelebrationReady.TryGet(player, out NetworkBool ready) && ready;

    public int GetCelebrationReadyCount()
    {
        int readyCount = 0;
        foreach (PlayerRef player in Runner.ActivePlayers) { if (IsCelebrationPlayerReady(player)) readyCount++; }
        return readyCount;
    }

    public bool AreAllPlayersCelebrationReady()
    {
        int playerCount = 0;
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            playerCount++;
            if (!IsCelebrationPlayerReady(player)) return false;
        }
        return playerCount > 0;
    }
    #endregion

    #region Render & State Callbacks
    private void OnRoundRevealSequenceChanged() => OnRoundRevealRequested?.Invoke();
    private void OnTotalRoundsChanged() => OnLobbySettingsChanged?.Invoke();
    private void OnCurrentRoundChanged()
{
    OnLobbySettingsChanged?.Invoke();
    OnRoundNumberChanged?.Invoke();
}    private void OnCurrentGameModeChanged() { } // Kept because [OnChangedRender] references it by name.
    #endregion
}