using Fusion;
using UnityEngine;

public class MatiraMatibayManager : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private RoundStateManager roundStateManager;
    [SerializeField] private ArenaShrinkController arenaShrinkController;
    [SerializeField] private WinnerDetector winnerDetector;
    [SerializeField] private MatiraMatibayPlacementManager placementManager;
    [SerializeField] private MatiraMatibayScoreManager scoreManager;

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
    }

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

        playerRegistry =
            runner.GetComponentInChildren<PlayerRegistry>();

        if (playerRegistry == null)
            return;

        initialized = true;

        Debug.Log(
            $"[MATIRA MANAGER] Connected to registry for Runner " +
            $"{runner.name}. Current players: " +
            $"{playerRegistry.Players.Count}"
        );
    }

    private void OnDestroy()
    {
        if (roundStateManager == null)
            return;

        roundStateManager.OnRoundStarted -= HandleRoundStarted;
        roundStateManager.OnRoundEnded -= HandleRoundEnded;
        roundStateManager.OnRoundReset -= HandleRoundReset;
    }

    private void HandleRoundStarted()
    {
        Debug.Log("Matira Matibay started!");

        roundEnded = false;

        placementManager.ResetPlacement();
        winnerDetector.ResetWinnerDetector();

        scoreManager.StartScoring();

        arenaShrinkController.StartShrinking();
    }

    private void HandleRoundEnded()
    {
        Debug.Log("Matira Matibay ended!");

        scoreManager.StopScoring();
        arenaShrinkController.StopShrinking();

        roundStateManager.RestartRound();
    }

    private void HandleRoundReset()
    {
        Debug.Log("Resetting Matira Matibibay!");

        CurrentResult = null;

        arenaShrinkController.ResetArena();

        if (playerRegistry == null)
        {
            Debug.LogWarning(
                "[MATIRA MANAGER] Round reset skipped player " +
                "respawn because PlayerRegistry is not ready."
            );
            return;
        }

        foreach (NetworkObject playerObject in playerRegistry.Players)
        {
            if (playerObject == null)
                continue;

            PlayerElimination player =
                playerObject.GetComponent<PlayerElimination>();

            if (player != null)
            {
                player.ResetPlayer();
            }
        }
    }

    public void EndRound(PlayerElimination winner)
    {
        if (roundEnded)
            return;

        roundEnded = true;

        scoreManager.StopScoring();
        arenaShrinkController.StopShrinking();

        placementManager.AssignFinalPlacements(winner);

        CurrentResult = CreateRoundResult();

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

            result.results.Add(playerResult);
        }

        result.results.Sort(
            (a, b) => a.placement.CompareTo(b.placement)
        );

        return result;
    }
}