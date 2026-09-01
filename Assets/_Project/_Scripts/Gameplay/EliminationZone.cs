using UnityEngine;

public class EliminationZone : MonoBehaviour
{
    [SerializeField] private WinnerDetector winnerDetector;

    private void OnTriggerEnter(Collider other)
    {
        PlayerElimination player = other.GetComponentInParent<PlayerElimination>();

        if (player != null)
        {
            player.Eliminate();

            winnerDetector.CheckForWinner();
        }
    }
}