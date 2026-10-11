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
        NetworkManager.Instance.OpenPublicLobby();

        ShowJoinPanel();
    }

    public void ShowJoinPanel()
    {
        joinLobbyPanel.SetActive(true);
    }

    public void BackFromJoin()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.Disconnect();

        joinLobbyPanel.SetActive(false);
    }

    private void ShowPendingStatus()
    {
        if (statusText == null)
            return;

        if (NetworkManager.Instance == null)
        {
            statusText.gameObject.SetActive(false);
            return;
        }

        MainMenuMessage message =
            NetworkManager.Instance.PendingMainMenuMessage;

        switch (message)
        {
            case MainMenuMessage.HostDisconnected:
                ShowStatus(
                    $"HOST DISCONNECTED\n" +
                    $"{NetworkManager.Instance.LastNetworkError}"
                );
                break;

            case MainMenuMessage.ConnectionFailed:
                ShowStatus("CONNECTION FAILED");
                break;

            default:
                statusText.gameObject.SetActive(false);
                return;
        }

        NetworkManager.Instance.ClearMainMenuMessage();
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