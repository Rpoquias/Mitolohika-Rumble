using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    private static readonly Dictionary<
        NetworkRunner,
        ThirdPersonCamera> Cameras =
        new Dictionary<NetworkRunner, ThirdPersonCamera>();

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

    [Header("Character Selection Camera")]
    [SerializeField] private float selectionDistance = 6f;
    [SerializeField] private float selectionHeight = 3.5f;
    [SerializeField] private float selectionLookHeight = 1.5f;

    private bool characterSelectionMode;

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
            runner =
                NetworkRunner.GetRunnerForGameObject(gameObject);

            if (runner == null || !runner.IsRunning)
                return;

            Cameras[runner] = this;
        }

        if (target == null)
        {
            TryFindLocalPlayer();

            if (target == null)
                return;

            // If selection was already opened before the
            // player was found, initialize the camera now.
            if (characterSelectionMode)
            {
                yaw = target.eulerAngles.y;
            }
        }
    }

    private void LateUpdate()
    {
        if (runner == null || !runner.IsRunning)
            return;

        if (target == null)
            return;

        if (characterSelectionMode)
        {
            UpdateCharacterSelectionCamera();
            return;
        }

        UpdateGameplayCamera();
    }

    private void UpdateGameplayCamera()
    {
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

        yaw +=
            mouseLook.x *
            finalMouseSensitivity;

        pitch -=
            mouseLook.y *
            finalMouseSensitivity;

        yaw +=
            mobileLook.x *
            finalMobileSensitivity;

        pitch -=
            mobileLook.y *
            finalMobileSensitivity;

        mobileLook = Vector2.zero;

        pitch = Mathf.Clamp(
            pitch,
            minPitch,
            maxPitch
        );

        Quaternion rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );

        Vector3 desiredPosition =
            target.position +
            rotation *
            new Vector3(
                0f,
                0f,
                -distance
            );

        desiredPosition.y += height;

        transform.position =
            Vector3.Lerp(
                transform.position,
                desiredPosition,
                followSpeed *
                Time.deltaTime
            );

        transform.LookAt(target.position);
    }

   private void UpdateCharacterSelectionCamera()
{
    if (target == null)
        return;

    // Place the camera in front of the character.
    Vector3 offset =
        target.forward *
        selectionDistance;

    Vector3 desiredPosition =
        target.position +
        offset;

    desiredPosition.y += selectionHeight;

    transform.position =
        Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

    // Look toward the character's upper body / face.
    Vector3 lookPosition =
        target.position +
        Vector3.up * selectionLookHeight;

    Quaternion desiredRotation =
        Quaternion.LookRotation(
            lookPosition - transform.position
        );

    transform.rotation =
        Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            followSpeed * Time.deltaTime
        );
}

    public void EnterCharacterSelection()
    {
        characterSelectionMode = true;

        pitch = 20f;

        if (target != null)
            yaw = target.eulerAngles.y;
    }

    public void ExitCharacterSelection()
    {
        characterSelectionMode = false;

        if (target != null)
            yaw = target.eulerAngles.y;
    }

    public void SetMobileLook(Vector2 look)
    {
        mobileLook = look;
    }

    public static bool TryGetCamera(
        NetworkRunner runner,
        out ThirdPersonCamera camera)
    {
        return Cameras.TryGetValue(
            runner,
            out camera
        );
    }

    public Transform CameraTransform =>
        transform;

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

    Transform cameraTarget =
        playerObject.transform.Find("CameraTarget");

    if (cameraTarget == null)
    {
        Debug.LogError(
            "[ThirdPersonCamera] CameraTarget not found on local player.");
        return;
    }

    target = cameraTarget;
}

    private void OnEnable()
    {
        lookAction.action.Enable();
    }

    private void OnDisable()
    {
        lookAction.action.Disable();
    }

    private void OnDestroy()
    {
        if (runner != null &&
            Cameras.TryGetValue(
                runner,
                out ThirdPersonCamera camera) &&
            camera == this)
        {
            Cameras.Remove(runner);
        }
    }
}