using System.Collections.Generic;
using UnityEngine;

public class MatiraMatibayManager : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private RoundStateManager roundStateManager;
    [SerializeField] private ArenaShrinkController arenaShrinkController;
    [SerializeField] private WinnerDetector winnerDetector;
[SerializeField] private MatiraMatibayPlacementManager placementManager;
[SerializeField] private MatiraMatibayScoreManager scoreManager;
[SerializeField] private List<PlayerElimination> players;

public MatiraMatibayRoundResult CurrentResult { get; private set; }
    private bool roundEnded = false;

    private void OnEnable()
    {
        roundStateManager.OnRoundStarted += HandleRoundStarted;
        roundStateManager.OnRoundEnded += HandleRoundEnded;
        roundStateManager.OnRoundReset += HandleRoundReset;
    }

    private void OnDisable()
    {
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

    foreach (PlayerElimination player in players)
    {
        player.ResetPlayer();
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

    foreach (PlayerElimination player in players)
    {
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