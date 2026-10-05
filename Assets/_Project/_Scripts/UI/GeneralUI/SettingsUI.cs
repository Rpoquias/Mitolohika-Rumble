using UnityEngine;

public class SettingsPanelController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject settingsPanel;

    public void ShowAudio()
    {
        audioPanel.SetActive(true);
        controlsPanel.SetActive(false);
    }

    public void ShowControls()
    {
        audioPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    public void ShowSettings()
    {
        settingsPanel.SetActive(true);
    }
        public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

       public void LeaveLobby()
    {
        if (NetworkManager.Instance == null)
            return;

        NetworkManager.Instance.Disconnect();
    }
}