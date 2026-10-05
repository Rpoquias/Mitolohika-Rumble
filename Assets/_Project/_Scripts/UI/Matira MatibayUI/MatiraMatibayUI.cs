using TMPro;
using UnityEngine;

public class MatiraMatibayUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ArenaShrinkController arenaShrinkController;

    [Header("Shrink Warning")]
    [SerializeField]
    private GameObject shrinkWarningPanel;

    [SerializeField]
    private TMP_Text shrinkWarningText;

    [SerializeField]
    private TMP_Text shrinkCountdownText;

    private void Update()
    {
        if (arenaShrinkController == null)
            return;

        if (arenaShrinkController.Object == null ||
            !arenaShrinkController.Object.IsValid)
        {
            return;
        }

        UpdateShrinkWarning();
    }

    private void UpdateShrinkWarning()
    {
        bool showWarning =
            arenaShrinkController.IsShrinkWarningActive;

        shrinkWarningPanel.SetActive(
            showWarning
        );

        if (!showWarning)
            return;

        float remainingTime =
            arenaShrinkController.RemainingShrinkTime;

        int countdown =
            Mathf.CeilToInt(
                remainingTime
            );

        shrinkWarningText.text =
            "TILES WILL DISAPPEAR IN";

        shrinkCountdownText.text =
            countdown.ToString();
    }
}