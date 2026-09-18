using TMPro;
using UnityEngine;

public class MatiraMatibayLeaderboardUICard : MonoBehaviour
{
    [Header("Player Information")]
    [SerializeField] private TMP_Text placementText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text scoreText;

    public void SetData(
        string placement,
        string playerName,
        int overallScore)
    {
        placementText.text = placement;
        playerNameText.text = playerName;
        scoreText.text = overallScore.ToString();
    }

    public void Clear()
    {
        placementText.text = "";
        playerNameText.text = "";
        scoreText.text = "";
    }
}