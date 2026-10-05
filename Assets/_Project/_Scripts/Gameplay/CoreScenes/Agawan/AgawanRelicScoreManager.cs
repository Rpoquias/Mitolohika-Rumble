using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class AgawanRelicScoreManager : NetworkBehaviour
{

    public event Action OnScoresChanged;
    [Header("References")]
    [SerializeField] private RelicController relicController;

    [Header("Scoring")]
    [SerializeField] private float secondsPerPoint = 1f;

   [Networked, Capacity(4), OnChangedRender(nameof(OnScoresNetworkChanged))]
public NetworkDictionary<PlayerRef, int> Scores => default;

    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, int> ScoreReachedTick => default;

    private PlayerRef _currentHolder = PlayerRef.None;
    private float _continuousHoldTime;
private bool _scoringActive;
   public override void Spawned()
{
    if (relicController == null)
    {
        Debug.LogError(
            "[RELIC SCORE] RelicController reference is missing."
        );
    }

    if (!HasStateAuthority)
        return;

    _currentHolder = PlayerRef.None;
    _continuousHoldTime = 0f;
    _scoringActive = false;
}
private void OnScoresNetworkChanged()
{
    OnScoresChanged?.Invoke();
}
public void StartScoring()
{
    if (!HasStateAuthority)
        return;

    _scoringActive = true;
    _currentHolder = PlayerRef.None;
    _continuousHoldTime = 0f;

    Debug.Log("[RELIC SCORE] Scoring started.");
}
public void StopScoring()
{
 if (!HasStateAuthority)
        return;

    if (!_scoringActive)
        return;


    _scoringActive = false;
    _currentHolder = PlayerRef.None;
    _continuousHoldTime = 0f;

    Debug.Log("[RELIC SCORE] Scoring stopped.");
}

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
        return;

    if (!_scoringActive)
        return;

    PlayerRef holder = relicController.CurrentHolder;
        // Holder changed.
        if (holder != _currentHolder)
        {
            _currentHolder = holder;
            _continuousHoldTime = 0f;

            if (holder != PlayerRef.None)
            {
                EnsurePlayerScoreExists(holder);

                Debug.Log(
                    $"[RELIC SCORE] New holder: {holder}"
                );
            }

            return;
        }

        // Nobody currently holds the Relic.
        if (holder == PlayerRef.None)
            return;

        EnsurePlayerScoreExists(holder);

        _continuousHoldTime += Runner.DeltaTime;

        while (_continuousHoldTime >= secondsPerPoint)
        {
            _continuousHoldTime -= secondsPerPoint;

            AddPoint(holder);
        }
    }

    private void EnsurePlayerScoreExists(PlayerRef player)
    {
        if (!Scores.ContainsKey(player))
        {
            Scores.Add(player, 0);
        }

        if (!ScoreReachedTick.ContainsKey(player))
        {
            ScoreReachedTick.Add(player, 0);
        }
    }

    private void AddPoint(PlayerRef player)
    {
        int currentScore = Scores[player];
        int newScore = currentScore + 1;

        Scores.Set(player, newScore);

        // Store the tick when this player reached their
        // current/final score.
        ScoreReachedTick.Set(
            player,
            Runner.Tick.Raw
        );

        Debug.Log(
            $"[RELIC SCORE] {player} scored +1. " +
            $"Total: {newScore}"
        );
    }

    public int GetScore(PlayerRef player)
    {
        if (Scores.TryGet(player, out int score))
            return score;

        return 0;
    }

    public int GetScoreReachedTick(PlayerRef player)
    {
        if (ScoreReachedTick.TryGet(
                player,
                out int tick))
        {
            return tick;
        }

        return 0;
    }

    public void InitializePlayer(PlayerRef player)
    {
        if (!HasStateAuthority)
            return;

        EnsurePlayerScoreExists(player);
    }

    public void ResetScores()
{
    if (!HasStateAuthority)
        return;

    Scores.Clear();
    ScoreReachedTick.Clear();

    _currentHolder = PlayerRef.None;
    _continuousHoldTime = 0f;
}
}