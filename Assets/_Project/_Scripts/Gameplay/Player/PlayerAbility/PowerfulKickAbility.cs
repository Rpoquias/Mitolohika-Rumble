
using Fusion;
using UnityEngine;

public class PowerfulKickAbility : CharacterAbility
{
    [Header("Kick")]
    [SerializeField] private float knockbackForce = 10f;

    [Tooltip("Upward launch velocity applied after hitstop.")]
    [SerializeField] private float upwardLaunchVelocity = 7f;

    [Tooltip("Duration of the impact freeze in seconds.")]
    [SerializeField] private float hitstopSeconds = 0.12f;

    [SerializeField] private float windupSeconds = 0.35f;

    [Networked]
    private TickTimer WindupTimer { get; set; }

    [Networked]
    private NetworkBool IsKicking { get; set; }

    public override bool CanActivate()
    {
        return base.CanActivate() && !IsKicking;
    }

    public override void Activate()
    {
        if (!HasStateAuthority || !CanActivate())
            return;

        IsKicking = true;

        WindupTimer = TickTimer.CreateFromSeconds(
            Runner,
            windupSeconds
        );

        NotifyActivated();
        StartCooldown();
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!HasStateAuthority || !IsKicking)
            return;

        if (!WindupTimer.Expired(Runner))
            return;

        IsKicking = false;
        WindupTimer = TickTimer.None;

        PlayerMovement target = FindBestTarget();

        if (target == null)
            return;

        ApplyKick(target);

        Audio?.Play(SFXID.TikbalangKick);
    }

  private void ApplyKick(PlayerMovement target)
{
    Vector3 direction =
        target.transform.position - transform.position;

    direction.y = 0f;

    if (direction.sqrMagnitude <= 0.001f)
        return;

    direction.Normalize();

    Vector3 launchVelocity =
        direction * knockbackForce;

    launchVelocity.y = upwardLaunchVelocity;

    target.ApplyKnockback(
        launchVelocity,
        hitstopSeconds,
        HitType.PowerfulKick
    );

    PlayerMovement attacker =
        GetComponent<PlayerMovement>();

    if (attacker != null)
        attacker.ApplyHitstop(hitstopSeconds);

    // Play the kick sound after a successful hit.
    Audio?.Play(SFXID.TikbalangKick);
}
}
