using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class MatiraMatibayStandingsProvider :
    MonoBehaviour,
    IStandingsProvider
{
    [Header("References")]
    [SerializeField]
    private MatiraMatibayEliminationManager eliminationManager;

    public event Action StandingsChanged;

    public string SecondaryHeaderLabel =>
        "STATUS";

    public bool IsReady
    {
        get
        {
            return eliminationManager != null &&
                   eliminationManager.Object != null &&
                   eliminationManager.Object.IsValid;
        }
    }

    private NetworkRunner runner;

    private void Awake()
    {
        runner =
            NetworkRunner.GetRunnerForGameObject(
                gameObject
            );
    }

    private void OnEnable()
    {
        if (eliminationManager != null)
        {
            eliminationManager.OnEliminationChanged +=
                HandleEliminationChanged;
        }
    }

    private void OnDisable()
    {
        if (eliminationManager != null)
        {
            eliminationManager.OnEliminationChanged -=
                HandleEliminationChanged;
        }
    }

    private void HandleEliminationChanged()
    {
        StandingsChanged?.Invoke();
    }

    public void GetStandings(
        List<StandingEntry> buffer)
    {
        buffer.Clear();

        if (!IsReady)
            return;

        if (runner == null)
        {
            runner =
                NetworkRunner.GetRunnerForGameObject(
                    eliminationManager.gameObject
                );
        }

        if (runner == null ||
            !runner.IsRunning)
        {
            return;
        }

        // --------------------------------------------------------
        // ALIVE PLAYERS FIRST
        // --------------------------------------------------------

        foreach (PlayerRef player
                 in runner.ActivePlayers)
        {
            if (eliminationManager.IsPlayerEliminated(player))
                continue;

            buffer.Add(
                new StandingEntry
                {
                    Player = player,
                    Placement =
                        eliminationManager.GetLivePlacement(
                            player
                        ),
                    Status = "ALIVE",
                    Detail = ""
                }
            );
        }

        // --------------------------------------------------------
        // ELIMINATED PLAYERS
        //
        // Reverse elimination order:
        //
        // Last eliminated  → highest standing
        // First eliminated → lowest standing
        // --------------------------------------------------------

        for (int i =
             eliminationManager.EliminationCount - 1;
             i >= 0;
             i--)
        {
            PlayerRef player =
                eliminationManager.EliminationOrder[i];

            buffer.Add(
                new StandingEntry
                {
                    Player = player,

                    Placement =
                        eliminationManager.GetLivePlacement(
                            player
                        ),

                    Status = "ELIMINATED",

                    Detail = ""
                }
            );
        }
    }
}