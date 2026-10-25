
using Fusion;
using UnityEngine;

public class CharacterSelectionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LobbyUIController uiController;
    [SerializeField] private CharacterDetailsUI detailsUI;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;
    [SerializeField] private PlayerControlController playerControlController;

    [Header("Characters")]
    [SerializeField] private CharacterData defaultCharacter;

    private NetworkPlayerState localPlayer;
    private CharacterData pendingCharacter;
    private bool waitingForPlayerObject;

    private void Start()
    {
        if (uiController == null)
            uiController = FindAnyObjectByType<LobbyUIController>();

        if (detailsUI == null)
            detailsUI = FindAnyObjectByType<CharacterDetailsUI>();

        if (thirdPersonCamera == null)
            thirdPersonCamera = FindAnyObjectByType<ThirdPersonCamera>();

        if (playerControlController == null)
            playerControlController = FindAnyObjectByType<PlayerControlController>();

        if (detailsUI != null && defaultCharacter != null)
            detailsUI.ShowCharacter(defaultCharacter);

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPlayerObjectAvailable -= HandlePlayerObjectAvailable;
            NetworkManager.Instance.OnPlayerObjectAvailable += HandlePlayerObjectAvailable;
        }

        FindLocalPlayer();
        OpenCharacterSelection();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnPlayerObjectAvailable -= HandlePlayerObjectAvailable;
    }

    private void FindLocalPlayer()
    {
        NetworkRunner runner = NetworkManager.Instance != null
            ? NetworkManager.Instance.Runner
            : null;

        if (runner == null || !runner.IsRunning)
        {
            localPlayer = null;
            return;
        }

        if (!runner.TryGetPlayerObject(runner.LocalPlayer, out NetworkObject playerObject))
        {
            localPlayer = null;
            return;
        }

        localPlayer = playerObject.GetComponent<NetworkPlayerState>();
    }

    private void HandlePlayerObjectAvailable(PlayerRef player)
    {
        NetworkRunner runner = NetworkManager.Instance != null
            ? NetworkManager.Instance.Runner
            : null;

        if (runner == null || player != runner.LocalPlayer)
            return;

        FindLocalPlayer();

        if (localPlayer != null && pendingCharacter != null)
        {
            CharacterData characterToSelect = pendingCharacter;
            pendingCharacter = null;
            waitingForPlayerObject = false;

            RequestCharacterChange(characterToSelect);
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

        // Update the preview immediately, without waiting for the network.
        if (detailsUI != null)
            detailsUI.ShowCharacter(character);

        FindLocalPlayer();

        if (localPlayer == null)
        {
            // Retry when the local network player object becomes available.
            pendingCharacter = character;
            waitingForPlayerObject = true;

            Debug.LogWarning(
                "[CHARACTER UI] Local player isn't available yet. Selection queued.");

            return;
        }

        pendingCharacter = null;
        waitingForPlayerObject = false;

        RequestCharacterChange(character);
    }

    private void RequestCharacterChange(CharacterData character)
    {
        if (character == null)
            return;

        // The object may have been despawned between lookup and this call.
        FindLocalPlayer();

        if (localPlayer == null)
        {
            pendingCharacter = character;
            waitingForPlayerObject = true;
            return;
        }

        localPlayer.RequestCharacterChange(character.characterID);

        Debug.Log($"[CHARACTER UI] Requested: {character.characterName}");
    }
}
