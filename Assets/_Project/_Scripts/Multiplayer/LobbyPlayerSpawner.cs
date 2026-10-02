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
    while (
        NetworkManager.Instance == null ||
        NetworkManager.Instance.Runner == null ||
        !NetworkManager.Instance.Runner.IsRunning)
    {
        await Awaitable.NextFrameAsync();
    }

    NetworkManager.Instance.OnPlayerJoinedEvent +=
        HandlePlayerJoined;

    NetworkManager.Instance.OnPlayerLeftEvent +=
        HandlePlayerLeft;

    NetworkManager.Instance.OnNetworkSceneLoadDoneEvent +=
        HandleNetworkSceneLoadDone;

    initialized = true;

    Debug.Log(
        "[LOBBY PLAYER SPAWNER] Initialized."
    );

    // Initial lobby setup.
    TrySpawnExistingPlayers();
}

 private void OnDestroy()
{
    if (NetworkManager.Instance == null)
        return;

    NetworkManager.Instance.OnPlayerJoinedEvent -=
        HandlePlayerJoined;

    NetworkManager.Instance.OnPlayerLeftEvent -=
        HandlePlayerLeft;

    NetworkManager.Instance.OnNetworkSceneLoadDoneEvent -=
        HandleNetworkSceneLoadDone;
}
    // --------------------------------------------------
    // Existing Players
    // --------------------------------------------------

private void HandleNetworkSceneLoadDone(Scene scene)
{
    if (!initialized)
        return;

    NetworkRunner runner =
        NetworkManager.Instance.Runner;

    if (runner == null ||
        !runner.IsRunning)
    {
        return;
    }

    if (!runner.IsServer)
        return;

    // Only react when the Lobby scene has loaded.
    if (scene.buildIndex !=
        NetworkManager.Instance.LobbySceneBuildIndex)
    {
        return;
    }

    Debug.Log(
        "[LOBBY PLAYER SPAWNER] Lobby scene loaded. " +
        "Checking for existing players."
    );

    TrySpawnExistingPlayers();
}
 private void TrySpawnExistingPlayers()
{
    if (!initialized)
        return;

    NetworkRunner runner =
        NetworkManager.Instance.Runner;

    if (runner == null ||
        !runner.IsRunning)
    {
        return;
    }

    if (!runner.IsServer)
        return;

    foreach (PlayerRef player in runner.ActivePlayers)
    {
        if (runner.TryGetPlayerObject(
                player,
                out NetworkObject existingPlayer))
        {
            Debug.Log(
                $"[LOBBY PLAYER SPAWNER] {player} already has " +
                $"PlayerObject: {existingPlayer.name}"
            );

            continue;
        }

        Debug.Log(
            $"[LOBBY PLAYER SPAWNER] Spawning existing " +
            $"player {player}."
        );

        SpawnExistingPlayer(
            runner,
            player
        );
    }
}

    // --------------------------------------------------
    // Player Joined
    // --------------------------------------------------

    private void HandlePlayerJoined(
        PlayerRef player)
    {
        NetworkRunner runner =
            NetworkManager.Instance.Runner;

        if (runner == null ||
            !runner.IsServer)
        {
            return;
        }

        Debug.Log(
            $"[LOBBY PLAYER SPAWNER] Player joined: {player}"
        );

        if (runner.TryGetPlayerObject(
                player,
                out NetworkObject existingPlayer))
        {
            Debug.Log(
                $"[LOBBY PLAYER SPAWNER] {player} already has " +
                $"PlayerObject: {existingPlayer.name}"
            );

            return;
        }

        if (characters == null ||
            characters.Length == 0)
        {
            Debug.LogError(
                "[LOBBY PLAYER SPAWNER] No characters assigned."
            );

            return;
        }

        CharacterID characterID;

        if (!NetworkManager.Instance.TryGetPlayerSelection(
                player,
                out characterID))
        {
            characterID =
                characters[0].characterID;

            NetworkManager.Instance.SetPlayerSelection(
                player,
                characterID
            );
        }

        CharacterData character =
            GetCharacterData(characterID);

        if (character == null)
        {
            Debug.LogError(
                $"[LOBBY PLAYER SPAWNER] No CharacterData for " +
                $"{characterID}."
            );

            return;
        }

        SpawnCharacter(
            runner,
            player,
            character,
            GetSpawnPosition(player)
        );
    }

    // --------------------------------------------------
    // Player Left
    // --------------------------------------------------

    private void HandlePlayerLeft(
        PlayerRef player)
    {
        NetworkRunner runner =
            NetworkManager.Instance.Runner;

        if (runner == null ||
            !runner.IsServer)
        {
            return;
        }

        if (runner.TryGetPlayerObject(
                player,
                out NetworkObject playerObject))
        {
            Debug.Log(
                $"[LOBBY PLAYER SPAWNER] Despawning " +
                $"{playerObject.name} for {player}"
            );

            runner.Despawn(playerObject);
        }
    }

    // --------------------------------------------------
    // Existing Player
    // --------------------------------------------------

    private void SpawnExistingPlayer(
        NetworkRunner runner,
        PlayerRef player)
    {
        CharacterID characterID;

        if (!NetworkManager.Instance.TryGetPlayerSelection(
                player,
                out characterID))
        {
            if (characters == null ||
                characters.Length == 0)
            {
                Debug.LogError(
                    "[LOBBY PLAYER SPAWNER] No characters assigned."
                );

                return;
            }

            characterID =
                characters[0].characterID;

            NetworkManager.Instance.SetPlayerSelection(
                player,
                characterID
            );
        }

        CharacterData character =
            GetCharacterData(characterID);

        if (character == null)
        {
            Debug.LogError(
                $"[LOBBY PLAYER SPAWNER] No CharacterData for " +
                $"{characterID}."
            );

            return;
        }

        SpawnCharacter(
            runner,
            player,
            character,
            GetSpawnPosition(player)
        );
    }

    // --------------------------------------------------
    // Character Change
    // --------------------------------------------------

    public void ChangeCharacter(
        PlayerRef player,
        CharacterID characterID)
    {
        NetworkRunner runner =
            NetworkManager.Instance.Runner;

        if (runner == null ||
            !runner.IsServer)
        {
            return;
        }

        CharacterData character =
            GetCharacterData(characterID);

        if (character == null)
        {
            Debug.LogError(
                $"[LOBBY PLAYER SPAWNER] No CharacterData for " +
                $"{characterID}."
            );

            return;
        }

        NetworkManager.Instance.SetPlayerSelection(
            player,
            characterID
        );

        Vector3 spawnPosition =
            GetSpawnPosition(player);

        if (runner.TryGetPlayerObject(
                player,
                out NetworkObject oldObject))
        {
            spawnPosition =
                oldObject.transform.position;

            runner.Despawn(oldObject);
        }

        SpawnCharacter(
            runner,
            player,
            character,
            spawnPosition
        );
    }

    // --------------------------------------------------
    // Spawn Character
    // --------------------------------------------------

   private void SpawnCharacter(
    NetworkRunner runner,
    PlayerRef player,
    CharacterData character,
    Vector3 spawnPosition)
{
    if (character.gameplayPrefab == null)
    {
        Debug.LogError(
            $"[LOBBY PLAYER SPAWNER] Gameplay prefab missing " +
            $"for {character.characterName}."
        );

        return;
    }

    NetworkObject playerObject =
        runner.Spawn(
            character.gameplayPrefab,
            spawnPosition,
            Quaternion.identity,
            player,
            (runner, spawnedObject) =>
            {
                NetworkPlayerState state =
                    spawnedObject.GetComponent<NetworkPlayerState>();

                if (state != null)
                {
                    state.SelectedCharacter =
                        character.characterID;
                }
                else
                {
                    Debug.LogError(
                        "[LOBBY PLAYER SPAWNER] " +
                        "Spawned player has no NetworkPlayerState."
                    );
                }
            }
        );

    if (playerObject == null)
    {
        Debug.LogError(
            $"[LOBBY PLAYER SPAWNER] Failed to spawn " +
            $"player for {player}."
        );

        return;
    }

    runner.SetPlayerObject(
        player,
        playerObject
    );

    Debug.Log(
        $"[LOBBY PLAYER SPAWNER] Spawned " +
        $"{character.characterName} for {player}."
    );
}

    // --------------------------------------------------
    // Character Data
    // --------------------------------------------------

    private CharacterData GetCharacterData(
        CharacterID characterID)
    {
        foreach (CharacterData character in characters)
        {
            if (character == null)
                continue;

            if (character.characterID == characterID)
                return character;
        }

        return null;
    }

    // --------------------------------------------------
    // Spawn Position
    // --------------------------------------------------

    private Vector3 GetSpawnPosition(
        PlayerRef player)
    {
        return new Vector3(
            player.PlayerId * 2f,
            1f,
            0f
        );
    }
}