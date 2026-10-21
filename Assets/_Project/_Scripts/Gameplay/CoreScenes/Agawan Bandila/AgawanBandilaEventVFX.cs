using Fusion;
using UnityEngine;

public class AgawanBandilaEventVFX : NetworkBehaviour
{
    [Header("Scene VFX")]
    [SerializeField] private GameObject speedSurgeVFX;
    [SerializeField] private GameObject slowMotionVFX;
    [SerializeField] private GameObject blackHoleVFX;

    // 0 = None, 1 = Speed Surge, 2 = Slow Motion, 3 = Black Hole.
    [Networked, OnChangedRender(nameof(ApplyVFX))]
    private int ActiveEventId { get; set; }

    public override void Spawned()
    {
        ApplyVFX();
    }

    public void SetEvent(int eventId)
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        if (ActiveEventId == eventId)
            return;

        ActiveEventId = eventId;

        // Apply immediately on the authority; harmless if OnChangedRender
        // also fires here (SetActive is guarded by activeSelf).
        ApplyVFX();
    }

    private void ApplyVFX()
    {
        int id = ActiveEventId;

        SetActive(speedSurgeVFX, id == 1);
        SetActive(slowMotionVFX, id == 2);
        SetActive(blackHoleVFX, id == 3);
    }

    private static void SetActive(GameObject vfx, bool active)
    {
        if (vfx != null && vfx.activeSelf != active)
            vfx.SetActive(active);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        SetActive(speedSurgeVFX, false);
        SetActive(slowMotionVFX, false);
        SetActive(blackHoleVFX, false);
    }
}