using System.Linq;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator _animator;

    [Header("Movement Animation")]
    [SerializeField] private string _speedParameter = "Speed";

    private PlayerMovement _movement;
    private int _speedHash;

    private void Awake()
    {
        _movement = GetComponentInParent<PlayerMovement>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        _speedHash = Animator.StringToHash(_speedParameter);
    }

    private void Update()
    {
        if (_animator == null || _movement == null)
            return;

        float speed = _movement.IsMoving ? 1f : 0f;
        _animator.SetFloat(_speedHash, speed);
    }

    public bool HasTrigger(string triggerName)
    {
        if (_animator == null || string.IsNullOrEmpty(triggerName))
            return false;

        int triggerHash = Animator.StringToHash(triggerName);

        return _animator.parameters.Any(parameter =>
            parameter.nameHash == triggerHash &&
            parameter.type == AnimatorControllerParameterType.Trigger);
    }

    public bool PlayAbilityAnimation(string triggerName)
    {
        if (!HasTrigger(triggerName))
            return false;

        _animator.SetTrigger(triggerName);
        return true;
    }
}