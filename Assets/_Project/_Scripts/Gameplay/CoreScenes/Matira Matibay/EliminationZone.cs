using Fusion;
using UnityEngine;

public class EliminationZone : MonoBehaviour
{
    [SerializeField] private RoundStateManager roundStateManager;

    private NetworkRunner runner;

    private void OnTriggerEnter(Collider other)
    {
        if (runner == null)
        {
            runner = NetworkRunner.GetRunnerForGameObject(gameObject);
        }

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

if (player == null || !player.Object.IsValid || player.Object.Runner == null)
    return;

if (player.IsEliminated)
    return;

player.Eliminate();
    }
}