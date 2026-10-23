
using TMPro;
using UnityEngine;

public class TileDestructionWarningUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ArenaShrinkController arenaShrinkController;

    [Header("Warning UI")]
    [SerializeField]
    private TMP_Text warningText;

    private int lastCountdownValue = -1;

    private void Update()
    {
        if (warningText == null)
            return;

        if (arenaShrinkController == null ||
            arenaShrinkController.Object == null ||
            !arenaShrinkController.Object.IsValid)
        {
            HideWarning();
            return;
        }

        UpdateWarning();
    }

    private void UpdateWarning()
    {
        if (!arenaShrinkController.IsShrinkWarningActive)
        {
            HideWarning();
            return;
        }

        int countdown = Mathf.CeilToInt(
            arenaShrinkController.RemainingShrinkTime
        );

        // Avoid rebuilding the TMP text every frame.
        if (countdown != lastCountdownValue)
        {
            warningText.text = $"TILES WILL DISAPPEAR IN {countdown}";
            lastCountdownValue = countdown;
        }

        if (!warningText.gameObject.activeSelf)
            warningText.gameObject.SetActive(true);
    }

    private void HideWarning()
    {
        if (warningText.gameObject.activeSelf)
            warningText.gameObject.SetActive(false);

        lastCountdownValue = -1;
    }
}