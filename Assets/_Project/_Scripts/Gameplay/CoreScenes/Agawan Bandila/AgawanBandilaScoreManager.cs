using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class AgawanBandilaScoreManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private AgawanBandilaManager flagManager;

    [Networked, Capacity(4), OnChangedRender(nameof(OnScoresNetworkChanged))]
    public NetworkDictionary<PlayerRef, int> Scores => default;

    // Server only: the tick when each player's CURRENT score was reached.
    // Used as the tie-break (earlier = better). Clients never fill this.
    private readonly Dictionary<PlayerRef, int> _scoreTicks =
        new Dictionary<PlayerRef, int>();

    private bool scoringActive;

    public event Action OnScoresChanged;

    private void OnScoresNetworkChanged()
    {
        OnScoresChanged?.Invoke();
    }

    public override void Spawned()
    {
        if (!HasStateAuthority)
            return;

        Scores.Clear();
        _scoreTicks.Clear();
        scoringActive = false;
    }

    public void StartScoring()
    {
        if (!HasStateAuthority)
            return;

        scoringActive = true;
        _scoreTicks.Clear();

        foreach (PlayerRef player in Runner.ActivePlayers)
            InitializePlayer(player);
    }

    public void StopScoring()
    {
        if (!HasStateAuthority)
            return;

        scoringActive = false;
    }

    // No FixedUpdateNetwork polling: bases call this when their
    // stored flags change, so the score updates in the same tick.
    public void NotifyStoredFlagsChanged(PlayerRef owner)
    {
        if (!HasStateAuthority || !scoringActive || flagManager == null)
            return;

        if (owner == PlayerRef.None)
            return;

        UpdateScore(owner, flagManager.GetStoredFlagCount(owner));
    }

    private void UpdateScore(PlayerRef player, int score)
    {
        int tick = Runner.Tick.Raw;

        if (Scores.TryGet(player, out int current))
        {
            if (current == score)
                return;

            Scores.Set(player, score);
            _scoreTicks[player] = tick;
            return;
        }

        Scores.Add(player, score);
        _scoreTicks[player] = tick;

    
    }

    // Tick when the player's current score was last attained.
    public int GetScoreTick(PlayerRef player)
    {
        return _scoreTicks.TryGetValue(player, out int tick)
            ? tick
            : int.MaxValue;
    }

    public int GetScore(PlayerRef player)
    {
        return Scores.TryGet(player, out int score) ? score : 0;
    }

    public void InitializePlayer(PlayerRef player)
    {
        if (!HasStateAuthority)
            return;

        int score =
            flagManager != null
                ? flagManager.GetStoredFlagCount(player)
                : 0;

        UpdateScore(player, score);
    }

    public void ResetScores()
    {
        if (!HasStateAuthority)
            return;

        Scores.Clear();
        _scoreTicks.Clear();
        scoringActive = false;

      
    }
}