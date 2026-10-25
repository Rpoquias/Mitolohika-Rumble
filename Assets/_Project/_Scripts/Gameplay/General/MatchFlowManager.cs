
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchFlowManager : MonoBehaviour
{
    public static MatchFlowManager Instance { get; private set; }

    [Header("Persistent UI")]
    [SerializeField] private MatchRevealUI matchRevealUI;
    [SerializeField] private MatchResultUI matchResultUI;

    private const string MATIRA_SCENE_PATH =
        "Assets/_Project/_Scenes/Main/MatiraMatibay.unity";

    private const string AGAWAN_RELIC_SCENE_PATH =
        "Assets/_Project/_Scenes/Main/AgawanRelic.unity";

    private const string AGAWAN_BANDILA_SCENE_PATH =
        "Assets/_Project/_Scenes/Main/AgawanBandila.unity";

    private const string CELEBRATION_SCENE_PATH =
        "Assets/_Project/_Scenes/Main/Celebration.unity";

    private Coroutine roundRevealCoroutine;

    private int lastHandledRound = -1;

    private bool returningToLobby;
    private bool revealCompleted;
    private bool resultCompleted;
    private bool roundTransitionRunning;

    private NetworkManager subscribedNetworkManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        NetworkManager.OnInstanceReady += HandleNetworkManagerReady;

        if (NetworkManager.Instance != null)
            HandleNetworkManagerReady(NetworkManager.Instance);
    }

    private void OnDisable()
    {
        NetworkManager.OnInstanceReady -= HandleNetworkManagerReady;
        UnsubscribeFromNetworkManager();

        UnsubscribeFromUIEvents();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void HandleNetworkManagerReady(NetworkManager manager)
    {
        if (manager == null || subscribedNetworkManager == manager)
            return;

        UnsubscribeFromNetworkManager();

        subscribedNetworkManager = manager;
        subscribedNetworkManager.OnNetworkSceneLoadDoneEvent +=
            HandleSceneLoadDone;
    }

    private void UnsubscribeFromNetworkManager()
    {
        if (subscribedNetworkManager == null)
            return;

        subscribedNetworkManager.OnNetworkSceneLoadDoneEvent -=
            HandleSceneLoadDone;

        subscribedNetworkManager = null;
    }

    private void UnsubscribeFromUIEvents()
    {
        if (matchRevealUI != null)
            matchRevealUI.OnRevealCompleted -= OnRevealCompleted;

        if (matchResultUI != null)
            matchResultUI.OnResultCompleted -= OnResultCompleted;
    }

    // ============================================================
    // RESET MATCH FLOW
    // ============================================================

    public void ResetFlow()
    {
        StopAllCoroutines();
        roundRevealCoroutine = null;

        UnsubscribeFromUIEvents();

        if (matchResultUI != null)
            matchResultUI.HideImmediately();

        returningToLobby = false;
        revealCompleted = false;
        resultCompleted = false;
        roundTransitionRunning = false;
        lastHandledRound = -1;

        Debug.Log("[MATCH FLOW] Flow state reset.");
    }

    // ============================================================
    // MATCH START
    // ============================================================

    public void StartMatch()
    {
        ResetFlow();

        NetworkManager networkManager = NetworkManager.Instance;

        if (networkManager == null)
        {
            Debug.LogWarning(
                "[MATCH FLOW] StartMatch failed: NetworkManager is null.");
            return;
        }

        NetworkRunner runner = networkManager.Runner;

        if (runner == null || !runner.IsRunning || !runner.IsServer)
        {
            Debug.LogWarning(
                "[MATCH FLOW] StartMatch failed: runner is not running or this is not the server.");
            return;
        }

        MatchSession matchSession = networkManager.CurrentMatchSession;

        if (matchSession == null || !matchSession.IsSpawned)
        {
            Debug.LogWarning(
                "[MATCH FLOW] StartMatch failed: MatchSession is missing or not spawned.");
            return;
        }

        returningToLobby = false;
        networkManager.SetJoinable(false);

        Debug.Log("[MATCH FLOW] Starting match reveal.");

        StartRoundReveal(matchSession, true);
    }

    // ============================================================
    // ROUND COMPLETE
    // ============================================================

    public void HandleRoundComplete(
        List<RoundPlacement> placements,
        RoundResultDetailType detailType,
        Dictionary<PlayerRef, int> detailValues)
    {
        NetworkManager networkManager = NetworkManager.Instance;

        if (networkManager == null)
            return;

        NetworkRunner runner = networkManager.Runner;

        if (runner == null || !runner.IsRunning || !runner.IsServer)
            return;

        MatchSession matchSession = networkManager.CurrentMatchSession;

        if (matchSession == null || !matchSession.IsSpawned)
            return;

        int completedRound = matchSession.CurrentRound;

        if (lastHandledRound == completedRound)
            return;

        if (roundTransitionRunning)
            return;

        if (placements == null || placements.Count == 0)
        {
            Debug.LogWarning(
                "[MATCH FLOW] Round completion ignored: no placements provided.");
            return;
        }

        lastHandledRound = completedRound;

        foreach (RoundPlacement placement in placements)
        {
            matchSession.RecordPlacement(
                placement.Player,
                placement.Placement);
        }

        List<RoundPlacement> cachedPlacements =
            new List<RoundPlacement>(placements);

        roundTransitionRunning = true;

        StartCoroutine(
            ProcessRoundComplete(
                matchSession,
                cachedPlacements,
                detailType,
                detailValues));
    }

    public void HandleRoundComplete(List<RoundPlacement> placements)
    {
        HandleRoundComplete(
            placements,
            RoundResultDetailType.Status,
            null);
    }

    private void OnResultCompleted()
    {
        resultCompleted = true;
    }

    private IEnumerator ProcessRoundComplete(
        MatchSession matchSession,
        List<RoundPlacement> placements,
        RoundResultDetailType detailType,
        Dictionary<PlayerRef, int> detailValues)
    {
        resultCompleted = false;

        if (matchResultUI != null)
        {
            matchResultUI.OnResultCompleted -= OnResultCompleted;
            matchResultUI.OnResultCompleted += OnResultCompleted;
        }

        matchSession.SetCurrentRoundResults(
            placements,
            detailType,
            detailValues);

        if (matchResultUI != null)
        {
            yield return new WaitUntil(() =>
                resultCompleted ||
                matchSession == null ||
                !matchSession.IsSpawned);

            if (matchResultUI != null)
                matchResultUI.OnResultCompleted -= OnResultCompleted;

            if (!IsCurrentSession(matchSession))
                yield break;

            if (!resultCompleted)
                yield break;

            matchResultUI.HideImmediately();
        }
        else
        {
            yield return new WaitForSeconds(3f);

            if (!IsCurrentSession(matchSession))
                yield break;
        }

        // FINAL ROUND
        if (matchSession.IsFinalRound)
        {
            matchSession.CalculateOverallWinners();

            AudioManager.Instance?.PlaySFX(SFXID.Victory);

            matchSession.ResetCelebrationReady();

            roundTransitionRunning = false;

            NetworkManager networkManager = NetworkManager.Instance;

            if (networkManager == null ||
                networkManager.Runner == null ||
                !networkManager.Runner.IsRunning ||
                !networkManager.Runner.IsServer)
            {
                yield break;
            }

            NetworkRunner runner = networkManager.Runner;

            DespawnCurrentPlayers(runner);
            LoadCelebrationScene(runner);

            yield break;
        }

        // NEXT ROUND
        revealCompleted = false;

        if (matchRevealUI == null)
        {
            roundTransitionRunning = false;
            yield break;
        }

        matchRevealUI.OnRevealCompleted -= OnRevealCompleted;
        matchRevealUI.OnRevealCompleted += OnRevealCompleted;

        bool advanced = matchSession.AdvanceRound();

        if (!advanced)
        {
            matchRevealUI.OnRevealCompleted -= OnRevealCompleted;
            roundTransitionRunning = false;
            yield break;
        }

        yield return new WaitUntil(() =>
            revealCompleted ||
            matchSession == null ||
            !matchSession.IsSpawned);

        if (matchRevealUI != null)
            matchRevealUI.OnRevealCompleted -= OnRevealCompleted;

        if (!IsCurrentSession(matchSession))
            yield break;

        if (!revealCompleted)
            yield break;

        roundTransitionRunning = false;

        LoadCurrentRoundScene();
    }

    // ============================================================
    // INITIAL ROUND REVEAL
    // ============================================================

    private void StartRoundReveal(
        MatchSession matchSession,
        bool startMatch)
    {
        if (roundRevealCoroutine != null)
        {
            StopCoroutine(roundRevealCoroutine);
            roundRevealCoroutine = null;
        }

        roundRevealCoroutine = StartCoroutine(
            StartRoundRevealCoroutine(matchSession, startMatch));
    }

    private IEnumerator StartRoundRevealCoroutine(
        MatchSession matchSession,
        bool startMatch)
    {
        if (matchRevealUI == null || !IsCurrentSession(matchSession))
        {
            roundRevealCoroutine = null;
            yield break;
        }

        revealCompleted = false;

        matchRevealUI.OnRevealCompleted -= OnRevealCompleted;
        matchRevealUI.OnRevealCompleted += OnRevealCompleted;

        if (startMatch)
            matchSession.BeginMatch();

        yield return new WaitUntil(() =>
            revealCompleted ||
            matchSession == null ||
            !matchSession.IsSpawned);

        if (matchRevealUI != null)
            matchRevealUI.OnRevealCompleted -= OnRevealCompleted;

        roundRevealCoroutine = null;

        if (!IsCurrentSession(matchSession))
            yield break;

        if (!revealCompleted)
            yield break;

        LoadCurrentRoundScene();
    }

    private void OnRevealCompleted()
    {
        revealCompleted = true;
    }

    private bool IsCurrentSession(MatchSession matchSession)
    {
        if (matchSession == null || !matchSession.IsSpawned)
            return false;

        NetworkManager networkManager = NetworkManager.Instance;

        return networkManager != null &&
               networkManager.Runner != null &&
               networkManager.Runner.IsRunning &&
               networkManager.Runner.IsServer &&
               networkManager.CurrentMatchSession == matchSession;
    }

    // ============================================================
    // LOAD GAMEPLAY SCENE
    // ============================================================

    private void LoadCurrentRoundScene()
    {
        NetworkManager networkManager = NetworkManager.Instance;

        if (networkManager == null)
            return;

        NetworkRunner runner = networkManager.Runner;

        if (runner == null || !runner.IsRunning || !runner.IsServer)
            return;

        MatchSession matchSession = networkManager.CurrentMatchSession;

        if (matchSession == null || !matchSession.IsSpawned)
            return;

        string scenePath = null;

        switch (matchSession.CurrentGameMode)
        {
            case GameModeType.MatiraMatibay:
                scenePath = MATIRA_SCENE_PATH;
                break;

            case GameModeType.AgawanRelic:
                scenePath = AGAWAN_RELIC_SCENE_PATH;
                break;

            case GameModeType.AgawanBandila:
                scenePath = AGAWAN_BANDILA_SCENE_PATH;
                break;

            default:
                Debug.LogWarning(
                    "[MATCH FLOW] Cannot load round: unknown game mode.");
                return;
        }

        Sprite loadingBackground = matchRevealUI != null
            ? matchRevealUI.GetLoadingBackground(
                matchSession.CurrentGameMode)
            : null;

        LoadingScreenUI.Instance?.SetLoadingBackground(
            loadingBackground);

        runner.LoadScene(
            SceneRef.FromPath(scenePath),
            LoadSceneMode.Single);
    }

    // ============================================================
    // NETWORK SCENE LOAD
    // ============================================================

    private void HandleSceneLoadDone(Scene scene)
    {
        if (returningToLobby)
            return;

        NetworkManager networkManager = NetworkManager.Instance;

        if (networkManager == null)
            return;

        NetworkRunner runner = networkManager.Runner;

        if (runner == null || !runner.IsRunning)
            return;

        if (scene.buildIndex == networkManager.LobbySceneBuildIndex)
            return;

        if (scene.name == "Celebration")
            return;

        if (!runner.IsServer)
            return;

        RoundStateManager roundStateManager =
            FindAnyObjectByType<RoundStateManager>();

        if (roundStateManager == null)
        {
            Debug.LogWarning(
                $"[MATCH FLOW] RoundStateManager not found in scene '{scene.name}'.");
            return;
        }

        roundStateManager.StartRound();
    }

    // ============================================================
    // CELEBRATION
    // ============================================================

    private void LoadCelebrationScene(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning || !runner.IsServer)
            return;

        LoadingScreenUI.Instance?.SetLoadingBackground();

        runner.LoadScene(
            SceneRef.FromPath(CELEBRATION_SCENE_PATH),
            LoadSceneMode.Single);
    }

    private void DespawnCurrentPlayers(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning || !runner.IsServer)
            return;

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (!runner.TryGetPlayerObject(
                    player,
                    out NetworkObject playerObject))
            {
                continue;
            }

            runner.Despawn(playerObject);
        }
    }

    // ============================================================
    // CELEBRATION READY
    // ============================================================

    public void CheckCelebrationReady()
    {
        if (returningToLobby)
            return;

        NetworkManager networkManager = NetworkManager.Instance;

        if (networkManager == null)
            return;

        NetworkRunner runner = networkManager.Runner;

        if (runner == null || !runner.IsRunning || !runner.IsServer)
            return;

        MatchSession matchSession = networkManager.CurrentMatchSession;

        if (matchSession == null || !matchSession.IsSpawned)
            return;

        if (!matchSession.AreAllPlayersCelebrationReady())
            return;

        returningToLobby = true;

        DespawnCurrentPlayers(runner);

        LoadingScreenUI.Instance?.SetLoadingBackground();

        runner.LoadScene(
            SceneRef.FromIndex(networkManager.LobbySceneBuildIndex),
            LoadSceneMode.Single);
    }
}
