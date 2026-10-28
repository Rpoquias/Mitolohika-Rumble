using UnityEngine;

public class KapiPlayerPresentation : MonoBehaviour
{
    [Header("Hit VFX")]
    [SerializeField] private ParticleSystem stunVfx;
    [SerializeField] private ParticleSystem knockbackVfx;
    [SerializeField] private ParticleSystem powerfulKickVfx;
    [SerializeField] private ParticleSystem confusionVfx;

    [Header("Smoke")]
    [SerializeField] private GameObject smokeOverlay;

    [SerializeField] private KapiPlayerAnimationController animationController;

    private KapiPlayerBumpAttack _bumpAttack;
    private KapiPlayerStatusEffects smokeStatus;
    private KapiPlayerControlEffects controlEffects;
    private KapiPlayerMovement movement;
    private int _lastBumpAnimationCount;
    private int lastHitSequence;
    private bool _hasInitializedBumpAnimation;
    private bool hasInitializedHitSequence;
    private bool lastStunState;
    private bool lastSmokeState;
    private bool lastReversedInputState;

    private void Awake()
    {
        movement = GetComponent<KapiPlayerMovement>();
        smokeStatus = GetComponent<KapiPlayerStatusEffects>();
        controlEffects = GetComponent<KapiPlayerControlEffects>();
        _bumpAttack = GetComponent<KapiPlayerBumpAttack>();
        if (animationController == null)
            animationController = GetComponentInChildren<KapiPlayerAnimationController>();

        if (smokeOverlay != null)
            smokeOverlay.SetActive(false);
    }

    private void Update()
    {
        UpdateStunPresentation();
        UpdateSmokePresentation();
        UpdateConfusionPresentation();
        UpdateHitPresentation();
        UpdateBumpAnimation();
        CleanupVFX(knockbackVfx);
        CleanupVFX(powerfulKickVfx);
    }

    private void UpdateBumpAnimation()
    {
        if (_bumpAttack == null || animationController == null)
            return;
        int count = _bumpAttack.BumpAnimationCount;
        if (!_hasInitializedBumpAnimation)
        {
            _lastBumpAnimationCount = count;
            _hasInitializedBumpAnimation = true;
            return;
        }
        if (count == _lastBumpAnimationCount)
            return;
        _lastBumpAnimationCount = count;
        animationController.PlayBumpAnimation();
    }

    private void UpdateStunPresentation()
    {
        if (movement == null || movement.IsStunned == lastStunState)
            return;
        lastStunState = movement.IsStunned;
        SetParticleActive(stunVfx, lastStunState);
    }

    private void UpdateHitPresentation()
    {
        if (movement == null)
            return;
        int sequence = movement.HitSequence;
        if (!hasInitializedHitSequence)
        {
            lastHitSequence = sequence;
            hasInitializedHitSequence = true;
            return;
        }
        if (sequence == lastHitSequence)
            return;
        lastHitSequence = sequence;

        switch (movement.LastHitType)
        {
            case HitType.Stun:
                PlayVFX(stunVfx);
                break;
            case HitType.Knockback:
                PlayVFX(knockbackVfx);
                ShakeLocalCamera(0.07f, 0.08f);
                break;
            case HitType.PowerfulKick:
                PlayVFX(powerfulKickVfx);
                ShakeLocalCamera(0.18f, 0.28f);
                break;
        }
    }

    private void ShakeLocalCamera(float duration, float magnitude)
    {
        if (KapiThirdPersonCamera.TryGetCamera(out KapiThirdPersonCamera camera))
            camera.Shake(duration, magnitude);
    }

    private void UpdateSmokePresentation()
    {
        bool active = smokeStatus != null && smokeStatus.IsInSmoke;
        if (active == lastSmokeState)
            return;
        lastSmokeState = active;
        if (smokeOverlay != null)
            smokeOverlay.SetActive(active);
    }

    private void UpdateConfusionPresentation()
    {
        bool reversed = controlEffects != null && controlEffects.IsInputReversed;
        if (reversed == lastReversedInputState)
            return;
        lastReversedInputState = reversed;
        SetParticleActive(confusionVfx, reversed);
    }

    private static void PlayVFX(ParticleSystem effect)
    {
        if (effect == null)
            return;
        effect.gameObject.SetActive(true);
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.Play(true);
    }

    private static void SetParticleActive(ParticleSystem effect, bool active)
    {
        if (effect == null)
            return;
        if (active)
        {
            effect.gameObject.SetActive(true);
            if (!effect.isPlaying)
                effect.Play(true);
        }
        else
        {
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.gameObject.SetActive(false);
        }
    }

    private static void CleanupVFX(ParticleSystem effect)
    {
        if (effect != null && effect.gameObject.activeSelf && !effect.IsAlive(true))
            effect.gameObject.SetActive(false);
    }
}