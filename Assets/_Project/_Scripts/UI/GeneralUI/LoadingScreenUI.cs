using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private Slider progressBar;

    private void Start()
    {
        loadingPanel.SetActive(false);

        if (NetworkManager.Instance == null)
            return;

        NetworkManager.Instance
            .OnNetworkSceneLoadStartEvent
            += ShowLoading;

        NetworkManager.Instance
            .OnNetworkSceneLoadDoneEvent
            += HideLoading;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance == null)
            return;

        NetworkManager.Instance
            .OnNetworkSceneLoadStartEvent
            -= ShowLoading;

        NetworkManager.Instance
            .OnNetworkSceneLoadDoneEvent
            -= HideLoading;
    }

    private void ShowLoading()
    {
        loadingPanel.SetActive(true);

        loadingText.text = "Loading...";

        progressBar.value = 0f;
    }

    private void HideLoading(Scene scene)
    {
        progressBar.value = 1f;

        loadingPanel.SetActive(false);
    }
}