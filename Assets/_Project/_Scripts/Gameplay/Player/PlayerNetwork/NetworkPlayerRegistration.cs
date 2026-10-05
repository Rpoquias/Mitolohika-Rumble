using Fusion;
using UnityEngine;

public class NetworkPlayerRegistration : NetworkBehaviour
{
    private PlayerRegistry registry;

    public override void Spawned()
    {
        registry = PlayerRegistry.Instance;
        if (registry == null) return;

        registry.RegisterPlayer(Object);

        Debug.Log($"<color=#FF80AB><b>[PLAYER REGISTRATION] OK</b> - {name} registered with PlayerRegistry</color>");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (registry == null) return;

        registry.UnregisterPlayer(Object);
        registry = null;
    }
}