using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class SmokeCloud : NetworkBehaviour
{
    [Networked]
    private PlayerRef Owner { get; set; }

    [Header("Smoke")]
    [SerializeField] private float lifetime = 4f;

    private readonly HashSet<PlayerMovement> playersInside = new();

    private TickTimer lifetimeTimer;

    public override void Spawned()
    {
        if (!HasStateAuthority)
            return;

        lifetimeTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                lifetime
            );
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (lifetimeTimer.Expired(Runner))
        {
            ClearPlayersInside();

            Runner.Despawn(Object);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority)
            return;

        PlayerMovement player =
            other.GetComponentInParent<PlayerMovement>();

        if (player == null)
            return;

        // Kapre cannot be affected by its own smoke.
        if (player.Object.InputAuthority == Owner)
            return;

        if (!playersInside.Add(player))
            return;

        SetSmokeEffect(player, true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!HasStateAuthority)
            return;

        PlayerMovement player =
            other.GetComponentInParent<PlayerMovement>();

        if (player == null)
            return;

        if (!playersInside.Remove(player))
            return;

        SetSmokeEffect(player, false);
    }

    private void SetSmokeEffect(
        PlayerMovement player,
        bool active)
    {
        if (player == null)
            return;

        RPC_SetSmokeEffect(
            player.Object.InputAuthority,
            active
        );
    }

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    private void RPC_SetSmokeEffect(
        PlayerRef target,
        bool active)
    {
        if (Runner.LocalPlayer != target)
            return;

        PlayerPresentation presentation =
            FindLocalPlayerPresentation();

        if (presentation == null)
            return;

        presentation.SetSmokeEffect(active);
    }

    private PlayerPresentation FindLocalPlayerPresentation()
    {
        if (PlayerRegistry.Instance == null)
            return null;

        foreach (NetworkObject playerObject
                 in PlayerRegistry.Instance.Players)
        {
            if (playerObject == null)
                continue;

            if (playerObject.InputAuthority != Runner.LocalPlayer)
                continue;

            return playerObject
                .GetComponent<PlayerPresentation>();
        }

        return null;
    }

    private void ClearPlayersInside()
    {
        foreach (PlayerMovement player in playersInside)
        {
            if (player == null)
                continue;

            SetSmokeEffect(player, false);
        }

        playersInside.Clear();
    }

    public void Initialize(PlayerRef owner)
    {
        if (!HasStateAuthority)
            return;

        Owner = owner;
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        if (!HasStateAuthority)
            return;

        playersInside.Clear();
    }
}