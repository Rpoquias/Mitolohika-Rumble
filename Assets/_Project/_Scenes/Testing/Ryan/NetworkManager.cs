using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion.Sockets;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    private readonly Dictionary<PlayerRef, CharacterID> playerSelections = new();
    public static NetworkManager Instance { get; private set; }

    [SerializeField] private NetworkRunner runner;
    [SerializeField]
private PlayerSpawner playerSpawner;

    public NetworkRunner Runner => runner;
    

    public event Action<List<SessionInfo>> OnSessionListUpdatedEvent;
    public event Action<PlayerRef> OnPlayerJoinedEvent;
public event Action<PlayerRef> OnPlayerLeftEvent;
private readonly List<PlayerRef> joinedPlayers = new();
public IReadOnlyList<PlayerRef> Players => joinedPlayers;
private NetworkSceneManagerDefault sceneManager;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        runner.AddCallbacks(this);
        
    }

public async void Host(string sessionName)
{
    NetworkStateMachine.Instance.SetState(
        NetworkStateMachine.State.Connecting
    );

   var result = await runner.StartGame(new StartGameArgs
{
    GameMode = GameMode.Host,
    SessionName = sessionName,
    IsVisible = true,
    IsOpen = true,
    Scene = SceneRef.FromIndex(2),
  SceneManager = GetOrCreateSceneManager()
});

    if (result.Ok)
    {
        Debug.Log($"Host started: {sessionName}");

        NetworkStateMachine.Instance.SetState(
            NetworkStateMachine.State.Connected
        );
    }
    else
    {
        Debug.LogError(
            $"Host failed: {result.ShutdownReason}"
        );

        NetworkStateMachine.Instance.SetState(
            NetworkStateMachine.State.Disconnected
        );
    }
}

    public async void OpenPublicLobby()
    {
        var result = await runner.JoinSessionLobby(SessionLobby.ClientServer);

        if (result.Ok)
        {
            Debug.Log("Joined public session lobby.");
        }
        else
        {
            Debug.LogError(
                $"Failed to join public session lobby: {result.ShutdownReason}"
            );
        }
    }

 public async void JoinSession(SessionInfo session)
{
    NetworkStateMachine.Instance.SetState(
        NetworkStateMachine.State.Connecting
    );

    Debug.Log($"Joining session: {session.Name}");

    var result = await runner.StartGame(new StartGameArgs
    {
        GameMode = GameMode.Client,
        SessionName = session.Name,
    SceneManager = GetOrCreateSceneManager()
    });

    if (result.Ok)
    {
        Debug.Log($"Joined session: {session.Name}");

        NetworkStateMachine.Instance.SetState(
            NetworkStateMachine.State.Connected
        );
    }
    else
    {
        Debug.LogError(
            $"Join failed: {result.ShutdownReason}"
        );

        NetworkStateMachine.Instance.SetState(
            NetworkStateMachine.State.Disconnected
        );
    }
}
private NetworkSceneManagerDefault GetOrCreateSceneManager()
{
    if (sceneManager == null)
        sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
    return sceneManager;
}   
private NetworkSceneInfo CreateSceneInfo(string sceneName)
{
    int buildIndex = SceneUtility.GetBuildIndexByScenePath(
        $"Assets/Scenes/{sceneName}.unity"
    );

    SceneRef sceneRef = SceneRef.FromIndex(buildIndex);

    NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
    sceneInfo.AddSceneRef(sceneRef, LoadSceneMode.Single);

    return sceneInfo;
}
    public void OnSessionListUpdated(
        NetworkRunner runner,
        List<SessionInfo> sessionList)
    {
        Debug.Log(
            $"Session list updated. Sessions found: {sessionList.Count}"
        );

        OnSessionListUpdatedEvent?.Invoke(sessionList);
    }

    private void ReturnToMainMenu()
{
    SceneManager.LoadScene("MainMenu 1");
    
    SceneManager.sceneLoaded += OnMainMenuLoaded;
}

private void OnMainMenuLoaded(
    UnityEngine.SceneManagement.Scene scene,
    LoadSceneMode mode)
{
    SceneManager.sceneLoaded -= OnMainMenuLoaded;

    if (scene.name != "MainMenu")
        return;

    MainMenuController mainMenu = 
         FindAnyObjectByType<MainMenuController>();

    if (mainMenu != null)
    {
        mainMenu.ShowJoinPanel();
    }
}

public void SetPlayerSelection(
    PlayerRef player,
    CharacterID characterID)
{
    playerSelections[player] = characterID;
}public bool TryGetPlayerSelection(
    PlayerRef player,
    out CharacterID characterID)
{
    return playerSelections.TryGetValue(
        player,
        out characterID
    );
}

    // ----------------------------
    // Fusion callbacks
    // ----------------------------

public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
{
    if (!joinedPlayers.Contains(player))
        joinedPlayers.Add(player);

    Debug.Log($"[NETWORK] Player joined: {player}");
    OnPlayerJoinedEvent?.Invoke(player);
}

public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
{
    joinedPlayers.Remove(player);

    Debug.Log($"[NETWORK] Player left: {player}");
    OnPlayerLeftEvent?.Invoke(player);
}
    public void OnInput(
        NetworkRunner runner,
        NetworkInput input)
    {
    }

    public void OnInputMissing(
        NetworkRunner runner,
        PlayerRef player,
        NetworkInput input)
    {
    }

  public void OnShutdown(
    NetworkRunner runner,
    ShutdownReason shutdownReason)
{
    Debug.Log($"Runner shutdown: {shutdownReason}");

    ReturnToMainMenu();
}
    public void OnConnectedToServer(
        NetworkRunner runner)
    {
        Debug.Log("Connected to server.");
    }


     public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"Disconnected from server: {reason}");
    }

    public void OnConnectRequest(
        NetworkRunner runner,
        NetworkRunnerCallbackArgs.ConnectRequest request,
        byte[] token)
    {
    }

    public void OnConnectFailed(
        NetworkRunner runner,
        NetAddress remoteAddress,
        NetConnectFailedReason reason)
    {
        Debug.LogError(
            $"Connection failed to {remoteAddress}: {reason}"
        );
    }

    public void OnUserSimulationMessage(
        NetworkRunner runner,
        SimulationMessagePtr message)
    {
    }

    public void OnReliableDataReceived(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        ReadOnlySpan<byte> data)
    {
    }

    public void OnReliableDataProgress(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        float progress)
    {
    }

    public void OnCustomAuthenticationResponse(
        NetworkRunner runner,
        Dictionary<string, object> data)
    {
    }

public void OnSceneLoadDone(NetworkRunner runner)
{
    string activeScene =
        SceneManager.GetActiveScene().name;

    Debug.Log(
        $"[NETWORK] Scene loaded: {activeScene}"
    );

    if (!runner.IsServer)
        return;

    if (activeScene != "Matira Matib")
        return;

    if (playerSpawner == null)
    {
        Debug.LogError(
            "[NETWORK] PlayerSpawner reference is NULL."
        );

        return;
    }

    playerSpawner.RespawnPlayersForGameplay();
}
public void OnSceneLoadStart(NetworkRunner runner)
{
    Debug.Log("Network scene loading started.");
}
    public void OnObjectEnterAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player)
    {
    }

    public void OnObjectExitAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player)
    {
    }

    public void OnHostMigration(
        NetworkRunner runner,
        HostMigrationToken hostMigrationToken)
    {
    }
}