using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class MatiraMatibayPlacementManager : MonoBehaviour
{
    [Header("Score")]
    [SerializeField] private MatiraMatibayScoreManager scoreManager;

    private PlayerRegistry playerRegistry;
    private NetworkRunner runner;

    private readonly List<PlayerElimination> eliminationOrder =
        new List<PlayerElimination>();

    private readonly Dictionary<PlayerElimination, int> placements =
        new Dictionary<PlayerElimination, int>();

    private bool initialized = false;

    private void Update()
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
        {
            Debug.LogWarning(
                $"[PLACEMENT] No PlayerRegistry found for Runner " +
                $"{runner.name}."
            );

            return;
        }

        Debug.Log(
            $"[PLACEMENT] Connected to registry for Runner " +
            $"{runner.name}. Existing players: " +
            $"{playerRegistry.Players.Count}"
        );

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
        if (playerRegistry == null)
            return;

        playerRegistry.OnPlayerRegistered -= RegisterPlayer;
        playerRegistry.OnPlayerUnregistered -= UnregisterPlayer;
    }

    // ---------------------------------
    // Player Registration
    // ---------------------------------

    private void RegisterPlayer(
        NetworkObject playerObject)
    {
        if (playerObject == null)
            return;

        PlayerElimination player =
            playerObject.GetComponent<PlayerElimination>();

        if (player == null)
        {
            Debug.LogWarning(
                $"[PLACEMENT] {playerObject.name} has no " +
                "PlayerElimination."
            );

            return;
        }

        if (eliminationOrder.Contains(player))
            return;

        player.OnPlayerEliminated += RecordElimination;

        Debug.Log(
            $"[PLACEMENT] Tracking {playerObject.name}"
        );
    }

    private void UnregisterPlayer(
        NetworkObject playerObject)
    {
        if (playerObject == null)
            return;

        PlayerElimination player =
            playerObject.GetComponent<PlayerElimination>();

        if (player == null)
            return;

        player.OnPlayerEliminated -= RecordElimination;

        eliminationOrder.Remove(player);
        placements.Remove(player);

        Debug.Log(
            $"[PLACEMENT] Stopped tracking {playerObject.name}"
        );
    }

    private void RecordElimination(
        PlayerElimination player)
    {
        if (player == null)
            return;

        if (eliminationOrder.Contains(player))
            return;

        eliminationOrder.Add(player);

        Debug.Log(
            player.gameObject.name +
            " elimination order: " +
            eliminationOrder.Count
        );
    }

    // ---------------------------------
    // Final Placement
    // ---------------------------------

    public void AssignFinalPlacements(
        PlayerElimination winner)
    {
        if (winner == null)
            return;

        if (scoreManager == null)
        {
            Debug.LogError(
                "[PLACEMENT] ScoreManager is not assigned."
            );

            return;
        }

        if (playerRegistry == null)
        {
            Debug.LogError(
                "[PLACEMENT] PlayerRegistry is not initialized."
            );

            return;
        }

        placements.Clear();

        // ---------------------------------
        // Winner
        // ---------------------------------

        // The last survivor is ALWAYS 1st.
        placements[winner] = 1;

        // ---------------------------------
        // Remaining Players
        // ---------------------------------

        List<PlayerElimination> remainingPlayers =
            new List<PlayerElimination>();

        foreach (NetworkObject playerObject in playerRegistry.Players)
        {
            if (playerObject == null)
                continue;

            PlayerElimination player =
                playerObject.GetComponent<PlayerElimination>();

            if (player == null)
                continue;

            if (player == winner)
                continue;

            remainingPlayers.Add(player);
        }

        // ---------------------------------
        // Sort by Overall Score
        // ---------------------------------

        remainingPlayers.Sort(
            (a, b) =>
            {
                int scoreA =
                    scoreManager.GetOverallScore(a);

                int scoreB =
                    scoreManager.GetOverallScore(b);

                return scoreB.CompareTo(scoreA);
            }
        );

        // ---------------------------------
        // Dense Ranking
        // ---------------------------------

        int currentPlacement = 2;

        int? previousScore = null;

        for (int i = 0; i < remainingPlayers.Count; i++)
        {
            PlayerElimination player =
                remainingPlayers[i];

            int playerScore =
                scoreManager.GetOverallScore(player);

            // If the score changes, move to
            // the next placement.
            //
            // Example:
            //
            // 20 → 2nd
            // 20 → 2nd
            // 15 → 3rd
            //
            if (previousScore.HasValue &&
                playerScore != previousScore.Value)
            {
                currentPlacement++;
            }

            placements[player] =
                currentPlacement;

            previousScore =
                playerScore;
        }

        DebugPlacements();
    }

    // ---------------------------------
    // Placement Access
    // ---------------------------------

    public int GetPlacement(
        PlayerElimination player)
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
        eliminationOrder.Clear();
        placements.Clear();
    }

    // ---------------------------------
    // Debug
    // ---------------------------------

    private void DebugPlacements()
    {
        foreach (
            KeyValuePair<PlayerElimination, int> entry
            in placements)
        {
            Debug.Log(
                entry.Key.gameObject.name +
                " → " +
                entry.Value +
                " Place"
            );
        }
    }
}