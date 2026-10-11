using UnityEngine;

[RequireComponent(typeof(PlayerBumpAttack))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerAudio : MonoBehaviour
{
    private PlayerBumpAttack _bumpAttack;
    private PlayerMovement _movement;

    private void Awake()
    {
        _bumpAttack = GetComponent<PlayerBumpAttack>();
        _movement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        if (_bumpAttack != null)
            _bumpAttack.OnBumpStarted += HandleBumpStarted;

        if (_movement != null)
            _movement.OnHitReceived += HandleHitReceived;
    }

    private void OnDisable()
    {
        if (_bumpAttack != null)
            _bumpAttack.OnBumpStarted -= HandleBumpStarted;

        if (_movement != null)
            _movement.OnHitReceived -= HandleHitReceived;
    }

    private void HandleBumpStarted()
    {
        PlayAt(SFXID.PlayerBump);
    }

    private void HandleHitReceived()
    {
        PlayAt(SFXID.PlayerHit);
    }

    private void PlayAt(SFXID id)
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySFXAtPosition(
            id,
            transform.position
        );
    }
}