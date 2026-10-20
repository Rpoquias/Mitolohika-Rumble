using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class AgawanRelicManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AgawanRelicPlacementManager placementManager;
    [SerializeField] private AgawanRelicScoreManager scoreManager;
    [SerializeField] private RoundStateManager roundStateManager;
    [SerializeField] private RelicController relicController;
[SerializeField] private AgawanRelicBoostManager relicBoostManager;
    [Header("Round Settings")]
    [SerializeField] private float roundDuration = 75f;

    private NetworkRunner runner;

    private bool initialized;
    private bool roundStarted;
    private bool roundEnded;

    private void OnEnable()
    {
        if (roundStateManager == null)
            return;

        roundStateManager.OnRoundStarted += HandleRoundStarted;
        roundStateManager.OnRoundReset += HandleRoundReset;
        roundStateManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (roundStateManager == null)
            return;

        roundStateManager.OnRoundStarted -= HandleRoundStarted;
        roundStateManager.OnRoundReset -= HandleRoundReset;
        roundStateManager.OnStateChanged -= HandleStateChanged;
    }

    private void Update()
    {
        if (!initialized)
        {
            TryInitialize();
            return;
        }

        if (!runner.IsRunning)
            return;

        if (!runner.IsServer)
            return;

        if (!roundStarted || roundEnded)
            return;

        UpdateTimer();
    }

    private void TryInitialize()
    {
        runner =
            NetworkRunner.GetRunnerForGameObject(gameObject);

        if (runner == null || !runner.IsRunning)
            return;

        initialized = true;
    }

    private void HandleStateChanged(
        RoundStateManager.RoundState state)
    {
        if (!initialized)
            TryInitialize();
    }

    private void HandleRoundStarted()
    {
        if (!initialized)
            TryInitialize();

        if (runner == null || !runner.IsServer)
            return;

        if (roundStarted)
            return;

  
        MatchSession matchSession =
            NetworkManager.Instance != null
                ? NetworkManager.Instance.CurrentMatchSession
                : null;

        if (matchSession == null)
        {
            Debug.LogError(
                "[RELIC MANAGER] MatchSession is missing."
            );

            return;
        }
              roundStarted = true;
        roundEnded = false;

// Spawn fresh collectibles when gameplay begins.
if (relicBoostManager != null)
{
    relicBoostManager.StartRoundCollectibles();
}

matchSession.StartRoundTimer(roundDuration);

scoreManager.StartScoring();
    }

    private void UpdateTimer()
    {
        MatchSession matchSession =
            NetworkManager.Instance != null
                ? NetworkManager.Instance.CurrentMatchSession
                : null;

        if (matchSession == null)
            return;

        if (matchSession.IsRoundTimerExpired())
        {
            EndRound();
        }
    }

    private void EndRound()
    {
        if (roundEnded)
            return;

        roundEnded = true;

if (relicBoostManager != null)
{
    relicBoostManager.ClearCollectibles();
}

        MatchSession matchSession =
            NetworkManager.Instance.CurrentMatchSession;

        if (matchSession != null)
        {
            matchSession.StopRoundTimer();
        }

        scoreManager.StopScoring();

        List<RoundPlacement> placements =
            placementManager.BuildFinalPlacements();

        roundStateManager.EndRound();

        if (MatchFlowManager.Instance == null)
        {
            Debug.LogError(
                "[RELIC MANAGER] MatchFlowManager is missing."
            );

            return;
        }

        Dictionary<PlayerRef, int> holdTimes =
            new Dictionary<PlayerRef, int>();

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            holdTimes[player] =
                scoreManager.GetScore(player);
        }

        MatchFlowManager.Instance.HandleRoundComplete(
            placements,
            RoundResultDetailType.HoldTime,
            holdTimes
        );
    }

private void HandleRoundReset()
{
    roundStarted = false;
    roundEnded = false;

    MatchSession matchSession =
        NetworkManager.Instance != null
            ? NetworkManager.Instance.CurrentMatchSession
            : null;

    if (matchSession != null)
        matchSession.StopRoundTimer();

    scoreManager.ResetScores();
    placementManager.ResetPlacement();
// Clear the previous round without spawning pickups yet.
if (relicBoostManager != null)
{
    relicBoostManager.ClearCollectibles();
}

    relicController.ResetRelic();
}
}