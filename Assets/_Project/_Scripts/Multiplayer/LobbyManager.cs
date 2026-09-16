using Fusion;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    public bool IsHost =>
        NetworkManager.Instance != null &&
        NetworkManager.Instance.Runner != null &&
        NetworkManager.Instance.Runner.IsServer;

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

    if (NetworkManager.Instance == null)
    {
        Debug.LogError("[LOBBY] NetworkManager.Instance is NULL.");
        return;
    }

    NetworkRunner runner = NetworkManager.Instance.Runner;

    if (runner == null)
    {
        Debug.LogError("[LOBBY] NetworkRunner is NULL.");
        return;
    }

    Debug.Log($"[LOBBY] IsServer: {runner.IsServer}");
    Debug.Log($"[LOBBY] IsConnectedToServer: {runner.IsConnectedToServer}");

    if (!runner.IsServer)
    {
        Debug.LogWarning("[LOBBY] This player is not the Host.");
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

        Debug.Log(
            $"[LOBBY] Player {player} ready = {state.IsReady}"
        );

        if (!state.IsReady)
        {
            Debug.LogWarning(
                $"[LOBBY] Player {player} is NOT ready."
            );

            return;
        }
    }

    const int gameplaySceneIndex = 3;

    Debug.Log(
        $"[LOBBY] Loading Gameplay scene index {gameplaySceneIndex}"
    );

    runner.LoadScene(
        SceneRef.FromIndex(gameplaySceneIndex)
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

            if (state == null || !state.IsReady)
            {
                return false;
            }
        }

        return hasPlayers;
    }
}