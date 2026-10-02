using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReadyBackToLobbyUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;
    [SerializeField] private TMP_Text readyCountText;

    private void Start()
    {
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(
                HandleReadyPressed
            );
        }
    }

    private void Update()
    {
        MatchSession matchSession =
            MatchSession.Instance;

        if (matchSession == null)
            return;

        UpdateReadyCount(matchSession);
        UpdateButton(matchSession);
    }

 private void UpdateReadyCount(
    MatchSession matchSession)
{
    if (readyCountText == null)
        return;

    int readyCount =
        matchSession.GetCelebrationReadyCount();

    int playerCount = 0;

    foreach (PlayerRef player in
             matchSession.Runner.ActivePlayers)
    {
        playerCount++;
    }

    readyCountText.text =
        $"{readyCount}/{playerCount} Players Ready";
}
    private void UpdateButton(
        MatchSession matchSession)
    {
        if (readyButton == null)
            return;

        bool isReady =
            matchSession.IsCelebrationPlayerReady(
                matchSession.Runner.LocalPlayer
            );

        readyButton.interactable =
            !isReady;

        if (readyButtonText != null)
        {
            readyButtonText.text =
                isReady
                    ? "READY!"
                    : "RETURN TO LOBBY";
        }
    }

    private void HandleReadyPressed()
    {
        MatchSession matchSession =
            MatchSession.Instance;

        if (matchSession == null)
        {
            Debug.LogError(
                "[READY BACK TO LOBBY] MatchSession is missing."
            );

            return;
        }

        matchSession.RequestCelebrationReady(
            true
        );
    }

    private void OnDestroy()
    {
        if (readyButton != null)
        {
            readyButton.onClick.RemoveListener(
                HandleReadyPressed
            );
        }
    }
}