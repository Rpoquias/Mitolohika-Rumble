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
    [SerializeField] private TMP_Text currentRoundText;
    [SerializeField] private TMP_Text gameModeText;

    private MatchSession matchSession;

    private void OnEnable()
    {
        Refresh();

        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void TrySubscribe()
    {
        if (MatchSession.Instance == null)
            return;

        if (matchSession == MatchSession.Instance)
            return;

        Unsubscribe();

        matchSession =
            MatchSession.Instance;

        matchSession.OnLobbySettingsChanged +=
            Refresh;
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

        matchSession.OnLobbySettingsChanged -=
            Refresh;

        matchSession = null;
    }

    public void Refresh()
    {
        UpdateRoundCountDisplay();
        UpdateCurrentRoundDisplay();
        UpdateGameModeDisplay();
        UpdateButtons();
    }

    private void UpdateRoundCountDisplay()
    {
        if (roundCountText == null)
            return;

        if (MatchSession.Instance == null)
        {
            roundCountText.text = "3 ROUNDS";
            return;
        }

        roundCountText.text =
            $"{MatchSession.Instance.TotalRounds} ROUNDS";
    }

    private void UpdateCurrentRoundDisplay()
    {
        if (currentRoundText == null)
            return;

        if (MatchSession.Instance == null)
        {
            currentRoundText.text = "ROUND 1 / 3";
            return;
        }

        currentRoundText.text =
            $"ROUND {MatchSession.Instance.CurrentRound} / " +
            $"{MatchSession.Instance.TotalRounds}";
    }

    private void UpdateGameModeDisplay()
    {
        if (gameModeText == null)
            return;

        if (MatchSession.Instance == null)
        {
            gameModeText.text = "MODE: -";
            return;
        }

        gameModeText.text =
            $"MODE: {MatchSession.Instance.CurrentGameMode}";
    }

    private void UpdateButtons()
    {
        bool isHost =
            LobbyManager.Instance != null &&
            LobbyManager.Instance.IsHost;

        SetButtonInteractable(
            threeRoundsButton,
            isHost
        );

        SetButtonInteractable(
            fiveRoundsButton,
            isHost
        );

        SetButtonInteractable(
            sevenRoundsButton,
            isHost
        );
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

        LobbyManager.Instance.SetRoundCount(
            roundCount
        );
    }
}