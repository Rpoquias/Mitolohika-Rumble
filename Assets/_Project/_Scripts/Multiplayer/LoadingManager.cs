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
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(string sceneName)
    {
        if (loadingCoroutine != null)
            StopCoroutine(loadingCoroutine);

        loadingCoroutine = StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
{
    SetState(LoadState.LoadingScene);
    SetProgress(0f);

    AsyncOperation operation =
        SceneManager.LoadSceneAsync(sceneName);

    if (operation == null)
    {
        Debug.LogError(
            $"[LOADING] Failed to load scene: {sceneName}"
        );

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

    // TEMPORARY: lets us visually confirm the loading screen works.
    yield return new WaitForSeconds(1f);

    operation.allowSceneActivation = true;

    yield return operation;

    SetState(LoadState.Ready);

    loadingCoroutine = null;
}

    private void SetState(LoadState state)
    {
        State = state;
        OnStateChanged?.Invoke(state);
    }

    private void SetProgress(float progress)
    {
        Progress = Mathf.Clamp01(progress);
        OnProgressChanged?.Invoke(Progress);
    }
}