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

        Debug.Log(
            $"[MATIRA MANAGER] Connected to Runner " +
            $"{runner.name}"
        );
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

    Debug.Log(
        "[MATIRA MANAGER] Matira Matibay started!"
    );

    roundEnded = false;

    placementManager.ResetPlacement();

    if (eliminationManager != null)
        eliminationManager.StartTracking();

    arenaShrinkController.StartShrinking();
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
        Debug.LogError(
            "[MATIRA MANAGER] Winner is invalid."
        );

        return;
    }

    if (roundEnded)
        return;

    roundEnded = true;

    eliminationManager?.StopTracking();

    arenaShrinkController.StopShrinking();

    placementManager.AssignFinalPlacements(winner);

    currentPlacements.Clear();

    foreach (PlayerRef player in runner.ActivePlayers)
    {
        int placement = placementManager.GetPlacement(player);

        if (placement <= 0)
        {
            Debug.LogWarning(
                $"[MATIRA MANAGER] Missing placement for {player}."
            );

            continue;
        }

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
    Debug.Log(
        "[MATIRA MANAGER] " +
        "Matira Matibay round ended."
    );

    if (runner == null ||
        !runner.IsServer)
    {
        return;
    }

    if (currentPlacements.Count == 0)
    {
        Debug.LogError(
            "[MATIRA MANAGER] " +
            "No placements received."
        );

        return;
    }

    if (MatchFlowManager.Instance == null)
    {
        Debug.LogError(
            "[MATIRA MANAGER] " +
            "MatchFlowManager is missing."
        );

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
            Debug.LogError(
                "[MATIRA MANAGER] " +
                "SpawnPointManager is NULL."
            );

            return;
        }

        foreach (PlayerRef player
                 in runner.ActivePlayers)
        {
            if (!runner.TryGetPlayerObject(
                    player,
                    out NetworkObject playerObject))
            {
                Debug.LogWarning(
                    $"[MATIRA MANAGER] " +
                    $"No PlayerObject for {player}."
                );

                continue;
            }

            PlayerElimination elimination =
                playerObject.GetComponent<PlayerElimination>();

            if (elimination == null)
            {
                Debug.LogWarning(
                    $"[MATIRA MANAGER] " +
                    $"{playerObject.name} has no " +
                    $"PlayerElimination."
                );

                continue;
            }

      Transform spawnPoint =
    spawnPointManager.GetSpawnPoint(player, runner);

            if (spawnPoint == null)
            {
                Debug.LogWarning(
                    $"[MATIRA MANAGER] " +
                    $"No spawn point for {player}."
                );

                continue;
            }

            elimination.ResetPlayer(
                spawnPoint.position,
                spawnPoint.rotation
            );

            Debug.Log(
                $"[MATIRA MANAGER] Reset {player} → " +
                $"{spawnPoint.name}"
            );
        }
    }
}