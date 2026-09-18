using TMPro;
using UnityEngine;

public class RoundUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoundStateManager roundStateManager;
    [SerializeField] private TMP_Text roundText;

    private void Update()
    {
        if (roundStateManager == null || !roundStateManager.IsSpawned)
            return;

        UpdateRoundDisplay();
    }

    private void UpdateRoundDisplay()
    {
        switch (roundStateManager.CurrentState)
        {
            case RoundStateManager.RoundState.Waiting:
                ShowWaiting();
                break;

            case RoundStateManager.RoundState.Countdown:
                ShowCountdown();
                break;

            default:
                Hide();
                break;
        }
    }

    private void ShowWaiting()
    {
        if (roundText == null)
            return;

        roundText.gameObject.SetActive(true);
        roundText.text = "WAITING FOR PLAYERS...";
    }

    private void ShowCountdown()
    {
        if (roundText == null)
            return;

        roundText.gameObject.SetActive(true);

        roundText.text =
            roundStateManager.CurrentCountdown.ToString();
    }

    private void Hide()
    {
        if (roundText == null)
            return;

        roundText.gameObject.SetActive(false);
    }
}