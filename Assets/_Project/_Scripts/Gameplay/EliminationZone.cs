using Fusion;
using UnityEngine;

public class EliminationZone : MonoBehaviour
{
    [SerializeField] private WinnerDetector winnerDetector;
    [SerializeField] private RoundStateManager roundStateManager;

    private NetworkRunner runner;

    private void Update()
    {
        if (runner == null)
        {
            runner = NetworkRunner.GetRunnerForGameObject(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (runner == null || !runner.IsRunning)
            return;

        if (!runner.IsServer)
            return;

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
    }
}