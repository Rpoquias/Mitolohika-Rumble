using System;
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
        ResetLocalState();
    }

    private void OnScoresNetworkChanged()
    {
        OnScoresChanged?.Invoke();
    }

    public void StartScoring()
    {
        if (!HasStateAuthority)
            return;

        ResetLocalState();
        _scoringActive = true;
    }

    public void StopScoring()
    {
        if (!HasStateAuthority || !_scoringActive)
            return;

        ResetLocalState();
    }

    public override void FixedUpdateNetwork()
    {
        // _scoringActive is only ever set on the authority, so it is checked first.
        if (!_scoringActive || !HasStateAuthority || relicController == null)
            return;

        PlayerRef holder = relicController.CurrentHolder;

        if (holder != _currentHolder)
        {
            _currentHolder = holder;
            _continuousHoldTime = 0f;

            // Dictionary entries are only created when the holder changes,
            // not every tick.
            if (holder != PlayerRef.None)
                EnsurePlayerScoreExists(holder);

            return;
        }

        if (holder == PlayerRef.None)
            return;

        _continuousHoldTime += Runner.DeltaTime;

        if (_continuousHoldTime < secondsPerPoint)
            return;

        while (_continuousHoldTime >= secondsPerPoint)
        {
            _continuousHoldTime -= secondsPerPoint;
            AddPoint(holder);
        }
    }

    private void EnsurePlayerScoreExists(PlayerRef player)
    {
        if (!Scores.ContainsKey(player))
            Scores.Add(player, 0);

        if (!ScoreReachedTick.ContainsKey(player))
            ScoreReachedTick.Add(player, 0);
    }

    private void AddPoint(PlayerRef player)
    {
        Scores.Set(player, Scores[player] + 1);

        // Tick when this player reached their current score (tie-breaker).
        ScoreReachedTick.Set(player, Runner.Tick.Raw);
    }

    public int GetScore(PlayerRef player)
    {
        return Scores.TryGet(player, out int score) ? score : 0;
    }

    public int GetScoreReachedTick(PlayerRef player)
    {
        return ScoreReachedTick.TryGet(player, out int tick) ? tick : 0;
    }

    public void InitializePlayer(PlayerRef player)
    {
        if (HasStateAuthority)
            EnsurePlayerScoreExists(player);
    }

    public void ResetScores()
    {
        if (!HasStateAuthority)
            return;

        Scores.Clear();
        ScoreReachedTick.Clear();

        ResetLocalState();
    }

    private void ResetLocalState()
    {
        _currentHolder = PlayerRef.None;
        _continuousHoldTime = 0f;
        _scoringActive = false;
    }
}