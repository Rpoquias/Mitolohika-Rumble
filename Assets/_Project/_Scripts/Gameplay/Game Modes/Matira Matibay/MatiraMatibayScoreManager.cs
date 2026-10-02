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
        public int winnerBonus;

        [HideInInspector]
        public float lastKnockbackTime = -Mathf.Infinity;

        [HideInInspector]
        public PlayerElimination lastKnockbackSource;
    }

    // ---------------------------------
    // Network Score Data
    // ---------------------------------

    public struct NetworkPlayerScore : INetworkStruct
    {
        public PlayerRef player;
        public int placement;
        public int survivalScore;
        public int knockoutCredit;
        public int winnerBonus;
        public int overallScore;
    }

    [Networked, Capacity(4), OnChangedRender(nameof(OnScoresNetworkChanged))]
    private NetworkArray<NetworkPlayerScore> NetworkScores => default;

    // ---------------------------------
    // Settings
    // ---------------------------------

    [Header("Knockout Settings")]
    [SerializeField] private float knockoutAttributionWindow = 3f;

    [Header("Round State")]
    [SerializeField] private RoundStateManager roundStateManager;

    [Header("Placement")]
    [SerializeField] private MatiraMatibayPlacementManager placementManager;

    [Header("Winner Bonus")]
    [SerializeField] private int winnerBonus = 10;

    // ---------------------------------
    // Local Score Data
    // ---------------------------------

    private readonly List<PlayerScore> playerScores = new List<PlayerScore>();

    private PlayerRegistry playerRegistry;
    private NetworkRunner runner;

    private float survivalTimer;
    private bool scoringActive = false;
    private bool initialized = false;
    private int lastSyncedSecond = -1;

    // ---------------------------------
    // Events
    // ---------------------------------

    public System.Action OnScoresChanged;

    // ---------------------------------
    // Network Callbacks
    // ---------------------------------

    private void OnScoresNetworkChanged()
    {
        OnScoresChanged?.Invoke();
    }

    // ---------------------------------
    // Unity
    // ---------------------------------

    private void OnEnable()
    {
        if (roundStateManager != null)
        {
            roundStateManager.OnRoundStarted += StartScoring;
            roundStateManager.OnRoundEnded += StopScoring;
            roundStateManager.OnRoundReset += ResetScores;
            roundStateManager.OnStateChanged += HandleStateChanged;
        }
    }   

    private void HandleStateChanged(RoundStateManager.RoundState state)
    {
        if (initialized)
            return;

        TryInitialize();
    }

    // ---------------------------------
    // Initialization
    // ---------------------------------

    private void TryInitialize()
    {
        runner = NetworkRunner.GetRunnerForGameObject(gameObject);

        if (runner == null || !runner.IsRunning)
            return;

        playerRegistry = PlayerRegistry.Instance;

        if (playerRegistry == null)
            return;

        if (roundStateManager == null || !roundStateManager.IsSpawned)
            return;

        playerRegistry.OnPlayerRegistered += RegisterPlayer;
        playerRegistry.OnPlayerUnregistered += UnregisterPlayer;

        foreach (NetworkObject playerObject in playerRegistry.Players)
        {
            RegisterPlayer(playerObject);
        }

        initialized = true;

        // Visual confirmation log
        Debug.Log("<color=red>[MatiraMatibayScoreManager] Script initialized and running successfully!</color>");

        if (roundStateManager.CurrentState == RoundStateManager.RoundState.Playing)
        {
            StartScoring();
        }
    }

    // ---------------------------------
    // Network Simulation
    // ---------------------------------

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !scoringActive)
            return;

        survivalTimer += Runner.DeltaTime;
        int currentSecond = Mathf.FloorToInt(survivalTimer);

        if (currentSecond == lastSyncedSecond)
            return;

        lastSyncedSecond = currentSecond;

        foreach (PlayerScore score in playerScores)
        {
            if (score.player == null)
                continue;

            if (!score.player.IsEliminated)
            {
                score.survivalScore = currentSecond;
            }
        }

        SyncScores();
    }

    // ---------------------------------
    // Cleanup
    // ---------------------------------

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
            roundStateManager.OnStateChanged -= HandleStateChanged;
        }

        foreach (PlayerScore score in playerScores)
        {
            UnsubscribeFromPlayer(score.player);
        }
    }

    // ---------------------------------
    // Player Registration
    // ---------------------------------

    private void RegisterPlayer(NetworkObject playerObject)
    {
        if (playerObject == null)
            return;

        PlayerElimination player = playerObject.GetComponent<PlayerElimination>();

        if (player == null)
            return;

        if (GetScore(player) != null)
            return;

        PlayerScore newScore = new PlayerScore
        {
            player = player,
            survivalScore = 0,
            knockoutCredit = 0,
            winnerBonus = 0,
            lastKnockbackTime = -Mathf.Infinity,
            lastKnockbackSource = null
        };

        playerScores.Add(newScore);

        PlayerBumpAttack bumpAttack = playerObject.GetComponent<PlayerBumpAttack>();
        if (bumpAttack != null)
        {
            bumpAttack.OnKnockbackApplied += RecordKnockback;
        }

        player.OnPlayerEliminated += RecordElimination;

        if (HasStateAuthority)
        {
            SyncScores();
        }
    }

    private void UnregisterPlayer(NetworkObject playerObject)
    {
        if (playerObject == null)
            return;

        PlayerElimination player = playerObject.GetComponent<PlayerElimination>();
        if (player == null)
            return;

        PlayerScore score = GetScore(player);
        if (score == null)
            return;

        UnsubscribeFromPlayer(player);
        playerScores.Remove(score);

        if (HasStateAuthority)
        {
            SyncScores();
        }
    }

    private void UnsubscribeFromPlayer(PlayerElimination player)
    {
        if (player == null)
            return;

        PlayerBumpAttack bumpAttack = player.GetComponent<PlayerBumpAttack>();
        if (bumpAttack != null)
        {
            bumpAttack.OnKnockbackApplied -= RecordKnockback;
        }

        player.OnPlayerEliminated -= RecordElimination;
    }

    // ---------------------------------
    // Scoring
    // ---------------------------------

    public void StartScoring()
    {
        if (!HasStateAuthority)
            return;

        ResetScores();
        scoringActive = true;
    }

    public void StopScoring()
    {
        if (!HasStateAuthority)
            return;

        scoringActive = false;
        SyncScores();
    }

    // ---------------------------------
    // Knockout Tracking
    // ---------------------------------

    public void RecordKnockback(PlayerElimination attacker, PlayerElimination victim)
    {
        if (!HasStateAuthority || !scoringActive)
            return;

        PlayerScore victimScore = GetScore(victim);
        if (victimScore == null)
            return;

        victimScore.lastKnockbackSource = attacker;
        victimScore.lastKnockbackTime = (float)Runner.SimulationTime;
    }

    public void RecordElimination(PlayerElimination eliminatedPlayer)
    {
        if (!HasStateAuthority || !scoringActive)
            return;

        PlayerScore victimScore = GetScore(eliminatedPlayer);
        if (victimScore == null)
            return;

        PlayerElimination attacker = victimScore.lastKnockbackSource;
        if (attacker != null)
        {
            float timeSinceKnockback = (float)Runner.SimulationTime - victimScore.lastKnockbackTime;

            if (timeSinceKnockback <= knockoutAttributionWindow)
            {
                PlayerScore attackerScore = GetScore(attacker);
                if (attackerScore != null)
                {
                    attackerScore.knockoutCredit++;
                }
            }
        }

        victimScore.lastKnockbackSource = null;
        victimScore.lastKnockbackTime = -Mathf.Infinity;

        SyncScores();
    }

    // ---------------------------------
    // Reset
    // ---------------------------------

    public void ResetScores()
    {
        if (!HasStateAuthority)
            return;

        scoringActive = false;
        survivalTimer = 0f;
        lastSyncedSecond = -1;

        foreach (PlayerScore score in playerScores)
        {
            score.survivalScore = 0;
            score.knockoutCredit = 0;
            score.winnerBonus = 0;
            score.lastKnockbackTime = -Mathf.Infinity;
            score.lastKnockbackSource = null;
        }

        ClearNetworkScores();
    }

    // ---------------------------------
    // Score Access
    // ---------------------------------

    public int GetOverallScore(PlayerElimination player)
    {
        PlayerScore score = GetScore(player);
        return score != null ? score.survivalScore + score.winnerBonus : 0;
    }

    public int GetWinnerBonus(PlayerElimination player)
    {
        PlayerScore score = GetScore(player);
        return score != null ? score.winnerBonus : 0;
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

    // ---------------------------------
    // Live Placement
    // ---------------------------------

    public int GetLivePlacement(PlayerRef player)
    {
        if (player == PlayerRef.None)
            return 0;

        int playerScore = 0;
        bool playerFound = false;

        for (int i = 0; i < NetworkScores.Length; i++)
        {
            NetworkPlayerScore score = NetworkScores[i];
            if (score.player == player)
            {
                playerScore = score.overallScore;
                playerFound = true;
                break;
            }
        }

        if (!playerFound)
            return 0;

        int rank = 1;
        List<int> higherScores = new List<int>();

        for (int i = 0; i < NetworkScores.Length; i++)
        {
            NetworkPlayerScore score = NetworkScores[i];
            if (score.player == PlayerRef.None)
                continue;

            if (score.overallScore > playerScore && !higherScores.Contains(score.overallScore))
            {
                higherScores.Add(score.overallScore);
            }
        }

        rank += higherScores.Count;
        return rank;
    }

    // ---------------------------------
    // Winner Bonus
    // ---------------------------------

    public void AwardWinnerBonus(PlayerElimination winner)
    {
        if (!HasStateAuthority || winner == null)
            return;

        PlayerScore score = GetScore(winner);
        if (score == null)
            return;

        score.winnerBonus = winnerBonus;

        SyncScores();
    }

    // ---------------------------------
    // Network Synchronization
    // ---------------------------------

    private void SyncScores()
    {
        if (!HasStateAuthority)
            return;

        for (int i = 0; i < NetworkScores.Length; i++)
        {
            NetworkScores.Set(i, default);
        }

        int count = Mathf.Min(playerScores.Count, NetworkScores.Length);

        for (int i = 0; i < count; i++)
        {
            PlayerScore score = playerScores[i];
            if (score.player == null)
                continue;

            NetworkObject playerObject = score.player.GetComponent<NetworkObject>();
            if (playerObject == null)
                continue;

            NetworkPlayerScore networkScore = new NetworkPlayerScore
            {
                player = playerObject.InputAuthority,
                placement = placementManager != null ? placementManager.GetPlacement(score.player) : 0,
                survivalScore = score.survivalScore,
                knockoutCredit = score.knockoutCredit,
                winnerBonus = score.winnerBonus,
                overallScore = score.survivalScore + score.winnerBonus
            };

            NetworkScores.Set(i, networkScore);
        }
    }

    private void ClearNetworkScores()
    {
        if (!HasStateAuthority)
            return;

        for (int i = 0; i < NetworkScores.Length; i++)
        {
            NetworkScores.Set(i, default);
        }
    }

    // ---------------------------------
    // Network Data Access
    // ---------------------------------

    public int NetworkScoreCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < NetworkScores.Length; i++)
            {
                if (NetworkScores[i].player != PlayerRef.None)
                    count++;
            }
            return count;
        }
    }

    public bool TryGetNetworkScore(int index, out NetworkPlayerScore score)
    {
        if (index < 0 || index >= NetworkScores.Length)
        {
            score = default;
            return false;
        }

        score = NetworkScores[index];
        return score.player != PlayerRef.None;
    }

    // ---------------------------------
    // Internal Lookup
    // ---------------------------------

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