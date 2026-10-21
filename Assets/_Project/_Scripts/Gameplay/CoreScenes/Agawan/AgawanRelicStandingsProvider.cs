using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class AgawanRelicStandingsProvider :
    MonoBehaviour,
    IStandingsProvider
{
    [Header("References")]
    [SerializeField] private AgawanRelicScoreManager scoreManager;

    // Kept for when you want to show a "holding" status icon/text again.
    [SerializeField] private RelicController relicController;

    public event Action StandingsChanged;

    public string SecondaryHeaderLabel => "HOLD TIME";

    public bool IsReady =>
        scoreManager != null &&
        scoreManager.Object != null &&
        scoreManager.Object.IsValid;

    private struct ScoreRow
    {
        public PlayerRef Player;
        public int Score;
    }

    // Cached comparison: highest score first, then lowest PlayerId for stability.
    private static readonly Comparison<ScoreRow> ByScoreDescending = (a, b) =>
    {
        int comparison = b.Score.CompareTo(a.Score);
        return comparison != 0
            ? comparison
            : a.Player.PlayerId.CompareTo(b.Player.PlayerId);
    };

    // Reused every call so GetStandings does not allocate.
    private readonly List<ScoreRow> _rows = new(4);
    private readonly List<string> _secondsLabels = new();

    private NetworkRunner _runner;

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

    public void GetStandings(List<StandingEntry> buffer)
    {
        buffer.Clear();

        if (!IsReady)
            return;

        if (_runner == null)
            _runner = NetworkRunner.GetRunnerForGameObject(scoreManager.gameObject);

        if (_runner == null || !_runner.IsRunning)
            return;

        _rows.Clear();

        // Read each score once instead of inside the sort comparer.
        foreach (PlayerRef player in _runner.ActivePlayers)
        {
            _rows.Add(new ScoreRow
            {
                Player = player,
                Score = scoreManager.GetScore(player)
            });
        }

        _rows.Sort(ByScoreDescending);

        for (int i = 0; i < _rows.Count; i++)
        {
            buffer.Add(new StandingEntry
            {
                Player = _rows[i].Player,
                Placement = i + 1,
                Status = string.Empty,
                Detail = GetSecondsLabel(_rows[i].Score)
            });
        }
    }

    private string GetSecondsLabel(int seconds)
    {
        if (seconds < 0)
            seconds = 0;

        while (_secondsLabels.Count <= seconds)
            _secondsLabels.Add($"{_secondsLabels.Count}s");

        return _secondsLabels[seconds];
    }
}