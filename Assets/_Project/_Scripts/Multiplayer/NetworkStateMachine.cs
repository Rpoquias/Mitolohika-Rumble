using System;
using UnityEngine;

public class NetworkStateMachine : MonoBehaviour
{
    public static NetworkStateMachine Instance { get; private set; }

    public enum State
    {
        Disconnected,
        Connecting,
        Connected,
        LoadingScene,
        Synchronizing,
        Ready
    }

    public State CurrentState { get; private set; }

    public event Action<State> OnStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetState(State.Disconnected);
    }

    public void SetState(State state)
    {
        if (CurrentState == state)
            return;

        CurrentState = state;

        Debug.Log($"[NETWORK STATE] {state}");

        OnStateChanged?.Invoke(state);
    }
}