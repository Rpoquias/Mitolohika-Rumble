using UnityEngine;

public class KapiPlayerControlController : MonoBehaviour
{
    public enum ControlMode { Round, FreePlay }

    [Header("Control Mode")]
    [SerializeField] private ControlMode controlMode;

    private RoundStateManager roundStateManager;
    private RoundStateManager.RoundState lastRoundState;
    private bool hasRoundState;
    private bool localUIControlLocked;

    private void Awake()
    {
        if (controlMode == ControlMode.Round)
            roundStateManager = FindAnyObjectByType<RoundStateManager>();
        ApplyControlState(IsGameplayAllowed());
    }

    private void Update()
    {
        if (controlMode != ControlMode.Round || roundStateManager == null)
            return;
        if (!hasRoundState || lastRoundState != roundStateManager.CurrentState)
        {
            lastRoundState = roundStateManager.CurrentState;
            hasRoundState = true;
            ApplyControlState(IsGameplayAllowed());
        }
    }

    public void SetLocalPlayerControlLock(bool locked)
    {
        localUIControlLocked = locked;
        ApplyControlState(IsGameplayAllowed());
    }

    private bool IsGameplayAllowed()
    {
        if (localUIControlLocked)
            return false;
        if (controlMode == ControlMode.FreePlay)
            return true;
        return roundStateManager != null &&
               roundStateManager.CurrentState == RoundStateManager.RoundState.Playing;
    }

    private void ApplyControlState(bool canPlay)
    {
        KapiPlayerMovement[] players = FindObjectsByType<KapiPlayerMovement>(FindObjectsSortMode.None);
        foreach (KapiPlayerMovement movement in players)
        {
            movement.SetCanMove(canPlay);
            KapiPlayerBumpAttack bump = movement.GetComponent<KapiPlayerBumpAttack>();
            if (bump != null) bump.SetCanBump(canPlay);
            KapiCharacterAbility ability = movement.GetComponent<KapiCharacterAbility>();
            if (ability != null) ability.SetCanUseAbility(canPlay);
        }
    }
}