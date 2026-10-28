using UnityEngine;

public class KapiPlayerControlEffects : MonoBehaviour
{
    private float _reversedInputUntil;
    public bool IsInputReversed => Time.time < _reversedInputUntil;

    public void ApplyReversedInput(float duration)
    {
        if (duration > 0f)
            _reversedInputUntil = Mathf.Max(_reversedInputUntil, Time.time + duration);
    }
}