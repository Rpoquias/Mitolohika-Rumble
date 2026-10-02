using Fusion;
using UnityEngine;

public class InvisibilityAbility : CharacterAbility
{
    [Header("Invisibility")]
    [SerializeField] private float duration = 3f;

    [Networked]
    private TickTimer InvisibilityTimer { get; set; }

    public bool IsInvisible =>
        !InvisibilityTimer.ExpiredOrNotRunning(Runner);

    public override void Activate()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!CanActivate())
        {
            Debug.Log(
                $"[ABILITY] {GetType().Name} is on cooldown."
            );

            return;
        }

        InvisibilityTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                duration
            );

        StartCooldown();

        Debug.Log(
            $"[ABILITY] {GetType().Name} activated."
        );
    }
}