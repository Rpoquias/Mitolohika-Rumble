using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpectatorUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpectatorController spectatorController;
   [Header("UI Visibility")]
[SerializeField] private GameObject gameplayUI;
[SerializeField] private GameObject eliminatedPanel;
    [SerializeField] private TMP_Text spectatedPlayerName;
    [SerializeField] private Button nextPlayerButton;
    [SerializeField] private Button previousPlayerButton;

    [Header("Display")]
    [SerializeField] private string eliminatedMessage = "ELIMINATED";
    [SerializeField] private string noPlayersMessage = "NO PLAYERS LEFT";

    private NetworkRunner runner;
private void OnEnable()
{
    InitializeSpectatorController();
}

private void InitializeSpectatorController()
{
    if (spectatorController == null)
    {
        spectatorController =
            FindAnyObjectByType<SpectatorController>();
    }

    if (spectatorController == null)
        return;

    spectatorController.OnSpectatingChanged -=
        HandleSpectatingChanged;

    spectatorController.OnSpectatingChanged +=
        HandleSpectatingChanged;
}
private void OnDisable()
{
    if (spectatorController != null)
    {
        spectatorController.OnSpectatingChanged -=
            HandleSpectatingChanged;
    }
}

   private void Start()
{
    InitializeSpectatorController();

    if (spectatorController == null)
    {
        Debug.LogError(
            "[SPECTATOR UI] SpectatorController not found."
        );

        enabled = false;
        return;
    }

    if (nextPlayerButton != null)
        nextPlayerButton.onClick.AddListener(
            spectatorController.NextPlayer
        );

    if (previousPlayerButton != null)
        previousPlayerButton.onClick.AddListener(
            spectatorController.PreviousPlayer
        );

    if (spectatedPlayerName != null)
        spectatedPlayerName.text = eliminatedMessage;

    TryInitializeRunner();
    HandleSpectatingChanged(
        spectatorController.IsSpectating
    );
}
    private void HandleSpectatingChanged(bool isSpectating)
{
    if (gameplayUI != null)
        gameplayUI.SetActive(!isSpectating);

    if (eliminatedPanel != null)
        eliminatedPanel.SetActive(isSpectating);

    if (!isSpectating && spectatedPlayerName != null)
        spectatedPlayerName.text = eliminatedMessage;

    if (isSpectating)
        UpdateSpectatedPlayerName();
}
    private void TryInitializeRunner()
{
    if (runner != null && runner.IsRunning)
        return;

    ThirdPersonCamera cameraController =
        FindAnyObjectByType<ThirdPersonCamera>();

    if (cameraController != null)
        runner = cameraController.Runner;
}   
private void Update()
{
    if (spectatorController != null &&
        spectatorController.IsSpectating)
    {
        UpdateSpectatedPlayerName();
    }
}
private void UpdateSpectatedPlayerName()
{
    if (spectatedPlayerName == null)
        return;

    TryInitializeRunner();

    if (!spectatorController.HasTargetPlayer)
    {
        spectatedPlayerName.text =
            $"{eliminatedMessage}\n{noPlayersMessage}";
        return;
    }

    if (runner == null || !runner.IsRunning)
    {
        spectatedPlayerName.text =
            $"{eliminatedMessage}\n{noPlayersMessage}";
        return;
    }

    PlayerRef targetPlayer =
        spectatorController.CurrentTargetPlayer;

    if (!runner.TryGetPlayerObject(
            targetPlayer,
            out NetworkObject playerObject))
    {
        spectatedPlayerName.text =
            $"{eliminatedMessage}\n{noPlayersMessage}";
        return;
    }

    int playerNumber = targetPlayer.RawEncoded + 1;

    spectatedPlayerName.text =
        $"{eliminatedMessage}\nSpectating: Player {playerNumber}";
}

    private void OnDestroy()
    {
        if (nextPlayerButton != null && spectatorController != null)
            nextPlayerButton.onClick.RemoveListener(spectatorController.NextPlayer);

        if (previousPlayerButton != null && spectatorController != null)
            previousPlayerButton.onClick.RemoveListener(spectatorController.PreviousPlayer);
    }
}