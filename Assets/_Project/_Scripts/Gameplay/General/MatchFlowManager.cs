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

private bool resultCompleted;

    private const string MATIRA_SCENE_PATH =
        "Assets/_Project/_Scenes/Testing/Ryan/MatiraMatibay.unity";

private const string AGAWAN_RELIC_SCENE_PATH =
    "Assets/_Project/_Scenes/Testing/Ryan/AgawanRelic.unity";

private const string AGAWAN_BANDILA_SCENE_PATH =
    "Assets/_Project/_Scenes/Testing/Ryan/AgawanBandila.unity";

    private const string CELEBRATION_SCENE_PATH =
        "Assets/_Project/_Scenes/Testing/Ryan/Celebration.unity";

    private Coroutine roundRevealCoroutine;

    private int lastHandledRound = -1;

    private bool returningToLobby;
    private bool revealCompleted;
    private bool roundTransitionRunning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnNetworkSceneLoadDoneEvent +=
                HandleSceneLoadDone;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnNetworkSceneLoadDoneEvent -=
                HandleSceneLoadDone;
        }
    }

    // ============================================================
    // MATCH START
    // ============================================================

    public void StartMatch()
    {
        returningToLobby = false;
        roundTransitionRunning = false;
        lastHandledRound = -1;

        NetworkManager networkManager =
            NetworkManager.Instance;

        if (networkManager == null)
            return;

        NetworkRunner runner =
            networkManager.Runner;

        if (runner == null || !runner.IsServer)
            return;

        MatchSession matchSession =
            networkManager.CurrentMatchSession;

        if (matchSession == null)
            return;

        Debug.Log(
            $"[MATCH FLOW] Starting match..."
        );

        /*
         * Subscribe BEFORE BeginMatch().
         *
         * BeginMatch() changes RoundRevealSequence.
         * MatchRevealUI will detect that change and
         * automatically play its reveal animation.
         */
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
    NetworkManager networkManager =
        NetworkManager.Instance;

    if (networkManager == null)
        return;

    NetworkRunner runner =
        networkManager.Runner;

    if (runner == null || !runner.IsServer)
        return;

    MatchSession matchSession =
        networkManager.CurrentMatchSession;

    if (matchSession == null)
        return;

    int completedRound =
        matchSession.CurrentRound;

    if (lastHandledRound == completedRound)
        return;

    if (roundTransitionRunning)
        return;

    lastHandledRound =
        completedRound;

    if (placements == null ||
        placements.Count == 0)
    {
        Debug.LogError(
            "[MATCH FLOW] No placements received."
        );

        return;
    }

    foreach (RoundPlacement placement in placements)
    {
        matchSession.RecordPlacement(
            placement.Player,
            placement.Placement
        );
    }

    List<RoundPlacement> cachedPlacements =
        new List<RoundPlacement>(
            placements
        );

    roundTransitionRunning = true;

    StartCoroutine(
        ProcessRoundComplete(
            matchSession,
            cachedPlacements,
            detailType,
            detailValues
        )
    );
}
public void HandleRoundComplete(
    List<RoundPlacement> placements)
{
    HandleRoundComplete(
        placements,
        RoundResultDetailType.Status,
        null
    );
}
private void OnResultCompleted()
{
    resultCompleted = true;
}private IEnumerator ProcessRoundComplete(
    MatchSession matchSession,
    List<RoundPlacement> placements,
    RoundResultDetailType detailType,
    Dictionary<PlayerRef, int> detailValues)
{
    resultCompleted = false;

    if (matchResultUI != null)
    {
        matchResultUI.OnResultCompleted +=
            OnResultCompleted;
    }

    // Tell the persistent Result UI what happened this round.
    matchSession.SetCurrentRoundResults(
        placements,
        detailType,
        detailValues
    );

    // Wait for Result UI to finish.
    if (matchResultUI != null)
    {
        yield return new WaitUntil(
            () => resultCompleted
        );

        matchResultUI.OnResultCompleted -=
            OnResultCompleted;

        matchResultUI.HideImmediately();
    }
    else
    {
        yield return new WaitForSeconds(3f);
    }

    // FINAL ROUND
    if (matchSession.IsFinalRound)
    {
        Debug.Log(
            "[MATCH FLOW] Final round complete."
        );

        matchSession.CalculateOverallWinners();
        matchSession.ResetCelebrationReady();

        roundTransitionRunning = false;

        DespawnCurrentPlayers(
            NetworkManager.Instance.Runner
        );

        LoadCelebrationScene(
            NetworkManager.Instance.Runner
        );

        yield break;
    }

    // NEXT ROUND
    revealCompleted = false;

    if (matchRevealUI == null)
    {
        Debug.LogError(
            "[MATCH FLOW] MatchRevealUI is missing."
        );

        roundTransitionRunning = false;
        yield break;
    }

    matchRevealUI.OnRevealCompleted +=
        OnRevealCompleted;

    bool advanced =
        matchSession.AdvanceRound();

    if (!advanced)
    {
        matchRevealUI.OnRevealCompleted -=
            OnRevealCompleted;

        roundTransitionRunning = false;
        yield break;
    }

    Debug.Log(
        $"[MATCH FLOW] Next round: " +
        $"{matchSession.CurrentRound}/" +
        $"{matchSession.TotalRounds} | " +
        $"Mode: {matchSession.CurrentGameMode}"
    );

    yield return new WaitUntil(
        () => revealCompleted
    );

    matchRevealUI.OnRevealCompleted -=
        OnRevealCompleted;

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
            StopCoroutine(
                roundRevealCoroutine
            );
        }

        roundRevealCoroutine =
            StartCoroutine(
                StartRoundRevealCoroutine(
                    matchSession,
                    startMatch
                )
            );
    }

    private IEnumerator StartRoundRevealCoroutine(
        MatchSession matchSession,
        bool startMatch)
    {
        if (matchRevealUI == null)
        {
            Debug.LogError(
                "[MATCH FLOW] MatchRevealUI is missing."
            );

            yield break;
        }

        revealCompleted = false;

        /*
         * Subscribe BEFORE changing RoundRevealSequence.
         */
        matchRevealUI.OnRevealCompleted +=
            OnRevealCompleted;

        if (startMatch)
        {
            /*
             * This selects the first mode on the SERVER.
             *
             * MatchRevealUI only presents that decision
             * visually.
             */
            matchSession.BeginMatch();

            Debug.Log(
                $"[MATCH FLOW] Round " +
                $"{matchSession.CurrentRound}/" +
                $"{matchSession.TotalRounds} | " +
                $"Mode: {matchSession.CurrentGameMode}"
            );
        }

        /*
         * MatchRevealUI sees the new RoundRevealSequence
         * and starts its animation automatically.
         */
        yield return new WaitUntil(
            () => revealCompleted
        );

        matchRevealUI.OnRevealCompleted -=
            OnRevealCompleted;

        roundRevealCoroutine = null;

        LoadCurrentRoundScene();
    }

    private void OnRevealCompleted()
    {
        revealCompleted = true;
    }

    // ============================================================
    // LOAD GAMEPLAY SCENE
    // ============================================================

    private void LoadCurrentRoundScene()
    {
        NetworkManager networkManager =
            NetworkManager.Instance;

        if (networkManager == null)
            return;

        NetworkRunner runner =
            networkManager.Runner;

        if (runner == null ||
            !runner.IsServer)
        {
            return;
        }

        MatchSession matchSession =
            networkManager.CurrentMatchSession;

        if (matchSession == null)
            return;

        string scenePath = null;

        switch (matchSession.CurrentGameMode)
        {
            case GameModeType.MatiraMatibay:

                scenePath =
                    MATIRA_SCENE_PATH;

                break;

            case GameModeType.AgawanRelic:

                scenePath =
                    AGAWAN_RELIC_SCENE_PATH;

                break;

            case GameModeType.AgawanBandila:

                scenePath =
                    AGAWAN_BANDILA_SCENE_PATH;

                break;

            default:

                Debug.LogError(
                    $"[MATCH FLOW] Unsupported game mode: " +
                    $"{matchSession.CurrentGameMode}"
                );

                return;
        }

        Debug.Log(
            $"[MATCH FLOW] Loading gameplay scene: " +
            $"{scenePath}"
        );

        runner.LoadScene(
            SceneRef.FromPath(scenePath),
            LoadSceneMode.Single
        );
    }

    // ============================================================
    // NETWORK SCENE LOAD
    // ============================================================

    private void HandleSceneLoadDone(
        Scene scene)
    {
        if (returningToLobby)
            return;

        NetworkManager networkManager =
            NetworkManager.Instance;

        if (networkManager == null)
            return;

        NetworkRunner runner =
            networkManager.Runner;

        if (runner == null)
            return;

        if (
            scene.buildIndex ==
            networkManager.LobbySceneBuildIndex
        )
        {
            return;
        }

        if (scene.name == "Celebration")
            return;

        if (!runner.IsServer)
            return;

        RoundStateManager roundStateManager =
            FindAnyObjectByType<RoundStateManager>();

        if (roundStateManager == null)
        {
            Debug.LogWarning(
                "[MATCH FLOW] No RoundStateManager found."
            );

            return;
        }

        roundStateManager.StartRound();
    }

    // ============================================================
    // CELEBRATION
    // ============================================================

    private void LoadCelebrationScene(
        NetworkRunner runner)
    {
        if (runner == null ||
            !runner.IsServer)
        {
            return;
        }

        runner.LoadScene(
            SceneRef.FromPath(
                CELEBRATION_SCENE_PATH
            ),
            LoadSceneMode.Single
        );
    }

    private void DespawnCurrentPlayers(
        NetworkRunner runner)
    {
        if (runner == null)
            return;

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (!runner.TryGetPlayerObject(
                    player,
                    out NetworkObject playerObject))
            {
                continue;
            }

            runner.Despawn(
                playerObject
            );
        }
    }

    // ============================================================
    // CELEBRATION READY
    // ============================================================

    public void CheckCelebrationReady()
    {
        if (returningToLobby)
            return;

        NetworkManager networkManager =
            NetworkManager.Instance;

        if (networkManager == null)
            return;

        NetworkRunner runner =
            networkManager.Runner;

        if (runner == null ||
            !runner.IsRunning ||
            !runner.IsServer)
        {
            return;
        }

        MatchSession matchSession =
            networkManager.CurrentMatchSession;

        if (matchSession == null)
            return;

        if (!matchSession.AreAllPlayersCelebrationReady())
            return;

        returningToLobby = true;

        DespawnCurrentPlayers(
            runner
        );

        runner.LoadScene(
            SceneRef.FromIndex(
                networkManager.LobbySceneBuildIndex
            ),
            LoadSceneMode.Single
        );
    }
}