using UnityEngine;
using UnityEngine.InputSystem;

public class KapiThirdPersonCamera : MonoBehaviour
{
    public static KapiThirdPersonCamera Instance { get; private set; }

    [Header("Camera")]
    [SerializeField] private float distance = 8f;
    [SerializeField] private float height = 5f;
    [SerializeField] private float followSpeed = 10f;

    [Header("Screen Shake")]
[SerializeField] private float maxShakeOffset = 0.35f;

private float shakeTimer;
private float shakeDuration;
private float shakeMagnitude;

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


[Header("Gameplay Camera Smoothing")]
[SerializeField] private float followSpeedXZ = 15f;
[SerializeField] private float followSpeedY = 6f;


[Header("Camera Collision")]
[SerializeField] private LayerMask collisionLayers = ~0;
[SerializeField, Min(0.01f)] private float cameraCollisionRadius = 0.2f;
[SerializeField, Min(0f)] private float collisionBuffer = 0.1f;

private Vector3 focusPoint;
private bool focusInitialized;
    private bool characterSelectionMode;

    public Transform CurrentTarget => target;
    private Transform target;
    private Vector2 mobileLook;

    private float yaw;
    private float pitch = 25f;


    private void Update()
    {
        if (Instance == null)
            Instance = this;

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
        if (target == null)
        {
            TryFindLocalPlayer();
            if (target == null)
                return;
        }

        if (target == null)
            return;

        if (characterSelectionMode)
        {
            UpdateCharacterSelectionCamera();
            return;
        }

        UpdateGameplayCamera();
    }


private Vector3 ResolveCameraCollision(
    Vector3 focusPosition,
    Vector3 desiredPosition)
{
    Vector3 direction = desiredPosition - focusPosition;
    float distanceToCamera = direction.magnitude;

    if (distanceToCamera <= 0.001f)
        return desiredPosition;

    direction /= distanceToCamera;

    if (Physics.SphereCast(
        focusPosition,
        cameraCollisionRadius,
        direction,
        out RaycastHit hit,
        distanceToCamera,
        collisionLayers,
        QueryTriggerInteraction.Ignore))
    {
        float safeDistance = Mathf.Max(
            0.05f,
            hit.distance - collisionBuffer
        );

        return focusPosition + direction * safeDistance;
    }

    return desiredPosition;
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

    Vector2 mouseLook = Vector2.zero;

    if (lookAction != null && lookAction.action != null)
    {
        mouseLook = lookAction.action.ReadValue<Vector2>();
    }

    yaw += mouseLook.x * finalMouseSensitivity;
    pitch -= mouseLook.y * finalMouseSensitivity;

    yaw += mobileLook.x * finalMobileSensitivity;
    pitch -= mobileLook.y * finalMobileSensitivity;

    mobileLook = Vector2.zero;

    pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

    // Smooth the point the camera follows, not the camera itself.
    Vector3 targetPosition = target.position;

    if (!focusInitialized)
    {
        focusPoint = targetPosition;
        focusInitialized = true;
    }

    float smoothingXZ =
        1f - Mathf.Exp(-followSpeedXZ * Time.deltaTime);

    float smoothingY =
        1f - Mathf.Exp(-followSpeedY * Time.deltaTime);

    focusPoint.x = Mathf.Lerp(
        focusPoint.x,
        targetPosition.x,
        smoothingXZ
    );

    focusPoint.y = Mathf.Lerp(
        focusPoint.y,
        targetPosition.y,
        smoothingY
    );

    focusPoint.z = Mathf.Lerp(
        focusPoint.z,
        targetPosition.z,
        smoothingXZ
    );

    Quaternion rotation = Quaternion.Euler(
        pitch,
        yaw,
        0f
    );

    Vector3 desiredPosition =
        focusPoint +
        rotation * new Vector3(0f, 0f, -distance) +
        Vector3.up * height;

        desiredPosition = ResolveCameraCollision(
    focusPoint,
    desiredPosition
);
Vector3 shakeOffset = Vector3.zero;

if (shakeTimer > 0f)
{
    shakeTimer -= Time.deltaTime;

    float intensity = Mathf.Clamp01(shakeTimer / shakeDuration);

    shakeOffset = Random.insideUnitSphere
                  * shakeMagnitude
                  * intensity;
}

transform.position = desiredPosition + rotation * shakeOffset;
transform.LookAt(focusPoint);
}

public void Shake(float duration, float magnitude)
{
    if (characterSelectionMode || duration <= 0f || magnitude <= 0f)
        return;

    // Keep the strongest active shake when impacts overlap.
    if (magnitude >= shakeMagnitude || shakeTimer <= 0f)
    {
        shakeMagnitude = Mathf.Min(magnitude, maxShakeOffset);
        shakeDuration = duration;
        shakeTimer = duration;
    }
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
        out KapiThirdPersonCamera camera)
    {
        camera = Instance != null ? Instance : FindAnyObjectByType<KapiThirdPersonCamera>();
        return camera != null;
    }

    public Transform CameraTransform =>
        transform;

  private void TryFindLocalPlayer()
{
    KapiPlayerMovement player = FindAnyObjectByType<KapiPlayerMovement>();
    if (player == null)
        return;

    Transform cameraTarget = player.transform.Find("CameraTarget");
    target = cameraTarget != null ? cameraTarget : player.transform;
focusInitialized = false;
}
public void SetTarget(Transform newTarget)
{
    if (newTarget == null)
        return;

    target = newTarget;
}
public void ReturnToLocalPlayer()
{
    target = null;
    TryFindLocalPlayer();
    if (target == null)
        return;
    yaw = target.eulerAngles.y;
    pitch = 25f;
    focusInitialized = false;
}

  private void OnEnable()
{
    if (lookAction != null && lookAction.action != null)
        lookAction.action.Enable();
}

private void OnDisable()
{
    if (lookAction != null && lookAction.action != null)
        lookAction.action.Disable();
}

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
