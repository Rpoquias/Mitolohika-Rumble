
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterPlayerIconListUI : MonoBehaviour
{
    [Header("All Players UI")]
    [SerializeField] private Transform allPlayersContainer;

    [Header("Local Player UI")]
    [SerializeField] private Transform localPlayerContainer;

    [Header("Shared Settings")]
    [SerializeField] private CharacterPlayerIconUI playerIconPrefab;
    [SerializeField] private CharacterData[] characters;

    private readonly List<CharacterPlayerIconUI> allPlayerIcons = new();
    private CharacterPlayerIconUI localPlayerIcon;

    private PlayerRegistry playerRegistry;
    private bool initialized;

    private void OnEnable()
    {
        SubscribeToNetworkEvents();
        TryInitialize();
    }

    private void OnDisable()
    {
        UnsubscribeFromNetworkEvents();
        UnsubscribeFromPlayerRegistry();

        ClearAllPlayerIcons();
        ClearLocalPlayerIcon();

        initialized = false;
    }

    // --------------------------------------------------
    // Initialization
    // --------------------------------------------------

    private void SubscribeToNetworkEvents()
    {
        if (NetworkManager.Instance == null)
            return;

        NetworkManager.Instance.OnPlayerJoinedEvent += HandlePlayerJoined;
        NetworkManager.Instance.OnPlayerLeftEvent += HandlePlayerLeft;
        NetworkManager.Instance.OnNetworkSceneLoadDoneEvent += HandleSceneLoadDone;
    }

    private void UnsubscribeFromNetworkEvents()
    {
        if (NetworkManager.Instance == null)
            return;

        NetworkManager.Instance.OnPlayerJoinedEvent -= HandlePlayerJoined;
        NetworkManager.Instance.OnPlayerLeftEvent -= HandlePlayerLeft;
        NetworkManager.Instance.OnNetworkSceneLoadDoneEvent -= HandleSceneLoadDone;
    }

    private void TryInitialize()
    {
        if (initialized || PlayerRegistry.Instance == null)
            return;

        playerRegistry = PlayerRegistry.Instance;

        playerRegistry.OnPlayerRegistered += HandlePlayerRegistered;
        playerRegistry.OnPlayerUnregistered += HandlePlayerUnregistered;

        initialized = true;
        Rebuild();
    }

    private void UnsubscribeFromPlayerRegistry()
    {
        if (playerRegistry == null)
            return;

        playerRegistry.OnPlayerRegistered -= HandlePlayerRegistered;
        playerRegistry.OnPlayerUnregistered -= HandlePlayerUnregistered;

        playerRegistry = null;
    }

    // --------------------------------------------------
    // Events
    // --------------------------------------------------

    private void HandleSceneLoadDone(Scene scene)
    {
        if (scene == SceneManager.GetActiveScene())
            TryInitialize();
    }

    private void HandlePlayerJoined(PlayerRef player)
    {
        TryInitialize();
    }

    private void HandlePlayerLeft(PlayerRef player)
    {
        if (!initialized)
            TryInitialize();
        else
            Rebuild();
    }

    private void HandlePlayerRegistered(NetworkObject playerObject)
    {
        Rebuild();
    }

    private void HandlePlayerUnregistered(NetworkObject playerObject)
    {
        Rebuild();
    }

    // --------------------------------------------------
    // Build UI
    // --------------------------------------------------

    private void Rebuild()
    {
        if (!initialized || playerRegistry == null)
            return;

        ClearAllPlayerIcons();
        ClearLocalPlayerIcon();

        List<NetworkObject> players = playerRegistry.Players
            .Where(player => player != null && player.IsValid)
            .OrderBy(player => player.InputAuthority.PlayerId)
            .ToList();

        NetworkRunner runner = NetworkManager.Instance != null
            ? NetworkManager.Instance.Runner
            : null;

        for (int i = 0; i < players.Count; i++)
        {
            NetworkObject playerObject = players[i];
            PlayerRef playerRef = playerObject.InputAuthority;

            // Create an icon in the all-players container.
            if (allPlayersContainer != null)
            {
                CharacterPlayerIconUI icon = Instantiate(
                    playerIconPrefab,
                    allPlayersContainer
                );

                icon.Setup(playerRef, i + 1, characters);
                allPlayerIcons.Add(icon);
            }

            // Create a separate icon only for the local player.
            if (runner != null &&
                runner.IsRunning &&
                playerRef == runner.LocalPlayer &&
                localPlayerContainer != null)
            {
                localPlayerIcon = Instantiate(
                    playerIconPrefab,
                    localPlayerContainer
                );

                localPlayerIcon.Setup(playerRef, i + 1, characters);
            }
        }
    }

    // --------------------------------------------------
    // Cleanup
    // --------------------------------------------------

    private void ClearAllPlayerIcons()
    {
        foreach (CharacterPlayerIconUI icon in allPlayerIcons)
        {
            if (icon != null)
                Destroy(icon.gameObject);
        }

        allPlayerIcons.Clear();
    }

    private void ClearLocalPlayerIcon()
    {
        if (localPlayerIcon != null)
        {
            Destroy(localPlayerIcon.gameObject);
            localPlayerIcon = null;
        }
    }
}
