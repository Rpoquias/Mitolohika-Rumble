
using TMPro;
using UnityEngine;

public class AgawanBandilaEventUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AgawanBandilaEventVFX eventVFX;

    [Tooltip("The panel to show while an event is active.")]
    [SerializeField] private GameObject eventPanel;

    [SerializeField] private TMP_Text eventText;

    [Header("Event Colors")]
    [SerializeField] private Color speedSurgeColor =
        new Color(0.26f, 0.65f, 0.96f);

    [SerializeField] private Color slowMotionColor =
        new Color(0.94f, 0.29f, 0.29f);

    [SerializeField] private Color blackHoleColor =
        new Color(0.67f, 0.36f, 0.96f);

    private void OnEnable()
    {
        if (eventVFX != null)
            eventVFX.EventChanged += HandleEventChanged;

        // Handle the case where this UI enables after an event has started.
        RefreshCurrentEvent();
    }

    private void OnDisable()
    {
        if (eventVFX != null)
            eventVFX.EventChanged -= HandleEventChanged;

        HideEvent();
    }

    private void HandleEventChanged(int eventId)
    {
        DisplayEvent(eventId);
    }

    private void RefreshCurrentEvent()
    {
        if (eventVFX == null)
        {
            HideEvent();
            return;
        }

        DisplayEvent(eventVFX.CurrentEventId);
    }

    private void DisplayEvent(int eventId)
    {
        if (eventPanel == null || eventText == null)
            return;

        switch (eventId)
        {
            case 1:
                SetEventDisplay("SPEED SURGE", speedSurgeColor);
                break;

            case 2:
                SetEventDisplay("SLOW MOTION", slowMotionColor);
                break;

            case 3:
                SetEventDisplay("GRAVITATIONAL SINGULARITY", blackHoleColor);
                break;

            default:
                HideEvent();
                break;
        }
    }

    private void SetEventDisplay(string message, Color color)
    {
        if (eventText.text != message)
            eventText.text = message;

        if (eventText.color != color)
            eventText.color = color;

        if (!eventPanel.activeSelf)
            eventPanel.SetActive(true);
    }

    private void HideEvent()
    {
        if (eventPanel != null && eventPanel.activeSelf)
            eventPanel.SetActive(false);
    }
}
