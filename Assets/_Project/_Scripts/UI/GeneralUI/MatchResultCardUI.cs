
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class MatchResultCardUI : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private Image placementIcon;
    [SerializeField] private PlacementIconsData placementIcons;

    [Header("Character Profile")]
    [SerializeField] private Image characterPortrait;

    [Header("Player Details")]
    [SerializeField] private TMP_Text playerText;
    [SerializeField] private TMP_Text infoText;

    public void Setup(
        int placement,
        PlayerRef player,
        string detail,
        CharacterData characterData)
    {
        // Placement icon
        Sprite placementSprite = placementIcons != null
            ? placementIcons.GetRoundIcon(placement)
            : null;

        if (placementIcon != null)
        {
            placementIcon.sprite = placementSprite;
            placementIcon.gameObject.SetActive(
                placementSprite != null
            );
        }

        // Player name and color
        if (playerText != null)
        {
            playerText.text = MatchSession.NameOf(player);
            playerText.color = MatchSession.ColorForPlayer(player);
        }

        // Character profile art
        Sprite portrait = characterData != null
            ? characterData.characterPortrait
            : null;

        if (characterPortrait != null)
        {
            characterPortrait.sprite = portrait;
            characterPortrait.gameObject.SetActive(
                portrait != null
            );
        }

        // Placement details
        if (infoText != null)
            infoText.text = detail;
    }
}
