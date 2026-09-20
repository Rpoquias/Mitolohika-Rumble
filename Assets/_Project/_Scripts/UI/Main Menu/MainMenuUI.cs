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
            statusText.text = "HOST DISCONNECTED";
            break;

        case MainMenuMessage.ConnectionFailed:
            statusText.text = "CONNECTION FAILED";
            break;

        default:
            statusText.gameObject.SetActive(false);
            return;
    }

    statusText.gameObject.SetActive(true);

    NetworkManager.Instance.ClearMainMenuMessage();

    if (statusCoroutine != null)
        StopCoroutine(statusCoroutine);

    statusCoroutine = StartCoroutine(HideStatusAfterDelay());
}
private IEnumerator HideStatusAfterDelay()
{
    yield return new WaitForSeconds(statusDuration);

    statusText.gameObject.SetActive(false);

    statusCoroutine = null;
}

    public void HostGame()
    {
        string roomName = roomNameInput.text.Trim();

        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogWarning("Room name cannot be empty.");
            return;
        }

        NetworkManager.Instance.Host(roomName);
    }

    public void ShowJoinPanel()
    {
        joinLobbyPanel.SetActive(true);
    }

    public void BackFromJoin()
    {
        NetworkManager.Instance.Disconnect();

        joinLobbyPanel.SetActive(false);
    }

    public void OpenJoinLobby()
    {
        NetworkManager.Instance.OpenPublicLobby();
    }
}