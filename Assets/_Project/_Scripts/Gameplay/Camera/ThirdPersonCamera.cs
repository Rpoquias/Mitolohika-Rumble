using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    private static readonly Dictionary<NetworkRunner, ThirdPersonCamera> Cameras
    = new Dictionary<NetworkRunner, ThirdPersonCamera>();

    [Header("Camera")]
    [SerializeField] private float distance = 8f;
    [SerializeField] private float height = 5f;
    [SerializeField] private float followSpeed = 10f;

    [Header("Mouse")]
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 60f;

   [Header("Mobile")]
[SerializeField] private float mobileSensitivity = 0.15f;


    public NetworkRunner Runner => runner;


    private NetworkRunner runner;
    private Transform target;
    private Vector2 mobileLook;

    private float yaw;
    private float pitch = 25f;
private void Update()
{
    if (runner == null)
    {
        runner = NetworkRunner.GetRunnerForGameObject(gameObject);

        if (runner == null || !runner.IsRunning)
            return;

        Cameras[runner] = this;

        Debug.Log(
            $"[CAMERA] Found Runner: {runner.name}"
        );
    }

    if (target == null)
    {
        TryFindLocalPlayer();

        if (target == null)
            return;
    }
}
private void OnDestroy()
{
    if (runner != null &&
        Cameras.TryGetValue(runner, out ThirdPersonCamera camera) &&
        camera == this)
    {
        Cameras.Remove(runner);
    }
}

   private void LateUpdate()
{
    if (runner == null || !runner.IsRunning)
        return;

    if (target == null)
        return;

 float sensitivityMultiplier = 1f;

if (MobileSettings.Instance != null)
{
    sensitivityMultiplier =
        MobileSettings.Instance.LookSensitivity;
}

float finalMouseSensitivity =
    mouseSensitivity * sensitivityMultiplier;

float finalMobileSensitivity =
    mobileSensitivity * sensitivityMultiplier;

Vector2 mouseLook =
    lookAction.action.ReadValue<Vector2>();

yaw += mouseLook.x * finalMouseSensitivity;
pitch -= mouseLook.y * finalMouseSensitivity;

yaw += mobileLook.x * finalMobileSensitivity;
pitch -= mobileLook.y * finalMobileSensitivity;
    mobileLook = Vector2.zero;

    pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

    Quaternion rotation =
        Quaternion.Euler(pitch, yaw, 0f);

    Vector3 desiredPosition =
        target.position +
        rotation * new Vector3(0f, 0f, -distance);

    desiredPosition.y += height;

    transform.position = Vector3.Lerp(
        transform.position,
        desiredPosition,
        followSpeed * Time.deltaTime
    );

    transform.LookAt(target.position);
}

public void SetMobileLook(Vector2 look)
{
    mobileLook = look;
}
public static bool TryGetCamera(
    NetworkRunner runner,
    out ThirdPersonCamera camera)
{
    return Cameras.TryGetValue(runner, out camera);
}
public Transform CameraTransform => transform;
    private void TryFindLocalPlayer()
    {
        if (!runner.IsRunning)
            return;

        if (!runner.TryGetPlayerObject(
                runner.LocalPlayer,
                out NetworkObject playerObject))
        {
            return;
        }

        target = playerObject.transform;
    }

    private void OnEnable()
    {
        lookAction.action.Enable();
    }

    private void OnDisable()
    {
        lookAction.action.Disable();
    }
}