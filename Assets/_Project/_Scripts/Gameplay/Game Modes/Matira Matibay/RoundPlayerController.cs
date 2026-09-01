using System.Collections.Generic;
using UnityEngine;

public class RoundPlayerController : MonoBehaviour
{
    [Header("Players")]
    [SerializeField] private List<PlayerMovement> players;

    [Header("Round State")]
    [SerializeField] private RoundStateManager roundStateManager;

    private void OnEnable()
    {
        if (roundStateManager == null)
            return;

        roundStateManager.OnRoundStarted += EnablePlayerMovement;
        roundStateManager.OnRoundEnded += DisablePlayerMovement;
    }

    private void OnDisable()
    {
        if (roundStateManager == null)
            return;

        roundStateManager.OnRoundStarted -= EnablePlayerMovement;
        roundStateManager.OnRoundEnded -= DisablePlayerMovement;
    }

    private void Start()
    {
        DisablePlayerMovement();
    }

    private void EnablePlayerMovement()
    {
        SetPlayerMovement(true);
    }

    private void DisablePlayerMovement()
    {
        SetPlayerMovement(false);
    }

    private void SetPlayerMovement(bool canMove)
    {
        foreach (PlayerMovement player in players)
        {
            if (player != null)
                player.SetCanMove(canMove);
        }
    }
}