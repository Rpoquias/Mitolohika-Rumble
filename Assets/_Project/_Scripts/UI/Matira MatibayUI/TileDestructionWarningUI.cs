
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

    private void Update()
    {
        if (arenaShrinkController == null)
            return;

        if (arenaShrinkController.Object == null ||
            !arenaShrinkController.Object.IsValid)
        {
            warningText.gameObject.SetActive(false);
            return;
        }

        UpdateWarning();
    }

    private void UpdateWarning()
    {
        if (warningText == null)
            return;

        // Prioritize the imminent crumbling tile warning.
        if (arenaShrinkController.IsCrumbleWarningActive)
        {
            int countdown = Mathf.CeilToInt(
                arenaShrinkController.RemainingCrumbleWarningTime
            );

            warningText.text = $"THIS TILE WILL CRUMBLE IN {countdown}";
            warningText.gameObject.SetActive(true);
            return;
        }

        // Otherwise, show the arena-shrink warning.
        if (arenaShrinkController.IsShrinkWarningActive)
        {
            int countdown = Mathf.CeilToInt(
                arenaShrinkController.RemainingShrinkTime
            );

            warningText.text = $"TILES WILL DISAPPEAR IN {countdown}";
            warningText.gameObject.SetActive(true);
            return;
        }

        warningText.gameObject.SetActive(false);
    }
}
