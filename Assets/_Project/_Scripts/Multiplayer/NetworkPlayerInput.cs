using System;
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

        runner.RemoveCallbacks(this);
    }

private void Update()
{
    if (!HasInputAuthority)
        return;

    // Reset one-frame buttons only after Fusion has consumed them.
    if (_resetButtons)
    {
        _input.Buttons.Set(EInputButton.Jump, false);
        _input.Buttons.Set(EInputButton.Bump, false);

        _resetButtons = false;
    }

    if (_cameraTransform == null)
    {
        if (ThirdPersonCamera.TryGetCamera(
                Runner,
                out ThirdPersonCamera camera))
        {
            _cameraTransform = camera.CameraTransform;
        }
    }

    Vector2 rawInput =
        moveAction.action.ReadValue<Vector2>();

    if (_cameraTransform != null)
    {
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
            Vector3.ClampMagnitude(movement, 1f);

        _input.MoveDirection = new Vector2(
            movement.x,
            movement.z
        );
    }
    else
    {
        _input.MoveDirection = rawInput;
    }

    // Latch the button.
    if (jumpAction.action.WasPressedThisFrame())
    {
        _input.Buttons.Set(
            EInputButton.Jump,
            true
        );
    }

    if (bumpAction.action.WasPressedThisFrame())
    {
        _input.Buttons.Set(
            EInputButton.Bump,
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

    // Required callbacks

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
        System.Collections.Generic.List<SessionInfo> sessionList) { }

    public void OnCustomAuthenticationResponse(
        NetworkRunner runner,
        System.Collections.Generic.Dictionary<string, object> data) { }

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