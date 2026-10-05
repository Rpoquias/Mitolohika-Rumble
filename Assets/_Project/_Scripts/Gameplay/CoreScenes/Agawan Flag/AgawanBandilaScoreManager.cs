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

private readonly Dictionary<PlayerRef, Dictionary<int, int>>
    firstReachedTicks =
        new Dictionary<PlayerRef, Dictionary<int, int>>();    private bool scoringActive;

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
    firstReachedTicks.Clear();
    scoringActive = false;
}
   public void StartScoring()
{
    if (!HasStateAuthority)
        return;

    scoringActive = true;
    firstReachedTicks.Clear();

    foreach (PlayerRef player in Runner.ActivePlayers)
    {
        InitializePlayer(player);
    }

    Debug.Log(
        "[BANDILA SCORE] Scoring started."
    );
}
    public void StopScoring()
    {
        if (!HasStateAuthority)
            return;

        scoringActive = false;

        Debug.Log(
            "[BANDILA SCORE] Scoring stopped."
        );
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (!scoringActive)
            return;

        if (flagManager == null)
            return;

        NetworkRunner runner = Runner;

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            int storedFlags =
                flagManager.GetStoredFlagCount(player);

            UpdateScore(player, storedFlags);
        }
    }

   private void UpdateScore(
    PlayerRef player,
    int score)
{
    if (!firstReachedTicks.TryGetValue(
            player,
            out Dictionary<int, int> history))
    {
        history =
            new Dictionary<int, int>();

        firstReachedTicks.Add(
            player,
            history
        );
    }

    if (!history.ContainsKey(score))
    {
        history.Add(
            score,
            Runner.Tick.Raw
        );
    }

    if (Scores.ContainsKey(player))
    {
        if (Scores[player] != score)
        {
            Scores.Set(
                player,
                score
            );

            Debug.Log(
                $"[BANDILA SCORE] " +
                $"{player} score updated: {score}"
            );
        }

        return;
    }

    Scores.Add(
        player,
        score
    );

    Debug.Log(
        $"[BANDILA SCORE] " +
        $"{player} score initialized: {score}"
    );
}

public int GetFirstReachedTick(
    PlayerRef player,
    int score)
{
    if (!firstReachedTicks.TryGetValue(
            player,
            out Dictionary<int, int> history))
    {
        return int.MaxValue;
    }

    if (history.TryGetValue(
            score,
            out int tick))
    {
        return tick;
    }

    return int.MaxValue;
}

    public int GetScore(PlayerRef player)
    {
        if (Scores.TryGet(player, out int score))
            return score;

        return 0;
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
    firstReachedTicks.Clear();
    scoringActive = false;

    Debug.Log(
        "[BANDILA SCORE] Scores reset."
    );
}
}