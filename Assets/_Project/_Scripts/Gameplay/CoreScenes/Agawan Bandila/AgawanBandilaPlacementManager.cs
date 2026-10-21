using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class AgawanBandilaPlacementManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AgawanBandilaScoreManager scoreManager;

    private struct PlayerResult
    {
        public PlayerRef Player;
        public int Score;
        public int ScoreTick;
    }

    // Highest score first; on a tie, whoever reached it first.
    private static int CompareResults(PlayerResult a, PlayerResult b)
    {
        int byScore = b.Score.CompareTo(a.Score);

        return byScore != 0
            ? byScore
            : a.ScoreTick.CompareTo(b.ScoreTick);
    }

    // Returns a NEW list every call, so callers (MatchFlowManager)
    // can keep it safely across rounds.
    public List<RoundPlacement> BuildFinalPlacements()
    {
        List<RoundPlacement> placements = new List<RoundPlacement>();

        if (scoreManager == null || NetworkManager.Instance == null)
            return placements;

        NetworkRunner runner = NetworkManager.Instance.Runner;

        if (runner == null || !runner.IsRunning || !runner.IsServer)
            return placements;

        List<PlayerResult> results = new List<PlayerResult>(4);

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            results.Add(new PlayerResult
            {
                Player = player,
                Score = scoreManager.GetScore(player),
                ScoreTick = scoreManager.GetScoreTick(player)
            });
        }

        results.Sort(CompareResults);

        int currentPlacement = 1;

        for (int i = 0; i < results.Count; i++)
        {
            if (i > 0)
            {
                PlayerResult previous = results[i - 1];
                PlayerResult current = results[i];

                if (current.Score != previous.Score ||
                    current.ScoreTick != previous.ScoreTick)
                {
                    currentPlacement = i + 1;
                }
            }

            placements.Add(new RoundPlacement
            {
                Player = results[i].Player,
                Placement = currentPlacement
            });
        }

        return placements;
    }
}