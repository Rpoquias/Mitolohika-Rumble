
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchModeCardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text modeText;
    [SerializeField] private Image modeCardImage;
    [SerializeField] private GameObject highlightObject;

    [Header("Played Mode Appearance")]
    [SerializeField, Range(0f, 1f)]
    private float unavailableBrightness = 0.3f;

    private GameModeData modeData;

    private Color originalTextColor;
    private Color originalCardImageColor;
    private bool originalColorsCached;

    public GameModeData ModeData => modeData;

    private void Awake()
    {
        CacheOriginalColors();
    }

    public void Setup(GameModeData data)
    {
        CacheOriginalColors();

        modeData = data;

        if (data == null)
            return;

        if (modeText != null)
            modeText.text = data.DisplayName;

        if (modeCardImage != null)
        {
            modeCardImage.sprite = data.ModeCardImage;
            modeCardImage.enabled = data.ModeCardImage != null;
        }

        SetHighlighted(false);
        SetAvailable(true);
    }

    private void CacheOriginalColors()
    {
        if (originalColorsCached)
            return;

        if (modeText != null)
            originalTextColor = modeText.color;

        if (modeCardImage != null)
            originalCardImageColor = modeCardImage.color;

        originalColorsCached = true;
    }

    public void SetAvailable(bool available)
    {
        CacheOriginalColors();

        float brightness = available
            ? 1f
            : unavailableBrightness;

        if (modeText != null)
        {
            modeText.color = ApplyBrightness(
                originalTextColor,
                brightness
            );
        }

        if (modeCardImage != null)
        {
            modeCardImage.color = ApplyBrightness(
                originalCardImageColor,
                brightness
            );
        }

        if (!available)
            SetHighlighted(false);
    }

    private Color ApplyBrightness(Color original, float brightness)
    {
        return new Color(
            original.r * brightness,
            original.g * brightness,
            original.b * brightness,
            original.a
        );
    }

    public void SetHighlighted(bool highlighted)
    {
        if (highlightObject != null)
            highlightObject.SetActive(highlighted);
    }

    public void ResetVisual()
    {
        SetHighlighted(false);
    }
}
