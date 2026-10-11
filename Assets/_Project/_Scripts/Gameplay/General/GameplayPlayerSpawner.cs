using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GameplayPlayerSpawner : MonoBehaviour
{
    [Header("Characters")]
    [SerializeField] private CharacterData[] characters;

    [Header("Gameplay Spawn Points")]
    [SerializeField] private SpawnPointManager spawnPointManager;

    private bool spawningPlayers;

    private readonly HashSet<PlayerRef> spawnedPlayers =
        new HashSet<PlayerRef>();

    private NetworkRunner _runner;

    private async void Start()
    {
        while (
            this != null &&
            (
                NetworkManager.Instance == null ||
                NetworkManager.Instance.Runner == null ||
                !NetworkManager.Instance.Runner.IsRunning
            ))
        {
            await Awaitable.NextFrameAsync();
        }

        if (this == null)
            return;

        _runner = NetworkManager.Instance.Runner;

        if (!_runner.IsServer)
            return;

        await SpawnPlayersWhenReady();
    }

    private async Awaitable SpawnPlayersWhenReady()
    {
        if (spawningPlayers)
            return;

        spawningPlayers = true;

        while (
            this != null &&
            _runner != null &&
            _runner.IsRunning &&
            _runner.IsServer
        )
        {
            if (TrySpawnAvailablePlayers())
            {
                if (AllActivePlayersSpawned())
                    break;
            }

            await Awaitable.NextFrameAsync();
        }

        spawningPlayers = false;
    }

    private bool TrySpawnAvailablePlayers()
    {
        if (spawnPointManager == null)
        {
            Debug.LogError(
                "[GAMEPLAY SPAWNER] SpawnPointManager is missing."
            );

            return false;
        }

        NetworkManager networkManager = NetworkManager.Instance;

        if (networkManager == null)
            return false;

        bool spawnedAny = false;

        foreach (PlayerRef player in _runner.ActivePlayers)
        {
            if (spawnedPlayers.Contains(player))
                continue;

            // Prevent duplicate player objects.
            if (_runner.TryGetPlayerObject(
                player,
                out NetworkObject existingPlayer))
            {
                spawnedPlayers.Add(player);
                continue;
            }

            if (!networkManager.TryGetPlayerSelection(
                player,
                out CharacterID characterID))
            {
                continue;
            }

            CharacterData character = GetCharacterData(characterID);

            if (character == null || character.gameplayPrefab == null)
            {
                Debug.LogWarning(
                    $"[GAMEPLAY SPAWNER] Character prefab unavailable for {player}."
                );

                continue;
            }

            Transform spawnPoint =
                spawnPointManager.GetSpawnPoint(player, _runner);

            if (spawnPoint == null)
            {
                Debug.LogError(
                    $"[GAMEPLAY SPAWNER] No spawn point available for {player}."
                );

                continue;
            }

            SpawnPlayer(player, character, spawnPoint);
            spawnedAny = true;
        }

        return spawnedAny;
    }

    private async void SpawnPlayer(
        PlayerRef player,
        CharacterData character,
        Transform spawnPoint)
    {
        if (spawnedPlayers.Contains(player))
            return;

        spawnedPlayers.Add(player);

        NetworkObject spawnedObject = null;

        try
        {
            spawnedObject = await _runner.SpawnAsync(
                character.gameplayPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                player,
                (runner, obj) =>
                {
                    NetworkPlayerState state =
                        obj.GetComponent<NetworkPlayerState>();

                    if (state != null)
                    {
                        state.SelectedCharacter = character.characterID;
                    }
                }
            );
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                $"[GAMEPLAY SPAWNER] Failed to spawn {player}: {exception.Message}"
            );
        }

        if (this == null)
            return;

        if (spawnedObject == null)
        {
            spawnedPlayers.Remove(player);
            return;
        }

        if (_runner == null || !_runner.IsRunning)
        {
            spawnedPlayers.Remove(player);
            return;
        }

        _runner.SetPlayerObject(player, spawnedObject);

        Debug.Log(
            $"[GAMEPLAY SPAWNER] Spawned {player} at {spawnPoint.name}."
        );
    }

    private bool AllActivePlayersSpawned()
    {
        foreach (PlayerRef player in _runner.ActivePlayers)
        {
            if (!_runner.TryGetPlayerObject(player, out _))
                return false;
        }

        return true;
    }

    private CharacterData GetCharacterData(CharacterID characterID)
    {
        foreach (CharacterData character in characters)
        {
            if (character != null &&
                character.characterID == characterID)
            {
                return character;
            }
        }

        return null;
    }
}