using System;
using Fusion;
using UnityEngine;
using System.Linq;

public class RoundStateManager : NetworkBehaviour
{
    public enum RoundState
    {
        Waiting,
        Countdown,
        Playing,
        RoundEnd
    }

    [Header("Round Settings")]
    [SerializeField] private float countdownDuration = 3f;

    [Header("Round Restart")]
    [SerializeField] private float restartDelay = 3f;

    // Networked source of truth.
    [Networked, OnChangedRender(nameof(OnNetworkRoundStateChanged))]
    private RoundState NetworkState { get; set; }

    [Networked]
    private int ExpectedPlayerCount { get; set; }

    [Networked, OnChangedRender(nameof(OnNetworkCountdownChanged))]
    private int NetworkCountdown { get; set; }

    [Networked, OnChangedRender(nameof(OnRoundResetSequenceChanged))]
    private int RoundResetSequence { get; set; }

    [Networked]
    private TickTimer StateTimer { get; set; }

    // Public API
    public RoundState CurrentState => NetworkState;
    public int CurrentCountdown => NetworkCountdown;
    public int CurrentExpectedPlayerCount => ExpectedPlayerCount;

    public event Action OnRoundStarted;
    public event Action OnRoundEnded;
    public event Action<int> OnCountdownTick;
    public event Action OnRoundReset;
    public event Action<RoundState> OnStateChanged;

    private int lastAppliedResetSequence;
private bool roundStarted;
private bool isSpawned;

public bool IsSpawned => isSpawned;

    public override void Spawned()
    {
        isSpawned = true;
        lastAppliedResetSequence = RoundResetSequence;

        // Visual confirmation log
        Debug.Log("<color=red>[RoundStateManager] Script spawned and running successfully!</color>");
    }

  public override void FixedUpdateNetwork()
{
    if (!HasStateAuthority)
        return;

    if (!roundStarted)
        return;

    switch (NetworkState)
    {
        case RoundState.Waiting:
            UpdateWaiting();
            break;

        case RoundState.Countdown:
            UpdateCountdown();
            break;

        case RoundState.Playing:
            // Gameplay is running.
            break;

        case RoundState.RoundEnd:
            UpdateRoundEnd();
            break;
    }
}

    private void UpdateWaiting()
    {
        if (ExpectedPlayerCount <= 0)
            return;

        int currentPlayerCount = Runner.ActivePlayers.Count();

        if (currentPlayerCount < ExpectedPlayerCount)
            return;

        if (!AllExpectedPlayersSpawned())
            return;

        EnterCountdown();
    }

    public void TriggerRoundEndSequence()
    {
        if (!HasStateAuthority)
            return;

        if (NetworkState != RoundState.Playing)
            return;

        NetworkState = RoundState.RoundEnd;
        NetworkCountdown = 0;

        // Start the delay timer before resetting
        StateTimer = TickTimer.CreateFromSeconds(Runner, restartDelay);
    }

    private bool AllExpectedPlayersSpawned()
    {
        int spawnedPlayerCount = 0;

        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(player, out NetworkObject playerObject))
            {
                return false;
            }

            if (playerObject == null)
                return false;

            spawnedPlayerCount++;
        }

        return spawnedPlayerCount >= ExpectedPlayerCount;
    }

    private void UpdateCountdown()
    {
        if (!StateTimer.Expired(Runner))
            return;

        if (NetworkCountdown > 1)
        {
            NetworkCountdown--;

            StateTimer = TickTimer.CreateFromSeconds(Runner, 1f);
        }
        else
        {
            NetworkCountdown = 0;
            EnterPlaying();
        }
    }

    private void UpdateRoundEnd()
    {
        if (!StateTimer.Expired(Runner))
            return;

        RoundResetSequence++;
        lastAppliedResetSequence = RoundResetSequence;
        OnRoundReset?.Invoke();

        EnterWaiting();
    }

  public void StartRound()
{
    if (!HasStateAuthority)
        return;

    if (roundStarted)
        return;

    roundStarted = true;

    CaptureExpectedPlayers();
    EnterWaiting();

    Debug.Log(
        "[ROUND] Round system started."
    );
}
    private void CaptureExpectedPlayers()
    {
        ExpectedPlayerCount = Runner.ActivePlayers.Count();
    }

    public void EndRound()
    {
        if (!HasStateAuthority)
            return;

        if (NetworkState != RoundState.Playing)
            return;

        NetworkState = RoundState.RoundEnd;
        NetworkCountdown = 0;
    }

    public void RestartRound()
    {
        if (!HasStateAuthority)
            return;

        if (NetworkState != RoundState.RoundEnd)
            return;

        StateTimer = TickTimer.CreateFromSeconds(Runner, restartDelay);
    }

    private void EnterWaiting()
    {
        NetworkState = RoundState.Waiting;
        NetworkCountdown = 0;
        StateTimer = TickTimer.None;
    }

    private void EnterCountdown()
    {
        NetworkState = RoundState.Countdown;
        NetworkCountdown = Mathf.CeilToInt(countdownDuration);
        StateTimer = TickTimer.CreateFromSeconds(Runner, 1f);
    }

    private void EnterPlaying()
    {
        NetworkState = RoundState.Playing;
        NetworkCountdown = 0;
        StateTimer = TickTimer.None;
    }

    private void OnNetworkRoundStateChanged()
    {
        RoundState newState = NetworkState;

        // Green debug log for round state changes
        Debug.Log($"<color=green>[ROUND] State changed to: {newState}</color>");

        OnStateChanged?.Invoke(newState);

        if (newState == RoundState.Playing)
        {
            OnRoundStarted?.Invoke();
        }
        else if (newState == RoundState.RoundEnd)
        {
            OnRoundEnded?.Invoke();
        }
    }

    private void OnRoundResetSequenceChanged()
    {
        if (RoundResetSequence == lastAppliedResetSequence)
            return;

        lastAppliedResetSequence = RoundResetSequence;

        if (HasStateAuthority)
            return;

        OnRoundReset?.Invoke();
    }

    private void OnNetworkCountdownChanged()
    {
        if (NetworkCountdown <= 0)
            return;

        OnCountdownTick?.Invoke(NetworkCountdown);
    }
}