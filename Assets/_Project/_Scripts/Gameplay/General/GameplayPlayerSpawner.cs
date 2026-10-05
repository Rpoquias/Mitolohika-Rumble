using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayPlayerSpawner : MonoBehaviour
{

    [Header("Characters")]
    [SerializeField] private CharacterData[] characters;

    [Header("Gameplay Spawn Points")]
    [SerializeField] private SpawnPointManager spawnPointManager;

    private bool spawningPlayers;
    private bool spawnedPlayers;

private async void Start()
{
    while (
        NetworkManager.Instance == null ||
        NetworkManager.Instance.Runner == null ||
        !NetworkManager.Instance.Runner.IsRunning)
    {
        await Awaitable.NextFrameAsync();
    }

    TrySpawnPlayers();
}

    private async void TrySpawnPlayers()
    {
 if (spawningPlayers || spawnedPlayers)
        return;

    NetworkManager networkManager =
        NetworkManager.Instance;

    if (networkManager == null)
        return;

    NetworkRunner runner =
        networkManager.Runner;

    if (runner == null ||
        !runner.IsRunning ||
        !runner.IsServer)
        return;

    if (spawnPointManager == null)
    {
        Debug.LogError(
            "[GAMEPLAY SPAWNER] " +
            "SpawnPointManager is NULL."
        );

        enabled = false;
        return;
    }


        // Validate everything before spawning.
        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (!networkManager.TryGetPlayerSelection(player, out CharacterID characterID)) return;

            CharacterData character = GetCharacterData(characterID);
            if (character == null || character.gameplayPrefab == null) return;
            if (spawnPointManager.GetSpawnPoint(player) == null) return;
        }

        spawningPlayers = true;
        await SpawnPlayers(runner);
        spawnedPlayers = true;
        spawningPlayers = false;

        Debug.Log($"<color=#FF4DFF><b>[GAMEPLAY SPAWNER] OK</b> - All gameplay players spawned</color>");
    }

    private CharacterData GetCharacterData(CharacterID characterID)
    {
        foreach (CharacterData character in characters) { if (character.characterID == characterID) return character; }
        return null;
    }

    private async Awaitable SpawnPlayers(NetworkRunner runner)
    {
        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (!NetworkManager.Instance.TryGetPlayerSelection(player, out CharacterID characterID)) continue;

            CharacterData character = GetCharacterData(characterID);
            if (character == null) continue;

            Transform spawnPoint = spawnPointManager.GetSpawnPoint(player);
            if (spawnPoint == null) continue;

            NetworkObject gameplayPlayer = await runner.SpawnAsync(character.gameplayPrefab, spawnPoint.position, spawnPoint.rotation, player, (r, spawnedObject) =>
            {
                NetworkPlayerState state = spawnedObject.GetComponent<NetworkPlayerState>();
                if (state != null) state.SelectedCharacter = character.characterID;
            });

            if (gameplayPlayer == null) continue;

            runner.SetPlayerObject(player, gameplayPlayer);
        }
    }
}