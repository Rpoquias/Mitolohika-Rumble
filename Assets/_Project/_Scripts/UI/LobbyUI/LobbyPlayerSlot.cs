
using System.Linq;
using Fusion;
using TMPro;
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
        UnsubscribeFromIdentity();

        player = playerRef;
        playerState = state;
        characters = characterData;

        SubscribeToIdentity();
        Refresh();
    }

    private void SubscribeToIdentity()
    {
        if (MatchSession.Instance != null)
        {
            MatchSession.Instance.OnPlayerIdentityChanged -= Refresh;
            MatchSession.Instance.OnPlayerIdentityChanged += Refresh;
            return;
        }

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnMatchSessionRegistered -=
                HandleMatchSessionRegistered;

            NetworkManager.Instance.OnMatchSessionRegistered +=
                HandleMatchSessionRegistered;
        }
    }

    private void HandleMatchSessionRegistered(MatchSession session)
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnMatchSessionRegistered -=
                HandleMatchSessionRegistered;
        }

        SubscribeToIdentity();
        Refresh();
    }

    private void UnsubscribeFromIdentity()
    {
        if (MatchSession.Instance != null)
        {
            MatchSession.Instance.OnPlayerIdentityChanged -= Refresh;
        }

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnMatchSessionRegistered -=
                HandleMatchSessionRegistered;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromIdentity();
    }

    public void Refresh()
    {
        // Identity is independent of the character object's load state.
        if (playerNameText != null)
        {
            playerNameText.text = MatchSession.NameOf(player);
            playerNameText.color = MatchSession.ColorForPlayer(player);
        }

        if (playerState == null || !playerState.IsValid)
        {
            if (characterText != null)
                characterText.text = "Loading...";

            if (readyText != null)
                readyText.text = "Not Ready";

            return;
        }

        CharacterData selectedCharacter =
            FindCharacter(playerState.SelectedCharacter);

        if (characterText != null)
        {
            characterText.text = selectedCharacter != null
                ? selectedCharacter.characterName
                : "None";
        }

        if (readyText != null)
        {
            readyText.text = playerState.IsReady
                ? "Ready"
                : "Not Ready";
        }
    }

    private CharacterData FindCharacter(CharacterID characterID)
    {
        if (characters == null)
            return null;

        return characters.FirstOrDefault(
            character => character != null &&
                         character.characterID == characterID);
    }
}
