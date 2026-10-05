using TMPro;
using UnityEngine;

public class RoundUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoundStateManager roundStateManager;
    [SerializeField] private GameObject roundPanel;
    [SerializeField] private TMP_Text roundText;

    private bool initialized;

    private void Update()
    {
        if (initialized)
            return;

        TryInitialize();
    }

    private void TryInitialize()
    {
        if (roundStateManager == null)
            return;

        if (!roundStateManager.IsSpawned)
            return;

        initialized = true;

        roundStateManager.OnStateChanged +=
            HandleStateChanged;

        roundStateManager.OnCountdownTick +=
            HandleCountdownTick;

        // Apply the current state immediately.
        HandleStateChanged(
            roundStateManager.CurrentState
        );

        Debug.Log(
            "[ROUND UI] Initialized."
        );
    }

    private void HandleStateChanged(
        RoundStateManager.RoundState state)
    {
        switch (state)
        {
            case RoundStateManager.RoundState.Waiting:
                ShowWaiting();
                break;

            case RoundStateManager.RoundState.Countdown:
                ShowCountdown();

                if (roundText != null)
                {
                    roundText.text =
                        roundStateManager.CurrentCountdown
                        .ToString();
                }

                break;

            case RoundStateManager.RoundState.Playing:
                Hide();
                break;

            case RoundStateManager.RoundState.RoundEnd:
                Hide();
                break;
        }
    }

    private void HandleCountdownTick(
        int countdown)
    {
        if (roundText == null)
            return;

        ShowPanel();

        roundText.text =
            countdown.ToString();
    }

    private void ShowWaiting()
    {
        ShowPanel();

        if (roundText == null)
            return;

        roundText.text =
            "WAITING FOR PLAYERS...";
    }

    private void ShowCountdown()
    {
        ShowPanel();
    }

    private void ShowPanel()
    {
        if (roundPanel != null)
            roundPanel.SetActive(true);
    }

    private void Hide()
    {
        if (roundPanel != null)
            roundPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (!initialized || roundStateManager == null)
            return;

        roundStateManager.OnStateChanged -=
            HandleStateChanged;

        roundStateManager.OnCountdownTick -=
            HandleCountdownTick;
    }
}

