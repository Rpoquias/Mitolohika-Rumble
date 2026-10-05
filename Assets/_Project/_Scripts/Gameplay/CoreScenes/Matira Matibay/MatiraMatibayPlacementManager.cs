using System.Collections.Generic;
using System.Text;
using Fusion;
using UnityEngine;

public class MatiraMatibayPlacementManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private MatiraMatibayEliminationManager eliminationManager;

    [SerializeField]
    private RoundStateManager roundStateManager;

    private NetworkRunner runner;

    private readonly Dictionary<PlayerRef, int> placements =
        new Dictionary<PlayerRef, int>();

    private bool initialized;

    private void OnEnable()
    {
        if (roundStateManager != null)
        {
            roundStateManager.OnStateChanged +=
                HandleStateChanged;
        }
    }

    private void OnDisable()
    {
        if (roundStateManager != null)
        {
            roundStateManager.OnStateChanged -=
                HandleStateChanged;
        }
    }

    private void HandleStateChanged(
        RoundStateManager.RoundState state)
    {
        if (!initialized)
            TryInitialize();
    }

    private void TryInitialize()
    {
        runner =
            NetworkRunner.GetRunnerForGameObject(
                gameObject
            );

        if (runner == null || !runner.IsRunning)
            return;

        initialized = true;
    }

    public void AssignFinalPlacements(
        PlayerRef winner)
    {
        if (!initialized)
            TryInitialize();

        placements.Clear();

        if (runner == null ||
            !runner.IsRunning ||
            !runner.IsServer)
        {
            return;
        }

        if (eliminationManager == null)
        {
            Debug.LogError(
                "[MATIRA PLACEMENT] " +
                "EliminationManager is NULL."
            );

            return;
        }

        if (winner == PlayerRef.None)
        {
            Debug.LogError(
                "[MATIRA PLACEMENT] " +
                "Winner is invalid."
            );

            return;
        }

        int totalPlayers = 0;

        foreach (PlayerRef player
                 in runner.ActivePlayers)
        {
            totalPlayers++;
        }

        if (totalPlayers == 0)
            return;

        // Last player alive.
        placements[winner] = 1;

        // Reverse elimination order.
        //
        // Last eliminated -> 2nd
        // Before that    -> 3rd
        // First eliminated -> 4th
        for (int i =
             eliminationManager.EliminationCount - 1;
             i >= 0;
             i--)
        {
            PlayerRef player =
                eliminationManager.EliminationOrder[i];

            placements[player] =
                totalPlayers - i;
        }

        LogFinalPlacements();
    }

    public int GetPlacement(
        PlayerRef player)
    {
        return placements.TryGetValue(
            player,
            out int placement)
            ? placement
            : 0;
    }

    public void ResetPlacement()
    {
        placements.Clear();
    }

    private void LogFinalPlacements()
    {
        StringBuilder sb =
            new StringBuilder();

        sb.AppendLine(
            "<color=red>" +
            "[MATIRA PLACEMENT] Final Placements:" +
            "</color>"
        );

        foreach (
            KeyValuePair<PlayerRef, int> entry
            in placements)
        {
            sb.AppendLine(
                $"<color=red>" +
                $" - {entry.Key} → " +
                $"{entry.Value} Place" +
                $"</color>"
            );
        }

        Debug.Log(sb.ToString());
    }
}