using System;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyPlayerSpawner : MonoBehaviour
{
    [Header("Characters")]
    [SerializeField] private CharacterData[] characters;

    private bool initialized;

    private async void Start()
    {
        while (NetworkManager.Instance == null || NetworkManager.Instance.Runner == null || !NetworkManager.Instance.Runner.IsRunning)
        {
            await Awaitable.NextFrameAsync();
        }

        NetworkManager.Instance.OnPlayerJoinedEvent += HandlePlayerJoined;
        NetworkManager.Instance.OnPlayerLeftEvent += HandlePlayerLeft;
        NetworkManager.Instance.OnNetworkSceneLoadDoneEvent += HandleNetworkSceneLoadDone;

        initialized = true;

        Debug.Log("<color=#FFE600><b>[LOBBY PLAYER SPAWNER] OK</b> - Initialized & listening for players</color>");

        TrySpawnExistingPlayers();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance == null) return;

        NetworkManager.Instance.OnPlayerJoinedEvent -= HandlePlayerJoined;
        NetworkManager.Instance.OnPlayerLeftEvent -= HandlePlayerLeft;
        NetworkManager.Instance.OnNetworkSceneLoadDoneEvent -= HandleNetworkSceneLoadDone;
    }

    // --------------------------------------------------
    // Existing Players
    // --------------------------------------------------

    private void HandleNetworkSceneLoadDone(Scene scene)
    {
        if (!initialized) return;

        NetworkRunner runner = NetworkManager.Instance.Runner;
        if (runner == null || !runner.IsRunning || !runner.IsServer) return;

        // Only react when the Lobby scene has loaded.
        if (scene.buildIndex != NetworkManager.Instance.LobbySceneBuildIndex) return;

        TrySpawnExistingPlayers();
    }

    private void TrySpawnExistingPlayers()
    {
        if (!initialized) return;

        NetworkRunner runner = NetworkManager.Instance.Runner;
        if (runner == null || !runner.IsRunning || !runner.IsServer) return;

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (runner.TryGetPlayerObject(player, out NetworkObject existingPlayer)) continue;
            SpawnExistingPlayer(runner, player);
        }
    }

    // --------------------------------------------------
    // Player Joined / Left
    // --------------------------------------------------

    private void HandlePlayerJoined(PlayerRef player)
    {
        NetworkRunner runner = NetworkManager.Instance.Runner;
        if (runner == null || !runner.IsServer) return;
        if (runner.TryGetPlayerObject(player, out NetworkObject existingPlayer)) return;
        if (characters == null || characters.Length == 0) return;

        if (!NetworkManager.Instance.TryGetPlayerSelection(player, out CharacterID characterID))
        {
            characterID = characters[0].characterID;
            NetworkManager.Instance.SetPlayerSelection(player, characterID);
        }

        CharacterData character = GetCharacterData(characterID);
        if (character == null) return;

        SpawnCharacter(runner, player, character, GetSpawnPosition(player));
    }

    private void HandlePlayerLeft(PlayerRef player)
    {
        NetworkRunner runner = NetworkManager.Instance.Runner;
        if (runner == null || !runner.IsServer) return;

        if (runner.TryGetPlayerObject(player, out NetworkObject playerObject)) runner.Despawn(playerObject);
    }

    private void SpawnExistingPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (!NetworkManager.Instance.TryGetPlayerSelection(player, out CharacterID characterID))
        {
            if (characters == null || characters.Length == 0) return;

            characterID = characters[0].characterID;
            NetworkManager.Instance.SetPlayerSelection(player, characterID);
        }

        CharacterData character = GetCharacterData(characterID);
        if (character == null) return;

        SpawnCharacter(runner, player, character, GetSpawnPosition(player));
    }

    // --------------------------------------------------
    // Character Change
    // --------------------------------------------------

    public void ChangeCharacter(PlayerRef player, CharacterID characterID)
    {
        NetworkRunner runner = NetworkManager.Instance.Runner;
        if (runner == null || !runner.IsServer) return;

        CharacterData character = GetCharacterData(characterID);
        if (character == null) return;

        NetworkManager.Instance.SetPlayerSelection(player, characterID);

        Vector3 spawnPosition = GetSpawnPosition(player);

        if (runner.TryGetPlayerObject(player, out NetworkObject oldObject))
        {
            spawnPosition = oldObject.transform.position;
            runner.Despawn(oldObject);
        }

        SpawnCharacter(runner, player, character, spawnPosition);
    }

    // --------------------------------------------------
    // Spawn Character
    // --------------------------------------------------

    private void SpawnCharacter(NetworkRunner runner, PlayerRef player, CharacterData character, Vector3 spawnPosition)
    {
        if (character.gameplayPrefab == null) return;

        NetworkObject playerObject = runner.Spawn(character.gameplayPrefab, spawnPosition, Quaternion.identity, player, (r, spawnedObject) =>
        {
            NetworkPlayerState state = spawnedObject.GetComponent<NetworkPlayerState>();
            if (state != null) state.SelectedCharacter = character.characterID;
        });

        if (playerObject == null) return;

        runner.SetPlayerObject(player, playerObject);
    }

    // --------------------------------------------------
    // Helpers
    // --------------------------------------------------

    private CharacterData GetCharacterData(CharacterID characterID)
    {
        foreach (CharacterData character in characters)
        {
            if (character == null) continue;
            if (character.characterID == characterID) return character;
        }

        return null;
    }

    private Vector3 GetSpawnPosition(PlayerRef player) => new Vector3(player.PlayerId * 2f, 1f, 0f);
}