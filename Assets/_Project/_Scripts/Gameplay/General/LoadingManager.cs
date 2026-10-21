
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance { get; private set; }

    public enum LoadState
    {
        Idle,
        LoadingScene,
        Ready
    }

    [Header("Loading")]
    [SerializeField, Min(0f)]
    private float minimumDisplayDuration = 0f;

    public LoadState State { get; private set; } = LoadState.Idle;
    public float Progress { get; private set; }

    public event Action<LoadState> OnStateChanged;
    public event Action<float> OnProgressChanged;

    private Coroutine loadingCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Enable this only if this manager should survive
        // scene transitions and is not already persistent.
        // DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(string sceneName)
    {
        LoadScene(sceneName, null);
    }

    public void LoadScene(string sceneName, Sprite loadingBackground)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[LOADING] Scene name is empty.");
            return;
        }

        // Do not abandon an AsyncOperation waiting for activation.
        if (loadingCoroutine != null ||
            State == LoadState.LoadingScene)
        {
            Debug.LogWarning(
                $"[LOADING] Ignored duplicate request for '{sceneName}'.");
            return;
        }

        LoadingScreenUI.Instance?.SetLoadingBackground(
            loadingBackground);

        loadingCoroutine = StartCoroutine(
            LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        SetProgress(0f);
        SetState(LoadState.LoadingScene);

        AsyncOperation operation = null;

        try
        {
            operation = SceneManager.LoadSceneAsync(
                sceneName, LoadSceneMode.Single);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[LOADING] Could not start loading '{sceneName}': " +
                exception.Message);
        }

        if (operation == null)
        {
            Debug.LogError(
                $"[LOADING] Failed to load scene '{sceneName}'.");
            loadingCoroutine = null;
            SetState(LoadState.Idle);
            yield break;
        }

        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            SetProgress(operation.progress / 0.9f);
            yield return null;
        }

        SetProgress(1f);

        if (minimumDisplayDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                minimumDisplayDuration);
        }

        operation.allowSceneActivation = true;

        yield return operation;

        loadingCoroutine = null;
        SetState(LoadState.Ready);
    }

    private void SetState(LoadState state)
    {
        if (State == state)
            return;

        State = state;
        OnStateChanged?.Invoke(state);
    }

    private void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (Mathf.Approximately(Progress, progress))
            return;

        Progress = progress;
        OnProgressChanged?.Invoke(Progress);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
