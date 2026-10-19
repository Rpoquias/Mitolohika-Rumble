using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Default")]
    [SerializeField] private Button defaultButton;

    private AudioManager audioManager;

    private void Start()
    {
        audioManager = AudioManager.Instance;

        if (audioManager == null)
        {
            enabled = false;
            return;
        }

        SetupSliders();

        masterVolumeSlider.onValueChanged.AddListener(
            OnMasterVolumeChanged
        );

        musicVolumeSlider.onValueChanged.AddListener(
            OnMusicVolumeChanged
        );

        sfxVolumeSlider.onValueChanged.AddListener(
            OnSFXVolumeChanged
        );

        defaultButton.onClick.AddListener(
            OnDefaultClicked
        );
    }

    private void SetupSliders()
    {
        masterVolumeSlider.minValue = 0f;
        masterVolumeSlider.maxValue = 1f;

        musicVolumeSlider.minValue = 0f;
        musicVolumeSlider.maxValue = 1f;

        sfxVolumeSlider.minValue = 0f;
        sfxVolumeSlider.maxValue = 1f;

        masterVolumeSlider.SetValueWithoutNotify(
            PlayerPrefs.GetFloat("MasterVolume", 1f)
        );

        musicVolumeSlider.SetValueWithoutNotify(
            PlayerPrefs.GetFloat("MusicVolume", 1f)
        );

        sfxVolumeSlider.SetValueWithoutNotify(
            PlayerPrefs.GetFloat("SFXVolume", 1f)
        );
    }

    private void OnMasterVolumeChanged(float value)
    {
        audioManager.SetMasterVolume(value);
    }

    private void OnMusicVolumeChanged(float value)
    {
        audioManager.SetMusicVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        audioManager.SetSFXVolume(value);
    }

    private void OnDefaultClicked()
    {
        masterVolumeSlider.SetValueWithoutNotify(1f);
        musicVolumeSlider.SetValueWithoutNotify(1f);
        sfxVolumeSlider.SetValueWithoutNotify(1f);

        audioManager.SetMasterVolume(1f);
        audioManager.SetMusicVolume(1f);
        audioManager.SetSFXVolume(1f);

        audioManager.SaveVolumeSettings();
    }

    private void OnDisable()
    {
        if (audioManager != null)
            audioManager.SaveVolumeSettings();
    }

    private void OnDestroy()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(
                OnMasterVolumeChanged
            );
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(
                OnMusicVolumeChanged
            );
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(
                OnSFXVolumeChanged
            );
        }

        if (defaultButton != null)
        {
            defaultButton.onClick.RemoveListener(
                OnDefaultClicked
            );
        }
    }
}