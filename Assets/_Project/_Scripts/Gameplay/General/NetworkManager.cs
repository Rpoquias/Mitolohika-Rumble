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

    [SerializeField] private int lobbySceneBuildIndex;
    [SerializeField] private NetworkObject matchSessionPrefab;
[SerializeField] private NetworkRunner runnerPrefab;
    public static NetworkManager Instance { get; private set; }
    public MatchSession CurrentMatchSession { get; private set; }
    public MainMenuMessage PendingMainMenuMessage { get; private set; }
    public string LANHostIP { get; private set; }
    public string LastNetworkError { get; private set; }
    public int LobbySceneBuildIndex => lobbySceneBuildIndex;

    private NetworkRunner runner;
    public NetworkRunner Runner => runner;

    private readonly Dictionary<PlayerRef, CharacterID> playerSelections = new();
    private bool intentionalShutdown;

    public event Action<List<SessionInfo>> OnSessionListUpdatedEvent;
    public event Action<PlayerRef> OnPlayerJoinedEvent;
    public event Action<PlayerRef> OnPlayerLeftEvent;
    public event Action<Scene> OnNetworkSceneLoadDoneEvent;
    public event Action OnNetworkSceneLoadStartEvent;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
    }

    // ----------------------------
    // Hosting / Joining
    // ----------------------------

    public async void Host(string sessionName)
    {
        LANHostIP = null;
        runner = GetOrCreateRunner();

        await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = sessionName,
            IsVisible = true,
            IsOpen = true,
            Scene = SceneRef.FromIndex(lobbySceneBuildIndex),
            SceneManager = GetOrCreateSceneManager()
        });
    }

    public async void HostLAN(string sessionName)
    {
        runner = GetOrCreateRunner();

        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = sessionName,
            Address = NetAddress.Any(LAN_PORT), // Listen on all local network interfaces.
            Scene = SceneRef.FromIndex(lobbySceneBuildIndex),
            SceneManager = GetOrCreateSceneManager()
        });

        if (result.Ok) LANHostIP = GetLocalIPAddress();
        else LastNetworkError = $"Shutdown: {result.ShutdownReason}\nError: {result.ErrorMessage}";
    }

    public async void JoinLAN(string ipAddress)
    {
        runner = GetOrCreateRunner();

        await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Client,
            Scene = SceneRef.FromIndex(lobbySceneBuildIndex),
            Address = NetAddress.CreateFromIpPort(ipAddress, LAN_PORT), // Connect directly to the LAN host.
            SceneManager = GetOrCreateSceneManager()
        });
    }

    public async void OpenPublicLobby()
    {
        runner = GetOrCreateRunner();
        await runner.JoinSessionLobby(SessionLobby.ClientServer);
    }

    public async void JoinSession(SessionInfo session)
    {
        runner = GetOrCreateRunner();

        await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = session.Name,
            Scene = SceneRef.FromIndex(lobbySceneBuildIndex),
            SceneManager = GetOrCreateSceneManager()
        });
    }

    public async void Disconnect()
    {
        if (runner == null) return;

        intentionalShutdown = true;
        await runner.Shutdown(destroyGameObject: true);
    }

    // ----------------------------
    // Helpers
    // ----------------------------

    private string GetLocalIPAddress()
    {
        foreach (var networkInterface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;

            foreach (var address in networkInterface.GetIPProperties().UnicastAddresses)
            {
                if (address.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) continue;

                string ip = address.Address.ToString();
                if (ip.StartsWith("10.") || ip.StartsWith("192.168.") || IsPrivate172(ip)) return ip;
            }
        }

        return "127.0.0.1";
    }

    private bool IsPrivate172(string ip)
    {
        string[] parts = ip.Split('.');
        if (parts.Length != 4) return false;
        if (!int.TryParse(parts[0], out int first)) return false;
        if (!int.TryParse(parts[1], out int second)) return false;
        return first == 172 && second >= 16 && second <= 31;
    }

    private NetworkSceneManagerDefault GetOrCreateSceneManager()
    {
        if (runner == null) return null;

        NetworkSceneManagerDefault manager = runner.GetComponent<NetworkSceneManagerDefault>();
        if (manager == null) manager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        return manager;
    }

    private void ReturnToMainMenu()
    {
        SceneManager.sceneLoaded += OnMainMenuLoaded;
        SceneManager.LoadScene("MainMenu");
    }

    private void OnMainMenuLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnMainMenuLoaded;
        if (scene.name != "MainMenu") return;

        MainMenuUI mainMenu = FindAnyObjectByType<MainMenuUI>();
        if (mainMenu != null) mainMenu.ShowJoinPanel();
    }

    public void SetPlayerSelection(PlayerRef player, CharacterID characterID) { playerSelections[player] = characterID; }

    public bool TryGetPlayerSelection(PlayerRef player, out CharacterID characterID) => playerSelections.TryGetValue(player, out characterID);

    private void SpawnMatchSession()
    {
        if (runner == null || !runner.IsServer) return;
        if (CurrentMatchSession != null) return;
        if (matchSessionPrefab == null) return;

        NetworkObject spawnedObject = runner.Spawn(matchSessionPrefab, position: null, rotation: null, inputAuthority: null, onBeforeSpawned: null, flags: NetworkSpawnFlags.DontDestroyOnLoad);

        CurrentMatchSession = spawnedObject.GetComponent<MatchSession>();
        if (CurrentMatchSession == null) return;

        Debug.Log($"<color=#B388FF><b>[NETWORK MANAGER] OK</b> - Runner active & MatchSession spawned (Total Rounds = {CurrentMatchSession.TotalRounds})</color>");
    }

    private NetworkRunner CreateRunner()
{
    if (runnerPrefab == null)
    {
        Debug.LogError("[NETWORK MANAGER] Runner prefab is not assigned.");
        return null;
    }

    NetworkRunner newRunner = Instantiate(runnerPrefab);

    newRunner.name = "NetworkRunner";

    newRunner.ProvideInput = true;
    newRunner.AddCallbacks(this);

    return newRunner;
}

    private NetworkRunner GetOrCreateRunner()
    {
        if (runner == null) runner = CreateRunner();
        return runner;
    }

    public void ClearMainMenuMessage() { PendingMainMenuMessage = MainMenuMessage.None; }

    public void RegisterMatchSession(MatchSession matchSession)
    {
        if (matchSession == null) return;
        CurrentMatchSession = matchSession;
    }

    // ----------------------------
    // Fusion callbacks
    // ----------------------------

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) => OnSessionListUpdatedEvent?.Invoke(sessionList);

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) => OnPlayerJoinedEvent?.Invoke(player);

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        playerSelections.Remove(player);
        OnPlayerLeftEvent?.Invoke(player);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        LastNetworkError = $"Runner shutdown: {shutdownReason}";
        playerSelections.Clear();

        if (this.runner == runner) this.runner = null;
        if (!intentionalShutdown) PendingMainMenuMessage = MainMenuMessage.HostDisconnected;

        ReturnToMainMenu();
        intentionalShutdown = false;
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Scene scene = SceneManager.GetActiveScene();
        OnNetworkSceneLoadDoneEvent?.Invoke(scene);

        if (!runner.IsServer) return;
        if (scene.buildIndex != lobbySceneBuildIndex) return;
        if (CurrentMatchSession != null) return;

        SpawnMatchSession();
    }

    public void OnSceneLoadStart(NetworkRunner runner) => OnNetworkSceneLoadStartEvent?.Invoke();

    // Unused callbacks required by INetworkRunnerCallbacks.
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
}