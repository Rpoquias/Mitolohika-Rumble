using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyPlayerSpawner : MonoBehaviour
{
    [Header("Characters")]
[SerializeField] private CharacterData[] characters;
[SerializeField] private CharacterData defaultCharacter;
    

    private bool initialized;
    private bool isLobbySceneLoaded;

    private SpawnPointManager spawnPointManager;

    // ============================================================
    // INITIALIZATION
    // ============================================================

    private async void Start()
{
    while (NetworkManager.Instance == null ||
           NetworkManager.Instance.Runner == null ||
           !NetworkManager.Instance.Runner.IsRunning)
    {
        await Awaitable.NextFrameAsync();
    }

    NetworkManager manager = NetworkManager.Instance;

    manager.OnPlayerJoinedEvent += HandlePlayerJoined;
    manager.OnPlayerLeftEvent += HandlePlayerLeft;
    manager.OnNetworkSceneLoadDoneEvent += HandleNetworkSceneLoadDone;

    initialized = true;

  
    // If the lobby is already loaded, initialize immediately.
    Scene activeScene = SceneManager.GetActiveScene();

    if (activeScene.buildIndex == manager.LobbySceneBuildIndex)
    {
        HandleNetworkSceneLoadDone(activeScene);
    }
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

    // ============================================================
    // SCENE HANDLING
    // ============================================================

  private void HandleNetworkSceneLoadDone(Scene scene)
{
    if (!initialized || NetworkManager.Instance == null)
        return;

    bool loadedLobby =
        scene.buildIndex ==
        NetworkManager.Instance.LobbySceneBuildIndex;

    isLobbySceneLoaded = loadedLobby;
    spawnPointManager = null;

    if (!loadedLobby)
    {
     

        return;
    }

  
  SpawnPointManager[] managers =
    FindObjectsByType<SpawnPointManager>( );



foreach (SpawnPointManager manager in managers)
{
    

    if (manager.gameObject.scene != scene)
        continue;

    spawnPointManager = manager;
    break;
}

if (spawnPointManager == null)
{
    
    return;
}



TrySpawnExistingPlayers();
}

    // ============================================================
    // SPAWN EXISTING PLAYERS
    // ============================================================

   private void TrySpawnExistingPlayers()
{

    if (!initialized || !isLobbySceneLoaded || spawnPointManager == null ||
        NetworkManager.Instance == null)
    {
     
        return;
    }

    NetworkRunner runner = NetworkManager.Instance.Runner;

    if (runner == null || !runner.IsRunning || !runner.IsServer)
    {
    
        return;
    }

    int playerCount = 0;

    foreach (PlayerRef player in runner.ActivePlayers)
    {
        playerCount++;


        if (runner.TryGetPlayerObject(player, out NetworkObject existingPlayer))
        {
           
            continue;
        }

       
        SpawnExistingPlayer(runner, player);
    }

  
}

   private void SpawnExistingPlayer(NetworkRunner runner, PlayerRef player)
{
   
    if (!isLobbySceneLoaded || spawnPointManager == null)
    {
       
        return;
    }

    if (!NetworkManager.Instance.TryGetPlayerSelection(
        player, out CharacterID characterID))
{
    if (defaultCharacter == null)
    {
        Debug.LogError(
            "[LOBBY SPAWNER] Default character is not assigned.");
        return;
    }

    characterID = defaultCharacter.characterID;
    NetworkManager.Instance.SetPlayerSelection(player, characterID);
}

    CharacterData character = GetCharacterData(characterID);

    if (character == null)
    {
       
        return;
    }

    if (character.lobbyPrefab == null)
    {
       
        return;
    }

    if (!TryGetSpawnPoint(player, runner, out Transform spawnPoint))
    {
        
        return;
    }

  

    SpawnCharacter(
        runner,
        player,
        character,
        spawnPoint.position,
        spawnPoint.rotation
    );
}

    // ============================================================
    // PLAYER JOINED / LEFT
    // ============================================================

    private void HandlePlayerJoined(PlayerRef player)
    {
        if (!isLobbySceneLoaded ||
            spawnPointManager == null ||
            NetworkManager.Instance == null)
        {
            return;
        }

        NetworkRunner runner =
            NetworkManager.Instance.Runner;

        if (runner == null ||
            !runner.IsRunning ||
            !runner.IsServer)
        {
            return;
        }

        if (runner.TryGetPlayerObject(
                player,
                out NetworkObject existingPlayer))
        {
            return;
        }

        SpawnExistingPlayer(runner, player);
    }

    private void HandlePlayerLeft(PlayerRef player)
    {
        if (NetworkManager.Instance == null)
            return;

        NetworkRunner runner =
            NetworkManager.Instance.Runner;

        if (runner == null ||
            !runner.IsRunning ||
            !runner.IsServer)
        {
            return;
        }

        if (runner.TryGetPlayerObject(
                player,
                out NetworkObject playerObject))
        {
            runner.Despawn(playerObject);
        }
    }

    // ============================================================
    // CHARACTER CHANGE
    // ============================================================

    public void ChangeCharacter(
        PlayerRef player,
        CharacterID characterID)
    {
        if (!isLobbySceneLoaded ||
            NetworkManager.Instance == null)
        {
            return;
        }

        NetworkRunner runner =
            NetworkManager.Instance.Runner;

        if (runner == null ||
            !runner.IsRunning ||
            !runner.IsServer)
        {
            return;
        }

        CharacterData character =
            GetCharacterData(characterID);

        if (character == null ||
            character.lobbyPrefab == null)
        {
          

            return;
        }

        Vector3 spawnPosition;
        Quaternion spawnRotation;

        bool hasExistingPlayer =
            runner.TryGetPlayerObject(
                player,
                out NetworkObject oldObject
            );

        if (hasExistingPlayer)
        {
            // Preserve the player's current position when
            // changing characters in the lobby.
            spawnPosition = oldObject.transform.position;
            spawnRotation = oldObject.transform.rotation;
        }
        else
        {
            if (!TryGetSpawnPoint(
                    player,
                    runner,
                    out Transform spawnPoint))
            {
               
                return;
            }

            spawnPosition = spawnPoint.position;
            spawnRotation = spawnPoint.rotation;
        }

        NetworkManager.Instance.SetPlayerSelection(
            player,
            characterID
        );

        if (hasExistingPlayer)
            runner.Despawn(oldObject);

        SpawnCharacter(
            runner,
            player,
            character,
            spawnPosition,
            spawnRotation
        );
    }

    // ============================================================
    // SPAWN POINT
    // ============================================================

    private bool TryGetSpawnPoint(
        PlayerRef player,
        NetworkRunner runner,
        out Transform spawnPoint)
    {
        spawnPoint = null;

        if (!isLobbySceneLoaded ||
            spawnPointManager == null ||
            runner == null ||
            !runner.IsRunning)
        {
            return false;
        }

        spawnPoint =
            spawnPointManager.GetSpawnPoint(
                player,
                runner
            );

        return spawnPoint != null;
    }

    // ============================================================
    // SPAWN CHARACTER
    // ============================================================

    private void SpawnCharacter(
        NetworkRunner runner,
        PlayerRef player,
        CharacterData character,
        Vector3 spawnPosition,
        Quaternion spawnRotation)
    {
        if (runner == null ||
            !runner.IsRunning ||
            !runner.IsServer ||
            character == null ||
            character.lobbyPrefab == null)
        {
            return;
        }

        NetworkObject playerObject = runner.Spawn(
            character.lobbyPrefab,
            spawnPosition,
            spawnRotation,
            player,
            (r, spawnedObject) =>
            {
                NetworkPlayerState state =
                    spawnedObject.GetComponent<NetworkPlayerState>();

                if (state != null)
                {
                    state.SelectedCharacter =
                        character.characterID;
                }
            }
        );

        if (playerObject == null)
        {
         

            return;
        }

        runner.SetPlayerObject(player, playerObject);
        NetworkManager.Instance?.NotifyPlayerObjectAvailable(player);
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private CharacterData GetCharacterData(
        CharacterID characterID)
    {
        if (characters == null)
            return null;

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