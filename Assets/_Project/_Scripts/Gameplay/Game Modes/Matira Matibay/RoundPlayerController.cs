using Fusion;
using UnityEngine;

public class RoundPlayerController : MonoBehaviour
{
    [Header("Player Registry")]
    [SerializeField] private PlayerRegistry playerRegistry;

    [Header("Round State")]
    [SerializeField] private RoundStateManager roundStateManager;

    private bool initialized = false;
    private NetworkRunner runner;

    private void Update()
    {
        if (!initialized)
        {
            TryInitialize();
            return;
        }

        // Keep movement synchronized with the current round state.
        ApplyMovementState(
            roundStateManager.CurrentState
            == RoundStateManager.RoundState.Playing
        );
    }

    private void TryInitialize()
    {
        runner =
            NetworkRunner.GetRunnerForGameObject(gameObject);

        if (runner == null || !runner.IsRunning)
            return;

        if (playerRegistry == null)
        {
            playerRegistry =
                runner.GetComponentInChildren<PlayerRegistry>();
        }

        if (playerRegistry == null)
            return;

        if (roundStateManager == null)
            return;

        initialized = true;

        Debug.Log(
            $"[ROUND PLAYER CONTROLLER] " +
            $"Connected to Runner {runner.name}"
        );

        ApplyMovementState(
            roundStateManager.CurrentState
            == RoundStateManager.RoundState.Playing
        );
    }

    private void ApplyMovementState(bool canMove)
    {
        if (playerRegistry == null)
            return;

        foreach (NetworkObject playerObject in playerRegistry.Players)
        {
            if (playerObject == null)
                continue;

            PlayerMovement movement =
                playerObject.GetComponent<PlayerMovement>();

            if (movement != null)
            {
                movement.SetCanMove(canMove);
            }
        }
    }
}