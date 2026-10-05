using System.Collections.Generic;
using System.Text;
using Fusion;
using UnityEngine;

public class AgawanRelicPlacementManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AgawanRelicScoreManager scoreManager;
    [SerializeField] private RelicController relicController;
    [SerializeField] private RoundStateManager roundStateManager;

    private PlayerRegistry playerRegistry;
    private NetworkRunner runner;

    private readonly Dictionary<PlayerRef, int> placements =
        new Dictionary<PlayerRef, int>();

    private bool initialized = false;

    private void OnEnable()
    {
        if (roundStateManager == null)
            return;

        roundStateManager.OnStateChanged += HandleStateChanged;
    }

    private void HandleStateChanged(
        RoundStateManager.RoundState state)
    {
        if (initialized)
            return;

        TryInitialize();
    }

    private void TryInitialize()
    {
        runner =
            NetworkRunner.GetRunnerForGameObject(gameObject);

        if (runner == null || !runner.IsRunning)
            return;

        playerRegistry = PlayerRegistry.Instance;

        if (playerRegistry == null)
            return;

        initialized = true;
    }

    private void OnDestroy()
    {
        if (roundStateManager != null)
        {
            roundStateManager.OnStateChanged -= HandleStateChanged;
        }
    }

    // ---------------------------------
    // Final Placement
    // ---------------------------------

    public List<RoundPlacement> BuildFinalPlacements()
    {
        List<RoundPlacement> result =
            new List<RoundPlacement>();

        if (runner == null ||
            playerRegistry == null ||
            scoreManager == null ||
            relicController == null)
        {
            return result;
        }

        placements.Clear();

        PlayerRef currentHolder =
            relicController.CurrentHolder;

        // ---------------------------------
        // Collect players
        // ---------------------------------

        List<PlayerScoreData> players =
            new List<PlayerScoreData>();

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            int score =
                scoreManager.GetScore(player);

            int reachedTick =
                scoreManager.GetScoreReachedTick(player);

            players.Add(
                new PlayerScoreData(
                    player,
                    score,
                    reachedTick
                )
            );
        }

        // ---------------------------------
        // Special Buzzer Rule
        // ---------------------------------

        if (currentHolder != PlayerRef.None)
        {
            // Holder is ALWAYS 1st.
            placements[currentHolder] = 1;

            // Remove holder from normal ranking.
            players.RemoveAll(
                player => player.Player == currentHolder
            );
        }

        // ---------------------------------
        // Rank Remaining Players
        // ---------------------------------

        players.Sort((a, b) =>
        {
            int scoreComparison =
                b.Score.CompareTo(a.Score);

            if (scoreComparison != 0)
                return scoreComparison;

            return a.ReachedTick.CompareTo(
                b.ReachedTick
            );
        });

        int startingPlacement =
            currentHolder != PlayerRef.None
                ? 2
                : 1;

        int currentPlacement = startingPlacement;

        for (int i = 0; i < players.Count; i++)
        {
            if (i > 0)
            {
                PlayerScoreData previous =
                    players[i - 1];

                PlayerScoreData current =
                    players[i];

                bool tied =
                    previous.Score == current.Score &&
                    previous.ReachedTick ==
                    current.ReachedTick;

                if (!tied)
                {
                    currentPlacement = startingPlacement + i;
                }
            }

            placements[players[i].Player] =
                currentPlacement;

            result.Add(
                new RoundPlacement
                {
                    Player = players[i].Player,
                    Placement = currentPlacement
                }
            );
        }

        // Add holder to result AFTER normal ranking.
        if (currentHolder != PlayerRef.None)
        {
            result.Add(
                new RoundPlacement
                {
                    Player = currentHolder,
                    Placement = 1
                }
            );
        }

        // If nobody held the Relic,
        // all players were ranked normally.
        if (currentHolder == PlayerRef.None)
        {
            result.Clear();

            foreach (PlayerScoreData player in players)
            {
                result.Add(
                    new RoundPlacement
                    {
                        Player = player.Player,
                        Placement =
                            placements[player.Player]
                    }
                );
            }
        }

        LogFinalPlacements();

        return result;
    }

    // ---------------------------------
    // Placement Access
    // ---------------------------------

    public int GetPlacement(PlayerRef player)
    {
        if (placements.TryGetValue(
            player,
            out int placement))
        {
            return placement;
        }

        return 0;
    }

    public void ResetPlacement()
    {
        placements.Clear();
    }

    // ---------------------------------
    // Debug
    // ---------------------------------

    private void LogFinalPlacements()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine(
            "<color=yellow>" +
            "[RELIC PLACEMENT] Final Placements:" +
            "</color>"
        );

        foreach (
            KeyValuePair<PlayerRef, int> entry
            in placements)
        {
            sb.AppendLine(
                $"<color=yellow>" +
                $" - {entry.Key} → " +
                $"{entry.Value} Place" +
                $"</color>"
            );
        }

        Debug.Log(sb.ToString());
    }

    // ---------------------------------
    // Player Score Data
    // ---------------------------------

    private struct PlayerScoreData
    {
        public PlayerRef Player;
        public int Score;
        public int ReachedTick;

        public PlayerScoreData(
            PlayerRef player,
            int score,
            int reachedTick)
        {
            Player = player;
            Score = score;
            ReachedTick = reachedTick;
        }
    }
}