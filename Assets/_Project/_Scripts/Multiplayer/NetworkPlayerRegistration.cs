using Fusion;
using UnityEngine;

public class NetworkPlayerRegistration : NetworkBehaviour
{
    private PlayerRegistry registry;

    public override void Spawned()
    {
        if (Runner == null)
        {
            Debug.LogError(
                $"[PLAYER REGISTRATION] Runner is null for {name}."
            );
            return;
        }

        registry =
            Runner.GetComponentInChildren<PlayerRegistry>();

        if (registry == null)
        {
            Debug.LogError(
                $"[PLAYER REGISTRATION] No PlayerRegistry found " +
                $"for Runner {Runner.name}."
            );
            return;
        }

        registry.RegisterPlayer(Object);

        Debug.Log(
            $"[PLAYER REGISTRATION] {name} registered " +
            $"with Runner {Runner.name}."
        );
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (registry == null)
            return;

        registry.UnregisterPlayer(Object);

        registry = null;
    }
}