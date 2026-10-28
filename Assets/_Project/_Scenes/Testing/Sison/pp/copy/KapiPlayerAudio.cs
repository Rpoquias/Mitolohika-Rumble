using UnityEngine;

[RequireComponent(typeof(KapiPlayerBumpAttack))]
[RequireComponent(typeof(KapiPlayerMovement))]
public class KapiPlayerAudio : MonoBehaviour
{
    [Header("Movement SFX")]
    [SerializeField] private SFXID _movementSFX = SFXID.PlayerMove;
    private KapiPlayerMovement _movement;
    private AudioSource _movementSource;
    private bool _isMovementPlaying;

    private void Awake()
    {
        _movement = GetComponent<KapiPlayerMovement>();
        if (AudioManager.Instance != null)
            _movementSource = AudioManager.Instance.CreateLoopingSFX(_movementSFX, transform);
    }

    private void Update()
    {
        if (_movement == null || _movementSource == null)
            return;
        bool shouldPlay =
            _movement.CanMove && !_movement.IsBusy && !_movement.IsStunned &&
            _movement.IsGrounded && _movement.IsMoving;
        if (shouldPlay == _isMovementPlaying)
            return;
        _isMovementPlaying = shouldPlay;
        if (shouldPlay)
        {
            if (!_movementSource.isPlaying)
                _movementSource.Play();
        }
        else if (_movementSource.isPlaying)
        {
            _movementSource.Stop();
        }
    }

    public void Play(SFXID sfxID)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(sfxID);
    }

    private void OnDestroy()
    {
        StopMovementSFX();
        if (_movementSource != null)
            Destroy(_movementSource.gameObject);
    }

    private void StopMovementSFX()
    {
        if (_movementSource != null)
            _movementSource.Stop();
        _isMovementPlaying = false;
    }
}