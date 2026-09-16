using System;
using Fusion;
using UnityEngine;

public class PlayerElimination : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnEliminatedChanged))]
    public NetworkBool IsEliminated { get; private set; }

    public PlayerRef PlayerRef => Object.InputAuthority;

    public bool IsAlive => !IsEliminated;

    public event Action<PlayerElimination> OnPlayerEliminated;

    private Vector3 startingPosition;
    private Quaternion startingRotation;

    public override void Spawned()
    {
        startingPosition = transform.position;
        startingRotation = transform.rotation;

        if (HasStateAuthority)
        {
            IsEliminated = false;
        }

        ApplyEliminationState();

        Debug.Log(
            $"[ELIMINATION] Spawned {name} | " +
            $"PlayerRef: {PlayerRef}"
        );
    }

    public void Eliminate()
    {
        if (IsEliminated)
            return;

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

    public void ResetPlayer()
    {
        if (!HasStateAuthority)
            return;

        IsEliminated = false;

        transform.position = startingPosition;
        transform.rotation = startingRotation;

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        EnableGameplay();
    }

    private void OnEliminatedChanged()
    {
        ApplyEliminationState();

        if (IsEliminated)
        {
            HandleNetworkElimination();
        }
    }

    private void HandleNetworkElimination()
    {
        Debug.Log(
            gameObject.name +
            " has been eliminated!"
        );

        DisableGameplay();

        OnPlayerEliminated?.Invoke(this);
    }

    private void ApplyEliminationState()
    {
        bool active = !IsEliminated;

        foreach (Renderer renderer in
                 GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = active;
        }

        foreach (Collider collider in
                 GetComponentsInChildren<Collider>())
        {
            collider.enabled = active;
        }
    }

    private void DisableGameplay()
    {
        PlayerMovement movement =
            GetComponent<PlayerMovement>();

        if (movement != null)
        {
            movement.enabled = false;
        }

        PlayerBumpAttack bumpAttack =
            GetComponent<PlayerBumpAttack>();

        if (bumpAttack != null)
        {
            bumpAttack.enabled = false;
        }
    }

    private void EnableGameplay()
    {
        PlayerMovement movement =
            GetComponent<PlayerMovement>();

        if (movement != null)
        {
            movement.enabled = true;
        }

        PlayerBumpAttack bumpAttack =
            GetComponent<PlayerBumpAttack>();

        if (bumpAttack != null)
        {
            bumpAttack.enabled = true;
        }
    }
}
