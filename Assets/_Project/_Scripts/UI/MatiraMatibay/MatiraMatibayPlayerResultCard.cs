using TMPro;
using UnityEngine;

public class MatiraMatibayPlayerResultCard : MonoBehaviour
{
    [SerializeField] private TMP_Text placementText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text knockoutText;

    public void SetData(
        string placement,
        string playerName,
        int overallScore,
        int knockoutCredit)
    {
        placementText.text = placement;
        playerNameText.text = playerName;
        scoreText.text = $"SCORE: {overallScore}";
        knockoutText.text = $"KNOCKOUTS: {knockoutCredit}";
    }
}