using UnityEngine;

public class PlayerPresentation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    [Header("VFX")]
    [SerializeField] private ParticleSystem stunVfx;

    [Header("Audio")]
    [SerializeField] private AudioClip kickClip;
    [SerializeField] private AudioClip stunClip;

    [Header("Invisibility")]
    [SerializeField] private GameObject characterVisual;
    [SerializeField] private GameObject localInvisibleVisual;

    [Header("Smoke")]
    [SerializeField] private GameObject smokeOverlay;

    private PlayerMovement movement;
    private InvisibilityAbility invisibility;

    private bool lastStunState;
    private bool lastInvisibleState;

    public void SetSmokeEffect(bool active)
    {
        if (smokeOverlay != null)
            smokeOverlay.SetActive(active);
    }
private void Awake()
{
    movement = GetComponent<PlayerMovement>();
    invisibility = GetComponent<InvisibilityAbility>();

    if (smokeOverlay != null)
        smokeOverlay.SetActive(false);

    if (localInvisibleVisual != null)
        localInvisibleVisual.SetActive(false);

    if (characterVisual != null)
        characterVisual.SetActive(true);
}

    private void Update()
    {
        UpdateStunPresentation();
        UpdateInvisibilityPresentation();
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

    private void SetStunPresentation(bool stunned)
    {
        if (stunned)
        {
            if (stunVfx != null)
                stunVfx.Play();

            if (audioSource != null && stunClip != null)
                audioSource.PlayOneShot(stunClip);
        }
        else
        {
            if (stunVfx != null)
                stunVfx.Stop();
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

        if (audioSource != null && kickClip != null)
            audioSource.PlayOneShot(kickClip);
    }
}