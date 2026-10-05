using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class LobbyPlayerListUI : MonoBehaviour
{

    [Header("Characters")]
[SerializeField] private CharacterData[] characters;
    [Header("Expanded List")]
    [SerializeField] private Transform playerListParent;
    [SerializeField] private LobbyPlayerSlot playerSlotPrefab;

    [Header("List Views")]
    [SerializeField] private GameObject expandedView;
    [SerializeField] private GameObject collapsedView;

    private readonly List<LobbyPlayerSlot> slots = new();

    private readonly Dictionary<
        PlayerRef,
        NetworkPlayerState> playerStates = new();

    private readonly Dictionary<
        PlayerRef,
        LobbyPlayerSlot> playerSlots = new();

    private PlayerRegistry playerRegistry;

    private bool collapsed;

    private void OnEnable()
    {
        TryInitialize();

        ShowExpanded();
    }

    private void OnDisable()
    {
        Unsubscribe();
        ClearSlots();
    }

    private void TryInitialize()
    {
        if (playerRegistry != null)
            return;

        playerRegistry =
            PlayerRegistry.Instance;

        if (playerRegistry == null)
        {
            Debug.LogWarning(
                "[LOBBY PLAYER LIST] " +
                "PlayerRegistry is not available yet."
            );

            return;
        }

        playerRegistry.OnPlayerRegistered +=
            HandlePlayerRegistered;

        playerRegistry.OnPlayerUnregistered +=
            HandlePlayerUnregistered;

        RebuildPlayerList();
    }

    private void Unsubscribe()
    {
        if (playerRegistry != null)
        {
            playerRegistry.OnPlayerRegistered -=
                HandlePlayerRegistered;

            playerRegistry.OnPlayerUnregistered -=
                HandlePlayerUnregistered;
        }

        foreach (NetworkPlayerState state in playerStates.Values)
        {
            if (state != null)
            {
                state.OnUIStateChanged -=
                    HandlePlayerStateChanged;
            }
        }

        playerStates.Clear();
        playerSlots.Clear();

        playerRegistry = null;
    }

    private void HandlePlayerRegistered(
        NetworkObject playerObject)
    {
        RebuildPlayerList();
    }

    private void HandlePlayerUnregistered(
        NetworkObject playerObject)
    {
        RebuildPlayerList();
    }
private void RebuildPlayerList()
{
    ClearSlots();

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

    foreach (NetworkObject playerObject in players)
    {
        PlayerRef player =
            playerObject.InputAuthority;

        NetworkPlayerState state =
            playerObject.GetComponent<NetworkPlayerState>();

        LobbyPlayerSlot slot =
            Instantiate(
                playerSlotPrefab,
                playerListParent
            );

        slot.Setup(
            player,
            state,
            characters
        );

        slots.Add(slot);
        playerSlots[player] = slot;

        if (state != null)
        {
            playerStates[player] = state;

            state.OnUIStateChanged +=
                HandlePlayerStateChanged;
        }
    }

    Debug.Log(
        $"[LOBBY PLAYER LIST] " +
        $"Created {slots.Count} player slots."
    );
}
    private void HandlePlayerStateChanged(
        NetworkPlayerState state)
    {
        if (state == null || !state.IsValid)
            return;

        PlayerRef player =
            state.Object.InputAuthority;

        if (playerSlots.TryGetValue(
                player,
                out LobbyPlayerSlot slot))
        {
            slot.Refresh();
        }
    }

    public void ToggleList()
    {
        if (collapsed)
            ShowExpanded();
        else
            ShowCollapsed();
    }

    private void ShowExpanded()
    {
        collapsed = false;

        if (expandedView != null)
            expandedView.SetActive(true);

        if (collapsedView != null)
            collapsedView.SetActive(false);
    }

    private void ShowCollapsed()
    {
        collapsed = true;

        if (expandedView != null)
            expandedView.SetActive(false);

        if (collapsedView != null)
            collapsedView.SetActive(true);
    }

    private void ClearSlots()
    {
        foreach (NetworkPlayerState state in playerStates.Values)
        {
            if (state != null)
            {
                state.OnUIStateChanged -=
                    HandlePlayerStateChanged;
            }
        }

        playerStates.Clear();
        playerSlots.Clear();

        foreach (LobbyPlayerSlot slot in slots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }

        slots.Clear();
    }
}