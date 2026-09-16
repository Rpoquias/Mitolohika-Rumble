using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private CharacterData[] characters;

  public void PlayerJoined(PlayerRef player)
{
    if (!Runner.IsServer)
        return;

    CharacterData defaultCharacter = characters[0];

    NetworkManager.Instance.SetPlayerSelection(
        player,
        defaultCharacter.characterID
    );

    SpawnCharacter(
        player,
        defaultCharacter,
        GetSpawnPosition(player)
    );
}
   public void ChangeCharacter(
    PlayerRef player,
    CharacterID characterID)
{
    if (!Runner.IsServer)
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

    if (Runner.TryGetPlayerObject(
        player,
        out NetworkObject oldObject))
    {
        spawnPosition =
            oldObject.transform.position;

        Runner.Despawn(oldObject);
    }

    SpawnCharacter(
        player,
        character,
        spawnPosition
    );
}
public void RespawnPlayersForGameplay()
{
    if (!Runner.IsServer)
        return;

    foreach (PlayerRef player in Runner.ActivePlayers)
    {
        if (Runner.TryGetPlayerObject(
            player,
            out NetworkObject existingObject))
        {
            Debug.LogWarning(
                $"[PLAYER SPAWNER] {player} already has a PlayerObject."
            );

            continue;
        }

        if (!NetworkManager.Instance.TryGetPlayerSelection(
            player,
            out CharacterID characterID))
        {
            Debug.LogWarning(
                $"[PLAYER SPAWNER] No character selection for {player}."
            );

            continue;
        }

        CharacterData character =
            GetCharacterData(characterID);

        if (character == null)
        {
            Debug.LogError(
                $"[PLAYER SPAWNER] No CharacterData for {characterID}."
            );

            continue;
        }

        Vector3 spawnPosition =
            GetSpawnPosition(player);

        SpawnCharacter(
            player,
            character,
            spawnPosition
        );
    }
}
    private void SpawnCharacter(
        PlayerRef player,
        CharacterData character,
        Vector3 spawnPosition)
    {
        NetworkObject playerObject = Runner.Spawn(
            character.characterPrefab,
            spawnPosition,
            Quaternion.identity,
            player
        );

        Runner.SetPlayerObject(
            player,
            playerObject
        );

        NetworkPlayerState state =
            playerObject.GetComponent<NetworkPlayerState>();

        if (state != null)
        {
            state.SelectedCharacter =
                character.characterID;

            state.IsReady = false;
        }

        Debug.Log(
            $"[PLAYER SPAWNER] {player} is now {character.characterName}"
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

    private Vector3 GetSpawnPosition(PlayerRef player)
    {
        return new Vector3(
            player.PlayerId * 2f,
            1f,
            0f
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
        }
    }
}