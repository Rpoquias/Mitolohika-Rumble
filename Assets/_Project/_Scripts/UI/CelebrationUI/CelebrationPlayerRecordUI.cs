using TMPro;
using UnityEngine;

public class CelebrationPlayerCardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text finalPlacementText;

    [Header("Placement Counts")]
    [SerializeField] private TMP_Text firstPlaceCountText;
    [SerializeField] private TMP_Text secondPlaceCountText;
    [SerializeField] private TMP_Text thirdPlaceCountText;
    [SerializeField] private TMP_Text fourthPlaceCountText;

    public void Setup(
        string characterName,
        MatchPlayerPlacementRecord record,
        int overallPlacement)
    {
        if (characterNameText != null)
            characterNameText.text = characterName;

        if (finalPlacementText != null)
            finalPlacementText.text =
                GetPlacementText(overallPlacement);

        if (firstPlaceCountText != null)
            firstPlaceCountText.text =
                $"1ST PLACE: {record.FirstPlaces}";

        if (secondPlaceCountText != null)
            secondPlaceCountText.text =
                $"2ND PLACE: {record.SecondPlaces}";

        if (thirdPlaceCountText != null)
            thirdPlaceCountText.text =
                $"3RD PLACE: {record.ThirdPlaces}";

        if (fourthPlaceCountText != null)
            fourthPlaceCountText.text =
                $"4TH PLACE: {record.FourthPlaces}";
    }

    private string GetPlacementText(int placement)
    {
        switch (placement)
        {
            case 1:
                return "1ST";

            case 2:
                return "2ND";

            case 3:
                return "3RD";

            case 4:
                return "4TH";

            default:
                return $"{placement}TH";
        }
    }
}