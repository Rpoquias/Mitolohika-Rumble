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
        playerNumberText.text = $"P{displayNumber}";

    SubscribeToIdentity();

    if (NetworkManager.Instance != null)
    {
        NetworkManager.Instance.OnPlayerObjectAvailable -=
            HandlePlayerObjectAvailable;

        NetworkManager.Instance.OnPlayerObjectAvailable +=
            HandlePlayerObjectAvailable;
    }

    RefreshIdentity();
    TryBindToPlayerState();
}
private void OnEnable()
{
    SubscribeToIdentity();
}
private void HandlePlayerObjectAvailable(PlayerRef spawnedPlayer)
{
    if (spawnedPlayer != player)
        return;

    TryBindToPlayerState();
}
private void SubscribeToIdentity()
{
    if (MatchSession.Instance != null)
    {
        MatchSession.Instance.OnPlayerIdentityChanged -=
            HandleIdentityChanged;

        MatchSession.Instance.OnPlayerIdentityChanged +=
            HandleIdentityChanged;

        RefreshIdentity();
    }
    else if (NetworkManager.Instance != null)
    {
        NetworkManager.Instance.OnMatchSessionRegistered -=
            HandleSessionRegistered;

        NetworkManager.Instance.OnMatchSessionRegistered +=
            HandleSessionRegistered;
    }
}

private void HandleSessionRegistered(MatchSession session)
{
    if (NetworkManager.Instance != null)
    {
        NetworkManager.Instance.OnMatchSessionRegistered -=
            HandleSessionRegistered;
    }

    SubscribeToIdentity();
}

private void HandleIdentityChanged()
{
    RefreshIdentity();
}

private void RefreshIdentity()
{
    if (playerNumberText != null)
    {
        playerNumberText.color =
            MatchSession.ColorForPlayer(player);
    }
}

private void UnsubscribeFromIdentity()
{
    if (MatchSession.Instance != null)
    {
        MatchSession.Instance.OnPlayerIdentityChanged -=
            HandleIdentityChanged;
    }

    if (NetworkManager.Instance != null)
    {
        NetworkManager.Instance.OnMatchSessionRegistered -=
            HandleSessionRegistered;
    }
    if (NetworkManager.Instance != null)
{
    NetworkManager.Instance.OnPlayerObjectAvailable -=
        HandlePlayerObjectAvailable;
}
}

private void TryBindToPlayerState()
{
    if (NetworkManager.Instance == null)
        return;

    NetworkRunner runner = NetworkManager.Instance.Runner;

    if (runner == null || !runner.IsRunning)
        return;

    if (!runner.TryGetPlayerObject(player, out NetworkObject playerObject))
        return;

    NetworkPlayerState newState =
        playerObject.GetComponent<NetworkPlayerState>();

    if (newState == null)
        return;

    // Nothing changed; avoid duplicate event subscriptions.
    if (playerState == newState)
    {
        Refresh();
        return;
    }

    // The character may have been despawned and replaced.
    if (playerState != null)
        playerState.OnUIStateChanged -= HandlePlayerStateChanged;

    playerState = newState;
    playerState.OnUIStateChanged += HandlePlayerStateChanged;

    // Refresh immediately, including the initial replicated state.
    Refresh();
}
private void OnDestroy()
{
    UnsubscribeFromIdentity();

    if (playerState != null)
        playerState.OnUIStateChanged -= HandlePlayerStateChanged;
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
        readyIndicator.SetActive(playerState.IsReady);

    CharacterData selectedCharacter =
        FindCharacter(playerState.SelectedCharacter);

    if (characterIcon == null)
        return;

    Sprite icon = selectedCharacter != null
        ? selectedCharacter.characterIcon
        : null;

    characterIcon.sprite = icon;
    characterIcon.enabled = icon != null;
}

private CharacterData FindCharacter(CharacterID characterID)
{
    if (characters == null)
        return null;

    for (int i = 0; i < characters.Length; i++)
    {
        CharacterData character = characters[i];

        if (character != null &&
            character.characterID == characterID)
        {
            return character;
        }
    }

    return null;
}

}