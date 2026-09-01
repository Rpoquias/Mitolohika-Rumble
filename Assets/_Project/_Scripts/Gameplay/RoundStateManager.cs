using System;
using System.Collections;
using UnityEngine;

public class RoundStateManager : MonoBehaviour
{
    public enum RoundState
    {
        Waiting,
        Countdown,
        Playing,
        RoundEnd
    }

    [Header("Round Settings")]
    [SerializeField] private float waitingDuration = 1f;
    [SerializeField] private float countdownDuration = 3f;

    [Header("Round Restart")]
    [SerializeField] private float restartDelay = 3f;

    public RoundState CurrentState { get; private set; }

    // Events
    public event Action OnRoundStarted;
    public event Action OnRoundEnded;
    public event Action<int> OnCountdownTick;
    public event Action OnRoundReset;

    private Coroutine roundCoroutine;
    private Coroutine restartCoroutine;

    private void Start()
    {
        StartRound();
    }

    public void StartRound()
    {
        if (roundCoroutine != null)
            StopCoroutine(roundCoroutine);

        roundCoroutine = StartCoroutine(RoundStartRoutine());
    }

    private IEnumerator RoundStartRoutine()
    {
        ChangeState(RoundState.Waiting);

        yield return new WaitForSeconds(waitingDuration);

        ChangeState(RoundState.Countdown);

        int countdown = Mathf.CeilToInt(countdownDuration);

        while (countdown > 0)
        {
            OnCountdownTick?.Invoke(countdown);

            yield return new WaitForSeconds(1f);

            countdown--;
        }

        ChangeState(RoundState.Playing);

        OnRoundStarted?.Invoke();

        roundCoroutine = null;
    }

    public void EndRound()
    {
        if (CurrentState != RoundState.Playing)
            return;

        ChangeState(RoundState.RoundEnd);

        OnRoundEnded?.Invoke();
    }

    public void RestartRound()
    {
        if (roundCoroutine != null)
        {
            StopCoroutine(roundCoroutine);
            roundCoroutine = null;
        }

        if (restartCoroutine != null)
            StopCoroutine(restartCoroutine);

        restartCoroutine = StartCoroutine(RestartRoutine());
    }

    private IEnumerator RestartRoutine()
    {
        yield return new WaitForSeconds(restartDelay);

        OnRoundReset?.Invoke();

        restartCoroutine = null;

        StartRound();
    }

    private void ChangeState(RoundState newState)
    {
        CurrentState = newState;

        Debug.Log("Round State: " + CurrentState);
    }
}