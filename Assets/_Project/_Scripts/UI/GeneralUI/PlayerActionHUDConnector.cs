
using Fusion;
using UnityEngine;

public class PlayerActionHUDConnector : NetworkBehaviour
{
    public override void Spawned()
    {
        if (!HasInputAuthority)
            return;

        ConnectHUD();
    }

    private void ConnectHUD()
    {
        PlayerActionHUD hud =
            FindFirstObjectByType<PlayerActionHUD>();

        if (hud == null)
        {
            Debug.LogWarning(
                "[PLAYER ACTION HUD] HUD not found."
            );
            return;
        }

        PlayerMovement movement =
            GetComponent<PlayerMovement>();

        PlayerBumpAttack bump =
            GetComponent<PlayerBumpAttack>();

        CharacterAbility ability =
            GetComponent<CharacterAbility>();

        hud.SetPlayer(movement, bump, ability);

        Debug.Log(
            "[PLAYER ACTION HUD] Connected to local player."
        );
    }
}