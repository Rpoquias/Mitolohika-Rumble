using UnityEngine;

public class KapiPlayerTargetingState : MonoBehaviour
{
    public bool IsUntargetable { get; private set; }
    public void SetUntargetable(bool untargetable) => IsUntargetable = untargetable;
}