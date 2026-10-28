using UnityEngine;

public class KapiPowerfulKickAbility : KapiCharacterAbility
{
    [Header("Kick")]
    [SerializeField] private float knockbackForce = 10f;
    [Tooltip("Upward launch velocity applied after hitstop.")]
    [SerializeField] private float upwardLaunchVelocity = 7f;
    [Tooltip("Duration of the impact freeze in seconds.")]
    [SerializeField] private float hitstopSeconds = 0.12f;
    [SerializeField] private float windupSeconds = 0.35f;

    private bool _isKicking;
    private float _kickAt;

    protected override void Update()
    {
        base.Update();
        if (!_isKicking || Time.time < _kickAt)
            return;

        _isKicking = false;
        KapiPlayerMovement target = FindBestTarget();
        if (target == null)
            return;
        ApplyKick(target);
        Audio?.Play(SFXID.TikbalangKick);
    }

    public override bool CanActivate() => base.CanActivate() && !_isKicking;

    public override void Activate()
    {
        if (!CanActivate())
            return;
        _isKicking = true;
        _kickAt = Time.time + windupSeconds;
        NotifyActivated();
        StartCooldown();
    }

    private void ApplyKick(KapiPlayerMovement target)
    {
        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            return;
        direction.Normalize();

        Vector3 launchVelocity = direction * knockbackForce;
        launchVelocity.y = upwardLaunchVelocity;
        target.ApplyKnockback(launchVelocity, hitstopSeconds, HitType.PowerfulKick);

        KapiPlayerMovement attacker = GetComponent<KapiPlayerMovement>();
        if (attacker != null)
            attacker.ApplyHitstop(hitstopSeconds);
    }
}