using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class MatiraMatibayManager : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private RoundStateManager roundStateManager;
    [SerializeField] private ArenaShrinkController arenaShrinkController;
    [SerializeField] private MatiraMatibayPlacementManager placementManager;
    [SerializeField] private MatiraMatibayEliminationManager eliminationManager;
    [SerializeField] private SpawnPointManager spawnPointManager;
[SerializeField] private MatiraMatibayHazardManager hazardManager;
    [Header("Spectator")]
    [SerializeField] private SpectatorController spectatorController;

    private NetworkRunner runner;

    private bool initialized;
    private bool roundEnded;

    private List<RoundPlacement> currentPlacements =
        new List<RoundPlacement>();

    // ============================================================
    // EVENTS
    // ============================================================

    private void OnEnable()
    {
        if (roundStateManager != null)
        {
            roundStateManager.OnRoundStarted +=
                HandleRoundStarted;

            roundStateManager.OnRoundEnded +=
                HandleRoundEnded;

            roundStateManager.OnRoundReset +=
                HandleRoundReset;

            roundStateManager.OnStateChanged +=
                HandleStateChanged;
        }

        if (eliminationManager != null)
        {
            eliminationManager.OnWinnerDecided += EndRound;
        }
    }

    private void OnDestroy()
    {
        if (roundStateManager != null)
        {
            roundStateManager.OnRoundStarted -=
                HandleRoundStarted;

            roundStateManager.OnRoundEnded -=
                HandleRoundEnded;

            roundStateManager.OnRoundReset -=
                HandleRoundReset;

            roundStateManager.OnStateChanged -=
                HandleStateChanged;
        }

        if (eliminationManager != null)
        {
            eliminationManager.OnWinnerDecided -= EndRound;
        }
    }

    // ============================================================
    // INITIALIZATION
    // ============================================================

    private void HandleStateChanged(
        RoundStateManager.RoundState state)
    {
        if (!initialized)
            TryInitialize();
    }

    private void TryInitialize()
    {
        runner =
            NetworkRunner.GetRunnerForGameObject(gameObject);

        if (runner == null || !runner.IsRunning)
            return;

        initialized = true;
    }

    // ============================================================
    // ROUND START
    // ============================================================

    private void HandleRoundStarted()
    {
        if (!initialized)
            TryInitialize();

        if (runner == null || !runner.IsServer)
            return;

        roundEnded = false;

        placementManager.ResetPlacement();

        if (eliminationManager != null)
            eliminationManager.StartTracking();

        arenaShrinkController.StartShrinking();
        arenaShrinkController.StartCrumbling();
        hazardManager?.StartHazards();
    }

    // ============================================================
    // ROUND END
    // ============================================================

    public void EndRound(PlayerRef winner)
    {
        if (!initialized)
            TryInitialize();

        if (runner == null || !runner.IsServer)
            return;

        if (!winner.IsValid || winner == PlayerRef.None)
        {
           

            return;
        }

        if (roundEnded)
            return;

        roundEnded = true;

        eliminationManager?.StopTracking();

        arenaShrinkController.StopShrinking();
        arenaShrinkController.StopCrumbling();
        hazardManager?.StopHazards();
        placementManager.AssignFinalPlacements(winner);

        currentPlacements.Clear();

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            int placement = placementManager.GetPlacement(player);

            if (placement <= 0)
                continue;

            currentPlacements.Add(
                new RoundPlacement
                {
                    Player = player,
                    Placement = placement
                }
            );
        }

        roundStateManager.EndRound();
        
    }

    // ============================================================
    // ROUND ENDED
    // ============================================================

    private void HandleRoundEnded()
    {
        if (runner == null ||
            !runner.IsServer)
        {
            return;
        }

        if (currentPlacements.Count == 0)
        {
           


            return;
        }

        if (MatchFlowManager.Instance == null)
        {
           
            return;
        }

        MatchFlowManager.Instance.HandleRoundComplete(
            new List<RoundPlacement>(
                currentPlacements
            )
        );
    }

    // ============================================================
    // RESET
    // ============================================================

    private void HandleRoundReset()
    {
        if (spectatorController != null)
            spectatorController.ResetSpectator();

        if (!initialized)
            TryInitialize();

        roundEnded = false;
        currentPlacements.Clear();

        if (eliminationManager != null)
            eliminationManager.ResetEliminations();

        if (placementManager != null)
            placementManager.ResetPlacement();

        if (arenaShrinkController != null)
            arenaShrinkController.ResetArena();
        hazardManager?.StopHazards();

        ResetAllPlayers();
    }

    // ============================================================
    // PLAYER RESET
    // ============================================================

    private void ResetAllPlayers()
    {
        if (runner == null ||
            !runner.IsRunning)
        {
            return;
        }

        if (!runner.IsServer)
            return;

        if (spawnPointManager == null)
        {
           

            return;
        }

        foreach (PlayerRef player
                 in runner.ActivePlayers)
        {
            if (!runner.TryGetPlayerObject(
                    player,
                    out NetworkObject playerObject))
            {
                continue;
            }

            PlayerElimination elimination =
                playerObject.GetComponent<PlayerElimination>();

            if (elimination == null)
                continue;

            Transform spawnPoint =
                spawnPointManager.GetSpawnPoint(player, runner);

            if (spawnPoint == null)
                continue;

            elimination.ResetPlayer(
                spawnPoint.position,
                spawnPoint.rotation
            );
        }
    }
}