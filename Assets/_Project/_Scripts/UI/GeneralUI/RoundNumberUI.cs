using TMPro;
using UnityEngine;

public class RoundNumberUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject roundNumberPanel;
    [SerializeField] private TMP_Text roundNumberText;

    private MatchSession matchSession;
    private bool initialized;

    private void Update()
    {
        if (initialized)
            return;

        TryInitialize();
    }

    private void TryInitialize()
    {
        if (matchSession == null)
        {
            matchSession = MatchSession.Instance;
        }

        if (matchSession == null)
            return;

        initialized = true;

        matchSession.OnRoundNumberChanged +=
            UpdateRoundNumber;

        // Display the current value immediately.
        UpdateRoundNumber();
    }

    private void UpdateRoundNumber()
    {
        if (roundNumberText != null)
        {
            roundNumberText.text =
                $"ROUND {matchSession.CurrentRound}";
        }

        Show();
    }

    private void Show()
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
        if (!initialized ||
            matchSession == null)
        {
            return;
        }

        matchSession.OnRoundNumberChanged -=
            UpdateRoundNumber;
    }
}