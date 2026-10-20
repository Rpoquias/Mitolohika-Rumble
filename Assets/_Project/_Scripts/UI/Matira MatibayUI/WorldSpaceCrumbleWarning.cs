
using TMPro;
using UnityEngine;

public class WorldSpaceCrumbleWarning : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private Transform visualRoot;

    private Camera targetCamera;

    private void LateUpdate()
    {
        UpdateTargetCamera();

        if (targetCamera == null)
            return;

        Transform target = visualRoot != null
            ? visualRoot
            : transform;

        // Face the camera while keeping the text upright.
        Vector3 direction =
            target.position - targetCamera.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            target.rotation =
                Quaternion.LookRotation(direction);
        }
    }

    private void UpdateTargetCamera()
    {
        if (targetCamera != null && targetCamera.isActiveAndEnabled)
            return;

        targetCamera = Camera.main;
    }

public void SetCountdown(float remainingTime)
{
    if (countdownText == null)
        return;

    countdownText.text =
        $"Tiles Crumbling In {Mathf.CeilToInt(remainingTime)}";
}
}
