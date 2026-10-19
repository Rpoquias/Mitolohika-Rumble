using Fusion;
using UnityEngine;

public class PowerfulKickAbility : CharacterAbility
{
    [Header("Kick")]
    [SerializeField] private float knockbackForce = 10f;

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

        // End the windup before applying the effect.
        IsKicking = false;
        WindupTimer = TickTimer.None;

        PlayerMovement target = FindBestTarget();

        if (target == null)
        {
            return;
        }


ApplyKick(target);

GetComponent<PlayerAudio>()?.Play(SFXID.TikbalangKick);
    }

    private void ApplyKick(PlayerMovement target)
    {
        Vector3 direction =
            target.transform.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        direction.Normalize();

        target.AddExternalForce(
            direction * knockbackForce
        );
    }
}