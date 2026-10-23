
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Network Settings")]
    [SerializeField] private int lobbySceneBuildIndex;
    [SerializeField] private NetworkObject matchSessionPrefab;
    [SerializeField] private NetworkRunner runnerPrefab;

    public static NetworkManager Instance { get; private set; }

    public MatchSession CurrentMatchSession { get; private set; }
    public MainMenuMessage PendingMainMenuMessage { get; private set; }
    public string LastNetworkError { get; private set; }
    public string ConnectionStatus { get; private set; }
    public int LobbySceneBuildIndex => lobbySceneBuildIndex;
    public NetworkRunner Runner => runner;
    public bool IsCancelling => cancellingOperation;

    public bool IsBusy =>
        sessionActive ||
        joiningLobby ||
        runnerCleanupInProgress ||
        cancellingOperation;

    public event Action<List<SessionInfo>> OnSessionListUpdatedEvent;
    public event Action<PlayerRef> OnPlayerJoinedEvent;
    public event Action<PlayerRef> OnPlayerLeftEvent;
    public event Action<Scene> OnNetworkSceneLoadDoneEvent;
    public event Action OnNetworkSceneLoadStartEvent;
    public event Action<string> OnConnectionStatusChanged;
    public event Action<MatchSession> OnMatchSessionRegistered;
    public static event Action<NetworkManager> OnInstanceReady;
    public event Action<PlayerRef> OnPlayerObjectAvailable;

    private NetworkRunner runner;

    private readonly Dictionary<PlayerRef, CharacterID> playerSelections = new();

    private bool intentionalShutdown;
    private bool joinsAllowed = true;
    private bool sessionActive;
    private bool joiningLobby;
    private bool runnerCleanupInProgress;
    private bool disconnectCleanupInProgress;
    private bool connectionEstablished;
    private bool cancellingOperation;

    private bool pendingJoinRequested;
    private SessionInfo pendingSession;

    private int operationId;
    private string currentOperationLabel;

private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;
    Application.runInBackground = true;

    OnInstanceReady?.Invoke(this);
}
    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= OnMainMenuLoaded;
        Instance = null;
    }

    private void SetConnectionStatus(string message)
    {
        ConnectionStatus = message;
        OnConnectionStatusChanged?.Invoke(message);
    }

    public void ShowStatusMessage(string message)
    {
        SetConnectionStatus(message);
    }

    // HOST
    public async void Host(string sessionName)
    {
        if (IsBusy || runner != null)
            return;

        int thisOperation = ++operationId;

        sessionActive = true;
        intentionalShutdown = false;
        joinsAllowed = true;
        currentOperationLabel = "HOSTING";

        SetConnectionStatus("HOSTING ROOM...\nPlease wait.");

        NetworkRunner currentRunner = GetOrCreateRunner();

        if (currentRunner == null)
        {
            sessionActive = false;
            currentOperationLabel = null;
            SetConnectionStatus("HOSTING FAILED.");
            return;
        }

        try
        {
            StartGameResult result = await currentRunner.StartGame(
                new StartGameArgs
                {
                    GameMode = GameMode.Host,
                    SessionName = sessionName,
                    PlayerCount = 4,
                    IsVisible = true,
                    IsOpen = true,
                    Scene = SceneRef.FromIndex(lobbySceneBuildIndex),
                    SceneManager = GetOrCreateSceneManager(currentRunner)
                });

            if (thisOperation != operationId || cancellingOperation)
                return;

            if (!result.Ok)
            {
                LastNetworkError = result.ErrorMessage;

                Debug.LogWarning(
                    $"[NETWORK MANAGER] Host failed: {result.ShutdownReason}");

                await CleanupFailedOperation(currentRunner, thisOperation);
                return;
            }

            Debug.Log($"[NETWORK MANAGER] Hosting session: {sessionName}");
        }
        catch (Exception exception)
        {
            if (thisOperation != operationId || cancellingOperation)
                return;

            LastNetworkError = exception.Message;
            Debug.LogException(exception);

            await CleanupFailedOperation(currentRunner, thisOperation);
        }
    }

    // PUBLIC SESSION LOBBY
    public async void OpenPublicLobby()
    {
        if (IsBusy || runner != null)
            return;

        int thisOperation = ++operationId;

        joiningLobby = true;
        intentionalShutdown = false;
        currentOperationLabel = "FINDING LOBBY";

        pendingJoinRequested = false;

        SetConnectionStatus("FINDING LOBBY...\nPlease wait.");

        NetworkRunner currentRunner = GetOrCreateRunner();

        if (currentRunner == null)
        {
            joiningLobby = false;
            currentOperationLabel = null;
            SetConnectionStatus("LOBBY SEARCH FAILED.");
            return;
        }

        try
        {
            var result = await currentRunner.JoinSessionLobby(
                SessionLobby.ClientServer);

            if (thisOperation != operationId || cancellingOperation)
                return;

            if (!result.Ok)
            {
                LastNetworkError = result.ShutdownReason.ToString();

                Debug.LogWarning(
                    $"[NETWORK MANAGER] Lobby search failed: " +
                    $"{result.ShutdownReason}");

                await CleanupFailedOperation(currentRunner, thisOperation);
                return;
            }

            Debug.Log("[NETWORK MANAGER] Joined public session lobby.");

            if (!pendingJoinRequested)
            {
                SetConnectionStatus(
                    "SEARCHING FOR ROOMS...\nPlease wait.");
            }
        }
        catch (Exception exception)
        {
            if (thisOperation != operationId || cancellingOperation)
                return;

            LastNetworkError = exception.Message;
            Debug.LogException(exception);

            await CleanupFailedOperation(currentRunner, thisOperation);
        }
        finally
        {
            if (thisOperation == operationId && !cancellingOperation)
            {
                joiningLobby = false;

                if (pendingJoinRequested)
                {
                    SessionInfo selectedSession = pendingSession;
                    pendingJoinRequested = false;
                    currentOperationLabel = null;

                    JoinSession(selectedSession);
                }
                else
                {
                    currentOperationLabel = null;
                }
            }
        }
    }

    // JOIN SESSION
    public async void JoinSession(SessionInfo session)
    {
        if (cancellingOperation ||
            runnerCleanupInProgress ||
            sessionActive)
        {
            Debug.LogWarning(
                "[NETWORK MANAGER] Join rejected: another operation is active.");
            return;
        }

        if (!session.IsValid || !session.IsOpen ||
            session.PlayerCount >= session.MaxPlayers)
        {
            SetConnectionStatus(
                "ROOM UNAVAILABLE.\nPlease select another room.");

            return;
        }

        // The session list can arrive before JoinSessionLobby finishes.
        // Queue the selected room instead of silently rejecting the click.
        if (joiningLobby)
        {
            pendingSession = session;
            pendingJoinRequested = true;
            currentOperationLabel = "JOINING ROOM";

            SetConnectionStatus("JOINING ROOM...\nPlease wait.");

            Debug.Log(
                $"[NETWORK MANAGER] Queued join for '{session.Name}'.");

            return;
        }

        if (runner == null)
        {
            Debug.LogWarning(
                "[NETWORK MANAGER] Cannot join: runner is missing.");

            SetConnectionStatus(
                "JOIN FAILED.\nPlease search for rooms again.");

            return;
        }

        int thisOperation = ++operationId;
        NetworkRunner currentRunner = runner;

        sessionActive = true;
        intentionalShutdown = false;
        currentOperationLabel = "JOINING ROOM";

        SetConnectionStatus("JOINING ROOM...\nPlease wait.");

        Debug.Log(
            $"[NETWORK MANAGER] Joining session '{session.Name}'...");

        try
        {
            StartGameResult result = await currentRunner.StartGame(
                new StartGameArgs
                {
                    GameMode = GameMode.Client,
                    SessionName = session.Name,
                    Scene = SceneRef.FromIndex(lobbySceneBuildIndex),
                    SceneManager = GetOrCreateSceneManager(currentRunner)
                });

            if (thisOperation != operationId || cancellingOperation)
                return;

            if (!result.Ok)
            {
                LastNetworkError = result.ErrorMessage;

                Debug.LogWarning(
                    $"[NETWORK MANAGER] Join failed: {result.ShutdownReason}");

                await CleanupFailedOperation(currentRunner, thisOperation);
                return;
            }

            connectionEstablished = true;

            Debug.Log(
                $"[NETWORK MANAGER] Successfully joined '{session.Name}'.");
        }
        catch (Exception exception)
        {
            if (thisOperation != operationId || cancellingOperation)
                return;

            LastNetworkError = exception.Message;
            Debug.LogException(exception);

            await CleanupFailedOperation(currentRunner, thisOperation);
        }
    }

    // CANCEL PENDING OPERATION
    public async Task CancelPendingOperation()
    {
        if (cancellingOperation)
            return;

        if (runner == null && !IsBusy)
            return;

        string cancelledOperation = currentOperationLabel;

        if (string.IsNullOrEmpty(cancelledOperation))
            cancelledOperation = "NETWORK OPERATION";

        ++operationId;

        cancellingOperation = true;
        intentionalShutdown = true;

        NetworkRunner runnerToCancel = runner;

        sessionActive = false;
        joiningLobby = false;
        pendingJoinRequested = false;
        currentOperationLabel = null;

        SetConnectionStatus(cancelledOperation + " CANCELLED.");

        Debug.Log(
            $"[NETWORK MANAGER] {cancelledOperation} cancelled by user.");

        try
        {
            if (runnerToCancel != null)
            {
                if (runnerToCancel.IsRunning)
                {
                    await runnerToCancel.Shutdown(
                        destroyGameObject: true);
                }
                else
                {
                    if (runner == runnerToCancel)
                        runner = null;

                    Destroy(runnerToCancel.gameObject);
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[NETWORK MANAGER] Cancellation cleanup: {exception.Message}");

            if (runner == runnerToCancel)
                runner = null;

            if (runnerToCancel != null)
                Destroy(runnerToCancel.gameObject);
        }
        finally
        {
            if (runner == null || runner == runnerToCancel)
            {
                if (runner == runnerToCancel)
                    runner = null;

                sessionActive = false;
                joiningLobby = false;
                connectionEstablished = false;
                disconnectCleanupInProgress = false;
                pendingJoinRequested = false;
                currentOperationLabel = null;
                intentionalShutdown = false;
            }

            cancellingOperation = false;
        }
    }

    public async void Disconnect()
    {
        await CancelPendingOperation();

        SetConnectionStatus(string.Empty);
        ReturnToMainMenu();
    }

    // FAILED OPERATION CLEANUP
    private async Task CleanupFailedOperation(
        NetworkRunner failedRunner,
        int failedOperationId)
    {
        if (failedOperationId != operationId || cancellingOperation)
            return;

        intentionalShutdown = true;
        runnerCleanupInProgress = true;

        try
        {
            if (failedRunner != null && failedRunner.IsRunning)
            {
                await failedRunner.Shutdown(destroyGameObject: true);
            }
            else if (failedRunner != null)
            {
                if (runner == failedRunner)
                    runner = null;

                Destroy(failedRunner.gameObject);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[NETWORK MANAGER] Failed-operation cleanup: " +
                $"{exception.Message}");

            if (runner == failedRunner)
                runner = null;

            if (failedRunner != null)
                Destroy(failedRunner.gameObject);
        }
        finally
        {
            if (failedOperationId == operationId)
            {
                if (runner == failedRunner)
                    runner = null;

                sessionActive = false;
                joiningLobby = false;
                connectionEstablished = false;
                disconnectCleanupInProgress = false;
                pendingJoinRequested = false;
                currentOperationLabel = null;
                intentionalShutdown = false;
            }

            runnerCleanupInProgress = false;
        }
    }

    // DISCONNECT CLEANUP
    private async Task CleanupRunnerAsync(NetworkRunner targetRunner)
    {
        if (targetRunner == null ||
            runnerCleanupInProgress ||
            cancellingOperation)
        {
            return;
        }

        runnerCleanupInProgress = true;
        bool isCurrentRunner = runner == targetRunner;

        if (isCurrentRunner)
        {
            intentionalShutdown = true;
            sessionActive = true;
            joiningLobby = false;
            connectionEstablished = false;
        }

        try
        {
            if (targetRunner.IsRunning)
            {
                await targetRunner.Shutdown(destroyGameObject: true);
            }
            else
            {
                if (runner == targetRunner)
                    runner = null;

                Destroy(targetRunner.gameObject);
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            if (runner == targetRunner)
                runner = null;

            if (targetRunner != null)
                Destroy(targetRunner.gameObject);
        }
        finally
        {
            if (runner == null || runner == targetRunner)
            {
                if (runner == targetRunner)
                    runner = null;

                sessionActive = false;
                joiningLobby = false;
                connectionEstablished = false;
                disconnectCleanupInProgress = false;
                currentOperationLabel = null;
                intentionalShutdown = false;
            }

            runnerCleanupInProgress = false;

            if (isCurrentRunner)
                ReturnToMainMenu();
        }
    }

    // RUNNER CREATION
    private NetworkRunner GetOrCreateRunner()
    {
        if (runner != null)
            return runner;

        runner = CreateRunner();
        return runner;
    }

    private NetworkRunner CreateRunner()
    {
        if (runnerPrefab == null)
        {
            Debug.LogError(
                "[NETWORK MANAGER] Runner prefab is not assigned.");

            return null;
        }

        NetworkRunner newRunner = Instantiate(runnerPrefab);

        newRunner.name = "NetworkRunner";
        newRunner.ProvideInput = true;
        newRunner.AddCallbacks(this);

        return newRunner;
    }

    private NetworkSceneManagerDefault GetOrCreateSceneManager(
        NetworkRunner targetRunner)
    {
        if (targetRunner == null)
            return null;

        NetworkSceneManagerDefault manager =
            targetRunner.GetComponent<NetworkSceneManagerDefault>();

        if (manager == null)
        {
            manager = targetRunner.gameObject
                .AddComponent<NetworkSceneManagerDefault>();
        }

        return manager;
    }

    // MAIN MENU
    private void ReturnToMainMenu()
    {
        SceneManager.sceneLoaded -= OnMainMenuLoaded;
        SceneManager.sceneLoaded += OnMainMenuLoaded;
        SceneManager.LoadScene("MainMenu");
    }

    private void OnMainMenuLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnMainMenuLoaded;

        if (scene.name != "MainMenu")
            return;

        MainMenuUI mainMenu = FindAnyObjectByType<MainMenuUI>();

        if (mainMenu != null)
            mainMenu.ShowJoinPanel();
    }

    // MATCH SESSION
    private void SpawnMatchSession()
    {
        if (runner == null ||
            !runner.IsServer ||
            CurrentMatchSession != null)
        {
            return;
        }

        if (matchSessionPrefab == null)
        {
            Debug.LogError(
                "[NETWORK MANAGER] MatchSession prefab is missing.");

            return;
        }

        NetworkObject spawnedObject = runner.Spawn(
            matchSessionPrefab,
            position: null,
            rotation: null,
            inputAuthority: null,
            onBeforeSpawned: null,
            flags: NetworkSpawnFlags.DontDestroyOnLoad);

        CurrentMatchSession = spawnedObject.GetComponent<MatchSession>();

        if (CurrentMatchSession == null)
        {
            Debug.LogError(
                "[NETWORK MANAGER] Spawned prefab has no MatchSession component.");

            return;
        }

        Debug.Log(
            $"[NETWORK MANAGER] MatchSession spawned. Total rounds: " +
            $"{CurrentMatchSession.TotalRounds}");
    }

    public void RegisterMatchSession(MatchSession matchSession)
    {
        if (matchSession != null)
            CurrentMatchSession = matchSession;

    OnMatchSessionRegistered?.Invoke(matchSession);
    }

    public void ClearMatchSession(MatchSession matchSession)
    {
        if (CurrentMatchSession == matchSession)
            CurrentMatchSession = null;
    }

    public void SetJoinable(bool joinable)
    {
        if (runner == null || !runner.IsRunning || !runner.IsServer)
            return;

        joinsAllowed = joinable;
        runner.SessionInfo.IsOpen = joinable;
        runner.SessionInfo.IsVisible = joinable;

        Debug.Log($"[NETWORK MANAGER] Session joinable: {joinable}");
    }

    public void SetPlayerSelection(PlayerRef player, CharacterID characterID)
    {
        playerSelections[player] = characterID;
    }

    public bool TryGetPlayerSelection(
        PlayerRef player,
        out CharacterID characterID)
    {
        return playerSelections.TryGetValue(player, out characterID);
    }

    public void ClearMainMenuMessage()
    {
        PendingMainMenuMessage = MainMenuMessage.None;
    }

    // FUSION CALLBACKS
    public void OnSessionListUpdated(
        NetworkRunner callbackRunner,
        List<SessionInfo> sessionList)
    {
        if (callbackRunner != runner)
            return;

        OnSessionListUpdatedEvent?.Invoke(sessionList);

        if (sessionActive || cancellingOperation)
            return;

        bool hasAvailableRooms = sessionList.Exists(session =>
            session.IsValid &&
            session.IsOpen &&
            session.PlayerCount < session.MaxPlayers);

        if (hasAvailableRooms)
        {
            if (!pendingJoinRequested)
            {
                SetConnectionStatus(
                    "LOBBY FOUND!\nSelect a room to join.");
            }
        }
        else if (!pendingJoinRequested)
        {
            SetConnectionStatus(
                "NO ROOMS FOUND.\nWaiting for available rooms...");
        }
    }
    public void NotifyPlayerObjectAvailable(PlayerRef player)
{
    OnPlayerObjectAvailable?.Invoke(player);
}

    public void OnPlayerJoined(
        NetworkRunner callbackRunner,
        PlayerRef player)
    {
        if (callbackRunner != runner)
            return;

        if (callbackRunner.IsServer &&
            !joinsAllowed &&
            player != callbackRunner.LocalPlayer)
        {
            callbackRunner.Disconnect(player);
            return;
        }

        OnPlayerJoinedEvent?.Invoke(player);
    }

public void OnPlayerLeft(
    NetworkRunner callbackRunner,
    PlayerRef player)
{
    if (callbackRunner != runner)
        return;

    playerSelections.Remove(player);
    if (joinsAllowed)
        CurrentMatchSession?.RemovePlayerIdentity(player);

    OnPlayerLeftEvent?.Invoke(player);
}
    public void OnShutdown(
        NetworkRunner shutdownRunner,
        ShutdownReason shutdownReason)
    {
        if (runner != shutdownRunner)
            return;

        bool wasCancelled = cancellingOperation;
        bool wasIntentional = intentionalShutdown;

        joinsAllowed = true;
        sessionActive = false;
        joiningLobby = false;
        connectionEstablished = false;
        pendingJoinRequested = false;

        playerSelections.Clear();
        CurrentMatchSession = null;
        runner = null;

        if (wasCancelled)
        {
            Debug.Log("[NETWORK MANAGER] Network operation cancelled.");
            return;
        }

        if (!wasIntentional)
        {
            LastNetworkError = $"Runner shutdown: {shutdownReason}";
            PendingMainMenuMessage = MainMenuMessage.HostDisconnected;
            ReturnToMainMenu();
        }

        intentionalShutdown = false;
    }

  
public void OnSceneLoadDone(NetworkRunner callbackRunner)
{
    if (callbackRunner != runner)
        return;

    Scene scene = SceneManager.GetActiveScene();
    OnNetworkSceneLoadDoneEvent?.Invoke(scene);

    bool inLobby = scene.buildIndex == lobbySceneBuildIndex;
if (callbackRunner.IsServer)
{
    SetJoinable(inLobby);

    if (inLobby)
    {
        if (CurrentMatchSession == null)
            SpawnMatchSession();
        else
            CurrentMatchSession.PruneInactiveIdentities();
    }
}

    if (inLobby)
    {
        currentOperationLabel = null;

        SetConnectionStatus(
            "LOBBY READY!\nWaiting for players...");
    }
}


    public void OnSceneLoadStart(NetworkRunner callbackRunner)
    {
        if (callbackRunner == runner)
            OnNetworkSceneLoadStartEvent?.Invoke();
    }

    public void OnConnectRequest(
        NetworkRunner callbackRunner,
        NetworkRunnerCallbackArgs.ConnectRequest request,
        byte[] token)
    {
        if (callbackRunner != runner || !callbackRunner.IsServer)
            return;

        if (!joinsAllowed)
        {
            request.Refuse();
            return;
        }

        request.Accept();
    }

    public void OnConnectedToServer(NetworkRunner callbackRunner)
    {
        if (callbackRunner != runner)
            return;

        connectionEstablished = true;
        Debug.Log("[NETWORK] Connected to server.");
    }

    public void OnDisconnectedFromServer(
        NetworkRunner callbackRunner,
        NetDisconnectReason reason)
    {
        if (callbackRunner != runner)
            return;

        Debug.LogWarning(
            $"[NETWORK] Disconnected from server: {reason}");

        if (disconnectCleanupInProgress ||
            runnerCleanupInProgress ||
            cancellingOperation)
        {
            return;
        }

        disconnectCleanupInProgress = true;
        LastNetworkError = $"Disconnected: {reason}";
        PendingMainMenuMessage = MainMenuMessage.HostDisconnected;

        _ = CleanupRunnerAsync(callbackRunner);
    }

    public void OnConnectFailed(
        NetworkRunner callbackRunner,
        NetAddress remoteAddress,
        NetConnectFailedReason reason)
    {
        if (callbackRunner != runner)
            return;

        Debug.LogWarning(
            $"[NETWORK] Connection failed. Address: {remoteAddress}, " +
            $"Reason: {reason}");

        LastNetworkError = $"Connection failed: {reason}";

        SetConnectionStatus(
            "CONNECTION FAILED.\nThe room could not be reached. Please try again.");
    }

    public void OnInput(NetworkRunner callbackRunner, NetworkInput input) { }

    public void OnInputMissing(
        NetworkRunner callbackRunner,
        PlayerRef player,
        NetworkInput input) { }

    public void OnUserSimulationMessage(
        NetworkRunner callbackRunner,
        SimulationMessagePtr message) { }

    public void OnReliableDataReceived(
        NetworkRunner callbackRunner,
        PlayerRef player,
        ReliableKey key,
        ReadOnlySpan<byte> data) { }

    public void OnReliableDataProgress(
        NetworkRunner callbackRunner,
        PlayerRef player,
        ReliableKey key,
        float progress) { }

    public void OnCustomAuthenticationResponse(
        NetworkRunner callbackRunner,
        Dictionary<string, object> data) { }

    public void OnObjectEnterAOI(
        NetworkRunner callbackRunner,
        NetworkObject obj,
        PlayerRef player) { }

    public void OnObjectExitAOI(
        NetworkRunner callbackRunner,
        NetworkObject obj,
        PlayerRef player) { }

    public void OnHostMigration(
        NetworkRunner callbackRunner,
        HostMigrationToken hostMigrationToken) { }
}
