using TMPro;
using UnityEngine;
using Fusion;

public class MatchResultCardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text placementText;
    [SerializeField] private TMP_Text playerText;
    [SerializeField] private TMP_Text infoText;

    public void Setup(
        int placement,
        PlayerRef player,
        string detail)
    {
        placementText.text =
            GetPlacementText(placement);

        playerText.text =
            player.ToString();

        infoText.text =
            detail;
    }

    private string GetPlacementText(
        int placement)
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