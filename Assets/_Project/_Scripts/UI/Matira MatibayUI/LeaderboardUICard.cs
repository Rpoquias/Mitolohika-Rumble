
using Fusion;
using TMPro;
using UnityEngine;

public class LeaderboardUICard : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI placementText;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI infoText;

    public void SetData(StandingEntry entry, string playerName)
    {
        if (playerNameText != null)
            playerNameText.text = playerName;

        if (placementText != null)
        {
            if (entry.Placement > 0)
            {
                placementText.text = GetPlacementText(entry.Placement);
                placementText.gameObject.SetActive(true);
            }
            else
            {
                placementText.text = "";
                placementText.gameObject.SetActive(false);
            }
        }

        if (infoText != null)
        {
            if (!string.IsNullOrEmpty(entry.Status) &&
                !string.IsNullOrEmpty(entry.Detail))
            {
                infoText.text = $"{entry.Status} • {entry.Detail}";
            }
            else if (!string.IsNullOrEmpty(entry.Status))
            {
                infoText.text = entry.Status;
            }
            else if (!string.IsNullOrEmpty(entry.Detail))
            {
                infoText.text = entry.Detail;
            }
            else
            {
                infoText.text = "";
            }
        }
    }

    // Use this overload when you have the player's PlayerRef.
    public void SetData(
        StandingEntry entry,
        string playerName,
        PlayerRef player)
    {
        SetData(entry, playerName);

        if (playerNameText != null)
            playerNameText.color = MatchSession.ColorForPlayer(player);
    }

    public void Clear()
    {
        if (placementText != null)
            placementText.text = "";

        if (playerNameText != null)
            playerNameText.text = "";

        if (infoText != null)
            infoText.text = "";

        gameObject.SetActive(false);
    }

    private string GetPlacementText(int placement)
    {
        switch (placement)
        {
            case 1: return "1ST";
            case 2: return "2ND";
            case 3: return "3RD";
            case 4: return "4TH";
            default: return $"{placement}TH";
        }
    }
}
