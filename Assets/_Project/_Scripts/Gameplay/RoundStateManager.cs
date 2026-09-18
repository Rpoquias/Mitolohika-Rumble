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

    private bool authorityInitialized = false;

    // Used to detect RoundEnd -> Waiting locally.
    private RoundState lastRenderedState;
    private bool isSpawned;

    public bool IsSpawned => isSpawned;

    private void Awake()
    {
        lastRenderedState = RoundState.Waiting;
    }

    public override void Spawned()
    {
        isSpawned = true;

        lastRenderedState = NetworkState;

        Debug.Log(
            $"[ROUND] Spawned | State: {NetworkState}"
        );
    }

    public override void FixedUpdateNetwork()
    {
        // Only State Authority controls the round.
        if (!HasStateAuthority)
            return;

        // Initialize the first round once.
        if (!authorityInitialized)
        {
            authorityInitialized = true;

            StartRound();
            return;
        }

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

    int currentPlayerCount =
        Runner.ActivePlayers.Count();

    if (currentPlayerCount < ExpectedPlayerCount)
        return;

    if (!AllExpectedPlayersSpawned())
        return;

    EnterCountdown();
}
private bool AllExpectedPlayersSpawned()
{
    int spawnedPlayerCount = 0;

    foreach (PlayerRef player in Runner.ActivePlayers)
    {
        if (!Runner.TryGetPlayerObject(
                player,
                out NetworkObject playerObject))
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

            StateTimer =
                TickTimer.CreateFromSeconds(
                    Runner,
                    1f
                );
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

        OnRoundReset?.Invoke();

        EnterWaiting();
    }

 public void StartRound()
{
    if (!HasStateAuthority)
        return;

    CaptureExpectedPlayers();

    EnterWaiting();
}
private void CaptureExpectedPlayers()
{
    ExpectedPlayerCount = Runner.ActivePlayers.Count();

    Debug.Log(
        $"[ROUND] Expected players captured: " +
        $"{ExpectedPlayerCount}"
    );
}
    public void EndRound()
    {
        if (!HasStateAuthority)
            return;

        if (NetworkState != RoundState.Playing)
            return;

        NetworkState = RoundState.RoundEnd;
        NetworkCountdown = 0;

        Debug.Log(
            "[ROUND] State changed to RoundEnd."
        );
    }

    public void RestartRound()
    {
        if (!HasStateAuthority)
            return;

        if (NetworkState != RoundState.RoundEnd)
            return;

        StateTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                restartDelay
            );

        Debug.Log(
            $"[ROUND] Restart timer started: " +
            $"{restartDelay} seconds"
        );
    }

    private void EnterWaiting()
    {
        NetworkState = RoundState.Waiting;
        NetworkCountdown = 0;

        // No timer here.
        StateTimer = TickTimer.None;

        Debug.Log(
            "[ROUND] Waiting for game to be ready."
        );
    }

    private void EnterCountdown()
    {
        NetworkState = RoundState.Countdown;

        NetworkCountdown =
            Mathf.CeilToInt(countdownDuration);

        StateTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                1f
            );

        Debug.Log(
            $"[ROUND] Countdown started: " +
            $"{NetworkCountdown}"
        );
    }

    private void EnterPlaying()
    {
        NetworkState = RoundState.Playing;
        NetworkCountdown = 0;
        StateTimer = TickTimer.None;

        Debug.Log(
            "[ROUND] State changed to Playing."
        );
    }

    private void OnNetworkRoundStateChanged()
    {
        RoundState newState = NetworkState;

        Debug.Log(
            $"[ROUND] Network state received: {newState}"
        );

        if (lastRenderedState == RoundState.RoundEnd &&
            newState == RoundState.Waiting)
        {
            Debug.Log("[ROUND] New round started.");
        }

        if (newState == RoundState.Playing)
        {
            OnRoundStarted?.Invoke();
        }
        else if (newState == RoundState.RoundEnd)
        {
            OnRoundEnded?.Invoke();
        }

        lastRenderedState = newState;
    }

    private void OnNetworkCountdownChanged()
    {
        if (NetworkCountdown <= 0)
            return;

        Debug.Log(
            $"[ROUND] Countdown: {NetworkCountdown}"
        );

        OnCountdownTick?.Invoke(NetworkCountdown);
    }
}