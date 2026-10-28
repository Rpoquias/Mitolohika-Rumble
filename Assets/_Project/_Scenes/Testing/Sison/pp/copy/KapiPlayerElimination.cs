using System;
using UnityEngine;

public class KapiPlayerElimination : MonoBehaviour
{
    public bool IsEliminated { get; private set; }
    public bool IsAlive => !IsEliminated;
    public event Action<KapiPlayerElimination> OnPlayerEliminated;
    public bool IsNetworkInitialized => isActiveAndEnabled;

    public void Eliminate()
    {
        if (IsEliminated)
            return;
        IsEliminated = true;
        ApplyEliminationState();
        DisableGameplay();
        OnPlayerEliminated?.Invoke(this);
    }

    public void ResetPlayer(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        IsEliminated = false;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = spawnPosition;
            rb.rotation = spawnRotation;
            rb.Sleep();
        }
        ApplyEliminationState();
        EnableGameplay();
    }

    private void ApplyEliminationState()
    {
        bool active = !IsEliminated;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            renderer.enabled = active;
        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            collider.enabled = active;
    }

    private void DisableGameplay()
    {
        KapiPlayerMovement movement = GetComponent<KapiPlayerMovement>();
        if (movement != null) movement.enabled = false;
        KapiPlayerBumpAttack bump = GetComponent<KapiPlayerBumpAttack>();
        if (bump != null) bump.enabled = false;
    }

    private void EnableGameplay()
    {
        KapiPlayerMovement movement = GetComponent<KapiPlayerMovement>();
        if (movement != null) movement.enabled = true;
        KapiPlayerBumpAttack bump = GetComponent<KapiPlayerBumpAttack>();
        if (bump != null) bump.enabled = true;
    }
}