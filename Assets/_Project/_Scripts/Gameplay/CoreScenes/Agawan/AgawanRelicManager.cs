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
        roundStateManager.OnRoundEnded += HandleRoundEnded;
        roundStateManager.OnRoundReset += HandleRoundReset;
        roundStateManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (roundStateManager == null)
            return;

        roundStateManager.OnRoundStarted -= HandleRoundStarted;
        roundStateManager.OnRoundEnded -= HandleRoundEnded;
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

        Debug.Log(
            $"[RELIC MANAGER] Connected to Runner: {runner.name}"
        );
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

    roundStarted = true;
    roundEnded = false;

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

    matchSession.StartRoundTimer(
        roundDuration
    );

    scoreManager.StartScoring();

    Debug.Log(
        $"[RELIC MANAGER] Agawan Relic started! " +
        $"Time: {roundDuration:F0}s"
    );
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

    MatchSession matchSession =
        NetworkManager.Instance.CurrentMatchSession;

    if (matchSession != null)
    {
        matchSession.StopRoundTimer();
    }

    scoreManager.StopScoring();

    Debug.Log(
        "[RELIC MANAGER] Relic timer expired."
    );

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

    private void HandleRoundEnded()
    {
        Debug.Log(
            "[RELIC MANAGER] Relic round ended."
        );
    }

private void HandleRoundReset()
{
    Debug.Log(
        "[RELIC MANAGER] Resetting Agawan Relic."
    );

    roundStarted = false;
    roundEnded = false;

  MatchSession matchSession =
    NetworkManager.Instance != null
        ? NetworkManager.Instance.CurrentMatchSession
        : null;

if (matchSession != null)
{
    matchSession.StopRoundTimer();
}

    scoreManager.ResetScores();
    placementManager.ResetPlacement();

    relicController.ResetRelic();
}
}