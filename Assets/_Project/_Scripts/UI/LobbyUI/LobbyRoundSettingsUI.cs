using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyRoundSettingsUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button threeRoundsButton;
    [SerializeField] private Button fiveRoundsButton;
    [SerializeField] private Button sevenRoundsButton;

    [Header("Display")]
    [SerializeField] private TMP_Text roundCountText;

    private MatchSession matchSession;

    private void OnEnable()
    {
        TrySubscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void TrySubscribe()
    {
        MatchSession currentSession = MatchSession.Instance;

        if (currentSession == null ||
            matchSession == currentSession)
        {
            return;
        }

        Unsubscribe();

        matchSession = currentSession;
        matchSession.OnLobbySettingsChanged += Refresh;
    }

    public void Open()
    {
        TrySubscribe();
        Refresh();
    }

    private void Unsubscribe()
    {
        if (matchSession == null)
            return;

        matchSession.OnLobbySettingsChanged -= Refresh;
        matchSession = null;
    }

    public void Refresh()
    {
        UpdateRoundCountDisplay();
        UpdateButtons();
    }

    private void UpdateRoundCountDisplay()
    {
        if (roundCountText == null)
            return;

        MatchSession session = MatchSession.Instance;

        int totalRounds = session != null
            ? session.TotalRounds
            : 3;

        roundCountText.text = $"{totalRounds} ROUNDS";
    }

    private void UpdateButtons()
    {
        bool isHost =
            LobbyManager.Instance != null &&
            LobbyManager.Instance.IsHost;

        SetButtonInteractable(threeRoundsButton, isHost);
        SetButtonInteractable(fiveRoundsButton, isHost);
        SetButtonInteractable(sevenRoundsButton, isHost);
    }

    private void SetButtonInteractable(
        Button button,
        bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }

    public void SelectThreeRounds()
    {
        SetRounds(3);
    }

    public void SelectFiveRounds()
    {
        SetRounds(5);
    }

    public void SelectSevenRounds()
    {
        SetRounds(7);
    }

    private void SetRounds(int roundCount)
    {
        if (LobbyManager.Instance == null)
        {
            Debug.LogError(
                "[LOBBY UI] LobbyManager is missing."
            );

            return;
        }

        LobbyManager.Instance.SetRoundCount(roundCount);
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
}