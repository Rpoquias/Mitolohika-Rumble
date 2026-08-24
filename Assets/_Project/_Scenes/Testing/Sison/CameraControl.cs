using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class CameraControl : MonoBehaviour
{
    [SerializeField] private float sensitivity = 100f;
    [SerializeField] private float maximumLookAngle = -90f;
    [SerializeField] private float minimumLookAngle = 60f;
    [SerializeField] private Transform playerBody;

    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TextMeshProUGUI sensitivityText;

    float xRotation = 0f;

    void Start()
    {
        // Set default slider value
        sensitivitySlider.value = sensitivity;

        // Update text at start
        UpdateSensitivity(sensitivity);

        // Listen to slider changes
        sensitivitySlider.onValueChanged.AddListener(UpdateSensitivity);
    }

    void Update()
    {
        // Ignore input when using UI
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, maximumLookAngle, minimumLookAngle);

            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            playerBody.Rotate(Vector3.up * mouseX);
        }
    }

    void UpdateSensitivity(float value)
    {
        sensitivity = value;

        // Update UI text
        sensitivityText.text = "Sensitivity: " + value.ToString("F0");
    }
}