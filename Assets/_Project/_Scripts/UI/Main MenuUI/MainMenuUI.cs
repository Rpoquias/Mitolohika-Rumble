
using TMPro;
using UnityEngine;
using System.Collections;

public class MainMenuUI : MonoBehaviour
{
    [Header("Host")]
    [SerializeField] private TMP_InputField roomNameInput;

    [Header("Join")]
    [SerializeField] private GameObject joinLobbyPanel;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private float statusDuration = 3f;

    private Coroutine statusCoroutine;

    private void Start()
    {
        ShowPendingStatus();
    }

    public void HostGame()
    {
        string roomName = roomNameInput.text.Trim();

        if (string.IsNullOrEmpty(roomName))
        {
            ShowStatus("ROOM NAME EMPTY");
            return;
        }

        if (NetworkManager.Instance == null)
        {
            ShowStatus("NETWORK MANAGER UNAVAILABLE");
            return;
        }

        Debug.Log($"[ONLINE] Hosting room: {roomName}");
        NetworkManager.Instance.Host(roomName);
    }

    public void JoinGame()
    {
        if (NetworkManager.Instance == null)
        {
            ShowStatus("NETWORK MANAGER UNAVAILABLE");
            return;
        }

        Debug.Log("[ONLINE] Opening join lobby.");

        ShowJoinPanel();
        NetworkManager.Instance.OpenPublicLobby();
    }

    public void ShowJoinPanel()
    {
        if (joinLobbyPanel != null)
            joinLobbyPanel.SetActive(true);
    }

    public void BackFromJoin()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.Disconnect();

        if (statusCoroutine != null)
        {
            StopCoroutine(statusCoroutine);
            statusCoroutine = null;
        }

        if (statusText != null)
            statusText.gameObject.SetActive(false);

        if (joinLobbyPanel != null)
            joinLobbyPanel.SetActive(false);
    }

    private void ShowPendingStatus()
    {
        if (NetworkManager.Instance == null ||
            statusText == null)
            return;

        MainMenuMessage message =
            NetworkManager.Instance.PendingMainMenuMessage;

        string messageText = null;

        switch (message)
        {
            case MainMenuMessage.HostDisconnected:
                messageText =
                    $"HOST DISCONNECTED\n" +
                    $"{NetworkManager.Instance.LastNetworkError}";
                break;

            case MainMenuMessage.ConnectionFailed:
                messageText = "CONNECTION FAILED";
                break;

            default:
                return;
        }

        // Open the panel BEFORE displaying the status.
        ShowJoinPanel();

        // Keep disconnect messages visible until dismissed.
        ShowPersistentStatus(messageText);

        NetworkManager.Instance.ClearMainMenuMessage();
    }

    private void ShowPersistentStatus(string message)
    {
        if (statusText == null)
            return;

        if (statusCoroutine != null)
        {
            StopCoroutine(statusCoroutine);
            statusCoroutine = null;
        }

        statusText.text = message;
        statusText.gameObject.SetActive(true);
    }

    private void ShowStatus(string message)
    {
        if (statusText == null)
            return;

        statusText.text = message;
        statusText.gameObject.SetActive(true);

        if (statusCoroutine != null)
            StopCoroutine(statusCoroutine);

        statusCoroutine = StartCoroutine(HideStatusAfterDelay());
    }

    private IEnumerator HideStatusAfterDelay()
    {
        yield return new WaitForSeconds(statusDuration);

        if (statusText != null)
            statusText.gameObject.SetActive(false);

        statusCoroutine = null;
    }
}
