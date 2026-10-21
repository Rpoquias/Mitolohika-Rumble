using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class AgawanBandilaStandingsProvider :
    MonoBehaviour,
    IStandingsProvider
{
    [Header("References")]
    [SerializeField] private AgawanBandilaScoreManager scoreManager;

    public event Action StandingsChanged;

    public string SecondaryHeaderLabel => "FLAGS";

    public bool IsReady =>
        scoreManager != null &&
        scoreManager.Object != null &&
        scoreManager.Object.IsValid;

    private NetworkRunner runner;

    // Reused every call: no list or delegate allocations.
    private readonly List<PlayerRef> _players = new List<PlayerRef>(4);
    private Comparison<PlayerRef> _comparison;

    // Cached score strings so the HUD doesn't allocate per refresh.
    private static readonly string[] ScoreStrings = BuildScoreStrings(32);

    private static string[] BuildScoreStrings(int count)
    {
        string[] strings = new string[count];

        for (int i = 0; i < count; i++)
            strings[i] = i.ToString();

        return strings;
    }

    private static string ScoreToString(int score)
    {
        return score >= 0 && score < ScoreStrings.Length
            ? ScoreStrings[score]
            : score.ToString();
    }

    private void Awake()
    {
        runner = NetworkRunner.GetRunnerForGameObject(gameObject);
        _comparison = CompareByScore;
    }

    private void OnEnable()
    {
        if (scoreManager != null)
            scoreManager.OnScoresChanged += HandleScoresChanged;
    }

    private void OnDisable()
    {
        if (scoreManager != null)
            scoreManager.OnScoresChanged -= HandleScoresChanged;
    }

    private void HandleScoresChanged()
    {
        StandingsChanged?.Invoke();
    }

    // Score high to low, then PlayerId.
    private int CompareByScore(PlayerRef a, PlayerRef b)
    {
        int comparison =
            scoreManager.GetScore(b).CompareTo(scoreManager.GetScore(a));

        return comparison != 0
            ? comparison
            : a.PlayerId.CompareTo(b.PlayerId);
    }

    public void GetStandings(List<StandingEntry> buffer)
    {
        buffer.Clear();

        if (!IsReady)
            return;

        if (runner == null)
        {
            runner = NetworkRunner.GetRunnerForGameObject(
                scoreManager.gameObject
            );
        }

        if (runner == null || !runner.IsRunning)
            return;

        _players.Clear();

        foreach (PlayerRef player in runner.ActivePlayers)
            _players.Add(player);

        _players.Sort(_comparison);

        for (int i = 0; i < _players.Count; i++)
        {
            PlayerRef player = _players[i];

            buffer.Add(new StandingEntry
            {
                Player = player,
                Placement = i + 1,
                Status = "",
                Detail = ScoreToString(scoreManager.GetScore(player))
            });
        }
    }
}