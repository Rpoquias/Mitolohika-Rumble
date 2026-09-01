using System.Collections.Generic;

public class MatiraMatibayRoundResult
{
    public class PlayerResult
    {
        public PlayerElimination player;
        public int placement;
        public int survivalScore;
        public int knockoutCredit;
    }

    public List<PlayerResult> results = new List<PlayerResult>();

    public PlayerResult GetPlayerResult(PlayerElimination player)
    {
        foreach (PlayerResult result in results)
        {
            if (result.player == player)
                return result;
        }

        return null;
    }
}