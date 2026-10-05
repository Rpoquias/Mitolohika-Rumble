using System;
using Fusion;
using UnityEngine;

public class NetworkPlayerState : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnSelectedCharacterChanged))]
    public CharacterID SelectedCharacter { get; set; }

    [Networked, OnChangedRender(nameof(OnReadyChanged))]
    public NetworkBool IsReady { get; set; }

    public System.Action<NetworkPlayerState> OnUIStateChanged;

    public bool IsInitialized { get; private set; }

    public bool IsValid => IsInitialized && Object != null && Object.IsValid;

    public override void Spawned()
    {
        IsInitialized = true;

        if (Runner.IsServer && HasInputAuthority) IsReady = true;

        Debug.Log($"<color=#FF5252><b>[NETWORK PLAYER STATE] OK</b> - Spawned | Player: {Object.InputAuthority} | Character: {SelectedCharacter} | IsReady: {IsReady}</color>");

        // Initial spawn does not trigger OnChangedRender,
        // so UI that subscribes should call Refresh() once.
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        IsInitialized = false;
        OnUIStateChanged = null;
    }

    private void OnSelectedCharacterChanged() => OnUIStateChanged?.Invoke(this);

    private void OnReadyChanged() => OnUIStateChanged?.Invoke(this);

    public void RequestCharacterChange(CharacterID characterID)
    {
        if (!HasInputAuthority) return;
        RPC_RequestCharacterChange(Object.InputAuthority, characterID);
    }

    public void ToggleReady()
    {
        if (!HasInputAuthority) return;
        RPC_ToggleReady();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestCharacterChange(PlayerRef requestingPlayer, CharacterID characterID)
    {
        if (requestingPlayer != Object.InputAuthority) return;
        if (SelectedCharacter == characterID) return;

        LobbyPlayerSpawner spawner = FindAnyObjectByType<LobbyPlayerSpawner>();
        if (spawner == null) return;

        spawner.ChangeCharacter(requestingPlayer, characterID);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ToggleReady() => IsReady = !IsReady;
}