using UnityEngine;

public class EliminationZone : MonoBehaviour
{
    [SerializeField] private WinnerDetector winnerDetector;
    [SerializeField] private RoundStateManager roundStateManager;

    private void OnTriggerEnter(Collider other)
    {
        if (roundStateManager != null &&
            roundStateManager.CurrentState !=
            RoundStateManager.RoundState.Playing)
        {
            return;
        }

        PlayerElimination player =
            other.GetComponentInParent<PlayerElimination>();

        if (player == null || player.IsEliminated)
            return;

        player.Eliminate();

        if (winnerDetector != null)
        {
            winnerDetector.CheckForWinner();
        }
    }
}
