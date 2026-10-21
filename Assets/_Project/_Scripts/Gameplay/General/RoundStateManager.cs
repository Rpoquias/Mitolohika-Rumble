using System;
using Fusion;
using UnityEngine;

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
private int lastPlayedCountdown = -1;
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
    if (!HasStateAuthority || Runner == null || !roundStarted)
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

    if (!AllExpectedPlayersSpawned())
        return;

    EnterCountdown();
}
  public void TriggerRoundEndSequence()
{
    EnterRoundEnd();
}

  
private bool AllExpectedPlayersSpawned()
{
    int spawnedCount = 0;

    foreach (PlayerRef player in Runner.ActivePlayers)
    {
        if (!Runner.TryGetPlayerObject(
                player, out NetworkObject playerObject))
        {
            return false;
        }

        if (playerObject == null || !playerObject.IsValid)
            return false;

        spawnedCount++;
    }

    return spawnedCount >= ExpectedPlayerCount;
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


    roundStarted = false;
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
}private void CaptureExpectedPlayers()
{
    int count = 0;

    foreach (PlayerRef player in Runner.ActivePlayers)
        count++;

    ExpectedPlayerCount = count;
}
public void EndRound()
{
    EnterRoundEnd();
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
    if (!HasStateAuthority)
        return;

    NetworkState = RoundState.Playing;
    NetworkCountdown = 0;
    StateTimer = TickTimer.None;

    // Gameplay event: only the authoritative instance starts systems.
    OnRoundStarted?.Invoke();

    Debug.Log("[ROUND] Gameplay started.");
}




private void EnterRoundEnd()
{
    if (!HasStateAuthority)
        return;

    if (NetworkState != RoundState.Playing)
        return;

    NetworkState = RoundState.RoundEnd;
    NetworkCountdown = 0;

    StateTimer = TickTimer.CreateFromSeconds(
        Runner,
        restartDelay
    );

    // Gameplay event: only the authoritative instance ends systems.
    OnRoundEnded?.Invoke();

    Debug.Log("[ROUND] Gameplay ended.");
}
private void OnNetworkRoundStateChanged()
{
    RoundState newState = NetworkState;

    Debug.Log(
        $"<color=green>[ROUND] State changed to: {newState}</color>"
    );

    switch (newState)
    {
        case RoundState.Waiting:
            lastPlayedCountdown = -1;
            break;

        case RoundState.Playing:
            AudioManager.Instance?.PlaySFX(SFXID.RoundStart);
            break;

        case RoundState.RoundEnd:
            AudioManager.Instance?.PlaySFX(SFXID.RoundEnd);
            break;
    }

    OnStateChanged?.Invoke(newState);
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

    // Only play the countdown SFX during the actual round countdown.
    if (NetworkState != RoundState.Countdown)
        return;

    OnCountdownTick?.Invoke(NetworkCountdown);

    if (NetworkCountdown == lastPlayedCountdown)
        return;

    lastPlayedCountdown = NetworkCountdown;

    AudioManager.Instance?.PlaySFX(SFXID.Countdown);
}
}