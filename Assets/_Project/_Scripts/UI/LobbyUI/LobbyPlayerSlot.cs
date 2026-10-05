using System.Linq;
using TMPro;
using Fusion;
using UnityEngine;

public class LobbyPlayerSlot : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private TMP_Text playerNameText;

    [Header("Character")]
    [SerializeField] private TMP_Text characterText;

    [Header("Ready")]
    [SerializeField] private TMP_Text readyText;

    private PlayerRef player;
    private NetworkPlayerState playerState;
    private CharacterData[] characters;

    public void Setup(
        PlayerRef playerRef,
        NetworkPlayerState state,
        CharacterData[] characterData)
    {
        player = playerRef;
        playerState = state;
        characters = characterData;

        if (playerNameText != null)
        {
            playerNameText.text =
                $"Player {player.PlayerId + 1}";
        }

        Refresh();
    }

    public void Refresh()
    {
        if (playerState == null ||
            !playerState.IsValid)
        {
            if (characterText != null)
                characterText.text = "Loading...";

            if (readyText != null)
                readyText.text = "Not Ready";

            return;
        }

        CharacterData selectedCharacter =
            FindCharacter(
                playerState.SelectedCharacter
            );

        if (characterText != null)
        {
            characterText.text =
                selectedCharacter != null
                    ? selectedCharacter.characterName
                    : "None";
        }

        if (readyText != null)
        {
            readyText.text =
                playerState.IsReady
                    ? "Ready"
                    : "Not Ready";
        }
    }

    private CharacterData FindCharacter(
        CharacterID characterID)
    {
        if (characters == null)
            return null;

        return characters.FirstOrDefault(
            character =>
                character != null &&
                character.characterID == characterID
        );
    }
}