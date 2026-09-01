using System.Collections.Generic;
using UnityEngine;

public class MatiraMatibayScoreManager : MonoBehaviour
{
    [System.Serializable]
    public class PlayerScore
    {
        public PlayerElimination player;
        public int survivalScore;
        public int knockoutCredit;

        [HideInInspector]
        public float lastKnockbackTime = -Mathf.Infinity;
        

        [HideInInspector]
        public PlayerElimination lastKnockbackSource;
    }
    private float survivalTimer;

    [Header("Players")]
    [SerializeField] private List<PlayerScore> playerScores;

    [Header("Knockout Settings")]
    [SerializeField] private float knockoutAttributionWindow = 3f;
    
    [Header("Round State")]
[SerializeField] private RoundStateManager roundStateManager;

    private bool scoringActive = false;


private void OnEnable()
{
    if (roundStateManager != null)
    {
        roundStateManager.OnRoundStarted += StartScoring;
        roundStateManager.OnRoundEnded += StopScoring;
        roundStateManager.OnRoundReset += ResetScores;
    }

    foreach (PlayerScore score in playerScores)
    {
        PlayerBumpAttack bumpAttack =
            score.player.GetComponent<PlayerBumpAttack>();

        if (bumpAttack != null)
        {
            bumpAttack.OnKnockbackApplied += RecordKnockback;
        }

        score.player.OnPlayerEliminated += RecordElimination;
    }
}

private void OnDisable()
{
    if (roundStateManager != null)
    {
        roundStateManager.OnRoundStarted -= StartScoring;
        roundStateManager.OnRoundEnded -= StopScoring;
        roundStateManager.OnRoundReset -= ResetScores;
    }

    foreach (PlayerScore score in playerScores)
    {
        PlayerBumpAttack bumpAttack =
            score.player.GetComponent<PlayerBumpAttack>();

        if (bumpAttack != null)
        {
            bumpAttack.OnKnockbackApplied -= RecordKnockback;
        }

        score.player.OnPlayerEliminated -= RecordElimination;
    }
}
private void Update()
{
    if (!scoringActive)
        return;

    survivalTimer += Time.deltaTime;

    int currentSecond = Mathf.FloorToInt(survivalTimer);

    foreach (PlayerScore score in playerScores)
    {
        if (!score.player.IsEliminated)
        {
            score.survivalScore = currentSecond;
        }
    }
}public void StartScoring()
{
    ResetScores();
    scoringActive = true;
}

    public void StopScoring()
    {
        scoringActive = false;
    }

    public void RecordKnockback(
        PlayerElimination attacker,
        PlayerElimination victim)
    {
        if (!scoringActive)
            return;

        PlayerScore victimScore = GetScore(victim);

        if (victimScore == null)
            return;

        victimScore.lastKnockbackSource = attacker;
        victimScore.lastKnockbackTime = Time.time;

        Debug.Log(
            attacker.gameObject.name +
            " knocked back " +
            victim.gameObject.name
        );
    }
public void ResetScores()
{
    scoringActive = false;
    survivalTimer = 0f;

    foreach (PlayerScore score in playerScores)
    {
        score.survivalScore = 0;
        score.knockoutCredit = 0;
        score.lastKnockbackTime = -Mathf.Infinity;
        score.lastKnockbackSource = null;
    }
}
    public void RecordElimination(PlayerElimination eliminatedPlayer)
    {
        if (!scoringActive)
            return;

        PlayerScore victimScore = GetScore(eliminatedPlayer);

        if (victimScore == null)
            return;

        PlayerElimination attacker = victimScore.lastKnockbackSource;

        if (attacker != null)
        {
            float timeSinceKnockback =
                Time.time - victimScore.lastKnockbackTime;

            if (timeSinceKnockback <= knockoutAttributionWindow)
            {
                PlayerScore attackerScore = GetScore(attacker);

                if (attackerScore != null)
                {
                    attackerScore.knockoutCredit++;

                    Debug.Log(
                        attacker.gameObject.name +
                        " earned Knockout Credit!"
                    );
                }
            }
        }

        // Clear attribution after elimination
        victimScore.lastKnockbackSource = null;
        victimScore.lastKnockbackTime = -Mathf.Infinity;
    }
    

    public int GetSurvivalScore(PlayerElimination player)
    {
        PlayerScore score = GetScore(player);

        return score != null ? score.survivalScore : 0;
    }

    public int GetKnockoutCredit(PlayerElimination player)
    {
        PlayerScore score = GetScore(player);

        return score != null ? score.knockoutCredit : 0;
    }

    private PlayerScore GetScore(PlayerElimination player)
    {
        foreach (PlayerScore score in playerScores)
        {
            if (score.player == player)
                return score;
        }

        return null;
    }
    


    
}