using TMPro;
using UnityEngine;

public class RoundTimerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject timerPanel;
    [SerializeField] private TMP_Text timerText;

    private MatchSession matchSession;

    private void Update()
    {
        if (matchSession == null)
        {
            matchSession =
                MatchSession.Instance;
        }

        if (matchSession == null ||
            matchSession.Object == null ||
            !matchSession.Object.IsValid)
        {
            HideTimer();
            return;
        }

        UpdateTimer();
    }

    private void UpdateTimer()
    {
        if (!matchSession.IsRoundTimerRunning)
        {
            HideTimer();
            return;
        }

        if (timerPanel != null)
        {
            timerPanel.SetActive(true);
        }

        float remaining =
            matchSession.RemainingRoundTime;

        int totalSeconds =
            Mathf.CeilToInt(remaining);

        int minutes =
            totalSeconds / 60;

        int seconds =
            totalSeconds % 60;

        if (timerText != null)
        {
            timerText.text =
                $"{minutes}:{seconds:00}";
        }
    }

    private void HideTimer()
    {
        if (timerPanel != null)
        {
            timerPanel.SetActive(false);
        }
    }
}