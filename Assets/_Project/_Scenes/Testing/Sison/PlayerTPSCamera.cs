using UnityEngine;

public class PlayerTPScamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Camera")]
    [SerializeField] private float distance = 8f;
    [SerializeField] private float height = 5f;
    [SerializeField] private float followSpeed = 10f;

    [Header("Right Joystick")]
    [SerializeField] private Joystick cameraJoystick;
    [SerializeField] private float cameraSensitivity = 120f;

    [Header("Vertical Rotation")]
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 60f;

    private float yaw;
    private float pitch = 25f;

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Read right joystick
        Vector2 look = new Vector2(
            cameraJoystick.Horizontal,
            cameraJoystick.Vertical
        );

        // Rotate camera
        yaw += look.x * cameraSensitivity * Time.deltaTime;
        pitch -= look.y * cameraSensitivity * Time.deltaTime;

        // Clamp vertical rotation
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Calculate camera position
        Vector3 desiredPosition =
            target.position + rotation * new Vector3(0f, 0f, -distance);

        desiredPosition.y += height;

        // Smooth follow
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        // Look at player
        transform.LookAt(target.position);
    }
}

