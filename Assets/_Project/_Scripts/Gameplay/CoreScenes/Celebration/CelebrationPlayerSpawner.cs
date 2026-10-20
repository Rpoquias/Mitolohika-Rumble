using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CelebrationPlayerSpawner : MonoBehaviour
{
    private const string CELEBRATION_SCENE_PATH =
        "Assets/_Project/_Scenes/Main/Celebration.unity";

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
          

            return;
        }

        if (spawnPointManager == null)
        {
           

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
                continue;
            }

            CharacterData character =
                GetCharacterData(characterID);

            if (character == null)
            {
              


                continue;
            }

            if (character.gameplayPrefab == null)
            {
                

                continue;
            }

            Transform spawnPoint =
                spawnPointManager.GetSpawnPoint(
                    placement,
                    placementIndex
                );

            if (spawnPoint == null)
            {
              
              
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
                           
                        }
                    }
                );

            if (playerObject == null)
            {
              
                continue;
            }

            runner.SetPlayerObject(
                player,
                playerObject
            );
        }

        spawnedPlayers = true;
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