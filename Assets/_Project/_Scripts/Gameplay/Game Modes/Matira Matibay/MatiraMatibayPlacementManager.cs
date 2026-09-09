using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class MatiraMatibayPlacementManager : MonoBehaviour
{
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
        // Find the Runner associated with this GameObject.
        runner = NetworkRunner.GetRunnerForGameObject(gameObject);

        if (runner == null || !runner.IsRunning)
            return;

        // Find the registry belonging to THIS Runner.
        playerRegistry =
            runner.GetComponentInChildren<PlayerRegistry>();

        if (playerRegistry == null)
        {
            Debug.LogWarning(
                $"[PLACEMENT] No PlayerRegistry found for Runner {runner.name}."
            );

            return;
        }

        Debug.Log(
            $"[PLACEMENT] Connected to registry for Runner {runner.name}. " +
            $"Existing players: {playerRegistry.Players.Count}"
        );

        playerRegistry.OnPlayerRegistered += RegisterPlayer;
        playerRegistry.OnPlayerUnregistered += UnregisterPlayer;

        // Register players that already exist.
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

    private void RegisterPlayer(NetworkObject playerObject)
    {
        if (playerObject == null)
            return;

        PlayerElimination player =
            playerObject.GetComponent<PlayerElimination>();

        if (player == null)
        {
            Debug.LogWarning(
                $"[PLACEMENT] {playerObject.name} has no PlayerElimination."
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

    private void UnregisterPlayer(NetworkObject playerObject)
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

    private void RecordElimination(PlayerElimination player)
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

    public void AssignFinalPlacements(PlayerElimination winner)
    {
        if (winner == null)
            return;

        if (placements.ContainsKey(winner))
            return;

        // Winner is always 1st.
        placements[winner] = 1;

        int placement = 2;

        // Work backwards through elimination order.
        for (int i = eliminationOrder.Count - 1; i >= 0; i--)
        {
            PlayerElimination player = eliminationOrder[i];

            if (player == winner)
                continue;

            placements[player] = placement;
            placement++;
        }

        DebugPlacements();
    }

    public int GetPlacement(PlayerElimination player)
    {
        if (placements.TryGetValue(player, out int placement))
            return placement;

        return 0;
    }

    public void ResetPlacement()
    {
        eliminationOrder.Clear();
        placements.Clear();
    }

    private void DebugPlacements()
    {
        foreach (KeyValuePair<PlayerElimination, int> entry in placements)
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