using Fusion;
using UnityEngine;

[RequireComponent(typeof(PlayerBumpAttack))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerAudio : NetworkBehaviour
{
    [Header("Movement SFX")]
    [SerializeField] private SFXID _movementSFX = SFXID.PlayerMove;

    private PlayerMovement _movement;
    private AudioSource _movementSource;

    private bool _isMovementPlaying;

    public override void Spawned()
    {
        _movement = GetComponent<PlayerMovement>();

        if (AudioManager.Instance != null)
        {
            _movementSource =
                AudioManager.Instance.CreateLoopingSFX(
                    _movementSFX,
                    transform
                );
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || _movement == null)
            return;

        bool shouldPlayMovement =
            _movement.CanMove &&
            !_movement.IsBusy &&
            !_movement.IsStunned &&
            _movement.IsGrounded &&
            _movement.IsMoving;

        if (shouldPlayMovement == _isMovementPlaying)
            return;

        _isMovementPlaying = shouldPlayMovement;

        RPC_SetMovementSFX(shouldPlayMovement);
    }

    public void Play(SFXID sfxID)
    {
        if (!HasStateAuthority)
            return;

        RPC_Play(sfxID);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Play(SFXID sfxID)
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySFX(sfxID);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetMovementSFX(bool shouldPlay)
    {
        if (_movementSource == null)
            return;

        if (shouldPlay)
        {
            if (!_movementSource.isPlaying)
                _movementSource.Play();
        }
        else
        {
            if (_movementSource.isPlaying)
                _movementSource.Stop();
        }
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        StopMovementSFX();

        if (_movementSource != null)
        {
            Destroy(_movementSource.gameObject);
            _movementSource = null;
        }
    }

    private void StopMovementSFX()
    {
        if (_movementSource != null)
            _movementSource.Stop();

        _isMovementPlaying = false;
    }
}