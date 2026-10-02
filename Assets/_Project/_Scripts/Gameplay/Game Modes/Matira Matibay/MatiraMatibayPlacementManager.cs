using System.Collections.Generic;
using System.Text;
using Fusion;
using UnityEngine;

public class MatiraMatibayPlacementManager : MonoBehaviour
{
    [Header("Score")]
    [SerializeField] private MatiraMatibayScoreManager scoreManager;

    [SerializeField] private RoundStateManager roundStateManager;
    private PlayerRegistry playerRegistry;
    private NetworkRunner runner;

    private readonly List<PlayerElimination> eliminationOrder =
        new List<PlayerElimination>();

    private readonly Dictionary<PlayerElimination, int> placements =
        new Dictionary<PlayerElimination, int>();

    private bool initialized = false;

    private void OnEnable()
    {
        if (roundStateManager == null)
            return;

        roundStateManager.OnStateChanged += HandleStateChanged;
    }

    private void HandleStateChanged(RoundStateManager.RoundState state)
    {
        if (initialized)
            return;

        TryInitialize();
    }

    private void TryInitialize()
    {
        runner = NetworkRunner.GetRunnerForGameObject(gameObject);

        if (runner == null || !runner.IsRunning)
            return;

        playerRegistry = PlayerRegistry.Instance;

        if (playerRegistry == null)
            return;

        playerRegistry.OnPlayerRegistered += RegisterPlayer;
        playerRegistry.OnPlayerUnregistered += UnregisterPlayer;

        foreach (NetworkObject playerObject in playerRegistry.Players)
        {
            RegisterPlayer(playerObject);
        }

        initialized = true;
    }

    private void OnDestroy()
    {
        if (roundStateManager != null)
        {
            roundStateManager.OnStateChanged -= HandleStateChanged;
        }

        if (playerRegistry == null)
            return;

        playerRegistry.OnPlayerRegistered -= RegisterPlayer;
        playerRegistry.OnPlayerUnregistered -= UnregisterPlayer;

        foreach (NetworkObject playerObject in playerRegistry.Players)
        {
            UnregisterPlayer(playerObject);
        }
    }

    // ---------------------------------
    // Player Registration
    // ---------------------------------

    private void RegisterPlayer(NetworkObject playerObject)
    {
        if (playerObject == null)
            return;

        PlayerElimination player = playerObject.GetComponent<PlayerElimination>();

        if (player == null)
            return;

        if (eliminationOrder.Contains(player))
            return;

        player.OnPlayerEliminated += RecordElimination;
    }

    private void UnregisterPlayer(NetworkObject playerObject)
    {
        if (playerObject == null)
            return;

        PlayerElimination player = playerObject.GetComponent<PlayerElimination>();

        if (player == null)
            return;

        player.OnPlayerEliminated -= RecordElimination;

        eliminationOrder.Remove(player);
        placements.Remove(player);
    }

    private void RecordElimination(PlayerElimination player)
    {
        if (player == null)
            return;

        if (eliminationOrder.Contains(player))
            return;

        eliminationOrder.Add(player);
    }

    // ---------------------------------
    // Final Placement
    // ---------------------------------

    public void AssignFinalPlacements(PlayerElimination winner)
    {
        if (winner == null || scoreManager == null || playerRegistry == null)
            return;

        placements.Clear();

        // Winner is ALWAYS 1st place
        placements[winner] = 1;

        // Collect remaining players
        List<PlayerElimination> remainingPlayers = new List<PlayerElimination>();

        foreach (NetworkObject playerObject in playerRegistry.Players)
        {
            if (playerObject == null)
                continue;

            PlayerElimination player = playerObject.GetComponent<PlayerElimination>();

            if (player == null || player == winner)
                continue;

            remainingPlayers.Add(player);
        }

        // Sort by Overall Score descending
        remainingPlayers.Sort((a, b) =>
        {
            int scoreA = scoreManager.GetOverallScore(a);
            int scoreB = scoreManager.GetOverallScore(b);
            return scoreB.CompareTo(scoreA);
        });

        // Dense Ranking starting from 2nd place
        int currentPlacement = 2;
        int? previousScore = null;

        for (int i = 0; i < remainingPlayers.Count; i++)
        {
            PlayerElimination player = remainingPlayers[i];
            int playerScore = scoreManager.GetOverallScore(player);

            if (previousScore.HasValue && playerScore != previousScore.Value)
            {
                currentPlacement++;
            }

            placements[player] = currentPlacement;
            previousScore = playerScore;
        }

        LogFinalPlacements();
    }

    // ---------------------------------
    // Placement Access
    // ---------------------------------

    public int GetPlacement(PlayerElimination player)
    {
        if (placements.TryGetValue(player, out int placement))
        {
            return placement;
        }

        return 0;
    }

    public void ResetPlacement()
    {
        eliminationOrder.Clear();
        placements.Clear();
    }

    // ---------------------------------
    // Debug
    // ---------------------------------

    private void LogFinalPlacements()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<color=red>[PLACEMENT] Final Placements Assigned:</color>");

        foreach (KeyValuePair<PlayerElimination, int> entry in placements)
        {
            sb.AppendLine($"<color=red> - {entry.Key.gameObject.name} → {entry.Value} Place</color>");
        }

        Debug.Log(sb.ToString());
    }
}