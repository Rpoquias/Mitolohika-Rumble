using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class MatiraMatibayEliminationManager : NetworkBehaviour
{
    [Header("Round State")]
    [SerializeField] private RoundStateManager roundStateManager;

    [Networked, Capacity(4)]
    public NetworkArray<PlayerRef> EliminationOrder => default;

    [Networked]
    public int EliminationCount { get; private set; }

    [Networked, OnChangedRender(nameof(OnEliminationSequenceChanged))]
    public int EliminationSequence { get; private set; }

    public event Action OnEliminationChanged;

    private PlayerRegistry playerRegistry;
    private NetworkRunner runner;

    private readonly HashSet<PlayerElimination>
        subscribedPlayers =
        new HashSet<PlayerElimination>();

    private bool initialized;
    private bool trackingActive;

    // ============================================================
    // INITIALIZATION
    // ============================================================

    private void OnEnable()
    {
        if (roundStateManager != null)
        {
            roundStateManager.OnStateChanged +=
                HandleStateChanged;
        }
    }

    private void OnDisable()
    {
        if (roundStateManager != null)
        {
            roundStateManager.OnStateChanged -=
                HandleStateChanged;
        }
    }

    private void HandleStateChanged(
        RoundStateManager.RoundState state)
    {
        if (!initialized)
            TryInitialize();
    }

    private void TryInitialize()
    {
        runner =
            NetworkRunner.GetRunnerForGameObject(
                gameObject
            );

        if (runner == null || !runner.IsRunning)
            return;

        playerRegistry =
            PlayerRegistry.Instance;

        if (playerRegistry == null)
            return;

        playerRegistry.OnPlayerRegistered +=
            RegisterPlayer;

        playerRegistry.OnPlayerUnregistered +=
            UnregisterPlayer;

        foreach (NetworkObject playerObject
                 in playerRegistry.Players)
        {
            RegisterPlayer(playerObject);
        }

        initialized = true;
    }

    private void OnDestroy()
    {
        if (playerRegistry == null)
            return;

        playerRegistry.OnPlayerRegistered -=
            RegisterPlayer;

        playerRegistry.OnPlayerUnregistered -=
            UnregisterPlayer;

        foreach (NetworkObject playerObject
                 in playerRegistry.Players)
        {
            UnregisterPlayer(playerObject);
        }
    }

    // ============================================================
    // PLAYER REGISTRATION
    // ============================================================

    private void RegisterPlayer(
        NetworkObject playerObject)
    {
        if (playerObject == null)
            return;

        PlayerElimination player =
            playerObject.GetComponent<PlayerElimination>();

        if (player == null)
            return;

        if (!subscribedPlayers.Add(player))
            return;

        player.OnPlayerEliminated +=
            HandlePlayerEliminated;
    }

    private void UnregisterPlayer(
        NetworkObject playerObject)
    {
        if (playerObject == null)
            return;

        PlayerElimination player =
            playerObject.GetComponent<PlayerElimination>();

        if (player == null)
            return;

        if (!subscribedPlayers.Remove(player))
            return;

        player.OnPlayerEliminated -=
            HandlePlayerEliminated;
    }

    // ============================================================
    // ROUND
    // ============================================================

    public void StartTracking()
    {
        if (!HasStateAuthority)
            return;

        ResetEliminations();

        trackingActive = true;

        Debug.Log(
            "[MATIRA ELIMINATION] Tracking started."
        );
    }

    public void StopTracking()
    {
        if (!HasStateAuthority)
            return;

        trackingActive = false;
    }

    public void ResetEliminations()
    {
        if (!HasStateAuthority)
            return;

        trackingActive = false;

        for (int i = 0;
             i < EliminationOrder.Length;
             i++)
        {
            EliminationOrder.Set(
                i,
                default
            );
        }

        EliminationCount = 0;
        EliminationSequence = 0;

        OnEliminationChanged?.Invoke();
    }

    // ============================================================
    // ELIMINATION
    // ============================================================

    private void HandlePlayerEliminated(
        PlayerElimination eliminatedPlayer)
    {
        if (!HasStateAuthority)
            return;

        if (!trackingActive)
            return;

        if (eliminatedPlayer == null)
            return;

        PlayerRef player =
            eliminatedPlayer.PlayerRef;

        if (!player.IsValid)
            return;

        // Prevent duplicate records.
        if (GetEliminationOrder(player) > 0)
            return;

        if (EliminationCount >=
            EliminationOrder.Length)
        {
            return;
        }

        EliminationOrder.Set(
            EliminationCount,
            player
        );

        EliminationCount++;

        EliminationSequence++;

        Debug.Log(
            $"[MATIRA ELIMINATION] " +
            $"{player} eliminated. " +
            $"Order: {EliminationCount}"
        );

        OnEliminationChanged?.Invoke();
    }

    // ============================================================
    // ACCESS
    // ============================================================

    public int GetEliminationOrder(
        PlayerRef player)
    {
        for (int i = 0;
             i < EliminationCount;
             i++)
        {
            if (EliminationOrder[i] == player)
                return i + 1;
        }

        return 0;
    }

    public bool IsPlayerEliminated(
        PlayerRef player)
    {
        return GetEliminationOrder(player) > 0;
    }

    public int GetLivePlacement(
        PlayerRef player)
    {
        if (runner == null ||
            !runner.IsRunning)
        {
            return 0;
        }

        int totalPlayers = 0;

        foreach (PlayerRef activePlayer
                 in runner.ActivePlayers)
        {
            totalPlayers++;
        }

        if (totalPlayers == 0)
            return 0;

        int eliminationOrder =
            GetEliminationOrder(player);

        // Eliminated players already have
        // a known relative placement.
        if (eliminationOrder > 0)
        {
            return totalPlayers -
                   eliminationOrder +
                   1;
        }

        // Only one player remains alive.
        if (EliminationCount ==
            totalPlayers - 1)
        {
            return 1;
        }

        // Multiple players are still alive.
        // Exact placement is not known yet.
        return 0;
    }

    private void OnEliminationSequenceChanged()
    {
        OnEliminationChanged?.Invoke();
    }
}