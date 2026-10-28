using UnityEngine;

public class KapiPlayerStatusEffects : MonoBehaviour
{
    private int _smokeSourceCount;

    [Header("Smoke Slow")]
    [SerializeField, Range(0.1f, 1f)]
    private float smokeSpeedMultiplier = 0.7f;

    public bool IsInSmoke => _smokeSourceCount > 0;
    public float SmokeSpeedMultiplier => smokeSpeedMultiplier;

    public void AddSmokeSource() => _smokeSourceCount++;
    public void RemoveSmokeSource() => _smokeSourceCount = Mathf.Max(0, _smokeSourceCount - 1);
}