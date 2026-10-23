
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CelebrationPlayerCardUI : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private TMP_Text playerNameText;

    [Header("Final Ranking")]
    [SerializeField] private Image finalPlacementIcon;
    [SerializeField] private PlacementIconsData placementIcons;

    [Header("Round Placement Icons")]
    [SerializeField] private Image firstPlaceIcon;
    [SerializeField] private Image secondPlaceIcon;
    [SerializeField] private Image thirdPlaceIcon;
    [SerializeField] private Image fourthPlaceIcon;

    [Header("Round Placement Counts")]
    [SerializeField] private TMP_Text firstPlaceCountText;
    [SerializeField] private TMP_Text secondPlaceCountText;
    [SerializeField] private TMP_Text thirdPlaceCountText;
    [SerializeField] private TMP_Text fourthPlaceCountText;

    public void Setup(
        PlayerRef player,
        MatchPlayerPlacementRecord record,
        int overallPlacement)
    {
        SetPlayerIdentity(
            MatchSession.NameOf(player),
            player
        );

        // Overall ranking uses the bottom-row icon.
        SetIcon(
            finalPlacementIcon,
            placementIcons != null
                ? placementIcons.GetFinalIcon(overallPlacement)
                : null
        );

        // Round statistics use the top-row icons.
        SetIcon(
            firstPlaceIcon,
            placementIcons != null
                ? placementIcons.GetRoundIcon(1)
                : null
        );

        SetIcon(
            secondPlaceIcon,
            placementIcons != null
                ? placementIcons.GetRoundIcon(2)
                : null
        );

        SetIcon(
            thirdPlaceIcon,
            placementIcons != null
                ? placementIcons.GetRoundIcon(3)
                : null
        );

        SetIcon(
            fourthPlaceIcon,
            placementIcons != null
                ? placementIcons.GetRoundIcon(4)
                : null
        );

        // Display only the number of round placements.
        if (firstPlaceCountText != null)
            firstPlaceCountText.text = record.FirstPlaces.ToString();

        if (secondPlaceCountText != null)
            secondPlaceCountText.text = record.SecondPlaces.ToString();

        if (thirdPlaceCountText != null)
            thirdPlaceCountText.text = record.ThirdPlaces.ToString();

        if (fourthPlaceCountText != null)
            fourthPlaceCountText.text = record.FourthPlaces.ToString();
    }

    private void SetPlayerIdentity(
        string playerName,
        PlayerRef player)
    {
        if (playerNameText == null)
            return;

        playerNameText.text = playerName;
        playerNameText.color = MatchSession.ColorForPlayer(player);
    }

    private void SetIcon(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
        image.gameObject.SetActive(sprite != null);
    }
}
