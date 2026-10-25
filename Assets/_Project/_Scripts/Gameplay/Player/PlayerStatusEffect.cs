
using Fusion;
using UnityEngine;

public class PlayerStatusEffects : NetworkBehaviour
{
    [Networked]
    private int SmokeSourceCount { get; set; }

    [Header("Smoke Slow")]
    [SerializeField, Range(0.1f, 1f)]
    private float smokeSpeedMultiplier = 0.7f;

    public bool IsInSmoke => SmokeSourceCount > 0;

    public float SmokeSpeedMultiplier => smokeSpeedMultiplier;

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

        SmokeSourceCount = Mathf.Max(0, SmokeSourceCount - 1);
    }
}
