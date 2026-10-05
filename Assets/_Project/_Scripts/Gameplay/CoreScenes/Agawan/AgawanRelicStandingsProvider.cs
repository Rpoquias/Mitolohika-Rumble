using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class AgawanRelicStandingsProvider :
    MonoBehaviour,
    IStandingsProvider
{
    [Header("References")]
    [SerializeField]
    private AgawanRelicScoreManager scoreManager;

    [SerializeField]
    private RelicController relicController;

    public event Action StandingsChanged;

    public string SecondaryHeaderLabel =>
        "HOLD TIME";

    public bool IsReady
    {
        get
        {
            return scoreManager != null &&
                   scoreManager.Object != null &&
                   scoreManager.Object.IsValid;
        }
    }

    private NetworkRunner runner;

    private void Awake()
    {
        runner =
            NetworkRunner.GetRunnerForGameObject(
                gameObject
            );
    }

    private void OnEnable()
    {
        if (scoreManager != null)
        {
            scoreManager.OnScoresChanged +=
                HandleScoresChanged;
        }
    }

    private void OnDisable()
    {
        if (scoreManager != null)
        {
            scoreManager.OnScoresChanged -=
                HandleScoresChanged;
        }
    }

    private void HandleScoresChanged()
    {
        StandingsChanged?.Invoke();
    }

    public void GetStandings(
        List<StandingEntry> buffer)
    {
        buffer.Clear();

        if (!IsReady)
            return;

        if (runner == null)
        {
            runner =
                NetworkRunner.GetRunnerForGameObject(
                    scoreManager.gameObject
                );
        }

        if (runner == null ||
            !runner.IsRunning)
        {
            return;
        }

        List<PlayerRef> players =
            new List<PlayerRef>();

        foreach (PlayerRef player
                 in runner.ActivePlayers)
        {
            players.Add(player);
        }

        // Highest relic score first.
        players.Sort(
            (a, b) =>
            {
                int scoreA =
                    scoreManager.GetScore(a);

                int scoreB =
                    scoreManager.GetScore(b);

                int comparison =
                    scoreB.CompareTo(scoreA);

                if (comparison != 0)
                    return comparison;

                return a.PlayerId.CompareTo(
                    b.PlayerId
                );
            }
        );

        for (int i = 0;
             i < players.Count;
             i++)
        {
            PlayerRef player =
                players[i];

            int score =
                scoreManager.GetScore(player);

            bool holding =
                relicController != null &&
                relicController.CurrentHolder == player;

            buffer.Add(
                new StandingEntry
                {
                    Player = player,

                    Placement = i + 1,

                    Status =
                        holding
                            ? ""
                            : "",

                    Detail =
                        $"{score}s"
                }
            );
        }
    }
}