using UnityEngine;

public class PowerfulKickAbility : CharacterAbility
{
    [Header("Kick")]
    [SerializeField] private float knockbackForce = 10f;

    public override void Activate()
    {
        if (!CanActivate())
        {
            Debug.Log(
                $"[ABILITY] {GetType().Name} is on cooldown."
            );

            return;
        }

        PlayerMovement target =
            FindBestTarget();

        if (target == null)
        {
            Debug.Log(
                $"[ABILITY] {GetType().Name} activated, " +
                $"but no valid target was found."
            );

            return;
        }

        Debug.Log(
            $"[ABILITY] {GetType().Name} hit {target.name}!"
        );

        ApplyKick(target);

        StartCooldown();
    }

    private void ApplyKick(PlayerMovement target)
    {
        Vector3 direction =
            target.transform.position -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        direction.Normalize();

        target.AddExternalForce(
            direction * knockbackForce
        );
    }
}