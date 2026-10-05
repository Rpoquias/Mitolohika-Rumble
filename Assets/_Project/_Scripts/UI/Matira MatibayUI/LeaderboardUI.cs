using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Provider")]
    [SerializeField] private MonoBehaviour providerSource;

    [Header("Leaderboard Panel")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Button standingButton;

    [Header("Headers")]
    [SerializeField] private TextMeshProUGUI secondaryHeaderText;

    [Header("Leaderboard")]
    [SerializeField] private LeaderboardUICard cardPrefab;
    [SerializeField] private Transform cardContainer;

    private IStandingsProvider standingsProvider;

    private NetworkRunner runner;

    private readonly List<StandingEntry> entries =
        new List<StandingEntry>();

    private readonly List<LeaderboardUICard> cards =
        new List<LeaderboardUICard>();

    private bool initialized;

    // ============================================================
    // UNITY
    // ============================================================
private void Awake()
{
    standingsProvider =
        providerSource as IStandingsProvider;


    if (standingButton != null)
    {
        standingButton.onClick.AddListener(
            ToggleLeaderboard
        );
    }

}

    private void OnEnable()
    {
        if (standingsProvider == null)
            return;

        standingsProvider.StandingsChanged +=
            HandleStandingsChanged;

        TryInitialize();
    }

    private void OnDisable()
    {
        if (standingsProvider != null)
        {
            standingsProvider.StandingsChanged -=
                HandleStandingsChanged;
        }

        initialized = false;
    }

    private void OnDestroy()
    {
        if (standingButton != null)
        {
            standingButton.onClick.RemoveListener(
                ToggleLeaderboard
            );
        }
    }

    private void Update()
    {
        if (initialized)
            return;

        TryInitialize();
    }

    // ============================================================
    // INITIALIZATION
    // ============================================================

    private void TryInitialize()
    {
        if (standingsProvider == null)
            return;

        if (!standingsProvider.IsReady)
            return;

        if (cardPrefab == null)
            return;

        if (cardContainer == null)
            return;

        if (runner == null)
        {
            if (providerSource != null)
            {
                runner =
                    NetworkRunner.GetRunnerForGameObject(
                        providerSource.gameObject
                    );
            }

            if (runner == null &&
                NetworkManager.Instance != null)
            {
                runner =
                    NetworkManager.Instance.Runner;
            }
        }

        if (runner == null ||
            !runner.IsRunning)
        {
            return;
        }

        initialized = true;

        UpdateHeader();
        Refresh();
    }

    // ============================================================
    // HEADER
    // ============================================================

    private void UpdateHeader()
    {
        if (secondaryHeaderText == null)
            return;

        secondaryHeaderText.text =
            standingsProvider.SecondaryHeaderLabel;
    }

    // ============================================================
    // STANDINGS
    // ============================================================

    private void HandleStandingsChanged()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (!initialized)
            return;

        if (standingsProvider == null ||
            !standingsProvider.IsReady)
        {
            return;
        }

        standingsProvider.GetStandings(
            entries
        );

        DisplayEntries();
    }

    // ============================================================
    // DISPLAY
    // ============================================================

    private void DisplayEntries()
    {
        EnsureCardCount(
            entries.Count
        );

        for (int i = 0;
             i < cards.Count;
             i++)
        {
            if (i >= entries.Count)
            {
                cards[i].Clear();
                cards[i].gameObject.SetActive(false);
                continue;
            }

            StandingEntry entry =
                entries[i];

            cards[i].gameObject.SetActive(true);

            cards[i].SetData(
                entry,
                GetPlayerName(entry.Player)
            );
        }
    }

    private void EnsureCardCount(
        int requiredCount)
    {
        while (cards.Count < requiredCount)
        {
            LeaderboardUICard card =
                Instantiate(
                    cardPrefab,
                    cardContainer
                );

            cards.Add(card);
        }
    }

    // ============================================================
    // PLAYER NAME
    // ============================================================

    private string GetPlayerName(
        PlayerRef player)
    {
        if (runner != null &&
            player == runner.LocalPlayer)
        {
            return "YOU";
        }

        return $"PLAYER {player.PlayerId}";
    }

    // ============================================================
    // PANEL TOGGLE
    // ============================================================

    private void ToggleLeaderboard()
    {
        if (leaderboardPanel == null)
            return;

        bool shouldShow =
            !leaderboardPanel.activeSelf;

        leaderboardPanel.SetActive(
            shouldShow
        );
    }
}