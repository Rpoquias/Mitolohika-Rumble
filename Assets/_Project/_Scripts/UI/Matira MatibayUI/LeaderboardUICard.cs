using TMPro;
using UnityEngine;

public class LeaderboardUICard : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI placementText;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI infoText;

    public void SetData(
        StandingEntry entry,
        string playerName)
    {
        playerNameText.text = playerName;

        // Placement
        if (entry.Placement > 0)
        {
            placementText.text =
                GetPlacementText(entry.Placement);

            placementText.gameObject.SetActive(true);
        }
        else
        {
            placementText.text = "";
            placementText.gameObject.SetActive(false);
        }

        // Status + Detail
        if (!string.IsNullOrEmpty(entry.Status) &&
            !string.IsNullOrEmpty(entry.Detail))
        {
            infoText.text =
                $"{entry.Status} • {entry.Detail}";
        }
        else if (!string.IsNullOrEmpty(entry.Status))
        {
            infoText.text =
                entry.Status;
        }
        else if (!string.IsNullOrEmpty(entry.Detail))
        {
            infoText.text =
                entry.Detail;
        }
        else
        {
            infoText.text = "";
        }
    }

    public void Clear()
    {
        placementText.text = "";
        playerNameText.text = "";
        infoText.text = "";

        gameObject.SetActive(false);
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