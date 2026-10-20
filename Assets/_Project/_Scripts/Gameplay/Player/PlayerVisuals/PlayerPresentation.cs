using Fusion;
using UnityEngine;

public class PlayerPresentation : NetworkBehaviour
{

    [SerializeField] private Target targetIndicator;
    [Header("References")]
    [SerializeField] private Animator animator;
  [Header("Hit VFX")]
[SerializeField] private ParticleSystem stunVfx;
[SerializeField] private ParticleSystem knockbackVfx;
[SerializeField] private ParticleSystem powerfulKickVfx;
[SerializeField] private ParticleSystem confusionVfx;

[Header("Invisibility")]
[SerializeField] private GameObject characterVisual;
[SerializeField] private GameObject localInvisibleVisual;
[SerializeField] private ParticleSystem invisibilityPotionVfx;

private bool hasInitializedInvisibility;
    [Header("Smoke")]
    [SerializeField] private GameObject smokeOverlay;

    [Header("Sirena")]
[SerializeField] private SirenaAbility sirenaAbility;
[SerializeField] private PlayerAnimationController animationController;
[SerializeField] private GameObject waterholeVfx;

[SerializeField] private string diveTrigger = "Dive";
[SerializeField] private string emergeTrigger = "Emerge";

private bool lastDivingState;
private bool lastEmergingState;

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
    sirenaAbility = GetComponent<SirenaAbility>();
    if (animationController == null)
    animationController = GetComponentInChildren<PlayerAnimationController>();
    if (smokeOverlay != null)
        smokeOverlay.SetActive(false);

    if (localInvisibleVisual != null)
        localInvisibleVisual.SetActive(false);

    if (characterVisual != null)
        characterVisual.SetActive(true);

        if (waterholeVfx != null)
    waterholeVfx.SetActive(false);

        
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
    UpdateSirenaPresentation();
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
        ShakeLocalCamera(0.07f, 0.08f);
        break;

    case HitType.PowerfulKick:
        PlayVFX(powerfulKickVfx, "Powerful Kick");
        ShakeLocalCamera(0.18f, 0.28f);
        break;
}
}
private void ShakeLocalCamera(float duration, float magnitude)
{
    if (movement == null || movement.Object == null)
        return;

    // Only shake the camera belonging to the player being hit.
    if (!movement.Object.HasInputAuthority)
        return;

    if (ThirdPersonCamera.TryGetCamera(
            movement.Runner,
            out ThirdPersonCamera camera))
    {
        camera.Shake(duration, magnitude);
    }
}
private void UpdateSirenaPresentation()
{
    if (sirenaAbility == null || sirenaAbility.Object == null)
        return;

    bool diving = sirenaAbility.IsDiving;
    bool emerging = sirenaAbility.IsEmerging;

    // Dive starts.
    if (diving && !lastDivingState)
    {
        if (animationController != null)
            animationController.PlayAbilityAnimation(diveTrigger);

        if (characterVisual != null)
            characterVisual.SetActive(false);

        if (waterholeVfx != null)
            waterholeVfx.SetActive(true);
    }

    // Emergence starts.
    if (emerging && !lastEmergingState)
    {
        if (waterholeVfx != null)
            waterholeVfx.SetActive(false);

        if (characterVisual != null)
            characterVisual.SetActive(true);

        if (animationController != null)
            animationController.PlayAbilityAnimation(emergeTrigger);
    }

    lastDivingState = diving;
    lastEmergingState = emerging;
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
    CleanupVFX(knockbackVfx);
    CleanupVFX(powerfulKickVfx);
}
private void CleanupVFX(ParticleSystem effect)
{
    if (effect == null || !effect.gameObject.activeSelf)
        return;

    if (!effect.IsAlive(true))
        effect.gameObject.SetActive(false);
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
    if (invisibility == null || invisibility.Object == null)
        return;

    bool invisible = invisibility.IsInvisible;

    if (hasInitializedInvisibility &&
        invisible == lastInvisibleState)
    {
        return;
    }

    hasInitializedInvisibility = true;
    lastInvisibleState = invisible;

    SetInvisibilityPresentation(invisible);
}

private void SetInvisibilityPresentation(bool invisible)
{
    if (invisibility == null || invisibility.Object == null)
        return;

    bool isLocalPlayer =
        invisibility.Object.InputAuthority ==
        invisibility.Runner.LocalPlayer;

    // Hide the player's off-screen indicator while invisible.
    if (targetIndicator != null)
        targetIndicator.SetHidden(invisible);

    // Hide the regular character model.
    if (characterVisual != null)
        characterVisual.SetActive(!invisible);

    // Only the White Lady's owner sees the faint local model.
    if (localInvisibleVisual != null)
    {
        localInvisibleVisual.SetActive(
            invisible && isLocalPlayer
        );
    }

    // Keep the existing invisibility potion VFX.
    if (invisibilityPotionVfx != null)
    {
        if (invisible)
        {
            if (!invisibilityPotionVfx.isPlaying)
                invisibilityPotionVfx.Play(true);
        }
        else
        {
            invisibilityPotionVfx.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }
}

   public void PlayKickEffects()
{
    if (animator != null)
        animator.SetTrigger("Kick");
}
}