using UnityEngine;
using UnityEngine.UI;

public class MobileSettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider joystickSizeSlider;
    [SerializeField] private Slider buttonSizeSlider;
    [SerializeField] private Slider lookSensitivitySlider;

    [Header("Default")]
    [SerializeField] private Button defaultButton;

    private void Start()
    {
        SetupSliders();

        joystickSizeSlider.onValueChanged.AddListener(OnJoystickSizeChanged);
        buttonSizeSlider.onValueChanged.AddListener(OnButtonSizeChanged);
        lookSensitivitySlider.onValueChanged.AddListener(OnLookSensitivityChanged);

        defaultButton.onClick.AddListener(OnDefaultClicked);
    }

private void SetupSliders()
{
    joystickSizeSlider.minValue = 0.75f;
    joystickSizeSlider.maxValue = 1.5f;

    buttonSizeSlider.minValue = 0.75f;
    buttonSizeSlider.maxValue = 1.5f;

    lookSensitivitySlider.minValue = 0.5f;
    lookSensitivitySlider.maxValue = 2f;

    joystickSizeSlider.value =
        MobileSettings.Instance.JoystickScale;

    buttonSizeSlider.value =
        MobileSettings.Instance.ButtonScale;

    lookSensitivitySlider.value =
        MobileSettings.Instance.LookSensitivity;
}
private void OnJoystickSizeChanged(float value)
{
    MobileSettings.Instance.SetJoystickScale(value);

    if (MobileInputProvider.Instance != null)
    {
        MobileInputProvider.Instance.ApplyJoystickScale(value);
    }
}

private void OnButtonSizeChanged(float value)
{
    MobileSettings.Instance.SetButtonScale(value);

    if (MobileInputProvider.Instance != null)
    {
        MobileInputProvider.Instance.ApplyButtonScale(value);
    }
}
    private void OnLookSensitivityChanged(float value)
    {
        MobileSettings.Instance.SetLookSensitivity(value);
    }

    private void OnDefaultClicked()
    {
        MobileSettings.Instance.ResetToDefaults();

        joystickSizeSlider.value = 1f;
        buttonSizeSlider.value = 1f;
        lookSensitivitySlider.value = 1f;
    }
}