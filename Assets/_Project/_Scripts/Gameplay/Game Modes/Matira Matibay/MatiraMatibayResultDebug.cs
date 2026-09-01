using UnityEngine;

public class MatiraMatibayResultDebug : MonoBehaviour
{
    [SerializeField] private MatiraMatibayManager matiraManager;
    [SerializeField] private RoundStateManager roundStateManager;

    private void OnEnable()
    {
        if (roundStateManager != null)
            roundStateManager.OnRoundEnded += DisplayResult;
    }

    private void OnDisable()
    {
        if (roundStateManager != null)
            roundStateManager.OnRoundEnded -= DisplayResult;
    }

    public void DisplayResult()
    {
        MatiraMatibayRoundResult result =
            matiraManager.CurrentResult;

        if (result == null)
        {
            Debug.LogWarning("No round result available.");
            return;
        }

        Debug.Log("===== MATIRA MATIBAY RESULT =====");

        foreach (MatiraMatibayRoundResult.PlayerResult player in result.results)
        {
            Debug.Log(
                player.placement + " Place | " +
                player.player.gameObject.name +
                " | Survival: " +
                player.survivalScore +
                " | Knockouts: " +
                player.knockoutCredit
            );
        }

        Debug.Log("=================================");
    }
}