using Fusion;
using UnityEngine;

public class PlayerPresentation : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
  [Header("Hit VFX")]
[SerializeField] private ParticleSystem stunVfx;
[SerializeField] private ParticleSystem knockbackVfx;
[SerializeField] private ParticleSystem confusionVfx;

    [Header("Invisibility")]
    [SerializeField] private GameObject characterVisual;
    [SerializeField] private GameObject localInvisibleVisual;

    [Header("Smoke")]
    [SerializeField] private GameObject smokeOverlay;

private PlayerStatusEffects smokeStatus;
private bool lastSmokeState;
private int lastHitSequence;
private bool hasInitializedHitSequence;
    private PlayerMovement movement;
    private InvisibilityAbility invisibility;

    private bool lastStunState;
    private bool lastInvisibleState;
private PlayerControlEffects controlEffects;
private bool lastReversedInputState;
  private void Awake()
{
    movement = GetComponent<PlayerMovement>();
    invisibility = GetComponent<InvisibilityAbility>();
    smokeStatus = GetComponent<PlayerStatusEffects>();
    controlEffects = GetComponent<PlayerControlEffects>();

    if (smokeOverlay != null)
        smokeOverlay.SetActive(false);

    if (localInvisibleVisual != null)
        localInvisibleVisual.SetActive(false);

    if (characterVisual != null)
        characterVisual.SetActive(true);

        
}

public override void Render()
{
    UpdateHitPresentation();
}
private void UpdateConfusionPresentation()
{
    if (controlEffects == null || controlEffects.Object == null)
        return;

    bool reversed = controlEffects.IsInputReversed;

    if (reversed == lastReversedInputState)
        return;

    lastReversedInputState = reversed;

    if (confusionVfx == null)
        return;

    if (reversed)
    {
        confusionVfx.gameObject.SetActive(true);
        confusionVfx.Play(true);
    }
    else
    {
        confusionVfx.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        confusionVfx.gameObject.SetActive(false);
    }
}
private void Update()
{
    UpdateStunPresentation();
    UpdateInvisibilityPresentation();
    UpdateSmokePresentation();
    UpdateConfusionPresentation();
    UpdateKnockbackVFX();
}
    private void UpdateStunPresentation()
    {
        if (movement == null)
            return;

        bool stunned = movement.IsStunned;

        if (stunned == lastStunState)
            return;

        lastStunState = stunned;

        SetStunPresentation(stunned);
    }


private void UpdateHitPresentation()
{
    if (movement == null || movement.Object == null)
        return;

    int currentHitSequence = movement.HitSequence;

    if (!hasInitializedHitSequence)
    {
        lastHitSequence = currentHitSequence;
        hasInitializedHitSequence = true;
        return;
    }

    if (currentHitSequence == lastHitSequence)
        return;

    lastHitSequence = currentHitSequence;

    Debug.Log(
        $"[PlayerPresentation] {name} received {movement.LastHitType}"
    );

    switch (movement.LastHitType)
    {
        case HitType.Stun:
            PlayVFX(stunVfx, "Stun");
            break;

        case HitType.Knockback:
            PlayVFX(knockbackVfx, "Knockback");
            break;
    }
}
private void PlayVFX(ParticleSystem effect, string effectName)
{
    if (effect == null)
    {
        Debug.LogWarning(
            $"[PlayerPresentation] {effectName} VFX is not assigned!",
            this
        );

        return;
    }

    effect.gameObject.SetActive(true);

    effect.Stop(
        true,
        ParticleSystemStopBehavior.StopEmittingAndClear
    );

    effect.Play(true);

    Debug.Log(
        $"[PlayerPresentation] Playing {effectName} VFX.",
        effect
    );
}
private void UpdateKnockbackVFX()
{
    if (knockbackVfx == null ||
        !knockbackVfx.gameObject.activeSelf)
    {
        return;
    }

    if (!knockbackVfx.IsAlive(true))
    {
        knockbackVfx.gameObject.SetActive(false);
    }
}
private void UpdateSmokePresentation()
{
    if (smokeStatus == null ||
        smokeStatus.Object == null)
    {
        return;
    }

    // Only the affected local player sees the screen overlay.
    bool isLocalPlayer =
        smokeStatus.Object.InputAuthority ==
        smokeStatus.Runner.LocalPlayer;

    bool active =
        isLocalPlayer && smokeStatus.IsInSmoke;

    if (active == lastSmokeState)
        return;

    lastSmokeState = active;

    if (smokeOverlay != null)
        smokeOverlay.SetActive(active);
}

private void SetStunPresentation(bool stunned)
{
    if (stunVfx == null)
        return;

    if (stunned)
    {
        stunVfx.gameObject.SetActive(true);
        stunVfx.Play(true);
    }
    else
    {
        stunVfx.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        stunVfx.gameObject.SetActive(false);
    }
}
    private void UpdateInvisibilityPresentation()
    {
        if (invisibility == null)
            return;

        bool invisible = invisibility.IsInvisible;

        if (invisible == lastInvisibleState)
            return;

        lastInvisibleState = invisible;

        SetInvisibilityPresentation(invisible);
    }

    private void SetInvisibilityPresentation(bool invisible)
    {
        bool isLocalPlayer =
            invisibility.Object.InputAuthority ==
            invisibility.Runner.LocalPlayer;

        if (!invisible)
        {
            if (characterVisual != null)
                characterVisual.SetActive(true);

            if (localInvisibleVisual != null)
                localInvisibleVisual.SetActive(false);

            return;
        }

        if (isLocalPlayer)
        {
            // White Lady can faintly see herself.
            if (characterVisual != null)
                characterVisual.SetActive(false);

            if (localInvisibleVisual != null)
                localInvisibleVisual.SetActive(true);
        }
        else
        {
            // Other players cannot see White Lady.
            if (characterVisual != null)
                characterVisual.SetActive(false);

            if (localInvisibleVisual != null)
                localInvisibleVisual.SetActive(false);
        }
    }

   public void PlayKickEffects()
{
    if (animator != null)
        animator.SetTrigger("Kick");
}
}