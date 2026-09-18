using TMPro;
using UnityEngine;

public class MatiraMatibayUI : MonoBehaviour
{
    [Header("Network UI State")]
    [SerializeField] private MatiraMatibayUIState uiState;

    [Header("Shrink Warning")]
    [SerializeField] private GameObject shrinkWarningPanel;
    [SerializeField] private TMP_Text shrinkWarningText;
    [SerializeField] private TMP_Text shrinkCountdownText;

private void Update()
{
    if (uiState == null)
    {

        return;
    }

    if (!uiState.IsReady)
    {
        return;
    }

    UpdateShrinkWarning();
}
    private void UpdateShrinkWarning()
    {
        bool showWarning =
            uiState.Phase == MatiraMatibayUIPhase.ShrinkWarning;

        shrinkWarningPanel.SetActive(showWarning);

        if (!showWarning)
            return;

        float remainingTime = uiState.GetRemainingTime();

        int countdown =
            Mathf.CeilToInt(remainingTime);

        shrinkWarningText.text =
            "TILES WILL DISAPPEAR IN";

        shrinkCountdownText.text =
            countdown.ToString();
    }
}