using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyReadyUI : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;

    [Header("Ready Count")]
    [SerializeField] private TMP_Text readyCountText;

    [Header("Start Status")]
    [SerializeField] private TMP_Text startStatusText;

    private NetworkRunner runner;
    private NetworkPlayerState localPlayer;

    private void Update()
    {
        if (runner == null || !runner.IsRunning)
        {
            TryInitialize();
            return;
        }

        UpdateLocalPlayer();
        UpdateUI();
    }

    private void TryInitialize()
    {
        if (NetworkManager.Instance == null)
            return;

        runner = NetworkManager.Instance.Runner;
    }

    private void UpdateLocalPlayer()
    {
        // Drop the reference the moment it's no longer safe to use.
        if (localPlayer != null && !localPlayer.IsValid)
        {
            localPlayer = null;
        }

        if (localPlayer != null)
            return;

        if (!runner.TryGetPlayerObject(runner.LocalPlayer, out NetworkObject playerObject))
            return;

        localPlayer = playerObject.GetComponent<NetworkPlayerState>();
    }

    private void UpdateUI()
    {
        UpdateButton();
        UpdateReadyCount();
        UpdateStartStatus();
    }

    private void UpdateButton()
    {
        if (actionButtonText == null)
            return;

        if (runner.IsServer)
        {
            actionButtonText.text = "START";

            actionButton.interactable =
                LobbyManager.Instance != null &&
                LobbyManager.Instance.AllPlayersReady;

            return;
        }

        if (localPlayer == null || !localPlayer.IsValid)
        {
            actionButtonText.text = "READY";
            actionButton.interactable = false;
            return;
        }

        actionButtonText.text = localPlayer.IsReady ? "UNREADY" : "READY";
        actionButton.interactable = true;
    }

    private void UpdateReadyCount()
    {
        if (readyCountText == null)
            return;

        int readyPlayers = 0;
        int totalPlayers = 0;

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            totalPlayers++;

            if (!runner.TryGetPlayerObject(player, out NetworkObject playerObject))
                continue;

            NetworkPlayerState state = playerObject.GetComponent<NetworkPlayerState>();

            if (state != null && state.IsValid && state.IsReady)
            {
                readyPlayers++;
            }
        }

        readyCountText.text = $"READY: {readyPlayers}/{totalPlayers}";
    }

    private void UpdateStartStatus()
    {
        if (startStatusText == null)
            return;

        if (!runner.IsServer)
        {
            startStatusText.text = "WAITING FOR HOST...";
            return;
        }

        if (LobbyManager.Instance == null)
            return;

        startStatusText.text = LobbyManager.Instance.AllPlayersReady
            ? "ALL PLAYERS READY"
            : "WAITING FOR PLAYERS...";
    }

    public void OnActionButtonPressed()
    {
        if (runner == null || !runner.IsRunning)
            return;

        if (runner.IsServer)
        {
            LobbyManager.Instance?.StartGame();
            return;
        }

        if (localPlayer == null || !localPlayer.IsValid)
        {
            UpdateLocalPlayer();
            return;
        }

        localPlayer.ToggleReady();
    }
    public void StopTracking()
    {
        enabled = false;
    }
}