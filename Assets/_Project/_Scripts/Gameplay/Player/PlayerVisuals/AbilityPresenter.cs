using Fusion;
using UnityEngine;

public class AbilityPresenter : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterAbility _ability;
    [SerializeField] private PlayerAnimationController _animationController;
    [SerializeField] private PlayerAudio _playerAudio;

    [Header("Animation")]
    [SerializeField] private string _triggerName = "Ability";

    private ChangeDetector _changeDetector;
    private int _lastActivationCount;

    public override void Spawned()
    {
        if (_ability == null)
            _ability = GetComponent<CharacterAbility>();

        if (_animationController == null)
            _animationController =
                GetComponentInChildren<PlayerAnimationController>();

        if (_playerAudio == null)
            _playerAudio = GetComponent<PlayerAudio>();

        if (_ability == null)
        {
            Debug.LogError(
                "[AbilityPresenter] No CharacterAbility assigned.",
                this
            );

            return;
        }

        _changeDetector = _ability.GetChangeDetector(
            ChangeDetector.Source.SimulationState
        );

        _lastActivationCount = _ability.ActivationCount;
    }
public override void Render()
{
    if (_ability == null ||
        _animationController == null ||
        _changeDetector == null)
    {
        return;
    }

    foreach (var change in _changeDetector.DetectChanges(_ability))
    {
        if (change != nameof(CharacterAbility.ActivationCount))
            continue;

        int currentCount = _ability.ActivationCount;

        if (currentCount == _lastActivationCount)
            continue;

        _lastActivationCount = currentCount;
        _animationController.PlayAbilityAnimation(_triggerName);
    }
}
}