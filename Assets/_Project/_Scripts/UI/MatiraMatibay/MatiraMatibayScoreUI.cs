using Fusion;
using TMPro;
using UnityEngine;

public class MatiraMatibayScoreUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MatiraMatibayScoreManager scoreManager;
    [SerializeField] private TMP_Text survivalScoreText;
    [SerializeField] private TMP_Text knockoutCreditText;

    private NetworkRunner runner;
    private bool initialized;

    private void Update()
    {
        if (!initialized)
        {
            TryInitialize();
            return;
        }

        UpdateScoreDisplay();
    }

    private void TryInitialize()
    {
        if (scoreManager == null)
            return;

        runner =
            NetworkRunner.GetRunnerForGameObject(
                scoreManager.gameObject
            );

        if (runner == null || !runner.IsRunning)
            return;

       if (scoreManager.Object == null)
    return;

        initialized = true;
    }

    private void UpdateScoreDisplay()
    {
        if (runner == null)
            return;

        PlayerRef localPlayer =
            runner.LocalPlayer;

        for (int i = 0;
             i < scoreManager.NetworkScoreCount;
             i++)
        {
            if (!scoreManager.TryGetNetworkScore(
                    i,
                    out MatiraMatibayScoreManager.NetworkPlayerScore score))
            {
                continue;
            }

            if (score.player != localPlayer)
                continue;

            if (survivalScoreText != null)
            {
                survivalScoreText.text =
                    $"SURVIVAL: {score.survivalScore}";
            }

            if (knockoutCreditText != null)
            {
                knockoutCreditText.text =
                    $"KNOCKOUTS: {score.knockoutCredit}";
            }

            return;
        }
    }
}