
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

    private readonly List<StandingEntry> entries = new();
    private readonly List<LeaderboardUICard> cards = new();

    private bool initialized;

    private void Awake()
    {
        standingsProvider = providerSource as IStandingsProvider;

        // The panel is controlled only by the standing button.
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        if (standingButton != null)
            standingButton.onClick.AddListener(ToggleLeaderboard);
    }

    private void OnEnable()
    {
        if (standingsProvider == null)
            return;

        standingsProvider.StandingsChanged += HandleStandingsChanged;

        TryInitialize();
    }

    private void OnDisable()
    {
        if (standingsProvider != null)
            standingsProvider.StandingsChanged -= HandleStandingsChanged;

        initialized = false;
    }

    private void OnDestroy()
    {
        if (standingButton != null)
            standingButton.onClick.RemoveListener(ToggleLeaderboard);
    }

    private void Update()
    {
        if (!initialized)
            TryInitialize();
    }

    // ============================================================
    // INITIALIZATION
    // ============================================================

    private void TryInitialize()
    {
        if (standingsProvider == null ||
            !standingsProvider.IsReady ||
            cardPrefab == null ||
            cardContainer == null)
        {
            return;
        }

        if (runner == null)
        {
            if (providerSource != null)
            {
                runner = NetworkRunner.GetRunnerForGameObject(
                    providerSource.gameObject
                );
            }

            if (runner == null && NetworkManager.Instance != null)
                runner = NetworkManager.Instance.Runner;
        }

        if (runner == null || !runner.IsRunning)
            return;

        initialized = true;

        UpdateHeader();
        Refresh();
    }

    // ============================================================
    // HEADER
    // ============================================================

    private void UpdateHeader()
    {
        if (secondaryHeaderText != null)
            secondaryHeaderText.text = standingsProvider.SecondaryHeaderLabel;
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
        if (!initialized ||
            standingsProvider == null ||
            !standingsProvider.IsReady)
        {
            return;
        }

        standingsProvider.GetStandings(entries);
        DisplayEntries();
    }

    // ============================================================
    // DISPLAY
    // ============================================================

   private void DisplayEntries()
{
    EnsureCardCount(entries.Count);

    for (int i = 0; i < cards.Count; i++)
    {
        if (i >= entries.Count)
        {
            cards[i].Clear();
            continue;
        }

        StandingEntry entry = entries[i];

        cards[i].gameObject.SetActive(true);

        string playerName = GetPlayerName(entry.Player);

        cards[i].SetData(
            entry,
            playerName,
            entry.Player
        );
    }
}
    private void EnsureCardCount(int requiredCount)
    {
        while (cards.Count < requiredCount)
        {
            LeaderboardUICard card = Instantiate(
                cardPrefab,
                cardContainer
            );

            cards.Add(card);
        }
    }

    // ============================================================
    // PLAYER NAME
    // ============================================================

private string GetPlayerName(PlayerRef player)
{
    string playerName = MatchSession.NameOf(player);

    if (string.IsNullOrWhiteSpace(playerName))
        playerName = $"Player {player.PlayerId}";

    if (runner != null && player == runner.LocalPlayer)
        return $"{playerName} (YOU)";

    return playerName;
}

    // ============================================================
    // PANEL TOGGLE
    // ============================================================

    private void ToggleLeaderboard()
    {
        if (leaderboardPanel == null)
            return;

        leaderboardPanel.SetActive(!leaderboardPanel.activeSelf);
    }
}
