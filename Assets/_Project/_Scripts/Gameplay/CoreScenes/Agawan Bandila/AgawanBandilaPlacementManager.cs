using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class AgawanBandilaPlacementManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AgawanBandilaScoreManager scoreManager;

    private readonly List<RoundPlacement> placements =
        new List<RoundPlacement>();

    public List<RoundPlacement> BuildFinalPlacements()
    {
        placements.Clear();

        if (scoreManager == null)
        {
            Debug.LogError(
                "[BANDILA PLACEMENT] " +
                "ScoreManager is NULL."
            );

            return placements;
        }

        NetworkRunner runner =
            NetworkManager.Instance.Runner;

        if (runner == null ||
            !runner.IsRunning ||
            !runner.IsServer)
        {
            return placements;
        }

        List<PlayerResult> results =
            new List<PlayerResult>();

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            int score =
                scoreManager.GetScore(player);

            int reachedTick =
                scoreManager.GetFirstReachedTick(
                    player,
                    score
                );

            results.Add(
                new PlayerResult
                {
                    Player = player,
                    Score = score,
                    ReachedTick = reachedTick
                }
            );
        }

        results = results
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.ReachedTick)
            .ToList();

        int currentPlacement = 1;

        for (int i = 0; i < results.Count; i++)
        {
            if (i > 0)
            {
                PlayerResult previous =
                    results[i - 1];

                PlayerResult current =
                    results[i];

                bool sameScore =
                    current.Score == previous.Score;

                bool sameReachedTick =
                    current.ReachedTick ==
                    previous.ReachedTick;

                if (!sameScore ||
                    !sameReachedTick)
                {
                    currentPlacement = i + 1;
                }
            }

            placements.Add(
                new RoundPlacement
                {
                    Player = results[i].Player,
                    Placement = currentPlacement
                }
            );

            Debug.Log(
                $"[BANDILA PLACEMENT] " +
                $"{results[i].Player} | " +
                $"Flags: {results[i].Score} | " +
                $"Reached Tick: {results[i].ReachedTick} | " +
                $"Placement: {currentPlacement}"
            );
        }

        return placements;
    }

    private struct PlayerResult
    {
        public PlayerRef Player;
        public int Score;
        public int ReachedTick;
    }
}