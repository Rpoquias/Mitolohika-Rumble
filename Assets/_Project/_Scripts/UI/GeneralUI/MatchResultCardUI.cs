
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class MatchResultCardUI : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private Image placementIcon;
    [SerializeField] private PlacementIconsData placementIcons;

    [Header("Player Details")]
    [SerializeField] private TMP_Text playerText;
    [SerializeField] private TMP_Text infoText;

    public void Setup(
        int placement,
        PlayerRef player,
        string detail)
    {
        Sprite sprite = placementIcons != null
            ? placementIcons.GetRoundIcon(placement)
            : null;

        if (placementIcon != null)
        {
            placementIcon.sprite = sprite;
            placementIcon.enabled = sprite != null;
            placementIcon.gameObject.SetActive(sprite != null);
        }

        if (playerText != null)
        {
            playerText.text = MatchSession.NameOf(player);
            playerText.color = MatchSession.ColorForPlayer(player);
        }

        if (infoText != null)
            infoText.text = detail;
    }
}
