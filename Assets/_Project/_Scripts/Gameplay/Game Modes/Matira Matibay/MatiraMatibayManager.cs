using Fusion;
using UnityEngine;
using System.Collections.Generic;
public class MatiraMatibayManager : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private RoundStateManager roundStateManager;
    [SerializeField] private ArenaShrinkController arenaShrinkController;
    [SerializeField] private WinnerDetector winnerDetector;
    [SerializeField] private MatiraMatibayPlacementManager placementManager;
    [SerializeField] private MatiraMatibayScoreManager scoreManager;
[SerializeField] private SpawnPointManager spawnPointManager;
[SerializeField] private MatiraMatibayResultState resultState;
    public MatiraMatibayRoundResult CurrentResult { get; private set; }

    private PlayerRegistry playerRegistry;
    private NetworkRunner runner;

    private bool initialized = false;
    private bool roundEnded = false;

    private void OnEnable()
{
    if (roundStateManager == null)
        return;

    roundStateManager.OnRoundStarted += HandleRoundStarted;
    roundStateManager.OnRoundEnded += HandleRoundEnded;
    roundStateManager.OnRoundReset += HandleRoundReset;
    roundStateManager.OnStateChanged += HandleStateChanged;
}

    private void TryInitialize()
    {
        runner =
            NetworkRunner.GetRunnerForGameObject(gameObject);

        if (runner == null || !runner.IsRunning)
            return;

       playerRegistry = PlayerRegistry.Instance;

        if (playerRegistry == null)
            return;

        initialized = true;

        Debug.Log(
            $"[MATIRA MANAGER] Connected to registry for Runner " +
            $"{runner.name}. Current players: " +
            $"{playerRegistry.Players.Count}"
        );
    }

private void HandleStateChanged(
    RoundStateManager.RoundState state)
{
    if (initialized)
        return;

    TryInitialize();
}
  private void OnDestroy()
{
    if (roundStateManager == null)
        return;

    roundStateManager.OnRoundStarted -= HandleRoundStarted;
    roundStateManager.OnRoundEnded -= HandleRoundEnded;
    roundStateManager.OnRoundReset -= HandleRoundReset;
    roundStateManager.OnStateChanged -= HandleStateChanged;
}

 private void HandleRoundStarted()
{
    Debug.Log("Matira Matibay started!");

    roundEnded = false;

    placementManager.ResetPlacement();

    scoreManager.StartScoring();

    arenaShrinkController.StartShrinking();
}
private void HandleRoundEnded()
{
    Debug.Log(
        "[MATIRA MANAGER] Matira Matibay round ended."
    );

    if (runner == null || !runner.IsServer)
    {
        return;
    }

    if (CurrentResult == null)
    {
        Debug.LogError(
            "[MATIRA MANAGER] CurrentResult is missing."
        );

        return;
    }

    if (MatchFlowManager.Instance == null)
    {
        Debug.LogError(
            "[MATIRA MANAGER] MatchFlowManager is missing."
        );

        return;
    }

    List<RoundPlacement> placements =
        new List<RoundPlacement>();

    foreach (
        MatiraMatibayRoundResult.PlayerResult playerResult
        in CurrentResult.results)
    {
        if (playerResult.player == null)
            continue;

        PlayerRef player =
            playerResult.player.Object.InputAuthority;

        if (!player.IsValid)
        {
            Debug.LogWarning(
                "[MATIRA MANAGER] Invalid PlayerRef " +
                $"for {playerResult.player.name}."
            );

            continue;
        }

        placements.Add(
            new RoundPlacement
            {
                Player = player,
                Placement = playerResult.placement
            }
        );
    }

    MatchFlowManager.Instance.HandleRoundComplete(
        placements
    );
}

private void HandleRoundReset()
{
    Debug.Log("[MATIRA MANAGER] Resetting Matira Matibay!");

    CurrentResult = null;

    if (resultState != null)
    {
        resultState.HideResult();
    }

    arenaShrinkController.ResetArena();

    ResetAllPlayers();
}private void ResetAllPlayers()
{
    if (runner == null || !runner.IsRunning)
        return;

    if (!runner.IsServer)
        return;

    if (spawnPointManager == null)
    {
        Debug.LogError(
            "[MATIRA MANAGER] SpawnPointManager is NULL."
        );

        return;
    }

    foreach (PlayerRef player in runner.ActivePlayers)
    {
        if (!runner.TryGetPlayerObject(
                player,
                out NetworkObject playerObject))
        {
            Debug.LogWarning(
                $"[MATIRA MANAGER] No PlayerObject for {player}."
            );

            continue;
        }

        PlayerElimination elimination =
            playerObject.GetComponent<PlayerElimination>();

        if (elimination == null)
        {
            Debug.LogWarning(
                $"[MATIRA MANAGER] {playerObject.name} has no " +
                $"PlayerElimination."
            );

            continue;
        }

        Transform spawnPoint =
            spawnPointManager.GetSpawnPoint(player);

        if (spawnPoint == null)
        {
            Debug.LogWarning(
                $"[MATIRA MANAGER] No spawn point for {player}."
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
 public void EndRound(PlayerElimination winner)
{
      if (runner == null || !runner.IsServer)
        return;

    if (roundEnded)
        return;
    roundEnded = true;

scoreManager.StopScoring();
arenaShrinkController.StopShrinking();

scoreManager.AwardWinnerBonus(winner);

placementManager.AssignFinalPlacements(winner);

CurrentResult = CreateRoundResult();
    if (resultState != null)
    {
        resultState.ShowResult(CurrentResult);
    }

    roundStateManager.EndRound();
}
    public MatiraMatibayRoundResult CreateRoundResult()
{
    MatiraMatibayRoundResult result =
        new MatiraMatibayRoundResult();

    if (playerRegistry == null)
    {
        Debug.LogWarning(
            "[MATIRA MANAGER] Cannot create result. " +
            "PlayerRegistry is not initialized."
        );

        return result;
    }

    foreach (NetworkObject playerObject in playerRegistry.Players)
    {
        if (playerObject == null)
            continue;

        PlayerElimination player =
            playerObject.GetComponent<PlayerElimination>();

        if (player == null)
            continue;

        MatiraMatibayRoundResult.PlayerResult playerResult =
            new MatiraMatibayRoundResult.PlayerResult();

        playerResult.player = player;

        playerResult.placement =
            placementManager.GetPlacement(player);

        playerResult.survivalScore =
            scoreManager.GetSurvivalScore(player);

        playerResult.knockoutCredit =
            scoreManager.GetKnockoutCredit(player);

        playerResult.winnerBonus =
            scoreManager.GetWinnerBonus(player);

        playerResult.overallScore =
            scoreManager.GetOverallScore(player);

        result.results.Add(playerResult);
    }

    result.results.Sort(
        (a, b) =>
            a.placement.CompareTo(b.placement)
    );

    return result;
}
}