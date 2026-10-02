using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchFlowManager : MonoBehaviour
{
    public static MatchFlowManager Instance { get; private set; }

    private const string MATIRA_SCENE_PATH =
        "Assets/_Project/_Scenes/Testing/Ryan/MatiraMatibay.unity";


        private const string CELEBRATION_SCENE_PATH =
    "Assets/_Project/_Scenes/Testing/Ryan/Celebration.unity";

    private int lastHandledRound = -1;
    private bool returningToLobby;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void StartMatch()
    {

        returningToLobby = false;
        NetworkManager networkManager =
            NetworkManager.Instance;

        if (networkManager == null)
        {
            Debug.LogError(
                "[MATCH FLOW] NetworkManager is missing."
            );

            return;
        }

        NetworkRunner runner =
            networkManager.Runner;

        if (runner == null)
        {
            Debug.LogError(
                "[MATCH FLOW] NetworkRunner is missing."
            );

            return;
        }

        if (!runner.IsServer)
        {
            Debug.LogWarning(
                "[MATCH FLOW] Only the Host can start the match."
            );

            return;
        }

        MatchSession matchSession =
            networkManager.CurrentMatchSession;

        if (matchSession == null)
        {
            Debug.LogError(
                "[MATCH FLOW] MatchSession is missing."
            );

            return;
        }

        lastHandledRound = -1;

        matchSession.BeginMatch();

        Debug.Log(
            $"[MATCH FLOW] Match started. " +
            $"Round {matchSession.CurrentRound}/" +
            $"{matchSession.TotalRounds} | " +
            $"Mode: {matchSession.CurrentGameMode}"
        );

        LoadCurrentRoundScene();
    }

  public void HandleRoundComplete(
    List<RoundPlacement> placements)
    {
        NetworkManager networkManager =
            NetworkManager.Instance;

        if (networkManager == null)
        {
            Debug.LogError(
                "[MATCH FLOW] NetworkManager is missing."
            );

            return;
        }

        NetworkRunner runner =
            networkManager.Runner;

        if (runner == null)
        {
            Debug.LogError(
                "[MATCH FLOW] NetworkRunner is missing."
            );

            return;
        }

        if (!runner.IsServer)
        {
            return;
        }

     MatchSession matchSession =
    networkManager.CurrentMatchSession;

if (matchSession == null)
{
    Debug.LogError(
        "[MATCH FLOW] MatchSession is missing."
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

int completedRound =
    matchSession.CurrentRound;

        if (lastHandledRound == completedRound)
        {
            Debug.LogWarning(
                $"[MATCH FLOW] Round {completedRound} " +
                "has already been handled."
            );

            return;
        }

        lastHandledRound = completedRound;

        Debug.Log(
            $"[MATCH FLOW] Round {completedRound}/" +
            $"{matchSession.TotalRounds} complete."
        );
if (matchSession.IsFinalRound)
{
    Debug.Log(
        "[MATCH FLOW] Final round complete."
    );

    matchSession.CalculateOverallWinners();

    matchSession.ResetCelebrationReady();

    DespawnCurrentPlayers(runner);

    LoadCelebrationScene(runner);

    return;
}

        if (!matchSession.AdvanceRound())
        {
            return;
        }

        Debug.Log(
            $"[MATCH FLOW] Starting next round: " +
            $"Round {matchSession.CurrentRound}/" +
            $"{matchSession.TotalRounds} | " +
            $"Mode: {matchSession.CurrentGameMode}"
        );

        LoadCurrentRoundScene();
    }
    private void DespawnCurrentPlayers(
    NetworkRunner runner)
{
    foreach (PlayerRef player in runner.ActivePlayers)
    {
        if (!runner.TryGetPlayerObject(
                player,
                out NetworkObject playerObject))
        {
            continue;
        }

        Debug.Log(
            $"[MATCH FLOW] Despawning gameplay player " +
            $"{playerObject.name} for {player}."
        );

        runner.Despawn(playerObject);
    }
}

private void LoadCelebrationScene(
    NetworkRunner runner)
{
    Debug.Log(
        $"[MATCH FLOW] Loading Celebration scene: " +
        $"{CELEBRATION_SCENE_PATH}"
    );

    runner.LoadScene(
        SceneRef.FromPath(CELEBRATION_SCENE_PATH),
        LoadSceneMode.Single
    );
}

    private void LoadCurrentRoundScene()
    {
        NetworkManager networkManager =
            NetworkManager.Instance;

        if (networkManager == null)
        {
            Debug.LogError(
                "[MATCH FLOW] NetworkManager is missing."
            );

            return;
        }

        NetworkRunner runner =
            networkManager.Runner;

        if (runner == null)
        {
            Debug.LogError(
                "[MATCH FLOW] NetworkRunner is missing."
            );

            return;
        }

        if (!runner.IsServer)
        {
            return;
        }

        MatchSession matchSession =
            networkManager.CurrentMatchSession;

        if (matchSession == null)
        {
            Debug.LogError(
                "[MATCH FLOW] MatchSession is missing."
            );

            return;
        }

        if (matchSession.CurrentGameMode !=
            GameModeType.MatiraMatibay)
        {
            Debug.LogWarning(
                $"[MATCH FLOW] Mode " +
                $"{matchSession.CurrentGameMode} " +
                "does not have a scene yet."
            );

            return;
        }

        Debug.Log(
            $"[MATCH FLOW] Loading Matira Matibay scene. " +
            $"Round {matchSession.CurrentRound}/" +
            $"{matchSession.TotalRounds}"
        );

        runner.LoadScene(
            SceneRef.FromPath(MATIRA_SCENE_PATH),
            LoadSceneMode.Single
        );
    }
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
        !runner.IsRunning)
    {
        return;
    }

    if (!runner.IsServer)
        return;

    MatchSession matchSession =
        networkManager.CurrentMatchSession;

    if (matchSession == null)
    {
        Debug.LogError(
            "[MATCH FLOW] MatchSession is missing."
        );

        return;
    }

    if (!matchSession.AreAllPlayersCelebrationReady())
        return;

    returningToLobby = true;

    Debug.Log(
        "[MATCH FLOW] All players are ready. " +
        "Returning to Lobby."
    );

    DespawnCurrentPlayers(runner);

    runner.LoadScene(
        SceneRef.FromIndex(
            networkManager.LobbySceneBuildIndex
        ),
        LoadSceneMode.Single
    );
}
}