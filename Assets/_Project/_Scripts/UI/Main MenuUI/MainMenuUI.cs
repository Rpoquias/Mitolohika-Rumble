
using System.Collections;
using TMPro;
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [Header("Host")]
    [SerializeField] private TMP_InputField roomNameInput;

    [Header("Join")]
    [SerializeField] private GameObject joinLobbyPanel;

    [Header("Status")]
    [SerializeField] private GameObject statusContainer;
    [SerializeField] private TMP_Text statusText;
    [SerializeField, Min(0.1f)] private float statusDuration = 3f;

    private Coroutine statusCoroutine;
    private NetworkManager subscribedNetworkManager;

    private void OnEnable()
    {
        SubscribeToNetworkManager();
        ShowPendingStatus();
    }

    private void OnDisable()
    {
        UnsubscribeFromNetworkManager();
        StopStatusCoroutine();
    }

    private void SubscribeToNetworkManager()
    {
        NetworkManager manager = NetworkManager.Instance;

        if (manager == null)
            return;

        if (subscribedNetworkManager == manager)
        {
            RefreshConnectionStatus(manager);
            return;
        }

        UnsubscribeFromNetworkManager();

        subscribedNetworkManager = manager;
        subscribedNetworkManager.OnConnectionStatusChanged +=
            HandleConnectionStatusChanged;

        RefreshConnectionStatus(manager);
    }

    private void UnsubscribeFromNetworkManager()
    {
        if (subscribedNetworkManager == null)
            return;

        subscribedNetworkManager.OnConnectionStatusChanged -=
            HandleConnectionStatusChanged;

        subscribedNetworkManager = null;
    }

    private void RefreshConnectionStatus(NetworkManager manager)
    {
        if (manager == null)
            return;

        if (string.IsNullOrWhiteSpace(manager.ConnectionStatus))
        {
            ClearStatus();
            return;
        }

        ShowNetworkStatus(manager.ConnectionStatus);
    }

    public void HostGame()
    {
        NetworkManager manager = NetworkManager.Instance;

        if (manager == null)
        {
            ShowTemporaryStatus(
                "ONLINE SERVICE UNAVAILABLE.\nPlease restart the game.");
            return;
        }

        string roomName = roomNameInput != null
            ? roomNameInput.text.Trim()
            : string.Empty;

        if (string.IsNullOrWhiteSpace(roomName))
        {
            ShowTemporaryStatus(
                "ROOM NAME IS EMPTY.\nEnter a room name first.");
            return;
        }

        if (manager.IsBusy || manager.Runner != null)
        {
            ShowTemporaryStatus(
                "PLEASE WAIT.\nAnother network operation is in progress.");
            return;
        }

        manager.Host(roomName);
    }

    public void JoinGame()
    {
        NetworkManager manager = NetworkManager.Instance;

        if (manager == null)
        {
            ShowTemporaryStatus(
                "ONLINE SERVICE UNAVAILABLE.\nPlease restart the game.");
            return;
        }

        if (manager.IsBusy || manager.Runner != null)
        {
            ShowTemporaryStatus(
                "PLEASE WAIT.\nAnother network operation is in progress.");
            return;
        }

        ShowJoinPanel();
        manager.OpenPublicLobby();
    }

    public void ShowJoinPanel()
    {
        if (joinLobbyPanel != null)
            joinLobbyPanel.SetActive(true);
    }

    public async void BackFromJoin()
    {
        if (joinLobbyPanel != null)
            joinLobbyPanel.SetActive(false);

        NetworkManager manager = NetworkManager.Instance;

        if (manager != null)
            await manager.CancelPendingOperation();

        ShowTemporaryStatus("FINDING LOBBY CANCELLED.");
    }

    public async void BackFromHost()
    {
        NetworkManager manager = NetworkManager.Instance;

        if (manager != null)
            await manager.CancelPendingOperation();

        ShowTemporaryStatus("HOSTING CANCELLED.");
    }

    private void HandleConnectionStatusChanged(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            ClearStatus();
            return;
        }

        ShowNetworkStatus(message);
    }

    private void ShowPendingStatus()
    {
        NetworkManager manager = NetworkManager.Instance;

        if (manager == null)
            return;

        switch (manager.PendingMainMenuMessage)
        {
            case MainMenuMessage.HostDisconnected:
            {
                string message =
                    "CONNECTION LOST.\n" +
                    "The host left or the connection was interrupted.";

                if (!string.IsNullOrWhiteSpace(manager.LastNetworkError))
                    message += "\n\nDetails: " + manager.LastNetworkError;

                ShowJoinPanel();
                ShowTemporaryStatus(message);
                manager.ClearMainMenuMessage();
                return;
            }

            case MainMenuMessage.ConnectionFailed:
            {
                ShowJoinPanel();

                ShowTemporaryStatus(
                    "CONNECTION FAILED.\n" +
                    "Check your connection and try again.");

                manager.ClearMainMenuMessage();
                return;
            }
        }

        RefreshConnectionStatus(manager);
    }

    private void ShowNetworkStatus(string message)
    {
        if (statusContainer == null || statusText == null)
            return;

        if (IsTerminalStatus(message))
        {
            ShowTemporaryStatus(message);
            return;
        }

        NetworkManager manager = NetworkManager.Instance;

        if (manager != null && manager.IsBusy)
        {
            StopStatusCoroutine();

            statusText.text = message;
            statusContainer.SetActive(true);
            return;
        }

        ShowTemporaryStatus(message);
    }

    private bool IsTerminalStatus(string message)
    {
        return message.IndexOf(
                   "CANCEL",
                   System.StringComparison.OrdinalIgnoreCase) >= 0
            || message.IndexOf(
                   "FAIL",
                   System.StringComparison.OrdinalIgnoreCase) >= 0
            || message.IndexOf(
                   "LOST",
                   System.StringComparison.OrdinalIgnoreCase) >= 0
            || message.IndexOf(
                   "DISCONNECTED",
                   System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void ShowTemporaryStatus(string message)
    {
        if (statusContainer == null || statusText == null)
            return;

        StopStatusCoroutine();

        statusText.text = message;
        statusContainer.SetActive(true);

        statusCoroutine = StartCoroutine(HideStatusAfterDelay());
    }

    private void ClearStatus()
    {
        StopStatusCoroutine();

        if (statusContainer != null)
            statusContainer.SetActive(false);
    }

    private void StopStatusCoroutine()
    {
        if (statusCoroutine == null)
            return;

        StopCoroutine(statusCoroutine);
        statusCoroutine = null;
    }

    private IEnumerator HideStatusAfterDelay()
    {
        yield return new WaitForSecondsRealtime(statusDuration);

        if (statusContainer != null)
            statusContainer.SetActive(false);

        statusCoroutine = null;
    }
}
