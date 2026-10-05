using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyUIController : MonoBehaviour
{
    [Header("Main UI")]
    [SerializeField] private GameObject lobbyUI;
    [SerializeField] private GameObject characterSelectionUI;

[SerializeField]
private LobbyRoundSettingsUI lobbyRoundSettingsUI;

    [Header("Lobby Panels")]
    [SerializeField] private GameObject roundSettingsUI;
    [SerializeField] private GameObject roundSettingsButton;

    [Header("Common UI")]
    [SerializeField] private GameObject readyButton;
    [SerializeField] private GameObject settingsButton;

    [Header("Gameplay UI")]
    [SerializeField] private GameObject mobileControls;


    private void Awake()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnNetworkSceneLoadDoneEvent +=
                HandleNetworkSceneLoaded;
        }
    }

    private void Start()
    {
        // Handles the case where this UI exists after
        // the network scene has already finished loading.
        UpdateHostOnlyUI();
    }

    private void HandleNetworkSceneLoaded(Scene scene)
    {
        if (scene != SceneManager.GetActiveScene())
            return;

        UpdateHostOnlyUI();
    }

    private void UpdateHostOnlyUI()
    {
        if (roundSettingsButton == null)
            return;

        if (NetworkManager.Instance == null)
            return;

        NetworkRunner runner =
            NetworkManager.Instance.Runner;

        if (runner == null || !runner.IsRunning)
            return;

        bool isHost = runner.IsServer;

        roundSettingsButton.SetActive(isHost);

        // Clients should never have the round settings panel open.
        if (!isHost && roundSettingsUI != null)
        {
            roundSettingsUI.SetActive(false);
        }
    }

    public void ShowCharacterSelection()
    {
        if (lobbyUI != null)
            lobbyUI.SetActive(false);

        if (roundSettingsUI != null)
            roundSettingsUI.SetActive(false);

        if (characterSelectionUI != null)
            characterSelectionUI.SetActive(true);

        if (mobileControls != null)
            mobileControls.SetActive(false);

        if (MobileInputProvider.Instance != null)
        {
            MobileInputProvider.Instance
                .SetInputEnabled(false);
        }

        if (readyButton != null)
            readyButton.SetActive(true);

        if (settingsButton != null)
            settingsButton.SetActive(true);
    }

    public void ShowLobby()
    {
        if (lobbyUI != null)
            lobbyUI.SetActive(true);

        if (roundSettingsUI != null)
            roundSettingsUI.SetActive(false);

        if (characterSelectionUI != null)
            characterSelectionUI.SetActive(false);

        if (mobileControls != null)
            mobileControls.SetActive(true);

        if (MobileInputProvider.Instance != null)
        {
            MobileInputProvider.Instance
                .SetInputEnabled(true);
        }

        if (readyButton != null)
            readyButton.SetActive(true);

        if (settingsButton != null)
            settingsButton.SetActive(true);

        // One-time check whenever we return to the lobby.
        UpdateHostOnlyUI();
    }

public void ShowRoundSettings()
{
    if (NetworkManager.Instance == null)
        return;

    NetworkRunner runner =
        NetworkManager.Instance.Runner;

    if (runner == null || !runner.IsServer)
        return;

    if (roundSettingsUI != null)
        roundSettingsUI.SetActive(true);

    if (lobbyRoundSettingsUI != null)
        lobbyRoundSettingsUI.Open();
}

    public void HideRoundSettings()
    {
        if (roundSettingsUI != null)
            roundSettingsUI.SetActive(false);
    }

    public void ToggleRoundSettings()
    {
        if (roundSettingsUI == null)
            return;

        NetworkRunner runner =
            NetworkManager.Instance != null
                ? NetworkManager.Instance.Runner
                : null;

        if (runner == null || !runner.IsServer)
            return;

        roundSettingsUI.SetActive(
            !roundSettingsUI.activeSelf
        );
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnNetworkSceneLoadDoneEvent -=
                HandleNetworkSceneLoaded;
        }
    }
}