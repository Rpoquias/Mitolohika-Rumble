using Fusion;
using UnityEngine;

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
        Debug.LogWarning(
            "[LOBBY] Game has already started."
        );

        return;
    }

    if (NetworkManager.Instance == null)
    {
        Debug.LogError(
            "[LOBBY] NetworkManager.Instance is NULL."
        );

        return;
    }

    NetworkRunner runner =
        NetworkManager.Instance.Runner;

    if (runner == null)
    {
        Debug.LogError(
            "[LOBBY] NetworkRunner is NULL."
        );

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

    MatchSession matchSession =
        NetworkManager.Instance.CurrentMatchSession;

    if (matchSession == null)
    {
        Debug.LogError(
            "[LOBBY] Cannot start game. MatchSession is missing."
        );

        return;
    }

    if (MatchFlowManager.Instance == null)
    {
        Debug.LogError(
            "[LOBBY] MatchFlowManager is missing."
        );

        return;
    }

    Debug.Log(
        $"[LOBBY] Starting match with " +
        $"{matchSession.TotalRounds} rounds."
    );

    hasStartedGame = true;

    MatchFlowManager.Instance.StartMatch();
}
public int TotalRounds
{
    get
    {
        NetworkManager networkManager =
            NetworkManager.Instance;

        if (networkManager == null)
        {
            return 3;
        }

        MatchSession matchSession =
            networkManager.CurrentMatchSession;

        if (matchSession == null)
        {
            return 3;
        }

        return matchSession.TotalRounds;
    }
}
public void SetRoundCount(int roundCount)
{
    if (!IsHost)
    {
        Debug.LogWarning(
            "[LOBBY] Only the Host can change the number of rounds."
        );

        return;
    }

    NetworkManager networkManager =
        NetworkManager.Instance;

    if (networkManager == null)
    {
        Debug.LogError(
            "[LOBBY] NetworkManager does not exist."
        );

        return;
    }

    MatchSession matchSession =
        networkManager.CurrentMatchSession;

    if (matchSession == null)
    {
        Debug.LogError(
            "[LOBBY] MatchSession does not exist yet."
        );

        return;
    }

    matchSession.SetTotalRounds(roundCount);
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
