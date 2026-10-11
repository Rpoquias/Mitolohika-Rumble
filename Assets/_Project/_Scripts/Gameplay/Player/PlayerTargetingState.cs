using Fusion;

public class PlayerTargetingState : NetworkBehaviour
{
    [Networked]
    public NetworkBool IsUntargetable { get; private set; }

    public void SetUntargetable(bool untargetable)
    {
        if (!HasStateAuthority)
            return;

        IsUntargetable = untargetable;
    }
}