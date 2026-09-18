using Fusion;
using UnityEngine;

public class NetworkPlayerState : NetworkBehaviour
{
    [Networked]
    public CharacterID SelectedCharacter { get; set; }

    [Networked]
    public NetworkBool IsReady { get; set; }

    public bool IsInitialized { get; private set; }

    // True only while this object is actually spawned and safe to read from.
    public bool IsValid =>
        IsInitialized && Object != null && Object.IsValid;

    public override void Spawned()
    {
        IsInitialized = true;

        if (Runner.IsServer && HasInputAuthority)
        {
            IsReady = true;

            Debug.Log(
                $"[NETWORK PLAYER STATE] HOST READY SET | " +
                $"Player: {Object.InputAuthority} | " +
                $"IsReady: {IsReady}"
            );
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        IsInitialized = false;
    }

    public void RequestCharacterChange(CharacterID characterID)
    {
        if (!HasInputAuthority)
            return;

        RPC_RequestCharacterChange(Object.InputAuthority, characterID);
    }

    public void ToggleReady()
    {
        if (!HasInputAuthority)
            return;

        RPC_ToggleReady();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestCharacterChange(
        PlayerRef requestingPlayer,
        CharacterID characterID)
    {
        if (requestingPlayer != Object.InputAuthority)
            return;

        if (SelectedCharacter == characterID)
            return;

        PlayerSpawner spawner = FindAnyObjectByType<PlayerSpawner>();

        if (spawner == null)
            return;

        spawner.ChangeCharacter(requestingPlayer, characterID);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ToggleReady()
    {
        IsReady = !IsReady;
    }
}