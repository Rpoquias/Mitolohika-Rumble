using Fusion;

public class NetworkPlayerState : NetworkBehaviour
{
    [Networked]
    public CharacterID SelectedCharacter { get; set; }

    [Networked]
    public NetworkBool IsReady { get; set; }

    public void RequestCharacterChange(CharacterID characterID)
    {
        if (!HasInputAuthority)
            return;

        RPC_RequestCharacterChange(
            Object.InputAuthority,
            characterID
        );
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

        PlayerSpawner spawner =
            Runner.GetComponentInChildren<PlayerSpawner>();

        if (spawner == null)
            return;

        spawner.ChangeCharacter(
            requestingPlayer,
            characterID
        );
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ToggleReady()
    {
        IsReady = !IsReady;
    }
}