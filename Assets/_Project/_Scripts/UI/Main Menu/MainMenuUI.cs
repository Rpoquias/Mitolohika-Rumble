using TMPro;
using UnityEngine;
using System.Collections;

public class MainMenuUI : MonoBehaviour
{
    [Header("Host")]
    [SerializeField] private TMP_InputField roomNameInput;

    [Header("Join")]
    [SerializeField] private GameObject joinLobbyPanel;
    [SerializeField] private TMP_InputField ipAddressInput;


    [Header("Status")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private float statusDuration = 3f;

private Coroutine statusCoroutine;

private ConnectionMode connectionMode = ConnectionMode.Online;
public void SelectOnline()
{
    connectionMode = ConnectionMode.Online;
    Debug.Log("[MAIN MENU] Connection mode: Online");
}

public void SelectLAN()
{
    connectionMode = ConnectionMode.LAN;
    Debug.Log("[MAIN MENU] Connection mode: LAN");
}
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
    statusText.text =
        $"HOST DISCONNECTED\n{NetworkManager.Instance.LastNetworkError}";
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
        statusText.text = "ROOM NAME EMPTY";
        statusText.gameObject.SetActive(true);
        return;
    }
    switch (connectionMode)
    {
        case ConnectionMode.Online:

            Debug.Log($"[ONLINE] Hosting room: {roomName}");

            NetworkManager.Instance.Host(roomName);

            break;

        case ConnectionMode.LAN:

            Debug.Log($"[LAN] Hosting local room: {roomName}");

            NetworkManager.Instance.HostLAN(roomName);

            break;
    }
}

public void JoinLANGame()
{
    string ipAddress = ipAddressInput.text.Trim();

    if (string.IsNullOrEmpty(ipAddress))
    {
        Debug.LogWarning("[LAN] IP address cannot be empty.");
        return;
    }

    Debug.Log($"[LAN] Joining host at {ipAddress}");

    NetworkManager.Instance.JoinLAN(ipAddress);
}
public void JoinGame()
{
    switch (connectionMode)
    {
        case ConnectionMode.Online:
            Debug.Log("[ONLINE] Opening online join lobby.");
            FindAnyObjectByType<MainMenu>().ShowJoinPanel();
            break;

        case ConnectionMode.LAN:
            Debug.Log("[LAN] Opening local join lobby.");
            FindAnyObjectByType<MainMenu>().ShowLocalJoinPanel();
            break;
    }
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
    switch (connectionMode)
    {
        case ConnectionMode.Online:
            Debug.Log("[ONLINE] Opening online join lobby.");
            NetworkManager.Instance.OpenPublicLobby();
            FindAnyObjectByType<MainMenu>().ShowJoinPanel();
            break;

        case ConnectionMode.LAN:
            Debug.Log("[LAN] Opening local join lobby.");
            FindAnyObjectByType<MainMenu>().ShowLocalJoinPanel();
            break;
    }
}
}