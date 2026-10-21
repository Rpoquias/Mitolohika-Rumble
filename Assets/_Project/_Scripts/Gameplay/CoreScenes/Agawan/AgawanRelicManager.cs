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

    [Tooltip("How often (seconds) to check whether the round timer expired.")]
    [SerializeField, Min(0.02f)] private float timerCheckInterval = 0.1f;

    private NetworkRunner _runner;
    private MatchSession _matchSession;
    private float _nextTimerCheckTime;

    private bool _roundStarted;
    private bool _roundEnded;

    private void OnEnable()
    {
        if (roundStateManager == null)
            return;

        roundStateManager.OnRoundStarted += HandleRoundStarted;
        roundStateManager.OnRoundReset += HandleRoundReset;
    }

    private void OnDisable()
    {
        if (roundStateManager == null)
            return;

        roundStateManager.OnRoundStarted -= HandleRoundStarted;
        roundStateManager.OnRoundReset -= HandleRoundReset;
    }

    private void Update()
    {
        // _roundStarted is only ever true on the server, so this is the
        // single cheap early-out for every client and every idle frame.
        if (!_roundStarted || _roundEnded)
            return;

        float now = Time.unscaledTime;

        if (now < _nextTimerCheckTime)
            return;

        _nextTimerCheckTime = now + timerCheckInterval;

        if (_runner == null || !_runner.IsRunning || !_runner.IsServer)
            return;

        if (_matchSession != null && _matchSession.IsRoundTimerExpired())
            EndRound();
    }

    private bool TryResolveRunner()
    {
        if (_runner == null || !_runner.IsRunning)
            _runner = NetworkRunner.GetRunnerForGameObject(gameObject);

        return _runner != null && _runner.IsRunning;
    }

    private static MatchSession GetMatchSession()
    {
        return NetworkManager.Instance != null
            ? NetworkManager.Instance.CurrentMatchSession
            : null;
    }

    private void HandleRoundStarted()
    {
        if (!TryResolveRunner() || !_runner.IsServer)
            return;

        if (_roundStarted)
            return;

        _matchSession = GetMatchSession();

        if (_matchSession == null)
        {
            Debug.LogError("[RELIC MANAGER] MatchSession is missing.");
            return;
        }

        _roundStarted = true;
        _roundEnded = false;
        _nextTimerCheckTime = 0f;

        // Spawn fresh collectibles when gameplay begins.
        if (relicBoostManager != null)
            relicBoostManager.StartRoundCollectibles();

        _matchSession.StartRoundTimer(roundDuration);
        scoreManager.StartScoring();
    }

    private void EndRound()
    {
        if (_roundEnded)
            return;

        _roundEnded = true;

        if (relicBoostManager != null)
            relicBoostManager.ClearCollectibles();

        if (_matchSession != null)
            _matchSession.StopRoundTimer();

        scoreManager.StopScoring();

        List<RoundPlacement> placements = placementManager.BuildFinalPlacements();

        roundStateManager.EndRound();

        if (MatchFlowManager.Instance == null)
        {
            Debug.LogError("[RELIC MANAGER] MatchFlowManager is missing.");
            return;
        }

        Dictionary<PlayerRef, int> holdTimes = new Dictionary<PlayerRef, int>();

        foreach (PlayerRef player in _runner.ActivePlayers)
            holdTimes[player] = scoreManager.GetScore(player);

        MatchFlowManager.Instance.HandleRoundComplete(
            placements,
            RoundResultDetailType.HoldTime,
            holdTimes
        );
    }

    private void HandleRoundReset()
    {
        _roundStarted = false;
        _roundEnded = false;

        // Look the session up fresh: a reset may belong to a new session.
        MatchSession session = GetMatchSession();

        if (session != null)
            session.StopRoundTimer();

        _matchSession = null;

        scoreManager.ResetScores();
        placementManager.ResetPlacement();

        // Clear the previous round without spawning pickups yet.
        if (relicBoostManager != null)
            relicBoostManager.ClearCollectibles();

        relicController.ResetRelic();
    }
}