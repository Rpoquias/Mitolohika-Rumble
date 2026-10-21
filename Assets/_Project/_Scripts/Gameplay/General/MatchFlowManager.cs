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
private void OnDestroy()
{
    if (Instance == this)
        Instance = null;
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

    // ============================================================
    // MATCH START
    // ============================================================

   
public void StartMatch()
{

    returningToLobby = false;
    roundTransitionRunning = false;
    lastHandledRound = -1;

    NetworkManager networkManager = NetworkManager.Instance;

    if (networkManager == null)
    {
        return;
    }

    NetworkRunner runner = networkManager.Runner;

    if (runner == null)
    {
        return;
    }

    if (!runner.IsRunning)
    {
        return;
    }

    if (!runner.IsServer)
    {
        return;
    }

    MatchSession matchSession = networkManager.CurrentMatchSession;

    if (matchSession == null)
    {
 

        return;
    }

    if (!matchSession.IsSpawned)
    {
 

        return;
    }

    networkManager.SetJoinable(false);


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

    // Mark the round handled only after validation succeeds.
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
 

    matchSession.CalculateOverallWinners();

    AudioManager.Instance?.PlaySFX(SFXID.Victory);

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


        roundRevealCoroutine = null;
        yield break;
    }

    if (matchSession == null || !matchSession.IsSpawned)
    {


        roundRevealCoroutine = null;
        yield break;
    }

    revealCompleted = false;

    // Subscribe before changing the replicated reveal sequence.
    matchRevealUI.OnRevealCompleted += OnRevealCompleted;

    if (startMatch)
        matchSession.BeginMatch();

    yield return new WaitUntil(() => revealCompleted);

    matchRevealUI.OnRevealCompleted -= OnRevealCompleted;
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

                return;
        }

// Select the loading artwork before Fusion starts loading.
Sprite loadingBackground = matchRevealUI != null
    ? matchRevealUI.GetLoadingBackground(
        matchSession.CurrentGameMode
    )
    : null;

// A missing mode background automatically uses the general one.
LoadingScreenUI.Instance?.SetLoadingBackground(
    loadingBackground
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
LoadingScreenUI.Instance?.SetLoadingBackground();
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


DespawnCurrentPlayers(runner);

LoadingScreenUI.Instance?.SetLoadingBackground();

runner.LoadScene(
    SceneRef.FromIndex(
        networkManager.LobbySceneBuildIndex
    ),
    LoadSceneMode.Single
);
    }

}