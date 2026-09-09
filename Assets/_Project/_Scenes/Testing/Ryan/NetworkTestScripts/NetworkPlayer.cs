using Fusion;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    public PlayerRef PlayerRef => Object.InputAuthority;

    public bool IsLocalPlayer => HasInputAuthority;

    [Networked]
    public int PlayerNumber { get; set; }

    [Networked, OnChangedRender(nameof(OnEliminatedChanged))]
    public NetworkBool IsEliminated { get; set; }

    public bool IsAlive => !IsEliminated;

    private PlayerElimination _playerElimination;

    public override void Spawned()
    {
        _playerElimination = GetComponent<PlayerElimination>();

        if (HasStateAuthority)
        {
            PlayerNumber = Object.InputAuthority.PlayerId;
            IsEliminated = false;
        }

        ApplyEliminationState();

        Debug.Log(
            $"NetworkPlayer spawned | " +
            $"PlayerRef: {PlayerRef} | " +
            $"PlayerNumber: {PlayerNumber} | " +
            $"Local: {IsLocalPlayer}"
        );
    }

    public override void Render()
    {
        ApplyEliminationState();
    }

    public void RequestElimination()
    {
        if (HasStateAuthority)
        {
            SetEliminated();
        }
        else
        {
            RPC_RequestElimination();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestElimination()
    {
        SetEliminated();
    }

    private void SetEliminated()
    {
        if (IsEliminated)
            return;

        IsEliminated = true;
    }

    public void ResetElimination()
    {
        if (!HasStateAuthority)
            return;

        IsEliminated = false;
    }

    private void OnEliminatedChanged()
    {
        if (_playerElimination == null)
            _playerElimination = GetComponent<PlayerElimination>();

        if (_playerElimination == null)
            return;

        if (IsEliminated)
        {
        
        }
    }

    private void ApplyEliminationState()
    {
        bool active = !IsEliminated;

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = active;
        }

        foreach (Collider collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = active;
        }
    }
}