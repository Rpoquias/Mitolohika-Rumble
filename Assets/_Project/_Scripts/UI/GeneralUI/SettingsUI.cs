
using UnityEngine;

public class SettingsPanelController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject settingsPanel;

    public void ShowAudio()
    {
        if (audioPanel != null)
            audioPanel.SetActive(true);

        if (controlsPanel != null)
            controlsPanel.SetActive(false);
    }

    public void ShowControls()
    {
        if (audioPanel != null)
            audioPanel.SetActive(false);

        if (controlsPanel != null)
            controlsPanel.SetActive(true);
    }

    public void ShowSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void LeaveLobby()
    {
        NetworkManager manager = NetworkManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning(
                "Cannot leave lobby: NetworkManager instance not found.");
            return;
        }

        manager.Disconnect();
    }
}
