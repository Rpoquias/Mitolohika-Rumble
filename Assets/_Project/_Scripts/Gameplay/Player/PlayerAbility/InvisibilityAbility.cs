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
    if (!HasStateAuthority)
        return;

    if (!CanActivate())
    {
        
        return;
    }

    InvisibilityTimer = TickTimer.CreateFromSeconds(
        Runner,
        duration
    );

    Audio?.Play(SFXID.WhiteLadyInvisibility);

    StartCooldown();

}
}