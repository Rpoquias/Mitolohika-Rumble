using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CelebrationPlayerSpawner : MonoBehaviour
{
    private const string CELEBRATION_SCENE_PATH =
        "Assets/_Project/_Scenes/Testing/Ryan/Celebration.unity";

    [Header("Characters")]
    [SerializeField]
    private CharacterData[] characters;

    [Header("Celebration Spawn Points")]
    [SerializeField]
    private CelebrationSpawnPointManager spawnPointManager;

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

    private void TrySpawnPlayers()
    {
        if (spawnedPlayers)
            return;

        if (SceneManager.GetActiveScene().path !=
            CELEBRATION_SCENE_PATH)
        {
            return;
        }

        NetworkManager networkManager =
            NetworkManager.Instance;

        if (networkManager == null)
            return;

        NetworkRunner runner =
            networkManager.Runner;

        if (runner == null ||
            !runner.IsRunning)
        {
            return;
        }

        if (!runner.IsServer)
        {
            return;
        }

        MatchSession matchSession =
            networkManager.CurrentMatchSession;

        if (matchSession == null)
        {
            Debug.LogError(
                "[CELEBRATION SPAWNER] MatchSession is missing."
            );

            return;
        }

        if (spawnPointManager == null)
        {
            Debug.LogError(
                "[CELEBRATION SPAWNER] " +
                "CelebrationSpawnPointManager is NULL."
            );

            return;
        }

        List<PlayerRef> players =
            new List<PlayerRef>();

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (matchSession.TryGetOverallPlacement(
                    player,
                    out int placement))
            {
                players.Add(player);
            }
            else
            {
                Debug.LogWarning(
                    $"[CELEBRATION SPAWNER] " +
                    $"No overall placement for {player}."
                );
            }
        }

        SpawnPlayers(
            runner,
            matchSession,
            players
        );
    }

  private async void SpawnPlayers(
    NetworkRunner runner,
    MatchSession matchSession,
    List<PlayerRef> players)
{
    Dictionary<int, int> placementCounts =
        new Dictionary<int, int>();

    foreach (PlayerRef player in players)
    {
        if (!matchSession.TryGetOverallPlacement(
                player,
                out int placement))
        {
            continue;
        }

        if (!placementCounts.ContainsKey(placement))
        {
            placementCounts[placement] = 0;
        }

        int placementIndex =
            placementCounts[placement];

        placementCounts[placement]++;

        if (!NetworkManager.Instance.TryGetPlayerSelection(
                player,
                out CharacterID characterID))
        {
            Debug.LogWarning(
                $"[CELEBRATION SPAWNER] " +
                $"No character selection for {player}."
            );

            continue;
        }

        CharacterData character =
            GetCharacterData(characterID);

        if (character == null)
        {
            Debug.LogError(
                $"[CELEBRATION SPAWNER] " +
                $"No CharacterData for {characterID}."
            );

            continue;
        }

        if (character.gameplayPrefab == null)
        {
            Debug.LogError(
                $"[CELEBRATION SPAWNER] " +
                $"Gameplay prefab missing for " +
                $"{character.characterName}."
            );

            continue;
        }

        Transform spawnPoint =
            spawnPointManager.GetSpawnPoint(
                placement,
                placementIndex
            );

        if (spawnPoint == null)
        {
            Debug.LogError(
                $"[CELEBRATION SPAWNER] " +
                $"No spawn point for " +
                $"{placement} Place, index " +
                $"{placementIndex}."
            );

            continue;
        }

        NetworkObject playerObject =
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
                            "[CELEBRATION SPAWNER] " +
                            "Spawned player has no NetworkPlayerState."
                        );
                    }
                }
            );

        if (playerObject == null)
        {
            Debug.LogError(
                $"[CELEBRATION SPAWNER] " +
                $"Failed to spawn {player}."
            );

            continue;
        }

        runner.SetPlayerObject(
            player,
            playerObject
        );

        Debug.Log(
            $"[CELEBRATION SPAWNER] Spawned " +
            $"{character.characterName} for {player} " +
            $"at {placement} Place."
        );
    }

    spawnedPlayers = true;

    Debug.Log(
        "[CELEBRATION SPAWNER] " +
        "All celebration players spawned."
    );
}

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
}