using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class LobbyPlayerListUI : MonoBehaviour
{
    [SerializeField] private Transform playerListParent;
    [SerializeField] private LobbyPlayerSlot playerSlotPrefab;

    private readonly List<LobbyPlayerSlot> slots = new();

    private int lastPlayerCount = -1;

    private void Start()
    {
        RefreshPlayerList();
    }

    private void Update()
    {
        if (NetworkManager.Instance == null)
            return;

        NetworkRunner runner =
            NetworkManager.Instance.Runner;

        if (runner == null)
            return;

        int currentPlayerCount =
            runner.ActivePlayers.Count();

        if (currentPlayerCount != lastPlayerCount)
        {
            RefreshPlayerList();
        }

        foreach (LobbyPlayerSlot slot in slots)
        {
            if (slot != null)
                slot.Refresh();
        }
    }

    private void RefreshPlayerList()
    {
        NetworkRunner runner =
            NetworkManager.Instance.Runner;

        if (runner == null)
            return;

        ClearSlots();

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            LobbyPlayerSlot slot =
                Instantiate(
                    playerSlotPrefab,
                    playerListParent
                );

            slot.Setup(player);

            slots.Add(slot);
        }

        lastPlayerCount =
            runner.ActivePlayers.Count();

        Debug.Log(
            $"[LOBBY UI] Created {slots.Count} player slots."
        );
    }

    private void ClearSlots()
    {
        foreach (LobbyPlayerSlot slot in slots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }

        slots.Clear();
    }
}