using Fusion;
using UnityEngine;

public class PlayerControlEffects : NetworkBehaviour
{
    [Networked]
    private TickTimer ReversedInputTimer { get; set; }

    public bool IsInputReversed =>
        Object != null &&
        Runner != null &&
        !ReversedInputTimer.ExpiredOrNotRunning(Runner);

    public void ApplyReversedInput(float duration)
    {
        if (!HasStateAuthority || duration <= 0f)
            return;

        ReversedInputTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                duration
            );
    }
    
}