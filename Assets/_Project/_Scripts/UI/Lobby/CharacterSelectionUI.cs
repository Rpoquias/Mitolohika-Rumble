using Fusion;
using UnityEngine;

public class CharacterSelectionUI : MonoBehaviour
{
    private NetworkPlayerState localPlayer;

    private void Start()
    {
        FindLocalPlayer();
    }

    private void Update()
    {
        if (localPlayer != null)
            return;

        FindLocalPlayer();
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
                $"[CHARACTER UI] NetworkPlayerState missing " +
                $"from PlayerObject: {playerObject.name}"
            );

            return;
        }

        Debug.Log(
            $"[CHARACTER UI] Local player found: " +
            $"{localPlayer.name}"
        );
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

        Debug.Log(
            $"[CHARACTER UI] Selected: " +
            $"{character.characterName}"
        );
    }

}