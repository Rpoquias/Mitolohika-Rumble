using System.Collections.Generic;
using UnityEngine;

public class MatiraMatibayPlacementManager : MonoBehaviour
{
    private List<PlayerElimination> eliminationOrder = new List<PlayerElimination>();

[SerializeField] private List<PlayerElimination> players;
    private Dictionary<PlayerElimination, int> placements =
        new Dictionary<PlayerElimination, int>();


private void OnEnable()
{
    foreach (PlayerElimination player in players)
    {
        player.OnPlayerEliminated += RecordElimination;
    }
}

private void OnDisable()
{
    foreach (PlayerElimination player in players)
    {
        player.OnPlayerEliminated -= RecordElimination;
    }
}
  private void RecordElimination(PlayerElimination player)
{
    eliminationOrder.Add(player);

    Debug.Log(
        player.gameObject.name +
        " elimination order: " +
        eliminationOrder.Count
    );
}
    public void AssignFinalPlacements(PlayerElimination winner)
    {
        if (placements.ContainsKey(winner))
            return;

        // Winner is always 1st Place
        placements[winner] = 1;

        // Work backwards through elimination order
        int placement = 2;

        for (int i = eliminationOrder.Count - 1; i >= 0; i--)
        {
            PlayerElimination player = eliminationOrder[i];

            if (player == winner)
                continue;

            placements[player] = placement;
            placement++;
        }

        DebugPlacements();
    }

    public int GetPlacement(PlayerElimination player)
    {
        if (placements.TryGetValue(player, out int placement))
            return placement;

        return 0;
    }
    

    public void ResetPlacement()
    {
        eliminationOrder.Clear();
        placements.Clear();
    }

    private void DebugPlacements()
    {
        foreach (KeyValuePair<PlayerElimination, int> entry in placements)
        {
            Debug.Log(
                entry.Key.gameObject.name +
                " → " +
                entry.Value +
                " Place"
            );
        }
    }
}