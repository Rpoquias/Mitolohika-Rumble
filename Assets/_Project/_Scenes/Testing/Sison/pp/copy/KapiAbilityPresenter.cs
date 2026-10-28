using UnityEngine;

public class KapiAbilityPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KapiCharacterAbility _ability;
    [SerializeField] private KapiPlayerAnimationController _animationController;
    [SerializeField] private KapiPlayerAudio _playerAudio;
    [Header("Animation")]
    [SerializeField] private string _triggerName = "Ability";

    private int _lastActivationCount;

    private void Awake()
    {
        if (_ability == null) _ability = GetComponent<KapiCharacterAbility>();
        if (_animationController == null)
            _animationController = GetComponentInChildren<KapiPlayerAnimationController>();
        if (_playerAudio == null) _playerAudio = GetComponent<KapiPlayerAudio>();
        if (_ability != null)
            _lastActivationCount = _ability.ActivationCount;
    }

    private void Update()
    {
        if (_ability == null || _animationController == null)
            return;
        if (_ability.ActivationCount == _lastActivationCount)
            return;
        _lastActivationCount = _ability.ActivationCount;
        _animationController.PlayAbilityAnimation(_triggerName);
    }
}