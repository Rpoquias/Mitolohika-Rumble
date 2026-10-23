using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator _animator;

    [Header("Movement")]
    [SerializeField] private string _speedParameter = "Speed";
    [SerializeField] private float _dampTime = 0.1f;

    [Header("Triggers")]
    [SerializeField] private string _bumpTrigger = "Bump";
    [SerializeField] private string _abilityTrigger = "Ability";

    private PlayerMovement _movement;
    private int _speedHash;
    private readonly HashSet<int> _triggerHashes = new();

private void Awake()
{
    _movement = GetComponentInParent<PlayerMovement>();

    if (_animator == null)
        _animator = GetComponentInChildren<Animator>();

    if (_animator == null)
    {
        Debug.LogWarning(
            $"[{nameof(PlayerAnimationController)}] No Animator found on {name}.",
            this
        );

        enabled = false;
        return;
    }

    _animator.keepAnimatorStateOnDisable = true;
    _speedHash = Animator.StringToHash(_speedParameter);

    foreach (var parameter in _animator.parameters)
    {
        if (parameter.type == AnimatorControllerParameterType.Trigger)
            _triggerHashes.Add(parameter.nameHash);
    }
}

    private void Update()
    {
        if (_animator == null || _movement == null) return;

        if (_movement.IsInHitstop)
        {
            _animator.speed = 0f;
            return;
        }

        _animator.speed = 1f;

        float target = _movement.IsMoving ? 1f : 0f;
        _animator.SetFloat(_speedHash, target, _dampTime, Time.deltaTime);
    }

    public bool HasTrigger(string name) =>
        _triggerHashes.Contains(Animator.StringToHash(name));

    public bool PlayTrigger(string name)
    {
        int hash = Animator.StringToHash(name);
        if (!_triggerHashes.Contains(hash) || !_animator.gameObject.activeInHierarchy)
            return false;

        _animator.ResetTrigger(hash); // avoids stale queued triggers
        _animator.SetTrigger(hash);
        return true;
    }

    public bool PlayBumpAnimation() => PlayTrigger(_bumpTrigger);
    public bool PlayAbilityAnimation() => PlayTrigger(_abilityTrigger);
    public bool PlayAbilityAnimation(string name) => PlayTrigger(name);
}