using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private Slider progressBar;

    private void Start()
    {
        loadingPanel.SetActive(false);
 LoadingManager.Instance.OnProgressChanged += UpdateProgress;
    LoadingManager.Instance.OnStateChanged += UpdateState;

    UpdateProgress(LoadingManager.Instance.Progress);
    UpdateState(LoadingManager.Instance.State);
    }

    private void OnDestroy()
    {
        if (LoadingManager.Instance == null)
            return;

        LoadingManager.Instance.OnProgressChanged -= UpdateProgress;
        LoadingManager.Instance.OnStateChanged -= UpdateState;
    }

    private void UpdateProgress(float progress)
    {
        progressBar.value = progress;
    }

    private void UpdateState(LoadingManager.LoadState state)
    {
        bool isLoading =
            state == LoadingManager.LoadState.LoadingScene;

        loadingPanel.SetActive(isLoading);

        if (isLoading)
        {
            loadingText.text = "Loading...";
            progressBar.value = 0f;
        }
    }
}