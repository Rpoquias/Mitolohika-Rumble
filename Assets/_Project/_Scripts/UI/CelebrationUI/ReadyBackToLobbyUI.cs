
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ReadyBackToLobbyUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;
    [SerializeField] private TMP_Text readyCountText;

    private MatchSession matchSession;
    private NetworkManager networkManager;

    private void OnEnable()
    {
        if (readyButton != null)
            readyButton.onClick.AddListener(HandleReadyPressed);

        networkManager = NetworkManager.Instance;

        if (networkManager != null)
        {
            networkManager.OnPlayerJoinedEvent += HandlePlayerChanged;
            networkManager.OnPlayerLeftEvent += HandlePlayerChanged;
            networkManager.OnNetworkSceneLoadDoneEvent += HandleSceneLoaded;
        }

        BindMatchSession();
        RefreshUI();
    }

    private void OnDisable()
    {
        if (readyButton != null)
            readyButton.onClick.RemoveListener(HandleReadyPressed);

        if (networkManager != null)
        {
            networkManager.OnPlayerJoinedEvent -= HandlePlayerChanged;
            networkManager.OnPlayerLeftEvent -= HandlePlayerChanged;
            networkManager.OnNetworkSceneLoadDoneEvent -= HandleSceneLoaded;
        }

        UnbindMatchSession();

        networkManager = null;
    }

    private void BindMatchSession()
    {
        MatchSession currentSession = MatchSession.Instance;

        if (currentSession == matchSession)
            return;

        UnbindMatchSession();

        if (currentSession == null || !currentSession.IsSpawned)
            return;

        matchSession = currentSession;
        matchSession.OnCelebrationReadyChanged += HandleReadyChanged;
    }

    private void UnbindMatchSession()
    {
        if (matchSession == null)
            return;

        matchSession.OnCelebrationReadyChanged -= HandleReadyChanged;
        matchSession = null;
    }

    private void HandleReadyChanged()
    {
        RefreshUI();
    }

    private void HandlePlayerChanged(PlayerRef player)
    {
        RefreshUI();
    }

    private void HandleSceneLoaded(Scene scene)
    {
        BindMatchSession();
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (!isActiveAndEnabled)
            return;

        if (matchSession == null || !matchSession.IsSpawned)
            BindMatchSession();

        if (matchSession == null || !matchSession.IsSpawned)
        {
            if (readyButton != null)
                readyButton.interactable = false;

            return;
        }

        NetworkRunner runner = matchSession.Runner;

        if (runner == null || !runner.IsRunning)
        {
            if (readyButton != null)
                readyButton.interactable = false;

            return;
        }

        UpdateReadyCount(runner);
        UpdateButton(runner);
    }

    private void UpdateReadyCount(NetworkRunner runner)
    {
        if (readyCountText == null)
            return;

        int playerCount = 0;

        foreach (PlayerRef player in runner.ActivePlayers)
            playerCount++;

        int readyCount = matchSession.GetCelebrationReadyCount();

        readyCountText.text =
            $"{readyCount}/{playerCount} Players Ready";
    }

    private void UpdateButton(NetworkRunner runner)
    {
        if (readyButton == null)
            return;

        bool isReady = matchSession.IsCelebrationPlayerReady(
            runner.LocalPlayer);

        readyButton.interactable = !isReady;

        if (readyButtonText != null)
        {
            readyButtonText.text = isReady
                ? "READY!"
                : "RETURN TO LOBBY";
        }
    }

    private void HandleReadyPressed()
    {
        if (matchSession == null || !matchSession.IsSpawned)
        {
            BindMatchSession();
            RefreshUI();
        }

        if (matchSession == null || !matchSession.IsSpawned)
        {
            Debug.LogWarning(
                "[READY BACK TO LOBBY] MatchSession is unavailable.");

            return;
        }

        NetworkRunner runner = matchSession.Runner;

        if (runner == null || !runner.IsRunning)
            return;

        if (matchSession.IsCelebrationPlayerReady(runner.LocalPlayer))
            return;

        // Prevent repeated requests while waiting for replicated state.
        readyButton.interactable = false;

        if (readyButtonText != null)
            readyButtonText.text = "READY!";

        matchSession.RequestCelebrationReady(true);
    }
}
