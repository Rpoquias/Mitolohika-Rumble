using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    public bool IsHost =>
        NetworkManager.Instance != null &&
        NetworkManager.Instance.Runner != null &&
        NetworkManager.Instance.Runner.IsServer;
private bool hasStartedGame = false;
    public bool AllPlayersReady
    {
        get
        {
            if (NetworkManager.Instance == null ||
                NetworkManager.Instance.Runner == null)
                return false;

            return CheckAllPlayersReady(
                NetworkManager.Instance.Runner
            );
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

 public void StartGame()
{
    Debug.Log("[LOBBY] START GAME BUTTON PRESSED");

    if (hasStartedGame)
    {
        Debug.LogWarning("[LOBBY] Game has already started.");
        return;
    }

    if (NetworkManager.Instance == null)
    {
        Debug.LogError("[LOBBY] NetworkManager.Instance is NULL.");
        return;
    }

    NetworkRunner runner =
        NetworkManager.Instance.Runner;

    if (runner == null)
    {
        Debug.LogError("[LOBBY] NetworkRunner is NULL.");
        return;
    }

    if (!runner.IsServer)
    {
        Debug.LogWarning(
            "[LOBBY] Only the Host can start the game."
        );
        return;
    }

    foreach (PlayerRef player in runner.ActivePlayers)
    {
        if (!runner.TryGetPlayerObject(
                player,
                out NetworkObject playerObject))
        {
            Debug.LogWarning(
                $"[LOBBY] Player {player} has no PlayerObject."
            );

            return;
        }

        NetworkPlayerState state =
            playerObject.GetComponent<NetworkPlayerState>();

        if (state == null)
        {
            Debug.LogWarning(
                $"[LOBBY] Player {player} has no NetworkPlayerState."
            );

            return;
        }

        if (!state.IsReady)
        {
            Debug.LogWarning(
                $"[LOBBY] Player {player} is NOT ready."
            );

            return;
        }
    }

    const int gameplaySceneIndex = 3;

    hasStartedGame = true;

    Debug.Log(
        $"[LOBBY] Loading Gameplay scene additively: " +
        $"{gameplaySceneIndex}"
    );

runner.LoadScene(
    SceneRef.FromIndex(gameplaySceneIndex),
    LoadSceneMode.Single
);
}
private bool CheckAllPlayersReady(NetworkRunner runner)
{
    bool hasPlayers = false;

    foreach (PlayerRef player in runner.ActivePlayers)
    {
        hasPlayers = true;

        if (!runner.TryGetPlayerObject(
                player,
                out NetworkObject playerObject))
        {
            return false;
        }

        NetworkPlayerState state =
            playerObject.GetComponent<NetworkPlayerState>();

        if (state == null)
        {
            return false;
        }

        if (!state.IsReady)
        {
            return false;
        }
    }

    return hasPlayers;
}
}