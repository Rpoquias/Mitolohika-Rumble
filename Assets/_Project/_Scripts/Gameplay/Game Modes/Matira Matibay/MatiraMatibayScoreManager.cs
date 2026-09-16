using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class MatiraMatibayScoreManager : NetworkBehaviour
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

    [Header("Knockout Settings")]
    [SerializeField] private float knockoutAttributionWindow = 3f;

    [Header("Round State")]
    [SerializeField] private RoundStateManager roundStateManager;

    private readonly List<PlayerScore> playerScores =
        new List<PlayerScore>();

    private PlayerRegistry playerRegistry;
    private NetworkRunner runner;

    private float survivalTimer;
    private bool scoringActive = false;
    private bool initialized = false;

    private void OnEnable()
    {
        if (roundStateManager != null)
        {
            roundStateManager.OnRoundStarted += StartScoring;
            roundStateManager.OnRoundEnded += StopScoring;
            roundStateManager.OnRoundReset += ResetScores;
        }
    }

private void Update()
{
    if (!initialized)
    {
        TryInitialize();
    }
}  private void TryInitialize()
{
    runner =
        NetworkRunner.GetRunnerForGameObject(gameObject);

    if (runner == null || !runner.IsRunning)
        return;

    playerRegistry =
        runner.GetComponentInChildren<PlayerRegistry>();

    if (playerRegistry == null)
        return;

    if (roundStateManager == null)
        return;

    if (!roundStateManager.IsSpawned)
        return;

    Debug.Log(
        $"[SCORE] Connected to registry for Runner " +
        $"{runner.name}. Existing players: " +
        $"{playerRegistry.Players.Count}"
    );

    playerRegistry.OnPlayerRegistered += RegisterPlayer;
    playerRegistry.OnPlayerUnregistered += UnregisterPlayer;

    foreach (NetworkObject playerObject in playerRegistry.Players)
    {
        RegisterPlayer(playerObject);
    }

    initialized = true;

    Debug.Log(
        $"[SCORE] Initialization complete. " +
        $"Tracking {playerScores.Count} players."
    );

    if (roundStateManager.CurrentState ==
        RoundStateManager.RoundState.Playing)
    {
        StartScoring();
    }
}
public override void FixedUpdateNetwork()
{
    if (!HasStateAuthority)
        return;

    if (!scoringActive)
        return;

    survivalTimer += Runner.DeltaTime;

    int currentSecond =
        Mathf.FloorToInt(survivalTimer);

    foreach (PlayerScore score in playerScores)
    {
        if (score.player == null)
            continue;

        if (!score.player.IsEliminated)
        {
            score.survivalScore = currentSecond;
        }
    }
}
    private void OnDestroy()
    {
        if (playerRegistry != null)
        {
            playerRegistry.OnPlayerRegistered -= RegisterPlayer;
            playerRegistry.OnPlayerUnregistered -= UnregisterPlayer;
        }

        if (roundStateManager != null)
        {
            roundStateManager.OnRoundStarted -= StartScoring;
            roundStateManager.OnRoundEnded -= StopScoring;
            roundStateManager.OnRoundReset -= ResetScores;
        }

        foreach (PlayerScore score in playerScores)
        {
            UnsubscribeFromPlayer(score.player);
        }
    }

    private void RegisterPlayer(NetworkObject playerObject)
    {
        if (playerObject == null)
            return;

        PlayerElimination player =
            playerObject.GetComponent<PlayerElimination>();

        if (player == null)
        {
            Debug.LogWarning(
                $"[SCORE] {playerObject.name} has no " +
                "PlayerElimination component."
            );

            return;
        }

        // Prevent duplicate PlayerScore entries.
        if (GetScore(player) != null)
            return;

        PlayerScore newScore = new PlayerScore
        {
            player = player,
            survivalScore = 0,
            knockoutCredit = 0,
            lastKnockbackTime = -Mathf.Infinity,
            lastKnockbackSource = null
        };

        playerScores.Add(newScore);

        PlayerBumpAttack bumpAttack =
            playerObject.GetComponent<PlayerBumpAttack>();

        if (bumpAttack != null)
        {
            bumpAttack.OnKnockbackApplied += RecordKnockback;
        }

        player.OnPlayerEliminated += RecordElimination;

        Debug.Log(
            $"[SCORE] Tracking {playerObject.name}. " +
            $"Total score records: {playerScores.Count}"
        );
    }

    private void UnregisterPlayer(NetworkObject playerObject)
    {
        if (playerObject == null)
            return;

        PlayerElimination player =
            playerObject.GetComponent<PlayerElimination>();

        if (player == null)
            return;

        PlayerScore score = GetScore(player);

        if (score == null)
            return;

        UnsubscribeFromPlayer(player);

        playerScores.Remove(score);

        Debug.Log(
            $"[SCORE] Stopped tracking {playerObject.name}. " +
            $"Total score records: {playerScores.Count}"
        );
    }

    private void UnsubscribeFromPlayer(
        PlayerElimination player)
    {
        if (player == null)
            return;

        PlayerBumpAttack bumpAttack =
            player.GetComponent<PlayerBumpAttack>();

        if (bumpAttack != null)
        {
            bumpAttack.OnKnockbackApplied -= RecordKnockback;
        }

        player.OnPlayerEliminated -= RecordElimination;
    }

    public void StartScoring()
    {
        ResetScores();
        scoringActive = true;

        Debug.Log("[SCORE] Scoring started.");
    }

    public void StopScoring()
    {
        scoringActive = false;

        Debug.Log("[SCORE] Scoring stopped.");
    }

  public void RecordKnockback(
    PlayerElimination attacker,
    PlayerElimination victim)
{
    if (!HasStateAuthority)
        return;

    if (!scoringActive)
        return;

    PlayerScore victimScore =
        GetScore(victim);

    if (victimScore == null)
        return;

    victimScore.lastKnockbackSource = attacker;
    victimScore.lastKnockbackTime =
        (float)Runner.SimulationTime;

    Debug.Log(
        attacker.gameObject.name +
        " knocked back " +
        victim.gameObject.name
    );
}
   public void ResetScores()
{
    if (!HasStateAuthority)
        return;

    scoringActive = false;
    survivalTimer = 0f;

    foreach (PlayerScore score in playerScores)
    {
        score.survivalScore = 0;
        score.knockoutCredit = 0;
        score.lastKnockbackTime =
            -Mathf.Infinity;
        score.lastKnockbackSource = null;
    }

    Debug.Log(
        $"[SCORE] Scores reset for " +
        $"{playerScores.Count} players."
    );
}

    public void RecordElimination(
        PlayerElimination eliminatedPlayer)
    {
          if (!HasStateAuthority)
        return;
        if (!scoringActive)
            return;

        PlayerScore victimScore =
            GetScore(eliminatedPlayer);

        if (victimScore == null)
            return;

        PlayerElimination attacker =
            victimScore.lastKnockbackSource;

        if (attacker != null)
        {
        float timeSinceKnockback =
    (float)Runner.SimulationTime -
    victimScore.lastKnockbackTime;

            if (timeSinceKnockback <=
                knockoutAttributionWindow)
            {
                PlayerScore attackerScore =
                    GetScore(attacker);

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

        // Clear attribution after elimination.
        victimScore.lastKnockbackSource = null;
        victimScore.lastKnockbackTime =
            -Mathf.Infinity;
    }

    public int GetSurvivalScore(
        PlayerElimination player)
    {
        PlayerScore score =
            GetScore(player);

        return score != null
            ? score.survivalScore
            : 0;
    }

    public int GetKnockoutCredit(
        PlayerElimination player)
    {
        PlayerScore score =
            GetScore(player);

        return score != null
            ? score.knockoutCredit
            : 0;
    }

    private PlayerScore GetScore(
        PlayerElimination player)
    {
        foreach (PlayerScore score in playerScores)
        {
            if (score.player == player)
                return score;
        }

        return null;
    }
}