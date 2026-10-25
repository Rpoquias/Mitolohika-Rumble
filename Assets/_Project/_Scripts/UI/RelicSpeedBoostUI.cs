
using System.Collections;
using TMPro;
using UnityEngine;

public class RelicSpeedBoostUI : MonoBehaviour
{
    public static RelicSpeedBoostUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text bonusText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Display")]
    [SerializeField, Min(0f)] private float displayDuration = 1.5f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.2f;

    private Coroutine displayRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[RelicSpeedBoostUI] Duplicate instance found.");
            enabled = false;
            return;
        }

        Instance = this;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void ShowBonus(int bonusPercent)
    {
        if (bonusPercent <= 0)
        {
            Hide();
            return;
        }

        bonusText.SetText("+{0}% MOVE SPEED", bonusPercent);

        if (displayRoutine != null)
            StopCoroutine(displayRoutine);

        displayRoutine = StartCoroutine(DisplayRoutine());
    }

    private IEnumerator DisplayRoutine()
    {
        yield return Fade(0f, 1f);

        yield return new WaitForSecondsRealtime(displayDuration);

        yield return Fade(1f, 0f);

        displayRoutine = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            canvasGroup.alpha = Mathf.Lerp(
                from,
                to,
                Mathf.Clamp01(elapsed / fadeDuration)
            );

            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private void Hide()
    {
        if (displayRoutine != null)
        {
            StopCoroutine(displayRoutine);
            displayRoutine = null;
        }

        canvasGroup.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
