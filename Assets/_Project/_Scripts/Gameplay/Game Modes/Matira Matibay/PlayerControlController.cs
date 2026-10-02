using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerControlController : MonoBehaviour
{
    public enum ControlMode
    {
        Round,
        FreePlay
    }

    [Header("Control Mode")]
    [SerializeField] private ControlMode controlMode;

 private RoundStateManager roundStateManager;

    private readonly Dictionary<
        NetworkObject,
        PlayerMovement> playerMovements = new();

    private bool initialized;
    private NetworkRunner runner;
    private PlayerRegistry playerRegistry;

    private void Update()
    {
        if (!initialized)
        {
            TryInitialize();
        }
    }

    private void TryInitialize()
    {
        runner =
            NetworkRunner.GetRunnerForGameObject(gameObject);

        if (runner == null ||
            !runner.IsRunning)
        {
            return;
        }

        playerRegistry =
            PlayerRegistry.Instance;

        if (playerRegistry == null)
            return;
if (controlMode == ControlMode.Round)
{
    if (roundStateManager == null)
    {
        roundStateManager =
            FindAnyObjectByType<RoundStateManager>();
    }

    if (roundStateManager == null)
    {
        Debug.LogWarning(
            "[PLAYER CONTROL] Round mode requires " +
            "a RoundStateManager."
        );

        return;
    }

    if (!roundStateManager.IsSpawned)
        return;
}
        initialized = true;

        Debug.Log(
            $"[PLAYER CONTROL] Initialized in " +
            $"{controlMode} mode."
        );

        playerRegistry.OnPlayerRegistered +=
            RegisterPlayer;

        playerRegistry.OnPlayerUnregistered +=
            UnregisterPlayer;

        foreach (
            NetworkObject playerObject
            in playerRegistry.Players)
        {
            RegisterPlayer(playerObject);
        }

        if (controlMode == ControlMode.Round)
        {
            roundStateManager.OnStateChanged +=
                HandleRoundStateChanged;

            HandleRoundStateChanged(
                roundStateManager.CurrentState
            );
        }
        else
        {
            ApplyControlState(
                canMove: true,
                canBump: true,
                canUseAbility: true
            );
        }
    }

    private void HandleRoundStateChanged(
        RoundStateManager.RoundState state)
    {
        bool canPlay =
            state ==
            RoundStateManager.RoundState.Playing;

        ApplyControlState(
            canMove: canPlay,
            canBump: canPlay,
            canUseAbility: canPlay
        );
    }

private void RegisterPlayer(NetworkObject playerObject)
{
    if (playerObject == null)
        return;

    PlayerMovement movement =
        playerObject.GetComponent<PlayerMovement>();

    if (movement != null)
        playerMovements[playerObject] = movement;

    if (controlMode == ControlMode.FreePlay)
    {
        ApplyControlStateToPlayer(
            playerObject,
            true,
            true,
            true
        );

        return;
    }

    if (controlMode == ControlMode.Round)
    {
        bool canPlay =
            roundStateManager != null &&
            roundStateManager.CurrentState ==
            RoundStateManager.RoundState.Playing;

        ApplyControlStateToPlayer(
            playerObject,
            canPlay,
            canPlay,
            canPlay
        );
    }
}
private void ApplyControlStateToPlayer(
    NetworkObject playerObject,
    bool canMove,
    bool canBump,
    bool canUseAbility)
{
    if (playerObject == null)
        return;

    PlayerMovement movement =
        playerObject.GetComponent<PlayerMovement>();

    if (movement != null)
        movement.SetCanMove(canMove);

    PlayerBumpAttack bump =
        playerObject.GetComponent<PlayerBumpAttack>();

    if (bump != null)
        bump.SetCanBump(canBump);

    CharacterAbility ability =
        playerObject.GetComponent<CharacterAbility>();

    if (ability != null)
        ability.SetCanUseAbility(
            canUseAbility
        );
}
    private void UnregisterPlayer(
        NetworkObject playerObject)
    {
        if (playerObject == null)
            return;

        playerMovements.Remove(playerObject);
    }

private void ApplyControlState(
    bool canMove,
    bool canBump,
    bool canUseAbility)
{
    foreach (
        NetworkObject playerObject
        in playerRegistry.Players)
    {
        if (playerObject == null)
            continue;

        PlayerMovement movement =
            playerObject.GetComponent<PlayerMovement>();

        if (movement != null)
            movement.SetCanMove(canMove);

        PlayerBumpAttack bump =
            playerObject.GetComponent<PlayerBumpAttack>();

        if (bump != null)
            bump.SetCanBump(canBump);

        CharacterAbility ability =
            playerObject.GetComponent<CharacterAbility>();

        if (ability != null)
            ability.SetCanUseAbility(
                canUseAbility
            );
    }
}
    private void OnDestroy()
    {
        if (roundStateManager != null)
        {
            roundStateManager.OnStateChanged -=
                HandleRoundStateChanged;
        }

        if (playerRegistry != null)
        {
            playerRegistry.OnPlayerRegistered -=
                RegisterPlayer;

            playerRegistry.OnPlayerUnregistered -=
                UnregisterPlayer;
        }
    }
}