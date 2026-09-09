using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private CharacterData[] characters;

    public void PlayerJoined(PlayerRef player)
    {
        if (!Runner.IsServer)
            return;

        int characterIndex = player.PlayerId % characters.Length;

        CharacterData character = characters[characterIndex];

        Vector3 spawnPosition = new Vector3(
            player.PlayerId * 2f,
            1f,
            0f
        );

        NetworkObject playerObject = Runner.Spawn(
            character.characterPrefab,
            spawnPosition,
            Quaternion.identity,
            player
        );

        Runner.SetPlayerObject(player, playerObject);

        Debug.Log(
            $"Player joined: {player} | Character: {character.characterName}"
        );
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (!Runner.IsServer)
            return;

        if (Runner.TryGetPlayerObject(
                player,
                out NetworkObject playerObject))
        {
            Runner.Despawn(playerObject);

            Debug.Log($"Player left: {player}");
        }
    }
}