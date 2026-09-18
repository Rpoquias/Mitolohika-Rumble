using Fusion;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private CharacterData[] characters;

    private void Start()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError(
                "[PLAYER SPAWNER] NetworkManager.Instance is NULL."
            );

            return;
        }

        NetworkManager.Instance.OnPlayerJoinedEvent += HandlePlayerJoined;
        NetworkManager.Instance.OnPlayerLeftEvent += HandlePlayerLeft;

        Debug.Log(
            "[PLAYER SPAWNER] Subscribed to NetworkManager events."
        );
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance == null)
            return;

        NetworkManager.Instance.OnPlayerJoinedEvent -= HandlePlayerJoined;
        NetworkManager.Instance.OnPlayerLeftEvent -= HandlePlayerLeft;
    }

    // --------------------------------------------------
    // Player Joined
    // --------------------------------------------------

    private void HandlePlayerJoined(PlayerRef player)
    {
        NetworkRunner runner =
            NetworkManager.Instance.Runner;

        if (runner == null || !runner.IsServer)
            return;

        Debug.Log(
            $"[PLAYER SPAWNER] Handling PlayerJoined: {player}"
        );

        if (characters == null || characters.Length == 0)
        {
            Debug.LogError(
                "[PLAYER SPAWNER] No characters assigned."
            );

            return;
        }

        CharacterData defaultCharacter = characters[0];

        NetworkManager.Instance.SetPlayerSelection(
            player,
            defaultCharacter.characterID
        );

        SpawnCharacter(
            runner,
            player,
            defaultCharacter,
            GetSpawnPosition(player)
        );
    }

    // --------------------------------------------------
    // Player Left
    // --------------------------------------------------

    private void HandlePlayerLeft(PlayerRef player)
    {
        NetworkRunner runner =
            NetworkManager.Instance.Runner;

        if (runner == null || !runner.IsServer)
            return;

        if (runner.TryGetPlayerObject(
            player,
            out NetworkObject playerObject))
        {
            Debug.Log(
                $"[PLAYER SPAWNER] Despawning PlayerObject " +
                $"{playerObject.name} for {player}"
            );

            runner.Despawn(playerObject);
        }
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

        if (runner == null || !runner.IsServer)
            return;

        CharacterData character =
            GetCharacterData(characterID);

        if (character == null)
        {
            Debug.LogError(
                $"[PLAYER SPAWNER] No CharacterData for {characterID}"
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
        NetworkObject playerObject =
            runner.Spawn(
                character.lobbyPrefab,
                spawnPosition,
                Quaternion.identity,
                player
            );

        runner.SetPlayerObject(
            player,
            playerObject
        );

        Debug.Log(
            $"[PLAYER SPAWNER] Spawned PlayerObject for {player}: " +
            $"{playerObject.name}"
        );

        if (runner.TryGetPlayerObject(
            player,
            out NetworkObject registeredObject))
        {
            Debug.Log(
                $"[PLAYER SPAWNER] PlayerObject confirmed for {player}: " +
                $"{registeredObject.name}"
            );
        }
        else
        {
            Debug.LogError(
                $"[PLAYER SPAWNER] PlayerObject was NOT registered " +
                $"for {player}!"
            );
        }

        NetworkPlayerState state =
            playerObject.GetComponent<NetworkPlayerState>();

        if (state != null)
        {
            state.SelectedCharacter =
                character.characterID;
        }

        Debug.Log(
            $"[PLAYER SPAWNER] {player} is now " +
            $"{character.characterName}"
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