using UnityEngine;

public class SettingsPanelController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject controlsPanel;

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

    public void ShowDefault()
    {
        audioPanel.SetActive(false);
        controlsPanel.SetActive(false);
    }

       public void LeaveLobby()
    {
        if (NetworkManager.Instance == null)
            return;

        NetworkManager.Instance.Disconnect();
    }
}