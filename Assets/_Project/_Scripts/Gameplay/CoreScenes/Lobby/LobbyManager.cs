using Fusion;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    private bool hasStartedGame = false;

    public bool IsHost => NetworkManager.Instance != null && NetworkManager.Instance.Runner != null && NetworkManager.Instance.Runner.IsServer;

    public bool AllPlayersReady => NetworkManager.Instance != null && NetworkManager.Instance.Runner != null && CheckAllPlayersReady(NetworkManager.Instance.Runner);

    public int TotalRounds
    {
        get
        {
            NetworkManager networkManager = NetworkManager.Instance;
            if (networkManager == null) return 3;
            MatchSession matchSession = networkManager.CurrentMatchSession;
            if (matchSession == null) return 3;
            return matchSession.TotalRounds;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void StartGame()
    {
        if (hasStartedGame) return;
        if (NetworkManager.Instance == null) return;

        NetworkRunner runner = NetworkManager.Instance.Runner;
        if (runner == null || !runner.IsServer) return;
        if (!CheckAllPlayersReady(runner)) return;

        MatchSession matchSession = NetworkManager.Instance.CurrentMatchSession;
        if (matchSession == null || MatchFlowManager.Instance == null) return;

        hasStartedGame = true;
        Debug.Log($"<color=#00E5FF><b>[LOBBY] OK</b> - Host started the match ({matchSession.TotalRounds} rounds, all players ready)</color>");
        MatchFlowManager.Instance.StartMatch();
    }

    public void SetRoundCount(int roundCount)
    {
        if (!IsHost) return;
        NetworkManager networkManager = NetworkManager.Instance;
        if (networkManager == null) return;
        MatchSession matchSession = networkManager.CurrentMatchSession;
        if (matchSession == null) return;
        matchSession.SetTotalRounds(roundCount);
    }

    private bool CheckAllPlayersReady(NetworkRunner runner)
    {
        bool hasPlayers = false;

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            hasPlayers = true;
            if (!runner.TryGetPlayerObject(player, out NetworkObject playerObject)) return false;
            NetworkPlayerState state = playerObject.GetComponent<NetworkPlayerState>();
            if (state == null || !state.IsReady) return false;
        }

        return hasPlayers;
    }
}