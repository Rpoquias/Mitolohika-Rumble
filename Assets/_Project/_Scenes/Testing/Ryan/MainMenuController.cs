using TMPro;
using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [Header("Host")]
    [SerializeField] private TMP_InputField roomNameInput;

    [SerializeField] private GameObject joinLobbyPanel  ;

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
    public void OpenJoinLobby()
    {
        NetworkManager.Instance.OpenPublicLobby();
    }
}