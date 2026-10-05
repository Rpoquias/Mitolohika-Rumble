using Fusion;
using UnityEngine;

public class CharacterSelectionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LobbyUIController uiController;
    [SerializeField] private CharacterDetailsUI detailsUI;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;
[SerializeField] private PlayerControlController playerControlController;
    private NetworkPlayerState localPlayer;

    private void Start()
    {
        FindLocalPlayer();

        OpenCharacterSelection();
    }

    private void FindLocalPlayer()
    {
        if (NetworkManager.Instance == null)
            return;

        NetworkRunner runner =
            NetworkManager.Instance.Runner;

        if (runner == null || !runner.IsRunning)
            return;

        if (!runner.TryGetPlayerObject(
                runner.LocalPlayer,
                out NetworkObject playerObject))
        {
            return;
        }

        localPlayer =
            playerObject.GetComponent<NetworkPlayerState>();

        if (localPlayer == null)
        {
            Debug.LogError(
                "[CHARACTER UI] NetworkPlayerState missing."
            );
        }
    }

public void OpenCharacterSelection()
{
    if (uiController != null)
        uiController.ShowCharacterSelection();

    if (thirdPersonCamera != null)
        thirdPersonCamera.EnterCharacterSelection();

    if (playerControlController != null)
        playerControlController.SetLocalPlayerControlLock(true);
}

public void CloseCharacterSelection()
{
    if (uiController != null)
        uiController.ShowLobby();

    if (thirdPersonCamera != null)
        thirdPersonCamera.ExitCharacterSelection();

    if (playerControlController != null)
        playerControlController.SetLocalPlayerControlLock(false);
}

    public void SelectCharacter(CharacterData character)
    {
        if (character == null)
            return;

        if (localPlayer == null)
            FindLocalPlayer();

        if (localPlayer == null)
            return;

        localPlayer.RequestCharacterChange(
            character.characterID
        );

        if (detailsUI != null)
            detailsUI.ShowCharacter(character);

        Debug.Log(
            $"[CHARACTER UI] Selected: {character.characterName}"
        );
    }
}