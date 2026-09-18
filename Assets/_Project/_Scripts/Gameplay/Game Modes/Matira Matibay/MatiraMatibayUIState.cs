using Fusion;
using UnityEngine;

public enum MatiraMatibayUIPhase
{
    Hidden,
    RoundCountdown,
    ShrinkWarning,
    Score,
    Leaderboard,
    Result
}

public class MatiraMatibayUIState : NetworkBehaviour
{
    [Networked]
    public MatiraMatibayUIPhase Phase { get; private set; }

    [Networked]
    public TickTimer Timer { get; private set; }
public bool IsReady { get; private set; }

public override void Spawned()
{
    IsReady = true;
}
    public float GetRemainingTime()
    {
        if (!Timer.IsRunning)
            return 0f;

        return Timer.RemainingTime(Runner) ?? 0f;
    }

    public void ShowRoundCountdown(float duration)
    {
        if (!HasStateAuthority)
            return;

        Phase = MatiraMatibayUIPhase.RoundCountdown;

        Timer = TickTimer.CreateFromSeconds(
            Runner,
            duration
        );
    }

public void ShowShrinkWarning(float duration)
{
    if (!HasStateAuthority)
    {
    
        return;
    }

    Phase = MatiraMatibayUIPhase.ShrinkWarning;

    Timer = TickTimer.CreateFromSeconds(
        Runner,
        duration
    );
}
    public void Hide()
    {
        if (!HasStateAuthority)
            return;

        Phase = MatiraMatibayUIPhase.Hidden;
        Timer = TickTimer.None;
    }

    public void ShowScore(float duration)
    {
        if (!HasStateAuthority)
            return;

        Phase = MatiraMatibayUIPhase.Score;

        Timer = TickTimer.CreateFromSeconds(
            Runner,
            duration
        );
    }

    public void ShowLeaderboard(float duration)
    {
        if (!HasStateAuthority)
            return;

        Phase = MatiraMatibayUIPhase.Leaderboard;

        Timer = TickTimer.CreateFromSeconds(
            Runner,
            duration
        );
    }

    public void ShowResult()
    {
        if (!HasStateAuthority)
            return;

        Phase = MatiraMatibayUIPhase.Result;
        Timer = TickTimer.None;
    }
}