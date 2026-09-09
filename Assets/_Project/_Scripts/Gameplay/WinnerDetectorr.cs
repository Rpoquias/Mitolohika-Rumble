using Fusion;
using UnityEngine;

public class WinnerDetector : MonoBehaviour
{
    [SerializeField] private MatiraMatibayManager matiraMatibayManager;

    private PlayerRegistry playerRegistry;
    private NetworkRunner runner;

    private bool initialized = false;
    private bool winnerFound = false;

    private void Update()
    {
        if (initialized)
            return;

        TryInitialize();
    }

    private void TryInitialize()
    {
        runner =
            NetworkRunner.GetRunnerForGameObject(gameObject);

        if (runner == null || !runner.IsRunning)
            return;

        playerRegistry =
            runner.GetComponentInChildren<PlayerRegistry>();

        if (playerRegistry == null)
            return;

        playerRegistry.OnPlayerRegistered += HandlePlayerRegistered;
        playerRegistry.OnPlayerUnregistered += HandlePlayerUnregistered;

        foreach (NetworkObject playerObject in playerRegistry.Players)
        {
            HandlePlayerRegistered(playerObject);
        }

        initialized = true;

        Debug.Log(
            $"[WINNER DETECTOR] Connected to registry for Runner " +
            $"{runner.name}. Current players: " +
            $"{playerRegistry.Players.Count}"
        );
    }

    private void OnDestroy()
    {
        if (playerRegistry == null)
            return;

        playerRegistry.OnPlayerRegistered -= HandlePlayerRegistered;
        playerRegistry.OnPlayerUnregistered -= HandlePlayerUnregistered;

        foreach (NetworkObject playerObject in playerRegistry.Players)
        {
            HandlePlayerUnregistered(playerObject);
        }
    }

    private void HandlePlayerRegistered(NetworkObject playerObject)
    {
        if (playerObject == null)
            return;

        PlayerElimination player =
            playerObject.GetComponent<PlayerElimination>();

        if (player == null)
            return;

        player.OnPlayerEliminated -= HandlePlayerEliminated;
        player.OnPlayerEliminated += HandlePlayerEliminated;
    }

    private void HandlePlayerUnregistered(NetworkObject playerObject)
    {
        if (playerObject == null)
            return;

        PlayerElimination player =
            playerObject.GetComponent<PlayerElimination>();

        if (player == null)
            return;

        player.OnPlayerEliminated -= HandlePlayerEliminated;
    }

    private void HandlePlayerEliminated(PlayerElimination player)
    {
        CheckForWinner();
    }

    public void CheckForWinner()
    {
        if (winnerFound)
            return;

        if (!initialized || playerRegistry == null)
            return;

        int aliveCount = 0;
        PlayerElimination lastAlivePlayer = null;

        foreach (NetworkObject playerObject in playerRegistry.Players)
        {
            if (playerObject == null)
                continue;

            PlayerElimination player =
                playerObject.GetComponent<PlayerElimination>();

            if (player == null)
                continue;

            if (!player.IsEliminated)
            {
                aliveCount++;
                lastAlivePlayer = player;
            }
        }

        Debug.Log(
            $"Alive players: {aliveCount}"
        );

        if (aliveCount == 1 && lastAlivePlayer != null)
        {
            winnerFound = true;

            Debug.Log(
                "WINNER: " +
                lastAlivePlayer.gameObject.name
            );

            matiraMatibayManager.EndRound(
                lastAlivePlayer
            );
        }
    }

    public void ResetWinnerDetector()
    {
        winnerFound = false;
    }
}