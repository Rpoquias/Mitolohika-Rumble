using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterPlayerIconUI : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private TMP_Text playerNumberText;

    [Header("Character")]
    [SerializeField] private Image characterIcon;

    [Header("Ready")]
    [SerializeField] private GameObject readyIndicator;

    private PlayerRef player;
    private CharacterData[] characters;

    private NetworkPlayerState playerState;

    public void Setup(
        PlayerRef playerRef,
        int displayNumber,
        CharacterData[] characterData)
    {
        player = playerRef;
        characters = characterData;

        if (playerNumberText != null)
        {
            playerNumberText.text =
                $"P{displayNumber}";
        }

        TryBindToPlayerState();
    }

 private void TryBindToPlayerState()
{
    if (NetworkManager.Instance == null)
        return;

    NetworkRunner runner =
        NetworkManager.Instance.Runner;

    if (runner == null || !runner.IsRunning)
        return;

    if (!runner.TryGetPlayerObject(
            player,
            out NetworkObject playerObject))
    {
        return;
    }

    playerState =
        playerObject.GetComponent<NetworkPlayerState>();

    if (playerState == null)
        return;

    playerState.OnUIStateChanged +=
        HandlePlayerStateChanged;

    Refresh();
}

private void OnDestroy()
{
    if (playerState != null)
    {
        playerState.OnUIStateChanged -=
            HandlePlayerStateChanged;
    }
}
    private void HandlePlayerStateChanged(
    NetworkPlayerState state)
{
    Refresh();
}

    public void Refresh()
    {
        if (playerState == null)
            return;

        if (readyIndicator != null)
        {
            readyIndicator.SetActive(
                playerState.IsReady
            );
        }

        CharacterData selectedCharacter =
            FindCharacter(
                playerState.SelectedCharacter
            );

        if (selectedCharacter != null)
        {
            if (characterIcon != null)
            {
                characterIcon.enabled = true;
                characterIcon.sprite =
                    selectedCharacter.characterIcon;
            }
        }
        else
        {
            if (characterIcon != null)
                characterIcon.enabled = false;
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