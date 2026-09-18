using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class MatiraMatibayLeaderboardUI : MonoBehaviour
{
[System.Serializable]
private class LeaderboardEntry
{
    public PlayerRef player;
    public int placement;
    public int overallScore;
}

    [Header("References")]
    [SerializeField] private MatiraMatibayScoreManager scoreManager;

    [Header("Leaderboard")]
    [SerializeField] private MatiraMatibayLeaderboardUICard cardPrefab;
    [SerializeField] private Transform cardContainer;

    private NetworkRunner runner;
    private bool initialized;

    private readonly List<LeaderboardEntry> entries =
        new List<LeaderboardEntry>();

    private readonly List<MatiraMatibayLeaderboardUICard> cards =
        new List<MatiraMatibayLeaderboardUICard>();

    private void Update()
    {
        if (!initialized)
        {
            TryInitialize();
            return;
        }

        UpdateLeaderboard();
    }

    private void TryInitialize()
    {
        if (scoreManager == null)
            return;

        runner =
            NetworkRunner.GetRunnerForGameObject(
                scoreManager.gameObject
            );

        if (runner == null || !runner.IsRunning)
            return;

        if (scoreManager.Object == null)
            return;

        if (cardPrefab == null)
            return;

        if (cardContainer == null)
            return;

        initialized = true;
    }

    private void UpdateLeaderboard()
    {
        BuildEntries();
        SortEntries();
        DisplayEntries();
    }

   private void BuildEntries()
{
    entries.Clear();

    int count =
        scoreManager.NetworkScoreCount;

    for (int i = 0; i < count; i++)
    {
        if (!scoreManager.TryGetNetworkScore(
                i,
                out MatiraMatibayScoreManager.NetworkPlayerScore score))
        {
            continue;
        }

       entries.Add(
    new LeaderboardEntry
    {
        player = score.player,
        placement =
            scoreManager.GetLivePlacement(score.player),
        overallScore = score.overallScore
    }
);
    }
}

 private void SortEntries()
{
    entries.Sort(
        (a, b) =>
            a.placement.CompareTo(b.placement)
    );
}

    private void DisplayEntries()
    {
        EnsureCardCount(entries.Count);

        for (int i = 0; i < cards.Count; i++)
        {
            if (i >= entries.Count)
            {
                cards[i].Clear();
                cards[i].gameObject.SetActive(false);
                continue;
            }

            LeaderboardEntry entry =
                entries[i];

            cards[i].gameObject.SetActive(true);

            cards[i].SetData(
                GetPlacementText(entry.placement),
                GetPlayerName(entry.player),
                entry.overallScore
            );
        }
    }

    private void EnsureCardCount(int requiredCount)
    {
        while (cards.Count < requiredCount)
        {
            MatiraMatibayLeaderboardUICard card =
                Instantiate(
                    cardPrefab,
                    cardContainer
                );

            cards.Add(card);
        }
    }

    private string GetPlacementText(int placement)
    {
        switch (placement)
        {
            case 1:
                return "1ST";

            case 2:
                return "2ND";

            case 3:
                return "3RD";

            case 4:
                return "4TH";

            default:
                return $"{placement}TH";
        }
    }

    private string GetPlayerName(PlayerRef player)
    {
        if (player == runner.LocalPlayer)
            return "YOU";

        return $"PLAYER {player.PlayerId}";
    }
}