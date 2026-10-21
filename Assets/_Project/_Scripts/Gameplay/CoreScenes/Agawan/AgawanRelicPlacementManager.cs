using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class AgawanRelicPlacementManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AgawanRelicScoreManager scoreManager;
    [SerializeField] private RelicController relicController;

    private readonly Dictionary<PlayerRef, int> _placements = new();
    private readonly List<PlayerScoreData> _ranked = new(4);

    private NetworkRunner _runner;

    // ---------------------------------
    // Final Placement
    // ---------------------------------

    public List<RoundPlacement> BuildFinalPlacements()
    {
        List<RoundPlacement> result = new List<RoundPlacement>(4);

        _placements.Clear();

        // Resolved on demand: no event subscription or state-change polling needed.
        if (_runner == null)
            _runner = NetworkRunner.GetRunnerForGameObject(gameObject);

        if (_runner == null || scoreManager == null || relicController == null)
            return result;

        PlayerRef holder = relicController.CurrentHolder;
        bool hasHolder = holder != PlayerRef.None;

        // Buzzer rule: whoever holds the relic when time runs out is always 1st.
        if (hasHolder)
        {
            _placements[holder] = 1;
            result.Add(new RoundPlacement { Player = holder, Placement = 1 });
        }

        _ranked.Clear();

        foreach (PlayerRef player in _runner.ActivePlayers)
        {
            if (hasHolder && player == holder)
                continue;

            _ranked.Add(new PlayerScoreData(
                player,
                scoreManager.GetScore(player),
                scoreManager.GetScoreReachedTick(player)
            ));
        }

        _ranked.Sort(CompareByScoreThenTick);

        int startingPlacement = hasHolder ? 2 : 1;
        int currentPlacement = startingPlacement;

        for (int i = 0; i < _ranked.Count; i++)
        {
            if (i > 0)
            {
                PlayerScoreData previous = _ranked[i - 1];
                PlayerScoreData current = _ranked[i];

                bool tied =
                    previous.Score == current.Score &&
                    previous.ReachedTick == current.ReachedTick;

                if (!tied)
                    currentPlacement = startingPlacement + i;
            }

            _placements[_ranked[i].Player] = currentPlacement;

            result.Add(new RoundPlacement
            {
                Player = _ranked[i].Player,
                Placement = currentPlacement
            });
        }

        _ranked.Clear();
        return result;
    }

    private static int CompareByScoreThenTick(PlayerScoreData a, PlayerScoreData b)
    {
        int scoreComparison = b.Score.CompareTo(a.Score);

        return scoreComparison != 0
            ? scoreComparison
            : a.ReachedTick.CompareTo(b.ReachedTick);
    }

    // ---------------------------------
    // Placement Access
    // ---------------------------------

    public int GetPlacement(PlayerRef player)
    {
        return _placements.TryGetValue(player, out int placement) ? placement : 0;
    }

    public void ResetPlacement()
    {
        _placements.Clear();
    }

    private readonly struct PlayerScoreData
    {
        public readonly PlayerRef Player;
        public readonly int Score;
        public readonly int ReachedTick;

        public PlayerScoreData(PlayerRef player, int score, int reachedTick)
        {
            Player = player;
            Score = score;
            ReachedTick = reachedTick;
        }
    }
}