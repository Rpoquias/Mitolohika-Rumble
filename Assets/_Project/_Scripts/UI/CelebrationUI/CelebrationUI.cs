using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class CelebrationUI : MonoBehaviour
{
    [Header("Player Cards")]
    [SerializeField] private Transform playerCardContainer;
    [SerializeField] private CelebrationPlayerCardUI playerCardPrefab;

    [Header("Characters")]
    [SerializeField] private CharacterData[] characters;

    private MatchSession matchSession;
    private NetworkRunner runner;

    private readonly List<CelebrationPlayerCardUI> playerCards =
        new List<CelebrationPlayerCardUI>();

    private bool initialized;

    private void Update()
    {
        if (initialized)
            return;

        TryInitialize();
    }

    private void TryInitialize()
    {
        if (NetworkManager.Instance == null)
            return;

        runner = NetworkManager.Instance.Runner;

        if (runner == null ||
            !runner.IsRunning)
        {
            return;
        }

        matchSession =
            NetworkManager.Instance.CurrentMatchSession;

        if (matchSession == null)
            return;

        if (matchSession.Object == null ||
            !matchSession.Object.IsValid)
        {
            return;
        }

        initialized = true;

        BuildCelebrationUI();
    }

    private void BuildCelebrationUI()
    {
        ClearCards();

        List<PlayerCelebrationData> players =
            new List<PlayerCelebrationData>();

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (!matchSession.TryGetPlacementRecord(
                    player,
                    out MatchPlayerPlacementRecord record))
            {
                continue;
            }

            if (!matchSession.TryGetOverallPlacement(
                    player,
                    out int overallPlacement))
            {
                continue;
            }

         

            players.Add(
                new PlayerCelebrationData
                {
                    Player = player,
                    Record = record,
                    OverallPlacement = overallPlacement
                }
            );
        }

        players.Sort(
            (a, b) =>
            {
                int placementCompare =
                    a.OverallPlacement.CompareTo(
                        b.OverallPlacement
                    );

                if (placementCompare != 0)
                    return placementCompare;

                return a.Player.PlayerId.CompareTo(
                    b.Player.PlayerId
                );
            }
        );

        foreach (PlayerCelebrationData player in players)
        {
            CreatePlayerCard(player);
        }
    }

private void CreatePlayerCard(PlayerCelebrationData player)
{
    if (playerCardPrefab == null ||
        playerCardContainer == null)
    {
        return;
    }

    CelebrationPlayerCardUI card = Instantiate(
        playerCardPrefab,
        playerCardContainer
    );

    card.Setup(
        player.Player,
        player.Record,
        player.OverallPlacement
    );

    playerCards.Add(card);
}
    private void ClearCards()
    {
        foreach (CelebrationPlayerCardUI card in playerCards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }

        playerCards.Clear();
    }

    private struct PlayerCelebrationData
    {
        public PlayerRef Player;
        public string CharacterName;
        public MatchPlayerPlacementRecord Record;
        public int OverallPlacement;
    }
}