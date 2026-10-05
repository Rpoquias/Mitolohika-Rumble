using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchModeCardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text modeText;
    [SerializeField] private Image modeIcon;
    [SerializeField] private Image background;
    [SerializeField] private GameObject highlightObject;

    private GameModeData modeData;

    public GameModeData ModeData =>
        modeData;

    public void Setup(GameModeData data)
    {
        modeData = data;

        if (data == null)
            return;

        if (modeText != null)
        {
            modeText.text = data.DisplayName;
        }

        if (modeIcon != null)
        {
            modeIcon.sprite = data.ModeIcon;
        }

        if (background != null &&
            data.ModeBackground != null)
        {
            background.sprite =
                data.ModeBackground;
        }

        SetHighlighted(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(highlighted);
        }
    }

    public void ResetVisual()
    {
        SetHighlighted(false);
    }
}