using TMPro;
using UnityEngine;

public class RoundNumberUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject roundNumberPanel;
    [SerializeField] private TMP_Text roundNumberText;

    private MatchSession matchSession;
    private bool subscribed;

    private void Update()
    {
        // Retry initialization if MatchSession has not spawned yet.
        if (!subscribed)
            TryInitialize();
    }

    private void TryInitialize()
    {
        matchSession = MatchSession.Instance;

        if (matchSession == null)
            return;

        matchSession.OnRoundNumberChanged += UpdateRoundNumber;
        subscribed = true;

        // Initialize the display immediately.
        UpdateRoundNumber();
    }

    private void UpdateRoundNumber()
    {
        if (matchSession == null)
            return;

        if (roundNumberText != null)
        {
            roundNumberText.text =
                $"ROUND {matchSession.CurrentRound} / " +
                $"{matchSession.TotalRounds}";
        }

        Show();
    }

    public void Show()
    {
        if (roundNumberPanel != null)
            roundNumberPanel.SetActive(true);
    }

    public void Hide()
    {
        if (roundNumberPanel != null)
            roundNumberPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (subscribed && matchSession != null)
        {
            matchSession.OnRoundNumberChanged -= UpdateRoundNumber;
        }
    }
}