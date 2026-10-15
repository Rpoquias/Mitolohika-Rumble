using UnityEngine;

public class StunAbility : CharacterAbility
{
    [Header("Stun")]
    [SerializeField] private float stunDuration = 1.5f;

    public override void Activate()
    {
        if (!HasStateAuthority)
            return;

        if (!CanActivate())
            return;

   PlayerMovement target = FindBestTarget();

if (target == null)
    return;

target.ApplyStun(stunDuration);

Audio?.Play(SFXID.AswangStun);

StartCooldown();
    }
}