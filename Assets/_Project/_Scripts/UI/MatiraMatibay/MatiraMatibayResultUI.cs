
using Fusion;
using UnityEngine;

public class MatiraMatibayResultUI : MonoBehaviour
{
    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;

    [Header("Network State")]
    [SerializeField] private MatiraMatibayResultState resultState;

    [Header("Player Cards")]
    [SerializeField] private MatiraMatibayPlayerResultCard cardPrefab;
    [SerializeField] private Transform cardContainer;

    private NetworkRunner runner;
    private bool initialized;

    private bool lastShowingResult;
    private int lastResultCount = -1;

    private void Update()
    {
        if (!initialized)
        {
            TryInitialize();
            return;
        }

        UpdateResultDisplay();
    }

    private void TryInitialize()
    {
        if (resultState == null)
            return;

        runner =
            NetworkRunner.GetRunnerForGameObject(
                resultState.gameObject
            );

        if (runner == null || !runner.IsRunning)
            return;

        if (resultState.Object == null)
            return;

        initialized = true;

        resultPanel.SetActive(false);
    }

    private void UpdateResultDisplay()
    {
        bool showResult = resultState.IsShowingResult;

        // Only update the UI when the result state changes.
        if (showResult != lastShowingResult ||
            resultState.ResultCount != lastResultCount)
        {
            if (showResult)
            {
                ShowResult();
            }
            else
            {
                HideResult();
            }

            lastShowingResult = showResult;
            lastResultCount = resultState.ResultCount;
        }
    }

    private void ShowResult()
    {
        resultPanel.SetActive(true);

        ClearCards();

        for (int i = 0; i < resultState.ResultCount; i++)
        {
            if (!resultState.TryGetResult(
                    i,
                    out MatiraMatibayResultState.NetworkResult result))
            {
                continue;
            }

            CreateCard(result);
        }

        Debug.Log(
            $"[RESULT UI] Showing {resultState.ResultCount} player cards."
        );
    }

    private void HideResult()
    {
        resultPanel.SetActive(false);

        ClearCards();

        Debug.Log("[RESULT UI] Result panel hidden.");
    }
private void CreateCard(
    MatiraMatibayResultState.NetworkResult result)
{
    if (cardPrefab == null || cardContainer == null)
        return;

    MatiraMatibayPlayerResultCard card =
        Instantiate(cardPrefab, cardContainer);

    card.SetData(
        GetPlacementText(result.placement),
        GetPlayerName(result.player),
        result.overallScore,
        result.knockoutCredit
    );
}
    private string GetPlacementText(int placement)
    {
        switch (placement)
        {
            case 1:
                return "1ST";

            case 2:
                return "2ND";

            case 3:
                return "3RD";

            case 4:
                return "4TH";

            default:
                return $"{placement}TH";
        }
    }

    private string GetPlayerName(PlayerRef player)
    {
        if (player == runner.LocalPlayer)
            return "YOU";

        return $"PLAYER {player.PlayerId}";
    }

    private void ClearCards()
    {
        if (cardContainer == null)
            return;

        for (int i = cardContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(cardContainer.GetChild(i).gameObject);
        }
    }
}
