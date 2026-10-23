
using Fusion;
using UnityEngine;

public class StunAbility : CharacterAbility
{
    [Header("Stun")]
    [SerializeField] private float stunDuration = 1.5f;

    [Header("Wind-up")]
    [SerializeField, Min(0f)] private float windupSeconds = 0.4f;

    [Networked]
    private TickTimer WindupTimer { get; set; }

    [Networked]
    private NetworkBool IsCasting { get; set; }

    public override bool CanActivate()
    {
        return base.CanActivate() && !IsCasting;
    }

    public override void Activate()
    {
        if (!HasStateAuthority || !CanActivate())
            return;

        IsCasting = true;

        WindupTimer = TickTimer.CreateFromSeconds(
            Runner,
            windupSeconds
        );

        // Play the ability animation on all peers.
        NotifyActivated();

        // Cooldown begins when casting starts.
        StartCooldown();
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!HasStateAuthority || !IsCasting)
            return;

        if (!WindupTimer.Expired(Runner))
            return;

        IsCasting = false;
        WindupTimer = TickTimer.None;

        PlayerMovement target = FindBestTarget();

        if (target == null)
            return;

        target.ApplyStun(stunDuration);
        Audio?.Play(SFXID.AswangStun);
    }
}
