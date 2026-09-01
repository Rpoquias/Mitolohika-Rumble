using System;
using UnityEngine;

public class PlayerElimination : MonoBehaviour
{
    public bool IsEliminated { get; private set; }

    public event Action<PlayerElimination> OnPlayerEliminated;

    private Vector3 startingPosition;
    private Quaternion startingRotation;

    private void Awake()
    {
        startingPosition = transform.position;
        startingRotation = transform.rotation;
    }

    public void Eliminate()
    {
        if (IsEliminated)
            return;

        IsEliminated = true;

        Debug.Log(gameObject.name + " has been eliminated!");

        OnPlayerEliminated?.Invoke(this);

        // Disable player gameplay
        PlayerMovement movement = GetComponent<PlayerMovement>();

        if (movement != null)
            movement.enabled = false;

        PlayerBumpAttack bumpAttack = GetComponent<PlayerBumpAttack>();

        if (bumpAttack != null)
            bumpAttack.enabled = false;
    }

    public void ResetPlayer()
    {
        IsEliminated = false;

        // Reset position and rotation
        transform.position = startingPosition;
        transform.rotation = startingRotation;

        // Reset physics
        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Re-enable gameplay
        PlayerMovement movement = GetComponent<PlayerMovement>();

        if (movement != null)
            movement.enabled = true;

        PlayerBumpAttack bumpAttack = GetComponent<PlayerBumpAttack>();

        if (bumpAttack != null)
            bumpAttack.enabled = true;
    }
}