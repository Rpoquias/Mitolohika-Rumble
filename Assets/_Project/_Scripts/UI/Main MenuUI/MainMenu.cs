using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject localJoinPanel;
    [SerializeField] private GameObject settingsPanel;


    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        HideAllPanels();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void ShowLobbyPanel()
    {
        HideAllPanels();

        if (lobbyPanel != null)
            lobbyPanel.SetActive(true);
    }

    public void ShowHostPanel()
    {
        HideAllPanels();

        if (hostPanel != null)
            hostPanel.SetActive(true);
    }

public void ShowJoinPanel()
{
    HideAllPanels();

    if (joinPanel != null)
        joinPanel.SetActive(true);
}

   public void ShowLocalJoinPanel()
{
    HideAllPanels();

    if (localJoinPanel != null)
        localJoinPanel.SetActive(true);
}
    public void ShowSettings()
    {
        HideAllPanels();

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    private void HideAllPanels()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);

        if (hostPanel != null)
            hostPanel.SetActive(false);

        if (joinPanel != null)
            joinPanel.SetActive(false);

        if (localJoinPanel != null)
            localJoinPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void LoadScene(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    public void QuitApplication()
    {
        Application.Quit();
    }
}