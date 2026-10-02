using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkPlayerInput : NetworkBehaviour, INetworkRunnerCallbacks
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference bumpAction;
    [SerializeField] private InputActionReference abilityAction;

    private NetworkInputData _input;
    private bool _resetButtons;
    private Transform _cameraTransform;

    public override void Spawned()
    {
        if (!HasInputAuthority)
            return;

        moveAction.action.Enable();
        jumpAction.action.Enable();
        bumpAction.action.Enable();
        abilityAction.action.Enable();

        Runner.AddCallbacks(this);
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        if (!HasInputAuthority)
            return;

        moveAction.action.Disable();
        jumpAction.action.Disable();
        bumpAction.action.Disable();
        abilityAction.action.Disable();

        runner.RemoveCallbacks(this);
    }

    private void Update()
    {
        if (!HasInputAuthority)
            return;

        ResetButtons();

        UpdateCameraReference();
        UpdateMovementInput();
        UpdateActionInput();
    }

    private void ResetButtons()
    {
        if (!_resetButtons)
            return;

        _input.Buttons.Set(EInputButton.Jump, false);
        _input.Buttons.Set(EInputButton.Bump, false);
        _input.Buttons.Set(EInputButton.Ability, false);

        _resetButtons = false;
    }

    private void UpdateCameraReference()
    {
        if (_cameraTransform != null)
            return;

        if (ThirdPersonCamera.TryGetCamera(
                Runner,
                out ThirdPersonCamera camera))
        {
            _cameraTransform = camera.CameraTransform;
        }
    }

    private void UpdateMovementInput()
    {
        Vector2 rawInput =
            moveAction.action.ReadValue<Vector2>();

        // Mobile joystick overrides the normal movement input
        // whenever the joystick is actually being used.
        if (MobileInputProvider.Instance != null)
        {
            Vector2 mobileInput =
                MobileInputProvider.Instance.MoveInput;

            if (mobileInput.sqrMagnitude > 0.001f)
            {
                rawInput = mobileInput;
            }
        }

        if (_cameraTransform == null)
        {
            _input.MoveDirection = rawInput;
            return;
        }

        Vector3 cameraForward =
            _cameraTransform.forward;

        Vector3 cameraRight =
            _cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 movement =
            cameraForward * rawInput.y +
            cameraRight * rawInput.x;

        movement =
            Vector3.ClampMagnitude(
                movement,
                1f
            );

        _input.MoveDirection =
            new Vector2(
                movement.x,
                movement.z
            );
    }

    private void UpdateActionInput()
    {
        bool mobileJumpPressed =
            MobileInputProvider.Instance != null &&
            MobileInputProvider.Instance.ConsumeJumpPressed();

        bool mobileBumpPressed =
            MobileInputProvider.Instance != null &&
            MobileInputProvider.Instance.ConsumeBumpPressed();

        bool mobileAbilityPressed =
            MobileInputProvider.Instance != null &&
            MobileInputProvider.Instance.ConsumeAbilityPressed();

        if (jumpAction.action.WasPressedThisFrame() ||
            mobileJumpPressed)
        {
            _input.Buttons.Set(
                EInputButton.Jump,
                true
            );
        }

        if (bumpAction.action.WasPressedThisFrame() ||
            mobileBumpPressed)
        {
            _input.Buttons.Set(
                EInputButton.Bump,
                true
            );
        }

        if (abilityAction.action.WasPressedThisFrame() ||
            mobileAbilityPressed)
        {
            _input.Buttons.Set(
                EInputButton.Ability,
                true
            );
        }
    }

    public void OnInput(
        NetworkRunner runner,
        NetworkInput input)
    {
        input.Set(_input);

        _resetButtons = true;
    }

    // --------------------------------------------------------------------
    // Required callbacks
    // --------------------------------------------------------------------

    public void OnPlayerJoined(
        NetworkRunner runner,
        PlayerRef player) { }

    public void OnPlayerLeft(
        NetworkRunner runner,
        PlayerRef player) { }

    public void OnInputMissing(
        NetworkRunner runner,
        PlayerRef player,
        NetworkInput input) { }

    public void OnShutdown(
        NetworkRunner runner,
        ShutdownReason shutdownReason) { }

    public void OnConnectedToServer(
        NetworkRunner runner) { }

    public void OnDisconnectedFromServer(
        NetworkRunner runner,
        NetDisconnectReason reason) { }

    public void OnConnectRequest(
        NetworkRunner runner,
        NetworkRunnerCallbackArgs.ConnectRequest request,
        byte[] token) { }

    public void OnConnectFailed(
        NetworkRunner runner,
        NetAddress remoteAddress,
        NetConnectFailedReason reason) { }

    public void OnUserSimulationMessage(
        NetworkRunner runner,
        SimulationMessagePtr message) { }

    public void OnSessionListUpdated(
        NetworkRunner runner,
        List<SessionInfo> sessionList) { }

    public void OnCustomAuthenticationResponse(
        NetworkRunner runner,
        Dictionary<string, object> data) { }

    public void OnHostMigration(
        NetworkRunner runner,
        HostMigrationToken hostMigrationToken) { }

    public void OnReliableDataReceived(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        ReadOnlySpan<byte> data) { }

    public void OnReliableDataProgress(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        float progress) { }

    public void OnSceneLoadDone(
        NetworkRunner runner) { }

    public void OnSceneLoadStart(
        NetworkRunner runner) { }

    public void OnObjectEnterAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player) { }

    public void OnObjectExitAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player) { }
}