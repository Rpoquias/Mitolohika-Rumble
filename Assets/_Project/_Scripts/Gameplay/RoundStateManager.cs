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
    [SerializeField] private float waitingDuration = 1f;
    [SerializeField] private float countdownDuration = 3f;

    [Header("Round Restart")]
    [SerializeField] private float restartDelay = 3f;

    // Networked source of truth.
    [Networked, OnChangedRender(nameof(OnNetworkRoundStateChanged))]
    private RoundState NetworkState { get; set; }

    [Networked, OnChangedRender(nameof(OnNetworkCountdownChanged))]
    private int NetworkCountdown { get; set; }

    [Networked]
    private TickTimer StateTimer { get; set; }

    // Keep the old public API so your other gameplay scripts
    // don't need to know that the state is now networked.
    public RoundState CurrentState => NetworkState;

    public event Action OnRoundStarted;
    public event Action OnRoundEnded;
    public event Action<int> OnCountdownTick;
    public event Action OnRoundReset;

    private bool authorityInitialized = false;

    // Used to detect RoundEnd -> Waiting locally.
    private RoundState lastRenderedState;

    private void Awake()
    {
        lastRenderedState = RoundState.Waiting;
    }

    public override void Spawned()
    {
        lastRenderedState = NetworkState;

        Debug.Log(
            $"[ROUND] Spawned | State: {NetworkState}"
        );
    }

    public override void FixedUpdateNetwork()
    {
        // Only the State Authority controls the round.
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
        if (!StateTimer.Expired(Runner))
            return;

        EnterCountdown();
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

        EnterWaiting();
    }

    public void StartRound()
    {
        if (!HasStateAuthority)
            return;

        EnterWaiting();
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
        // Only State Authority controls the restart timer.
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

        StateTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                waitingDuration
            );

        Debug.Log(
            $"[ROUND] Waiting for {waitingDuration} seconds."
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

        // RoundEnd -> Waiting means a new round is being reset.
        if (lastRenderedState == RoundState.RoundEnd &&
            newState == RoundState.Waiting)
        {
            OnRoundReset?.Invoke();
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