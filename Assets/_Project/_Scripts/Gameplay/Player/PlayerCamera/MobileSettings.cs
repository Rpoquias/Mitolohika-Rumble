using UnityEngine;

public class MobileSettings : MonoBehaviour
{
    public static MobileSettings Instance { get; private set; }

    private const string JoystickScaleKey = "Mobile_JoystickScale";
    private const string ButtonScaleKey = "Mobile_ButtonScale";
    private const string LookSensitivityKey = "Mobile_LookSensitivity";

    private const float DefaultJoystickScale = 1f;
    private const float DefaultButtonScale = 1f;
    private const float DefaultLookSensitivity = 1f;

    public float JoystickScale { get; private set; } = DefaultJoystickScale;
    public float ButtonScale { get; private set; } = DefaultButtonScale;
    public float LookSensitivity { get; private set; } = DefaultLookSensitivity;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
    }
public void SetJoystickScale(float value)
{
    JoystickScale = value;
    PlayerPrefs.SetFloat(JoystickScaleKey, value);
    PlayerPrefs.Save();
}

public void SetButtonScale(float value)
{
    ButtonScale = value;
    PlayerPrefs.SetFloat(ButtonScaleKey, value);
    PlayerPrefs.Save();
}

public void SetLookSensitivity(float value)
{
    LookSensitivity = value;
    PlayerPrefs.SetFloat(LookSensitivityKey, value);
    PlayerPrefs.Save();
}
    public void ResetToDefaults()
    {
        JoystickScale = DefaultJoystickScale;
        ButtonScale = DefaultButtonScale;
        LookSensitivity = DefaultLookSensitivity;

        PlayerPrefs.DeleteKey(JoystickScaleKey);
        PlayerPrefs.DeleteKey(ButtonScaleKey);
        PlayerPrefs.DeleteKey(LookSensitivityKey);

        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        JoystickScale = PlayerPrefs.GetFloat(
            JoystickScaleKey,
            DefaultJoystickScale
        );

        ButtonScale = PlayerPrefs.GetFloat(
            ButtonScaleKey,
            DefaultButtonScale
        );

        LookSensitivity = PlayerPrefs.GetFloat(
            LookSensitivityKey,
            DefaultLookSensitivity
        );
    }
}