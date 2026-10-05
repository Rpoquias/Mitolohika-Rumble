using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterPlayerIconListUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform container;
    [SerializeField] private CharacterPlayerIconUI playerIconPrefab;

    [Header("Characters")]
    [SerializeField] private CharacterData[] characters;

    private readonly List<CharacterPlayerIconUI> icons = new();

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

        ClearIcons();

        initialized = false;
    }

    // --------------------------------------------------------------------
    // Initialization
    // --------------------------------------------------------------------

    private void SubscribeToNetworkEvents()
    {
        if (NetworkManager.Instance == null)
            return;

        NetworkManager.Instance.OnPlayerJoinedEvent +=
            HandlePlayerJoined;

        NetworkManager.Instance.OnPlayerLeftEvent +=
            HandlePlayerLeft;

        NetworkManager.Instance.OnNetworkSceneLoadDoneEvent +=
            HandleSceneLoadDone;
    }

    private void UnsubscribeFromNetworkEvents()
    {
        if (NetworkManager.Instance == null)
            return;

        NetworkManager.Instance.OnPlayerJoinedEvent -=
            HandlePlayerJoined;

        NetworkManager.Instance.OnPlayerLeftEvent -=
            HandlePlayerLeft;

        NetworkManager.Instance.OnNetworkSceneLoadDoneEvent -=
            HandleSceneLoadDone;
    }

    private void TryInitialize()
    {
        if (initialized)
            return;

        if (PlayerRegistry.Instance == null)
            return;

        playerRegistry =
            PlayerRegistry.Instance;

        playerRegistry.OnPlayerRegistered +=
            HandlePlayerRegistered;

        playerRegistry.OnPlayerUnregistered +=
            HandlePlayerUnregistered;

        initialized = true;

        Rebuild();
    }

    private void UnsubscribeFromPlayerRegistry()
    {
        if (playerRegistry == null)
            return;

        playerRegistry.OnPlayerRegistered -=
            HandlePlayerRegistered;

        playerRegistry.OnPlayerUnregistered -=
            HandlePlayerUnregistered;

        playerRegistry = null;
    }

    // --------------------------------------------------------------------
    // Network events
    // --------------------------------------------------------------------

    private void HandleSceneLoadDone(Scene scene)
    {
        if (scene != SceneManager.GetActiveScene())
            return;

        TryInitialize();
    }

    private void HandlePlayerJoined(PlayerRef player)
    {
        TryInitialize();
    }

    private void HandlePlayerLeft(PlayerRef player)
    {
        if (!initialized)
        {
            TryInitialize();
            return;
        }

        Rebuild();
    }

    // --------------------------------------------------------------------
    // Player Registry events
    // --------------------------------------------------------------------

    private void HandlePlayerRegistered(
        NetworkObject playerObject)
    {
        Rebuild();
    }

    private void HandlePlayerUnregistered(
        NetworkObject playerObject)
    {
        Rebuild();
    }

    // --------------------------------------------------------------------
    // List
    // --------------------------------------------------------------------

    private void Rebuild()
    {
        if (!initialized)
            return;

        ClearIcons();

        if (playerRegistry == null)
            return;

        List<NetworkObject> players =
            playerRegistry.Players
                .Where(player =>
                    player != null &&
                    player.IsValid)
                .OrderBy(player =>
                    player.InputAuthority.PlayerId)
                .ToList();

        for (int i = 0; i < players.Count; i++)
        {
            NetworkObject playerObject =
                players[i];

            CharacterPlayerIconUI icon =
                Instantiate(
                    playerIconPrefab,
                    container
                );

            icon.Setup(
                playerObject.InputAuthority,
                i + 1,
                characters
            );

            icons.Add(icon);
        }

        Debug.Log(
            $"[CHARACTER PLAYER ICONS] " +
            $"Created {icons.Count} player icons."
        );
    }

    private void ClearIcons()
    {
        foreach (CharacterPlayerIconUI icon in icons)
        {
            if (icon != null)
                Destroy(icon.gameObject);
        }

        icons.Clear();
    }
}