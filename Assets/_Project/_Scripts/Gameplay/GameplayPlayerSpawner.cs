using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayPlayerSpawner : MonoBehaviour
{
    private const string GAMEPLAY_SCENE_PATH =
        "Assets/_Project/_Scenes/Testing/Ryan/MatiraMatibay.unity";

    [Header("Characters")]
    [SerializeField] private CharacterData[] characters;

    [Header("Gameplay Spawn Points")]
    [SerializeField] private SpawnPointManager spawnPointManager;
private bool spawningPlayers;
    private bool spawnedPlayers;


private async void Start()
{
    while (NetworkManager.Instance == null || NetworkManager.Instance.Runner == null || !NetworkManager.Instance.Runner.IsRunning)
    {
        await Awaitable.NextFrameAsync();
    }

    TrySpawnPlayers();
}

    private void TrySpawnPlayers()
    {


        if (spawningPlayers)
        return;

    if (spawnedPlayers)
        return;
        // This script must only work in the gameplay scene.
        if (SceneManager.GetActiveScene().path != GAMEPLAY_SCENE_PATH)
            return;

        if (NetworkManager.Instance == null)
            return;

        NetworkRunner runner =
            NetworkManager.Instance.Runner;

        if (runner == null || !runner.IsRunning)
            return;

        if (!runner.IsServer)
            return;

        if (spawnPointManager == null)
        {
            Debug.LogError(
                "[GAMEPLAY SPAWNER] SpawnPointManager is NULL."
            );

            enabled = false;
            return;
        }

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (!NetworkManager.Instance.TryGetPlayerSelection(
                    player,
                    out CharacterID characterID))
            {
                Debug.LogWarning(
                    $"[GAMEPLAY SPAWNER] No character selection " +
                    $"for {player}."
                );

                return;
            }

            CharacterData character =
                GetCharacterData(characterID);

            if (character == null)
            {
                Debug.LogError(
                    $"[GAMEPLAY SPAWNER] No CharacterData for " +
                    $"{characterID}."
                );

                return;
            }

            if (character.gameplayPrefab == null)
            {
                Debug.LogError(
                    $"[GAMEPLAY SPAWNER] Gameplay prefab missing " +
                    $"for {character.characterName}."
                );

                return;
            }

            if (spawnPointManager.GetSpawnPoint(player) == null)
            {
                Debug.LogError(
                    $"[GAMEPLAY SPAWNER] No spawn point for {player}."
                );

                return;
            }
        }

        SpawnPlayers(runner);

        spawnedPlayers = true;

        Debug.Log(
            "[GAMEPLAY SPAWNER] All gameplay players spawned."
        );
    }
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
private async void SpawnPlayers(NetworkRunner runner)
{
    foreach (PlayerRef player in runner.ActivePlayers)
    {
        if (!NetworkManager.Instance.TryGetPlayerSelection(
                player,
                out CharacterID characterID))
        {
            continue;
        }

        CharacterData character =
            GetCharacterData(characterID);

        if (character == null)
        {
            Debug.LogError(
                $"[GAMEPLAY SPAWNER] CharacterData not found for {characterID}."
            );

            continue;
        }

        Transform spawnPoint =
            spawnPointManager.GetSpawnPoint(player);

        if (spawnPoint == null)
        {
            Debug.LogError(
                $"[GAMEPLAY SPAWNER] No spawn point for {player}."
            );

            continue;
        }

        NetworkObject gameplayPlayer =
            await runner.SpawnAsync(
                character.gameplayPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
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
                            "[GAMEPLAY SPAWNER] " +
                            "Spawned player has no NetworkPlayerState."
                        );
                    }
                }
            );

        if (gameplayPlayer == null)
        {
            Debug.LogError(
                $"[GAMEPLAY SPAWNER] Failed to spawn gameplay player " +
                $"for {player}."
            );

            continue;
        }

        runner.SetPlayerObject(
            player,
            gameplayPlayer
        );

        Debug.Log(
            $"[GAMEPLAY SPAWNER] Spawned " +
            $"{character.characterName} for {player} " +
            $"at {spawnPoint.name}."
        );
    }

    spawnedPlayers = true;

    Debug.Log(
        "[GAMEPLAY SPAWNER] All gameplay players spawned."
    );
}
}
