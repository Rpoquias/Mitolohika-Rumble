using Fusion;
using UnityEngine;

public class RoundPlayerController : MonoBehaviour
{
    [Header("Round State")]
    [SerializeField] private RoundStateManager roundStateManager;

    private bool initialized = false;
    private NetworkRunner runner;
    private PlayerRegistry playerRegistry;

    private void Update()
    {
        if (!initialized)
        {
            TryInitialize();
            return;
        }

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

      playerRegistry = PlayerRegistry.Instance;

        if (playerRegistry == null)
            return;

        if (roundStateManager == null)
            return;

        if (!roundStateManager.IsSpawned)
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