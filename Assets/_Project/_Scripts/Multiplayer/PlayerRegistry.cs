using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerRegistry : SimulationBehaviour
{
    private readonly List<NetworkObject> players =
        new List<NetworkObject>();

    public IReadOnlyList<NetworkObject> Players => players;

    public event Action<NetworkObject> OnPlayerRegistered;
    public event Action<NetworkObject> OnPlayerUnregistered;

    public void RegisterPlayer(NetworkObject playerObject)
    {
        if (playerObject == null)
            return;

        if (players.Contains(playerObject))
            return;

        players.Add(playerObject);

        Debug.Log(
            $"[PLAYER REGISTRY] Registered {playerObject.name}. " +
            $"Total players: {players.Count}"
        );

        OnPlayerRegistered?.Invoke(playerObject);
    }

    public void UnregisterPlayer(NetworkObject playerObject)
    {
        if (playerObject == null)
            return;

        if (!players.Remove(playerObject))
            return;

        Debug.Log(
            $"[PLAYER REGISTRY] Unregistered {playerObject.name}. " +
            $"Total players: {players.Count}"
        );

        OnPlayerUnregistered?.Invoke(playerObject);
    }
}