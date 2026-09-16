using UnityEngine;
using UnityEngine.UI;

public class LobbyStartButtonUI : MonoBehaviour
{
    [SerializeField] private Button startButton;

    private void Start()
    {
        UpdateButton();
    }

    private void Update()
    {
        UpdateButton();
    }

  private void UpdateButton()
{
    if (LobbyManager.Instance == null)
    {
        startButton.interactable = false;
        Debug.Log("[START BUTTON] LobbyManager missing.");
        return;
    }

    bool isHost = LobbyManager.Instance.IsHost;
    bool allReady = LobbyManager.Instance.AllPlayersReady;



    startButton.interactable =
        isHost && allReady;
}
}