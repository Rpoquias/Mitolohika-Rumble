using System.Collections.Generic;
using UnityEngine;

public class WinnerDetector : MonoBehaviour
{
    [SerializeField] private List<PlayerElimination> players;
    [SerializeField] private MatiraMatibayManager matiraMatibayManager;

    private bool winnerFound = false;

    public void CheckForWinner()
    {
        if (winnerFound)
            return;

        int aliveCount = 0;
        PlayerElimination lastAlivePlayer = null;

        foreach (PlayerElimination player in players)
        {
            if (!player.IsEliminated)
            {
                aliveCount++;
                lastAlivePlayer = player;
            }
        }

        Debug.Log("Alive players: " + aliveCount);

        if (aliveCount == 1)
        {
            winnerFound = true;

            Debug.Log("WINNER: " + lastAlivePlayer.gameObject.name);

            matiraMatibayManager.EndRound(lastAlivePlayer);
        }
    }

    public void ResetWinnerDetector()
    {
        winnerFound = false;
    }
}