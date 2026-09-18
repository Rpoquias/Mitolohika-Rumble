using Fusion;
using UnityEngine;

public class NetworkPlayerRegistration : NetworkBehaviour
{
    private PlayerRegistry registry;

    public override void Spawned()
    {
        registry = PlayerRegistry.Instance;

        if (registry == null)
        {
            Debug.LogError(
                $"[PLAYER REGISTRATION] PlayerRegistry.Instance is NULL " +
                $"for {name}."
            );
            return;
        }

        registry.RegisterPlayer(Object);

        Debug.Log(
            $"[PLAYER REGISTRATION] {name} registered with PlayerRegistry."
        );
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        if (registry == null)
            return;

        registry.UnregisterPlayer(Object);

        registry = null;
    }
}