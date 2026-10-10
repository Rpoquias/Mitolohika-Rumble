
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class SmokeCloud : NetworkBehaviour
{
    [Networked]
    private PlayerRef Owner { get; set; }

    [Header("Smoke")]
    [SerializeField] private float lifetime = 4f;

    private readonly Dictionary<PlayerStatusEffects, HashSet<Collider>>
        playersInside = new();

    private TickTimer lifetimeTimer;

    public override void Spawned()
    {
        if (!HasStateAuthority)
            return;

        lifetimeTimer = TickTimer.CreateFromSeconds(
            Runner,
            lifetime
        );
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (!lifetimeTimer.Expired(Runner))
            return;

        ClearPlayersInside();

        // The player's networked status persists after this despawn.
        Runner.Despawn(Object);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority)
            return;

        PlayerStatusEffects status =
            other.GetComponentInParent<PlayerStatusEffects>();

        if (status == null || status.Object == null)
            return;

        // Kapre is immune to its own smoke.
        if (status.Object.InputAuthority == Owner)
            return;

        if (!playersInside.TryGetValue(
                status,
                out HashSet<Collider> colliders))
        {
            colliders = new HashSet<Collider>();
            playersInside.Add(status, colliders);
        }

        if (!colliders.Add(other))
            return;

        // One smoke source per player per cloud.
        if (colliders.Count == 1)
            status.AddSmokeSource();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!HasStateAuthority)
            return;

        PlayerStatusEffects status =
            other.GetComponentInParent<PlayerStatusEffects>();

        if (status == null)
            return;

        if (!playersInside.TryGetValue(
                status,
                out HashSet<Collider> colliders))
        {
            return;
        }

        if (!colliders.Remove(other))
            return;

        // Keep the effect until every collider has exited.
        if (colliders.Count > 0)
            return;

        playersInside.Remove(status);
        status.RemoveSmokeSource();
    }

    private void ClearPlayersInside()
    {
        foreach (KeyValuePair<PlayerStatusEffects, HashSet<Collider>>
                 entry in playersInside)
        {
            PlayerStatusEffects status = entry.Key;

            if (status == null || status.Object == null)
                continue;

            // Decrement once for this cloud, not once per collider.
            status.RemoveSmokeSource();
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
        // Safety cleanup for early despawns.
        // Normal expiration already empties the dictionary.
        if (hasState && runner.IsServer)
            ClearPlayersInside();
    }
}
