using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion.Sockets;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    
    private const ushort LAN_PORT = 7777;
private const string LAN_HOST_IP = "0.0.0.0";

[SerializeField]
private int lobbySceneBuildIndex;
[SerializeField]
private NetworkObject matchSessionPrefab;

public MatchSession CurrentMatchSession { get; private set; }
    private readonly Dictionary<PlayerRef, CharacterID> playerSelections = new();
    public static NetworkManager Instance { get; private set; }
    public MainMenuMessage PendingMainMenuMessage { get; private set; }
    

  private NetworkRunner runner;
public NetworkRunner Runner => runner;

    public event Action<List<SessionInfo>> OnSessionListUpdatedEvent;
    public event Action<PlayerRef> OnPlayerJoinedEvent;
public event Action<PlayerRef> OnPlayerLeftEvent;
public event Action<Scene> OnNetworkSceneLoadDoneEvent;

private bool intentionalShutdown;
public string LANHostIP { get; private set; }
public string LastNetworkError { get; private set; }

public int LobbySceneBuildIndex =>
    lobbySceneBuildIndex;

private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);

    Application.runInBackground = true;
}
public async void Host(string sessionName)
{
    LANHostIP = null;

    runner = GetOrCreateRunner();

    var result = await runner.StartGame(new StartGameArgs
    {
        GameMode = GameMode.Host,
        SessionName = sessionName,
        IsVisible = true,
        IsOpen = true,
        Scene = SceneRef.FromIndex(lobbySceneBuildIndex),
        SceneManager = GetOrCreateSceneManager()
    });

    if (result.Ok)
{
    Debug.Log($"Host started: {sessionName}");
}
    else
    {
        Debug.LogError(
            $"Host failed: {result.ShutdownReason}"
        );
    }
}public async void HostLAN(string sessionName)
{
    runner = GetOrCreateRunner();

    var result = await runner.StartGame(new StartGameArgs
    {
        GameMode = GameMode.Host,
        SessionName = sessionName,

        // Listen on all local network interfaces.
        Address = NetAddress.Any(LAN_PORT),

        Scene = SceneRef.FromIndex(lobbySceneBuildIndex),
        SceneManager = GetOrCreateSceneManager()
    });
if (result.Ok)
{
    LANHostIP = GetLocalIPAddress();

    Debug.Log($"LAN Host started: {sessionName}");
    Debug.Log($"LAN Host IP: {LANHostIP}");
}
    else
    {
        LastNetworkError =
            $"Shutdown: {result.ShutdownReason}\n" +
            $"Error: {result.ErrorMessage}";

        Debug.LogError(
            $"[LAN] Host failed: {LastNetworkError}"
        );
    }
}
public async void JoinLAN(string ipAddress)
{
    runner = GetOrCreateRunner();

    Debug.Log($"[LAN] Joining {ipAddress}:{LAN_PORT}");

    var result = await runner.StartGame(new StartGameArgs
    {
        GameMode = GameMode.Client,

        Scene = SceneRef.FromIndex(lobbySceneBuildIndex),

        // Connect directly to the LAN host.
        Address = NetAddress.CreateFromIpPort(
            ipAddress,
            LAN_PORT
        ),

        SceneManager = GetOrCreateSceneManager()
    });

    if (result.Ok)
    {
        Debug.Log(
            $"[LAN] Connected to {ipAddress}:{LAN_PORT}"
        );
    }
    else
    {
        Debug.LogError(
            $"[LAN] Join failed: {result.ShutdownReason}"
        );
    }
}
private string GetLocalIPAddress()
{
    foreach (var networkInterface in
        System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
    {
        if (networkInterface.OperationalStatus !=
            System.Net.NetworkInformation.OperationalStatus.Up)
        {
            continue;
        }

        var properties = networkInterface.GetIPProperties();

        foreach (var address in properties.UnicastAddresses)
        {
            if (address.Address.AddressFamily !=
                System.Net.Sockets.AddressFamily.InterNetwork)
            {
                continue;
            }

            string ip = address.Address.ToString();

            if (ip.StartsWith("10."))
                return ip;

            if (ip.StartsWith("192.168."))
                return ip;

            if (IsPrivate172(ip))
                return ip;
        }
    }

    return "127.0.0.1";
}

private bool IsPrivate172(string ip)
{
    string[] parts = ip.Split('.');

    if (parts.Length != 4)
        return false;

    if (!int.TryParse(parts[0], out int first))
        return false;

    if (!int.TryParse(parts[1], out int second))
        return false;

    return first == 172 && second >= 16 && second <= 31;
}
public async void OpenPublicLobby()
{
    runner = GetOrCreateRunner();

    var result = await runner.JoinSessionLobby(
        SessionLobby.ClientServer
    );

    if (result.Ok)
    {
        Debug.Log("Joined public session lobby.");
    }
    else
    {
        Debug.LogError(
            $"Failed to join public session lobby: " +
            $"{result.ShutdownReason}"
        );
    }
}
public async void JoinSession(SessionInfo session)
{
    Debug.Log($"Joining session: {session.Name}");

    runner = GetOrCreateRunner();

    var result = await runner.StartGame(new StartGameArgs
    {
        GameMode = GameMode.Client,
        SessionName = session.Name,
        Scene = SceneRef.FromIndex(lobbySceneBuildIndex),
        SceneManager = GetOrCreateSceneManager()
    });

    if (result.Ok)
    {
        Debug.Log($"Joined session: {session.Name}");
    }
    else
    {
        Debug.LogError(
            $"Join failed: {result.ShutdownReason}"
        );
    }
}
public async void Disconnect()
{
    if (runner == null)
        return;

    intentionalShutdown = true;

    await runner.Shutdown(
        destroyGameObject: true
    );
}
private NetworkSceneManagerDefault GetOrCreateSceneManager()
{
    if (runner == null)
        return null;

    NetworkSceneManagerDefault manager =
        runner.GetComponent<NetworkSceneManagerDefault>();

    if (manager == null)
    {
        manager =
            runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
    }

    return manager;
}

  private void ReturnToMainMenu()
{
    SceneManager.sceneLoaded += OnMainMenuLoaded;
    SceneManager.LoadScene("MainMenu 1");
}

private void OnMainMenuLoaded(
    Scene scene,
    LoadSceneMode mode)
{
    SceneManager.sceneLoaded -= OnMainMenuLoaded;

    if (scene.name != "MainMenu 1")
        return;

    MainMenuUI mainMenu =
        FindAnyObjectByType<MainMenuUI>();

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

private void SpawnMatchSession()
{
    if (runner == null)
    {
        Debug.LogError(
            "[MATCH] Cannot spawn MatchSession. Runner is NULL."
        );

        return;
    }

    if (!runner.IsServer)
    {
        return;
    }

    if (CurrentMatchSession != null)
    {
        Debug.LogWarning(
            "[MATCH] MatchSession already exists."
        );

        return;
    }

    if (matchSessionPrefab == null)
    {
        Debug.LogError(
            "[MATCH] MatchSession prefab is not assigned."
        );

        return;
    }
NetworkObject spawnedObject =
    runner.Spawn(
        matchSessionPrefab,
        position: null,
        rotation: null,
        inputAuthority: null,
        onBeforeSpawned: null,
        flags: NetworkSpawnFlags.DontDestroyOnLoad
    );

    CurrentMatchSession =
        spawnedObject.GetComponent<MatchSession>();

    if (CurrentMatchSession == null)
    {
        Debug.LogError(
            "[MATCH] Spawned MatchSession prefab " +
            "does not contain MatchSession component."
        );

        return;
    }

    Debug.Log(
        $"[MATCH] MatchSession spawned. " +
        $"Total Rounds = {CurrentMatchSession.TotalRounds}"
    );
}
private NetworkRunner CreateRunner()
{
    GameObject runnerObject =
        new GameObject("NetworkRunner");

    NetworkRunner newRunner =
        runnerObject.AddComponent<NetworkRunner>();

    // Gameplay input is enabled by RoundStateManager only when the round starts.
     newRunner.ProvideInput = true;
    newRunner.AddCallbacks(this);

    return newRunner;
}
private NetworkRunner GetOrCreateRunner()
{
    if (runner == null)
    {
        runner = CreateRunner();
    }

    return runner;
}
public void ClearMainMenuMessage()
{
    PendingMainMenuMessage = MainMenuMessage.None;
}

    // ----------------------------
    // Fusion callbacks
    // ----------------------------

public void OnSessionListUpdated(
    NetworkRunner runner,
    List<SessionInfo> sessionList)
{
    Debug.Log(
        $"Session list updated. Sessions found: {sessionList.Count}"
    );

    OnSessionListUpdatedEvent?.Invoke(sessionList);
}


public void OnPlayerJoined(
    NetworkRunner runner,
    PlayerRef player)
{
    Debug.Log($"[NETWORK] Player joined: {player}");

    OnPlayerJoinedEvent?.Invoke(player);
}
public void OnPlayerLeft(
    NetworkRunner runner,
    PlayerRef player)
{
    Debug.Log($"[NETWORK] Player left: {player}");

    playerSelections.Remove(player);

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
    LastNetworkError = $"Runner shutdown: {shutdownReason}";

    Debug.Log(LastNetworkError);

    playerSelections.Clear();

    if (this.runner == runner)
    {
        this.runner = null;
    }

    if (!intentionalShutdown)
    {
        PendingMainMenuMessage =
            MainMenuMessage.HostDisconnected;
    }

    ReturnToMainMenu();

    intentionalShutdown = false;
}
    public void OnConnectedToServer(
        NetworkRunner runner)
    {
        Debug.Log("Connected to server.");
    }


public void OnDisconnectedFromServer(
    NetworkRunner runner,
    NetDisconnectReason reason)
{
    Debug.Log(
        $"Disconnected from server: {reason}"
    );
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
    Scene scene = SceneManager.GetActiveScene();

    Debug.Log(
        $"[NETWORK] Scene load done: {scene.name} " +
        $"(Build Index: {scene.buildIndex})"
    );

    OnNetworkSceneLoadDoneEvent?.Invoke(scene);

    if (!runner.IsServer)
    {
        return;
    }

    if (scene.buildIndex != lobbySceneBuildIndex)
    {
        return;
    }

    if (CurrentMatchSession != null)
    {
        return;
    }

    Debug.Log(
        "[MATCH] Lobby loaded. Spawning MatchSession."
    );

    SpawnMatchSession();
}
public void RegisterMatchSession(MatchSession matchSession)
{
    if (matchSession == null)
        return;

    CurrentMatchSession = matchSession;

    Debug.Log(
        $"[MATCH] NetworkManager registered MatchSession. " +
        $"Round {matchSession.CurrentRound}/" +
        $"{matchSession.TotalRounds}"
    );
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
