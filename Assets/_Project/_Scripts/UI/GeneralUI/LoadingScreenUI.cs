
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreenUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private Slider progressBar;


[Header("Loading Background")]
[SerializeField] private Image backgroundImage;
[SerializeField] private Sprite generalBackground;
    [Header("Fusion Progress Animation")]
    [SerializeField, Min(0.01f)]
    private float estimatedFillSpeed = 0.15f;

    [SerializeField, Min(0f)]
    private float completionDisplayDuration = 0.12f;
public static LoadingScreenUI Instance { get; private set; }
    private Coroutine progressCoroutine;
    private Coroutine hideCoroutine;

    private bool isFusionLoading;
    private bool isUnityLoading;

    private void Start()
    {
        if (Instance != null && Instance != this)
{
    Destroy(gameObject);
    return;
}

Instance = this;
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        ConfigureSlider();
        SetLoadingBackground();
        SubscribeToEvents();
    }

    private void ConfigureSlider()
    {
        if (progressBar == null)
            return;

        progressBar.minValue = 0f;
        progressBar.maxValue = 1f;
        progressBar.wholeNumbers = false;
        progressBar.value = 0f;
    }

    private void SubscribeToEvents()
    {
        NetworkManager networkManager =
            NetworkManager.Instance;

        if (networkManager != null)
        {
            networkManager.OnNetworkSceneLoadStartEvent +=
                ShowFusionLoading;

            networkManager.OnNetworkSceneLoadDoneEvent +=
                OnFusionLoadDone;
        }

        LoadingManager loadingManager =
            LoadingManager.Instance;

        if (loadingManager != null)
        {
            loadingManager.OnStateChanged +=
                OnUnityLoadStateChanged;

            loadingManager.OnProgressChanged +=
                OnUnityProgressChanged;

            // Handle a load that was already underway
            // before this UI subscribed.
            OnUnityLoadStateChanged(
                loadingManager.State
            );
        }
    }
    
public void SetLoadingBackground(Sprite background = null)
{
    if (backgroundImage == null)
        return;

    Sprite selectedBackground =
        background != null
            ? background
            : generalBackground;

    backgroundImage.sprite = selectedBackground;
    backgroundImage.enabled = selectedBackground != null;
}
    private void ShowFusionLoading()
    {
        isFusionLoading = true;
        isUnityLoading = false;

        ShowLoadingPanel();

        if (progressBar != null)
            progressBar.value = 0f;

        loadingText.text = "Loading...";

        StopProgressAnimation();

        progressCoroutine =
            StartCoroutine(
                AnimateFusionProgress()
            );
    }

    private IEnumerator AnimateFusionProgress()
    {
        const float estimatedMaximum = 0.9f;

        while (isFusionLoading &&
               progressBar != null &&
               progressBar.value < estimatedMaximum)
        {
            progressBar.value = Mathf.MoveTowards(
                progressBar.value,
                estimatedMaximum,
                estimatedFillSpeed * Time.unscaledDeltaTime
            );

            yield return null;
        }

        progressCoroutine = null;
    }

    private void OnFusionLoadDone(Scene scene)
    {
        if (!isFusionLoading)
            return;

        isFusionLoading = false;

        CompleteLoading();
    }

    private void OnUnityLoadStateChanged(
        LoadingManager.LoadState state)
    {
        switch (state)
        {
            case LoadingManager.LoadState.LoadingScene:

                isUnityLoading = true;
                isFusionLoading = false;

                StopProgressAnimation();

                ShowLoadingPanel();

                if (loadingText != null)
                    loadingText.text = "Loading...";

                if (progressBar != null &&
                    LoadingManager.Instance != null)
                {
                    progressBar.value =
                        LoadingManager.Instance.Progress;
                }

                break;

            case LoadingManager.LoadState.Ready:

                isUnityLoading = false;

                CompleteLoading();

                break;

            case LoadingManager.LoadState.Idle:

                isUnityLoading = false;

                if (!isFusionLoading)
                    HideImmediately();

                break;
        }
    }

    private void OnUnityProgressChanged(float progress)
    {
        if (!isUnityLoading || progressBar == null)
            return;

        progressBar.value = Mathf.Clamp01(progress);
    }

    private void ShowLoadingPanel()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (loadingPanel != null)
            loadingPanel.SetActive(true);
    }

    private void CompleteLoading()
    {
        StopProgressAnimation();

        if (progressBar != null)
            progressBar.value = 1f;

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine =
            StartCoroutine(
                HideAfterCompletion()
            );
    }

    private IEnumerator HideAfterCompletion()
    {
        yield return new WaitForSecondsRealtime(
            completionDisplayDuration
        );

        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        hideCoroutine = null;
    }

    private void StopProgressAnimation()
    {
        if (progressCoroutine == null)
            return;

        StopCoroutine(progressCoroutine);
        progressCoroutine = null;
    }

    public void HideImmediately()
    {
        isFusionLoading = false;
        isUnityLoading = false;

        StopProgressAnimation();

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
    Instance = null;
        NetworkManager networkManager =
            NetworkManager.Instance;

        if (networkManager != null)
        {
            networkManager.OnNetworkSceneLoadStartEvent -=
                ShowFusionLoading;

            networkManager.OnNetworkSceneLoadDoneEvent -=
                OnFusionLoadDone;
        }

        LoadingManager loadingManager =
            LoadingManager.Instance;

        if (loadingManager != null)
        {
            loadingManager.OnStateChanged -=
                OnUnityLoadStateChanged;

            loadingManager.OnProgressChanged -=
                OnUnityProgressChanged;
        }
    }
}
