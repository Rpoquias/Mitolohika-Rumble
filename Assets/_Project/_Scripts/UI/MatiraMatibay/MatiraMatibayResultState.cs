using Fusion;
using UnityEngine;

public class MatiraMatibayResultState : NetworkBehaviour
{
    public struct NetworkResult : INetworkStruct
    {
        public PlayerRef player;
        public int placement;
        public int survivalScore;
            public int overallScore;
        public int knockoutCredit;
    }

    [Networked, Capacity(4)]
    private NetworkArray<NetworkResult> NetworkResults => default;

    [Networked]
    private int NetworkResultCount { get; set; }

    [Networked]
    public bool IsShowingResult { get; private set; }

    public int ResultCount => NetworkResultCount;

    public bool TryGetResult(
        int index,
        out NetworkResult result)
    {
        if (index < 0 || index >= NetworkResultCount)
        {
            result = default;
            return false;
        }

        result = NetworkResults[index];
        return true;
    }

    public void ShowResult(
        MatiraMatibayRoundResult roundResult)
    {
        if (!HasStateAuthority)
            return;

        ClearResults();

        int index = 0;

        foreach (
            MatiraMatibayRoundResult.PlayerResult result
            in roundResult.results)
        {
            if (result.player == null)
                continue;

            if (index >= 4)
                break;

            if (result.player.Object == null)
                continue;

            PlayerRef player =
                result.player.Object.InputAuthority;

          NetworkResult networkResult =
    new NetworkResult
    {
        player = player,
        placement = result.placement,
        survivalScore = result.survivalScore,
        overallScore = result.overallScore,
        knockoutCredit = result.knockoutCredit
    };

            NetworkResults.Set(
                index,
                networkResult
            );

            index++;
        }

        NetworkResultCount = index;
        IsShowingResult = true;

        Debug.Log(
            $"[RESULT] Showing round result for " +
            $"{NetworkResultCount} players."
        );
    }

    public void HideResult()
    {
        if (!HasStateAuthority)
            return;

        IsShowingResult = false;
        ClearResults();
    }

    private void ClearResults()
    {
        NetworkResultCount = 0;

        for (int i = 0; i < 4; i++)
        {
            NetworkResults.Set(
                i,
                default
            );
        }
    }
}