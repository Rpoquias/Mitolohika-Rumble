using Fusion;
using UnityEngine;

public class PlayerStatusEffects : NetworkBehaviour
{
    [Networked]
    private int SmokeSourceCount { get; set; }

    public bool IsInSmoke => SmokeSourceCount > 0;

    public void AddSmokeSource()
    {
        if (!HasStateAuthority)
            return;

        SmokeSourceCount++;
    }

    public void RemoveSmokeSource()
    {
        if (!HasStateAuthority)
            return;

        SmokeSourceCount = Mathf.Max(
            0,
            SmokeSourceCount - 1
        );
    }
}